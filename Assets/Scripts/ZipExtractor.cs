// Copyright (C) 2020-2026 Yilmaz Kiymaz (VoxelBoy), Bryan Denny (mandlar)
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
using System;
using System.IO;
using System.IO.Compression;

public static class ZipExtractor
{
    // Extracts every entry, overwriting existing files, and reports whole-percent progress weighted by uncompressed size.
    // Safe to call from a background thread.
    public static void ExtractToDirectory(string zipPath, string destinationDirectory, IProgress<int> progress)
    {
        Directory.CreateDirectory(destinationDirectory);

        var destinationRoot = Path.GetFullPath(destinationDirectory);
        if (destinationRoot.EndsWith(Path.DirectorySeparatorChar.ToString()) == false)
        {
            destinationRoot += Path.DirectorySeparatorChar;
        }

        using (var archive = ZipFile.OpenRead(zipPath))
        {
            long totalBytes = 0;
            foreach (var entry in archive.Entries)
            {
                totalBytes += entry.Length;
            }

            long extractedBytes = 0;
            var lastReportedPercent = -1;

            foreach (var entry in archive.Entries)
            {
                var targetPath = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName));

                // Guard against entries that would be written outside of the destination folder
                if (targetPath.StartsWith(destinationRoot, StringComparison.Ordinal) == false)
                {
                    throw new IOException($"Zip entry is outside of the destination folder: {entry.FullName}");
                }

                // Directory entries have no file name
                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(targetPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                entry.ExtractToFile(targetPath, true);

                extractedBytes += entry.Length;
                var percent = totalBytes > 0 ? (int)(extractedBytes * 100 / totalBytes) : 100;
                if (percent != lastReportedPercent)
                {
                    lastReportedPercent = percent;
                    progress?.Report(percent);
                }
            }
        }
    }
}

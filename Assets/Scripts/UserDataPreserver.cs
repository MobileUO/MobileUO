using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed class UserDataPreserver : IDisposable
{
    private static readonly string[] PreservedRelativePaths =
    {
        "settings.json",
        "Data/Profiles"
    };

    private readonly string backupRootPath;
    private readonly string dataRootPath;

    private UserDataPreserver(string dataRootPath, string backupRootPath)
    {
        this.dataRootPath = dataRootPath;
        this.backupRootPath = backupRootPath;
    }

    public static UserDataPreserver Create(string dataRootPath)
    {
        var backupRootPath = Path.Combine(
            Application.temporaryCachePath,
            "mobileuo-preserve",
            Guid.NewGuid().ToString("N")
        );

        Directory.CreateDirectory(backupRootPath);

        foreach (var relativePath in PreservedRelativePaths)
        {
            BackupPath(dataRootPath, backupRootPath, relativePath);
        }

        return new UserDataPreserver(dataRootPath, backupRootPath);
    }

    public static bool IsPreservedPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var normalizedPath = NormalizeRelativePath(relativePath);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return false;
        }

        return PreservedRelativePaths.Any(preservedPath =>
        {
            var normalizedPreservedPath = NormalizeRelativePath(preservedPath);
            return normalizedPath.Equals(normalizedPreservedPath, StringComparison.OrdinalIgnoreCase)
                   || normalizedPath.StartsWith(normalizedPreservedPath + "/", StringComparison.OrdinalIgnoreCase);
        });
    }

    public static string NormalizeRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        if (Path.IsPathRooted(relativePath))
        {
            return null;
        }

        if (Uri.TryCreate(relativePath, UriKind.Absolute, out var uri) && uri.IsAbsoluteUri)
        {
            return null;
        }

        var colonIndex = relativePath.IndexOf(':');
        var separatorIndex = relativePath.IndexOfAny(new[] {'/', '\\'});
        if (colonIndex >= 0 && (separatorIndex < 0 || colonIndex < separatorIndex))
        {
            return null;
        }

        var normalizedPath = relativePath.Replace('\\', '/').TrimStart('/');
        var pathSegments = normalizedPath
            .Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

        if (pathSegments.Length == 0 || pathSegments.Any(segment => segment == ".."))
        {
            return null;
        }

        return string.Join("/", pathSegments);
    }

    public void Restore()
    {
        foreach (var relativePath in PreservedRelativePaths)
        {
            RestorePath(dataRootPath, backupRootPath, relativePath);
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(backupRootPath))
            {
                Directory.Delete(backupRootPath, true);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not delete preserved user data backup: {e}");
        }
    }

    private static void BackupPath(string dataRootPath, string backupRootPath, string relativePath)
    {
        var sourcePath = Path.Combine(dataRootPath, relativePath);
        if (File.Exists(sourcePath))
        {
            var destinationPath = Path.Combine(backupRootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? backupRootPath);
            File.Copy(sourcePath, destinationPath, true);
        }
        else if (Directory.Exists(sourcePath))
        {
            CopyDirectory(sourcePath, Path.Combine(backupRootPath, relativePath));
        }
    }

    private static void RestorePath(string dataRootPath, string backupRootPath, string relativePath)
    {
        var backupPath = Path.Combine(backupRootPath, relativePath);
        var destinationPath = Path.Combine(dataRootPath, relativePath);

        if (File.Exists(backupPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? dataRootPath);
            File.Copy(backupPath, destinationPath, true);
        }
        else if (Directory.Exists(backupPath))
        {
            CopyDirectory(backupPath, destinationPath);
        }
    }

    private static void CopyDirectory(string sourceDirectoryPath, string destinationDirectoryPath)
    {
        Directory.CreateDirectory(destinationDirectoryPath);

        foreach (var sourceFilePath in Directory.GetFiles(sourceDirectoryPath, "*", SearchOption.AllDirectories))
        {
            var relativeFilePath = sourceFilePath.Substring(sourceDirectoryPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var destinationFilePath = Path.Combine(destinationDirectoryPath, relativeFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath) ?? destinationDirectoryPath);
            File.Copy(sourceFilePath, destinationFilePath, true);
        }
    }
}

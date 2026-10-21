using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Newtonsoft.Json.Linq;

public static class ManifestParser
{
    public static List<ManifestFile> ParseJson(string json)
    {
        var token = JToken.Parse(json);

        if (token.Type == JTokenType.Array)
        {
            return token
                .Values<string>()
                .Where(fileName => string.IsNullOrWhiteSpace(fileName) == false)
                .Select(fileName => new ManifestFile { FileName = fileName })
                .ToList();
        }

        if (token.Type != JTokenType.Object)
        {
            return null;
        }

        var root = (JObject) token;
        var filesToken = root["files"] ?? root["Files"];
        if (filesToken is not JArray filesArray)
        {
            return null;
        }

        var files = new List<ManifestFile>();

        foreach (var item in filesArray)
        {
            if (item.Type == JTokenType.String)
            {
                var fileName = item.Value<string>();
                if (string.IsNullOrWhiteSpace(fileName) == false)
                {
                    files.Add(new ManifestFile
                    {
                        FileName = fileName,
                        DownloadPath = fileName
                    });
                }

                continue;
            }

            if (item.Type != JTokenType.Object)
            {
                continue;
            }

            var fileObject = (JObject) item;
            var manifestPath =
                fileObject.Value<string>("fileName")
                ?? fileObject.Value<string>("filename")
                ?? fileObject.Value<string>("path")
                ?? fileObject.Value<string>("downloadPath");

            var hash = fileObject.Value<string>("hash")
                       ?? fileObject.Value<string>("md5")
                       ?? fileObject.Value<string>("sha256");
            var sha256 = fileObject.Value<string>("sha256");

            if (string.IsNullOrWhiteSpace(manifestPath))
            {
                continue;
            }

            files.Add(new ManifestFile
            {
                FileName = manifestPath,
                DownloadPath = fileObject.Value<string>("downloadPath")
                               ?? (string.IsNullOrWhiteSpace(sha256) ? manifestPath : $"files/{sha256}"),
                Hash = hash,
                Size = fileObject.Value<long?>("size") ?? 0
            });
        }

        return files;
    }

    public static List<ManifestFile> ParseXml(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);

        var fileNodes = document.SelectNodes("/releases/release/files/file");
        if (fileNodes == null || fileNodes.Count == 0)
        {
            fileNodes = document.SelectNodes("//files/file|//manifest/file|//file");
        }

        if (fileNodes == null || fileNodes.Count == 0)
        {
            return null;
        }

        var files = new List<ManifestFile>();
        foreach (XmlNode fileNode in fileNodes)
        {
            if (fileNode.Attributes == null)
            {
                continue;
            }

            var fileName =
                fileNode.Attributes["filename"]?.Value
                ?? fileNode.Attributes["fileName"]?.Value
                ?? fileNode.Attributes["path"]?.Value;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            files.Add(new ManifestFile
            {
                FileName = fileName,
                DownloadPath = fileName,
                Hash = fileNode.Attributes["hash"]?.Value ?? fileNode.Attributes["md5"]?.Value,
                Size = ParseLong(fileNode.Attributes["size"]?.Value)
            });
        }

        return files;
    }

    private static long ParseLong(string value)
    {
        return long.TryParse(value, out var parsed) ? parsed : 0;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

public class DirectoryDownloader : DownloaderBase
{
    private List<string> filesToDownload;
    private string resourcePathForFilesToDownload;
    private int concurrentDownloadCounter;
    private int numberOfFilesDownloaded;
    private int numberOfFilesToDownload;
    private Dictionary<string, int> downloadAttemptsPerFile = new Dictionary<string, int>();
    private List<Tuple<UnityWebRequest, string>> activeRequestAndFileNameTupleList = new List<Tuple<UnityWebRequest, string>>();
    private Coroutine downloadCoroutine;
    private string pathToSaveFiles;
    private int port;
    
    private const int MAX_CONCURRENT_DOWNLOADS = 2;
    private const int MAX_DOWNLOAD_ATTEMPTS = 3;
    private Dictionary<string, ManifestFile> manifestFilesByName;

    public override void Initialize(DownloadState downloadState, ServerConfiguration serverConfiguration, DownloadPresenter downloadPresenter)
    {
        base.Initialize(downloadState, serverConfiguration, downloadPresenter);

        pathToSaveFiles = serverConfiguration.GetPathToSaveFiles();
        port = int.Parse(serverConfiguration.FileDownloadServerPort);
        resourcePathForFilesToDownload = downloadState.ResourcePathForFilesToDownload ?? "";
        manifestFilesByName = downloadState.ManifestFilesToDownload?
            .ToDictionary(file => file.FileName, StringComparer.OrdinalIgnoreCase);
        filesToDownload = BuildPendingDownloadList(downloadState.FilesToDownload);
        numberOfFilesToDownload = filesToDownload.Count;
        downloadPresenter.SetFileList(filesToDownload);
        downloadCoroutine = downloadPresenter.StartCoroutine(DownloadFiles());
    }
    
    private IEnumerator DownloadFiles()
    {
        var directoryInfo = new DirectoryInfo(pathToSaveFiles);
        if (directoryInfo.Exists == false)
        {
            directoryInfo.Create();
        }

        if (filesToDownload.Count == 0)
        {
            serverConfiguration.AllFilesDownloaded = true;
            ServerConfigurationModel.SaveServerConfigurations();
            StateManager.GoToState<GameState>();
            yield break;
        }

        while (filesToDownload.Count > 0)
        {
            while (concurrentDownloadCounter < MAX_CONCURRENT_DOWNLOADS && filesToDownload.Count > 0)
            {
                var fileName = filesToDownload[0];
                filesToDownload.RemoveAt(0);
                if (downloadAttemptsPerFile.TryGetValue(fileName, out _) == false)
                {
                    downloadAttemptsPerFile[fileName] = 1;
                }

                DownloadFile(fileName);
            }

            UpdateDownloadProgress();

            yield return null;
        }

        //Wait until final downloads finish
        while (concurrentDownloadCounter > 0)
        {
            UpdateDownloadProgress();
            yield return null;
        }

        serverConfiguration.AllFilesDownloaded = true;
        ServerConfigurationModel.SaveServerConfigurations();
        
        StateManager.GoToState<GameState>();
    }

    private void UpdateDownloadProgress()
    {
        foreach (var tuple in activeRequestAndFileNameTupleList)
        {
            downloadPresenter.SetDownloadProgress(tuple.Item2, tuple.Item1.downloadProgress);
        }
    }

    private void DownloadFile(string fileName)
    {
        var downloadPath = manifestFilesByName != null
                           && manifestFilesByName.TryGetValue(fileName, out var manifestFile)
                           && string.IsNullOrWhiteSpace(manifestFile.DownloadPath) == false
            ? manifestFile.DownloadPath
            : fileName;
        var requestPath = string.IsNullOrWhiteSpace(resourcePathForFilesToDownload)
            ? downloadPath
            : $"{resourcePathForFilesToDownload.TrimEnd('/')}/{downloadPath.TrimStart('/')}";
        var uri = DownloadState.GetUri(serverConfiguration.FileDownloadServerUrl, port, requestPath);
        var request = UnityWebRequest.Get(uri);
        var filePath = Path.Combine(pathToSaveFiles, fileName);
        var fileDirectoryPath = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(fileDirectoryPath) == false)
        {
            Directory.CreateDirectory(fileDirectoryPath);
        }
        var fileDownloadHandler = new DownloadHandlerFile(filePath) {removeFileOnAbort = true};
        request.downloadHandler = fileDownloadHandler;
        request.SendWebRequest().completed += operation => SingleFileDownloadFinished(request, fileName);
        activeRequestAndFileNameTupleList.Add(new Tuple<UnityWebRequest, string>(request, fileName));
        ++concurrentDownloadCounter;
    }

    private void SingleFileDownloadFinished(UnityWebRequest request, string fileName)
    {
        //If download coroutine was stopped, do nothing
        if (downloadCoroutine == null)
        {
            return;
        }
        
        --concurrentDownloadCounter;
        activeRequestAndFileNameTupleList.RemoveAll(x => x.Item1 == request);
        if (request.result == UnityWebRequest.Result.Success)
        {
            if (DownloadedFileMatchesManifest(fileName))
            {
                Debug.Log($"Download finished - {fileName}");
                ++numberOfFilesDownloaded;
                downloadPresenter.SetFileDownloaded(fileName);
                downloadPresenter.UpdateView(numberOfFilesDownloaded, numberOfFilesToDownload);
            }
            else
            {
                RetryOrFail(fileName, $"Downloaded file failed hash verification: {fileName}");
            }
        }
        else
        {
            RetryOrFail(fileName, $"Error while downloading {fileName}: {request.error}");
        }

    }

    public override void Dispose()
    {
        if (downloadCoroutine != null)
        {
            downloadPresenter.StopCoroutine(downloadCoroutine);
            downloadCoroutine = null;
        }
        filesToDownload = null;
        downloadAttemptsPerFile?.Clear();
        downloadAttemptsPerFile = null;
        manifestFilesByName?.Clear();
        manifestFilesByName = null;
        activeRequestAndFileNameTupleList?.ForEach(tuple =>
        {
            var webRequest = tuple.Item1;
            webRequest?.Abort();
            webRequest?.Dispose();
        });
        activeRequestAndFileNameTupleList?.Clear();
        activeRequestAndFileNameTupleList = null;
        concurrentDownloadCounter = 0;
        numberOfFilesDownloaded = 0;
        numberOfFilesToDownload = 0;
        base.Dispose();
    }

    private List<string> BuildPendingDownloadList(List<string> manifestFiles)
    {
        var pendingDownloads = new List<string>();

        foreach (var fileName in manifestFiles)
        {
            if (UserDataPreserver.IsPreservedPath(fileName))
            {
                continue;
            }

            if (LocalFileMatchesManifest(fileName))
            {
                continue;
            }

            pendingDownloads.Add(fileName);
        }

        return pendingDownloads;
    }

    private bool LocalFileMatchesManifest(string fileName)
    {
        if (manifestFilesByName == null
            || manifestFilesByName.TryGetValue(fileName, out var manifestFile) == false
            || string.IsNullOrWhiteSpace(manifestFile.Hash))
        {
            return false;
        }

        var localFilePath = Path.Combine(pathToSaveFiles, fileName);
        if (File.Exists(localFilePath) == false)
        {
            return false;
        }

        var localHash = ComputeFileHash(localFilePath, manifestFile.Hash);
        return string.Equals(localHash, manifestFile.Hash, StringComparison.OrdinalIgnoreCase);
    }

    private bool DownloadedFileMatchesManifest(string fileName)
    {
        if (manifestFilesByName == null
            || manifestFilesByName.TryGetValue(fileName, out var manifestFile) == false
            || string.IsNullOrWhiteSpace(manifestFile.Hash))
        {
            return true;
        }

        var localFilePath = Path.Combine(pathToSaveFiles, fileName);
        if (File.Exists(localFilePath) == false)
        {
            return false;
        }

        var localHash = ComputeFileHash(localFilePath, manifestFile.Hash);
        return string.Equals(localHash, manifestFile.Hash, StringComparison.OrdinalIgnoreCase);
    }

    private void RetryOrFail(string fileName, string error)
    {
        if (downloadAttemptsPerFile[fileName] >= MAX_DOWNLOAD_ATTEMPTS)
        {
            downloadPresenter.StopCoroutine(downloadCoroutine);
            downloadCoroutine = null;
            downloadState.StopAndShowError(error);
            return;
        }

        var attempt = downloadAttemptsPerFile[fileName] + 1;
        downloadAttemptsPerFile[fileName] = attempt;
        Debug.Log($"Re-downloading file, attempt:{attempt}");
        filesToDownload.Insert(0, fileName);
    }

    private static string ComputeFileHash(string filePath, string expectedHash)
    {
        using var fileStream = File.OpenRead(filePath);

        byte[] hashBytes;
        if (expectedHash.Length == 64)
        {
            using var sha256 = SHA256.Create();
            hashBytes = sha256.ComputeHash(fileStream);
        }
        else
        {
            using var md5 = MD5.Create();
            hashBytes = md5.ComputeHash(fileStream);
        }

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

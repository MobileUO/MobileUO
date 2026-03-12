using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class DownloadState : IState
{
    public List<string> FilesToDownload;
    public List<ManifestFile> ManifestFilesToDownload;
    public string ResourcePathForFilesToDownload;
    
    public static readonly List<string> NeededUoFileExtensions = new() {".def", ".mul", ".idx", ".uop", ".enu", ".rle", ".txt"};
    public const string DefaultFileDownloadPort = "8080";
    
    private readonly DownloadPresenter downloadPresenter;
    
    private ServerConfiguration serverConfiguration;
    private DownloaderBase downloader;
    private const string H_REF_PATTERN = @"<a\shref=[^>]*>([^<]*)<\/a>";

    public DownloadState(DownloadPresenter downloadPresenter)
    {
        this.downloadPresenter = downloadPresenter;
        downloadPresenter.BackButtonPressed += OnBackButtonPressed;
        downloadPresenter.CellularWarningYesButtonPressed += OnCellularWarningYesButtonPressed;
        downloadPresenter.CellularWarningNoButtonPressed += OnCellularWarningNoButtonPressed;
    }

    private void OnCellularWarningYesButtonPressed()
    {
        downloadPresenter.ToggleCellularWarning(false);
        StartDirectoryDownloader();
    }
    
    private void OnCellularWarningNoButtonPressed()
    {
        downloadPresenter.ToggleCellularWarning(false);
        StateManager.GoToState<ServerConfigurationState>();
    }

    private void OnBackButtonPressed()
    {
        StateManager.GoToState<ServerConfigurationState>();
    }

    public void Enter()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        serverConfiguration = ServerConfigurationModel.ActiveConfiguration;
        Debug.Log($"Downloading files to {serverConfiguration.GetPathToSaveFiles()}");
        var port = int.Parse(serverConfiguration.FileDownloadServerPort);
        
        if (serverConfiguration.AllFilesDownloaded || Application.isEditor && string.IsNullOrEmpty(serverConfiguration.ClientPathForUnityEditor) == false)
        {
            StateManager.GoToState<GameState>();
        }
        else
        {
            downloadPresenter.gameObject.SetActive(true);

            //Figure out what kind of downloader we should use
            if (serverConfiguration.FileDownloadServerUrl.ToLowerInvariant().Contains(".zip"))
            {
                downloader = new ZipDownloader();
                downloader.Initialize(this, serverConfiguration, downloadPresenter);
            }
            else if (serverConfiguration.FileDownloadServerUrl.ToLowerInvariant().Contains("uooutlands.com"))
            {
                downloader = new OutlandsDownloader();
                downloader.Initialize(this, serverConfiguration, downloadPresenter);
            }
            else if (serverConfiguration.FileDownloadServerUrl.ToLowerInvariant().Contains("uorenaissance.com"))
            {
                downloader = new RenaissanceDownloader();
                downloader.Initialize(this, serverConfiguration, downloadPresenter);
            }
            else
            {
                //Get list of files to download from server
                var uri = GetUri(serverConfiguration.FileDownloadServerUrl, port);
                var request = UnityWebRequest.Get(uri);
                //This request should not take more than 5 seconds, the amount of data being received is very small
                request.timeout = 5;
                request.SendWebRequest().completed += operation =>
                {
                    if (request.isHttpError || request.isNetworkError)
                    {
                        var error = $"Error while making initial request to server: {request.error}";
                        StopAndShowError(error);
                        return;
                    }

                    var headers = request.GetResponseHeaders();

                    if (headers.TryGetValue("Content-Type", out var contentType))
                    {
                        if (contentType.Contains("application/json"))
                        {
                            try
                            {
                                Debug.Log($"Json response: {request.downloadHandler.text}");
                                ManifestFilesToDownload = ManifestParser.ParseJson(request.downloadHandler.text);
                            }
                            catch (Exception exception)
                            {
                                Debug.LogWarning($"Could not parse json manifest: {exception}");
                                ManifestFilesToDownload = null;
                            }
                        }
                        else if (contentType.Contains("application/xml") || contentType.Contains("text/xml"))
                        {
                            try
                            {
                                ManifestFilesToDownload = ManifestParser.ParseXml(request.downloadHandler.text);
                            }
                            catch (Exception exception)
                            {
                                Debug.LogWarning($"Could not parse xml manifest: {exception}");
                                ManifestFilesToDownload = null;
                            }
                        }
                        else if (contentType.Contains("text/html"))
                        {
                            ManifestFilesToDownload = new List<ManifestFile>(Regex
                                .Matches(request.downloadHandler.text, H_REF_PATTERN, RegexOptions.IgnoreCase)
                                .Cast<Match>()
                                .Select(match => new ManifestFile { FileName = match.Groups[1].Value }));
                        }
                    }

                    if (ManifestFilesToDownload == null)
                    {
                        ManifestFilesToDownload = ParseManifestWithoutContentType(request.downloadHandler.text);
                    }

                    if (ManifestFilesToDownload != null)
                    {
                        SetManifestAndDownload(ManifestFilesToDownload, GetResourcePathForDownloads(request.uri));
                    }
                    else
                    {
                        StopAndShowError("Could not determine file list to download");
                    }
                };
            }
        }
    }

    public void SetFileListAndDownload(List<string> filesList, string resourcePathForFilesToDownload = null)
    {
        SetManifestAndDownload(filesList.Select(fileName => new ManifestFile { FileName = fileName }).ToList(), resourcePathForFilesToDownload);
    }

    public void SetManifestAndDownload(List<ManifestFile> manifestFiles, string resourcePathForFilesToDownload = null)
    {
        ManifestFilesToDownload = FilterManifestFiles(manifestFiles);
        FilesToDownload = ManifestFilesToDownload.Select(file => file.FileName).ToList();
        ResourcePathForFilesToDownload = resourcePathForFilesToDownload;

        //Check that some of the essential UO files exist
        var hasAnimationFiles = UtilityMethods.EssentialUoFilesExist(FilesToDownload);
                    
        if (FilesToDownload.Count == 0 || hasAnimationFiles == false)
        {
            var error = "Download directory does not contain UO files such as anim.mul or animationFrame1.uop";
            StopAndShowError(error);
            return;
        }

        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
        {
            ShowCellularWarning();
        }
        else
        {
            StartDirectoryDownloader();
        }
    }

    private void StartDirectoryDownloader()
    {
        downloader = new DirectoryDownloader();
        downloader.Initialize(this, serverConfiguration, downloadPresenter);
    }

    private void ShowCellularWarning()
    {
        downloadPresenter.ToggleCellularWarning(true);
    }

    public static Uri GetUri(string serverUrl, int port, string fileName = null)
    {
        var scheme = port == 443 ? "https" : "http";
        var serverUrlWithScheme = serverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                                  || serverUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? serverUrl
            : $"{scheme}://{serverUrl}";

        var uriBuilder = new UriBuilder(serverUrlWithScheme);
        uriBuilder.Port = port == 80 || port == 443 ? -1 : port;

        if (string.IsNullOrEmpty(fileName))
        {
            return uriBuilder.Uri;
        }

        if (uriBuilder.Path.EndsWith("/") == false)
        {
            uriBuilder.Path = $"{uriBuilder.Path}/";
        }

        var baseUri = uriBuilder.Uri;
        return new Uri(baseUri, fileName);
    }

    public void StopAndShowError(string error)
    {
        Debug.LogError(error);
        //Stop downloads
        downloadPresenter.ShowError(error);
        downloadPresenter.ClearFileList();
    }

    public void Exit()
    {
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
        
        downloadPresenter.ClearFileList();
        downloadPresenter.gameObject.SetActive(false);
        downloader?.Dispose();
        
        FilesToDownload = null;
        ManifestFilesToDownload = null;
        serverConfiguration = null;
    }

    private static List<ManifestFile> FilterManifestFiles(List<ManifestFile> manifestFiles)
    {
        if (manifestFiles == null)
        {
            return null;
        }

        var filteredFiles = new List<ManifestFile>();

        foreach (var file in manifestFiles)
        {
            var normalizedPath = UserDataPreserver.NormalizeRelativePath(file?.FileName);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                continue;
            }

            if (NeededUoFileExtensions.Any(extension => normalizedPath.Contains(extension, StringComparison.OrdinalIgnoreCase)) == false)
            {
                continue;
            }

            filteredFiles.Add(new ManifestFile
            {
                FileName = normalizedPath,
                DownloadPath = UserDataPreserver.NormalizeRelativePath(file.DownloadPath) ?? normalizedPath,
                Hash = file.Hash,
                Size = file.Size
            });
        }

        return filteredFiles
            .GroupBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();
    }

    private static List<ManifestFile> ParseManifestWithoutContentType(string content)
    {
        try
        {
            return ManifestParser.ParseJson(content);
        }
        catch
        {
        }

        try
        {
            return ManifestParser.ParseXml(content);
        }
        catch
        {
        }

        return null;
    }

    private static string GetResourcePathForDownloads(Uri manifestUri)
    {
        var path = manifestUri.AbsolutePath;
        if (string.IsNullOrEmpty(path) || path == "/")
        {
            return null;
        }

        if (path.EndsWith("/"))
        {
            return path.TrimStart('/');
        }

        var lastSlashIndex = path.LastIndexOf('/');
        if (lastSlashIndex < 0)
        {
            return null;
        }

        return path.Substring(1, lastSlashIndex);
    }
}

using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows.Data;
using LocalServersDashBoard.Helpers;
using LocalServersDashBoard.Helpers.Api;
using LocalServersDashBoard.Properties;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace LocalServersDashBoard.ViewModels.Pages;

public partial class NodeAppViewModel : ObservableObject, INavigationAware
{
    [ObservableProperty] private INavigationService _navigationService;

    private bool _isInitialized = false;

    [ObservableProperty] private ISnackbarService _snackbarService;

    [ObservableProperty] private bool _progressBar;

    [ObservableProperty] private NodeAppApi _actions;

    // 您创建数据的位置 begin ---------------------

    [ObservableProperty] private List<NodeVersion>? _nodeVersionRes;
    [ObservableProperty] private List<NodeVersion>? _nodeVersion;
    [ObservableProperty] private NodeVersion _item;

    [ObservableProperty] private int _page = 1;

    [ObservableProperty] private string _nodeJsVersion;

    // 您创建数据的位置 end -----------------------


    public NodeAppViewModel(
        INavigationService navigationService,
        NodeAppApi pageApi,
        ISnackbarService snackbarService
    )
    {
        _navigationService = navigationService;
        Actions = pageApi;
        SnackbarService = snackbarService;
    }

    private void Init()
    {
        // 您的函数的初始化应该写在这里管理
        GetNodeJsVersionCache();
        NodeJsVersion = Settings.Default.NodeJsVersion;
    }

    // 添加其他函数 begin

    [RelayCommand]
    private async void GetNodeJsVersionCache()
    {
        try
        {
            var jwar = new JsonWAndR();
            NodeVersionRes = await jwar.ReadNodeJson("nodejs.json");
            Page = 1;
            SetPageData();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [RelayCommand]
    private async void GetNodeJsVersion()
    {
        ProgressBar = true;
        try
        {
            var res = await Actions.GetNodeVersion();
            NodeVersionRes = res;

            var jwar = new JsonWAndR();
            await jwar.WriteJson("nodejs.json", res);

            ProgressBar = false;
            Page = 1;
            SetPageData();
        }
        catch (Exception e)
        {
            ProgressBar = false;
            Console.WriteLine(e);
        }
    }

    private async void SetPageData()
    {
        NodeJsVersion = Settings.Default.NodeJsVersion;
        var subset = NodeVersionRes.Skip((Page - 1)*10).Take(10).ToList();
        for (int i = 0; i < subset.Count; i++)
        {
            var filePath = @"plugin";

            for (int p = 0; p < subset[i].Files.Count; p++)
            {
                if (subset[i].Files[p].Contains("win"))
                {
                    if (subset[i].Files[p].Contains("zip"))
                    {
                        if (subset[i].Files[p].Contains(Settings.Default.SystemArchitecture))
                        {
                            // win-x64-zip

                            var thisFileNameList = subset[i].Files[p].Split("-");
                            var thisFlieName = $"{thisFileNameList[0]}-{thisFileNameList[1]}.{thisFileNameList[2]}";

                            var downFileName = $"node-{subset[i].Version}-{thisFlieName}";


                            var (exists, size, error) = SafeCheckFile(filePath, downFileName);
                            if (exists)
                            {
                                subset[i].DownLoadPercent = "源文件存在";
                                subset[i].CanUse = true;
                            }
                        }
                    }
                }
            }

            if (subset[i].Version == NodeJsVersion)
            {
                subset[i].IsDq = "#67C23A";
                subset[i].DownLoadPercent = "当前使用";
            }
            else
            {
                subset[i].IsDq = "#ffffff";
            }
        }

        NodeVersion = subset;


        var collectionView = CollectionViewSource.GetDefaultView(NodeVersion);
        collectionView.Refresh();
    }

    [RelayCommand]
    private async void UpPage()
    {
        Page -= 1;
        Console.WriteLine($" Page : {Page}");
        if (Page <= 0)
        {
            Page += 1;
        }
        else
        {
            SetPageData();
        }
    }

    [RelayCommand]
    private async void DownPage()
    {
        Page += 1;
        SetPageData();
    }

    [RelayCommand]
    private async void SetUpNodeJs(NodeVersion item)
    {
        Item = item;
        Item.DownLoadPercent = $"正在建立连接......";

        var collectionView = CollectionViewSource.GetDefaultView(NodeVersion);
        collectionView.Refresh();

        for (int i = 0; i < item.Files.Count; i++)
        {
            if (item.Files[i].Contains("win"))
            {
                if (item.Files[i].Contains("zip"))
                {
                    if (item.Files[i].Contains(Settings.Default.SystemArchitecture))
                    {
                        // win-x64-zip

                        var thisFileNameList = item.Files[i].Split("-");
                        var thisFlieName = $"{thisFileNameList[0]}-{thisFileNameList[1]}.{thisFileNameList[2]}";

                        var uri = $"https://nodejs.org/dist/{item.Version}/node-{item.Version}-{thisFlieName}";
                        Console.WriteLine($" uri : {uri}");
                        await DownLoadWithProgressNodeJs(uri);
                    }
                }
            }
        }
    }


    private (bool Exists, long Size, string Error) SafeCheckFile(string directoryPath, string fileName)
    {
        try
        {
            // 验证目录路径
            if (!Directory.Exists(directoryPath))
                return (false, 0, "目录不存在");

            string fullPath = Path.Combine(directoryPath, fileName);

            // 验证文件
            if (!File.Exists(fullPath))
                return (false, 0, "文件不存在");

            // 获取文件大小
            var fileInfo = new FileInfo(fullPath);
            return (true, fileInfo.Length, string.Empty);
        }
        catch (UnauthorizedAccessException ex)
        {
            return (false, 0, $"无访问权限: {ex.Message}");
        }
        catch (PathTooLongException ex)
        {
            return (false, 0, $"路径过长: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, 0, $"未知错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="progressCallback"></param>
    public async Task DownLoadWithProgressNodeJs(string uri, Action<long, long> progressCallback = null)
    {
        string BaseUrl = "";
        string url = BaseUrl + uri;
        string filePath = @"plugin";
        // https://nodejs.org/dist/v25.4.0/node-v25.4.0-win-x64.zip
        Console.WriteLine(uri.Split("/"));
        string downFileName = uri.Split("/").Last();

        // 确保目录存在 
        Directory.CreateDirectory(filePath);

        string fileName = Path.GetFileName(new Uri(url).LocalPath);
        string fullPath = Path.Combine(filePath, fileName);

        try
        {
            using (HttpClient httpClient = new HttpClient())
            {
                // 先获取文件总大小
                var headResponse = await httpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Head, url));
                long? totalBytes = headResponse.Content.Headers.ContentLength;

                Console.WriteLine($"开始下载 {fileName} (大小: {FormatBytes(totalBytes ?? 0)})");
                var fileBytes = FormatBytes2(totalBytes ?? 0);

                var (exists, size, error) = SafeCheckFile(filePath, downFileName);
                if (exists)
                {
                    if (size == fileBytes)
                    {
                        Item.DownLoadPercent = $"该版本已存在";

                        var collectionView = CollectionViewSource.GetDefaultView(NodeVersion);
                        collectionView.Refresh();
                        return;
                    }
                }

                // 创建带进度处理的HttpContent 
                using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    var buffer = new byte[8192];
                    long bytesRead = 0;
                    int read;

                    while ((read = await stream.ReadAsync(buffer)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        bytesRead += read;

                        // 报告进度
                        if (totalBytes.HasValue)
                        {
                            double percentage = (double)bytesRead / totalBytes.Value * 100;
                            Console.WriteLine(
                                $"下载进度: {percentage:F2}% ({FormatBytes(bytesRead)}/{FormatBytes(totalBytes.Value)})");

                            Item.DownLoadPercent =
                                $"{percentage:F2}% ({FormatBytes(bytesRead)}/{FormatBytes(totalBytes.Value)})";
                            progressCallback?.Invoke(bytesRead, totalBytes.Value);

                            var collectionView = CollectionViewSource.GetDefaultView(NodeVersion);
                            collectionView.Refresh();
                        }
                    }
                }

                Console.WriteLine($"文件 {fileName} 下载完成");
                // 解压

                // var extractName = downFileName.Split(".")[0];

                // ExtractZip($"{filePath}/{extractName}", ".");
                //
                //
                // AppendToUserPath($"{filePath}/{extractName}/bin/");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载失败: {ex.Message}");
        }
    }

// 辅助方法：格式化字节大小 
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private static long FormatBytes2(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return bytes;
    }


    void ExtractAndFlatten(string zipPath, string extractPath)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Console.WriteLine($"TempDir: {tempDir}");
        try
        {
            // 解压到临时目录 
            ZipFile.ExtractToDirectory(zipPath, tempDir);

            // 获取第一级子目录
            string[] subDirs = Directory.GetDirectories(tempDir);
            if (subDirs.Length == 1)
            {
                // 确保目标目录存在且为空 
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
                Directory.CreateDirectory(extractPath);

                // 移动文件及子目录
                foreach (string file in Directory.GetFiles(subDirs[0]))
                {
                    string destPath = Path.Combine(extractPath, Path.GetFileName(file));
                    File.Move(file, destPath, true); // 直接使用覆盖参数 
                }

                foreach (string dir in Directory.GetDirectories(subDirs[0]))
                {
                    string destDir = Path.Combine(extractPath, Path.GetFileName(dir));
                    Directory.Move(dir, destDir);
                }
            }

            Directory.Delete(tempDir, true);
            Console.WriteLine($"解压完成: {extractPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"解压失败: {ex.Message}\n{ex.StackTrace}");
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }


    /// <summary>
    /// 安全追加路径到用户PATH环境变量
    /// </summary>
    /// <param name="pathToAdd">要追加的路径（绝对路径）</param>
    /// <returns>是否成功追加</returns>
    public static bool AppendToUserPath(string pathToAdd)
    {
        try
        {
            // 标准化路径格式（统一为Windows风格）
            string normalizedPath = Path.GetFullPath(pathToAdd).TrimEnd('\\');

            // 验证路径有效性
            if (!Directory.Exists(normalizedPath))
            {
                Console.WriteLine($"路径不存在: {normalizedPath}");
                return false;
            }

            // 获取当前用户PATH（包括系统PATH的副本）
            string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User)
                                 ?? Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine)
                                 ?? "";

            // 分割现有路径（去重和空值处理）
            var pathParts = currentPath
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.TrimEnd('\\'))
                .ToList();

            // 检查是否已存在 
            if (pathParts.Any(p => string.Equals(p, normalizedPath, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"路径已存在于PATH中: {normalizedPath}");
                return true;
            }

            // 构建新PATH（保留原有内容）
            string newPath = string.Join(';', pathParts.Concat(new[] { normalizedPath }));

            // 设置用户级PATH（不影响系统PATH）
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);

            Console.WriteLine($"成功追加路径: {normalizedPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"追加PATH失败: {ex.Message}");
            return false;
        }
    }

    [RelayCommand]
    private async Task SetGlobalPath(NodeVersion item)
    {
        ProgressBar = true;
        Task.Run(() =>
        {
            for (int i = 0; i < item.Files.Count; i++)
            {
                if (item.Files[i].Contains("win"))
                {
                    if (item.Files[i].Contains("zip"))
                    {
                        if (item.Files[i].Contains(Settings.Default.SystemArchitecture))
                        {
                            // win-x64-zip

                            var thisFileNameList = item.Files[i].Split("-");
                            var thisFlieName = $"{thisFileNameList[0]}-{thisFileNameList[1]}.{thisFileNameList[2]}";

                            string filePath = @"plugin";
                            var filename = $"node-{item.Version}-{thisFlieName}";
                            string fullPath = Path.Combine(filePath, filename);

                            string extPaht = Path.Combine(@"nodejs");
                            // 确保目录存在 
                            try
                            {
                                Directory.CreateDirectory(extPaht);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }


                            ExtractAndFlatten(Path.GetFullPath(fullPath), extPaht);

                            AppendToUserPath($"{extPaht}/");

                            Settings.Default.NodeJsVersion = item.Version;
                            Settings.Default.Save();

                            Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                UiMessageBox umb = new()
                                {
                                    Title = "提示",
                                    Content = $"已设置 {item.Version} 为主要版本",
                                    MinWidth = 300
                                };
                                umb.ShowDialogAsync();
                            });
                            SetPageData();
                        }
                    }
                }
            }

            ProgressBar = false;
        });
    }

    // 添加其他函数 end


    public Task OnNavigatedToAsync()
    {
        if (!_isInitialized)
            InitializeViewModel();
        Init();
        return Task.CompletedTask;
    }


    private void InitializeViewModel()
    {
        _isInitialized = true;
    }

    public Task OnNavigatedFromAsync()
    {
        NodeVersionRes = null;
        NodeVersion = null;

        return Task.CompletedTask;
    }
}
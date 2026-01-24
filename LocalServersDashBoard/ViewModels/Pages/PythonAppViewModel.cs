using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Data;
using LocalServersDashBoard.Helpers;
using LocalServersDashBoard.Helpers.Api;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using System.Management.Automation;
using System.Text;

namespace LocalServersDashBoard.ViewModels.Pages;

public partial class PythonAppViewModel : ObservableObject, INavigationAware, INotifyPropertyChanged
{
    [ObservableProperty] private INavigationService _navigationService;

    private bool _isInitialized = false;

    [ObservableProperty] private ISnackbarService _snackbarService;

    [ObservableProperty] private bool _progressBar;

    [ObservableProperty] private PythonAppApi _actions;

    [ObservableProperty] private PythonAppMain _pythonVersion;

    [ObservableProperty] private int _page;

    [ObservableProperty] private string _pythonVersionChecked;

    [ObservableProperty] private List<PythonAppChildren> _selectItemData;
    [ObservableProperty] private PythonAppChildren _selectItemChildren;

    // 您创建数据的位置 begin ---------------------
    [ObservableProperty] private string _commandText;
    [ObservableProperty] private string _uvInstallPath =  Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uv", "python");
    
    [ObservableProperty] private List<Item> _pythonList;
    [ObservableProperty] private string _defaultPythonVersion;

    // 您创建数据的位置 end -----------------------


    public PythonAppViewModel(
        INavigationService navigationService,
        PythonAppApi pageApi,
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

        // GetPageData();
        PythonUvExist();

        Task.Run(() =>
        {
            var UV_CACHE_DIR = @"uv/cache";
            if (!Directory.Exists(UV_CACHE_DIR))
            {
                Directory.CreateDirectory(UV_CACHE_DIR);
            }

            var UV_TOOL_DIR = @"uv/tool";
            if (!Directory.Exists(UV_TOOL_DIR))
            {
                Directory.CreateDirectory(UV_TOOL_DIR);
            }

            var UV_PYTHON_INSTALL_DIR = @"uv/python";
            if (!Directory.Exists(UV_PYTHON_INSTALL_DIR))
            {
                Directory.CreateDirectory(UV_PYTHON_INSTALL_DIR);
            }

            UV_CACHE_DIR = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uv", "cache");
            UV_TOOL_DIR = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uv", "tool");
            UV_PYTHON_INSTALL_DIR = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uv", "python");
            Console.WriteLine($" UV_CACHE_DIR : {UV_CACHE_DIR}");

            Environment.SetEnvironmentVariable("UV_PYTHON_INSTALL_MIRROR",
                "https://ghfast.top/https://github.com/astral-sh/python-build-standalone/releases/download",
                EnvironmentVariableTarget.Machine);
            
            
            
            Environment.SetEnvironmentVariable("UV_CACHE_DIR", UV_CACHE_DIR, EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("UV_TOOL_DIR", UV_TOOL_DIR, EnvironmentVariableTarget.Machine);
            Environment.SetEnvironmentVariable("UV_PYTHON_INSTALL_DIR", UV_PYTHON_INSTALL_DIR,
                EnvironmentVariableTarget.Machine);
            
        });
        
        RefreshPythonList();
        DefaultPyVersion();
    }

    [RelayCommand]
    private async void SetDefaultVersion(string param)
    {
        string version = param.Split("-")[1];
        string command = $"uv python default {version}";
        var (res, str) = await RunCommandCmd(command);
    }
    [RelayCommand]
    private async void DefaultPyVersion()
    {
        string command = "python -V";
        var (res, str) = await RunCommand(command);
        DefaultPythonVersion = str.Replace("[ERROR]","").Replace("\r","").Replace("\n","");
    }
    
    [RelayCommand]
    private async void OpenFolder()
    {
        var folderPath = UvInstallPath;
        // Windows（调用资源管理器）
        // Process.Start("explorer.exe", folderPath); 
    
        // 跨平台兼容写法（.NET 6+）
        Process.Start(new ProcessStartInfo(folderPath) { UseShellExecute = true });
    }

    // 添加其他函数 begin
    [RelayCommand]
    private async void InstallPython(string param)
    {
        
        CommandText = $"正在静默安装Python {param.Split("-")[1]}，请等待....";
        Application.Current.Dispatcher.InvokeAsync(async () =>
        { 
            string version = param.Split("-")[1];
        
            string command = $"uv python install {version} --install-dir {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uv", "python")}";
            var (res, str) = await RunCommandCmd(command);
            if (res)
            {
                
                CommandText = $" {param.Split("-")[1]}完成";
                
                Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    UiMessageBox umb = new()
                    {
                        Title = "提示",
                        Content = "安装完成，请刷新",
                        MinWidth = 300
                    };
                    await umb.ShowDialogAsync();
                    RefreshPythonList();
                });
            }
        });
        
    }

    [RelayCommand]
    private async void RefreshPythonList()
    {
        string command = "uv python list";
        var (res, str) = await RunCommandCmd(command);
        PythonList = [];
        foreach (var line in str.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var item = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var status = item[1];
            if (status == "<download")
            {
                status += ">";
            }
            var items = new Item()
            {
                Version = item[0],
                Status = status
            };
            PythonList.Add(items);
        }
    }

    [RelayCommand]
    private async void AddPythonUv()
    {
        CommandText = $"正在静默安装uv，请等待....";
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            string command = "irm https://astral.sh/uv/install.ps1  | iex";
            var (res, str) = await RunCommand(command);


            if (!res)
            {
                Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    UiMessageBox umb = new()
                    {
                        Title = "错误",
                        Content = "安装UV失败",
                        MinWidth = 300
                    };
                    await umb.ShowDialogAsync();
                });
            }

            CommandText = str;
        });
    }

    [RelayCommand]
    private async void PythonUvExist()
    {
        string command = "where uv";
        var (results, str) = await RunCommandCmd(command);
        if (str.Contains("[ERROR]"))
        {
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                UiMessageBox umb = new()
                {
                    Title = "错误",
                    Content = "本功能基于UV 请先安装UV\r安装完成后请重新进入本页面\r如果未显示uv path 请尝试重新启动程序",
                    PrimaryButtonText = "安装",
                    MinWidth = 300
                };
                var umbresult = await umb.ShowDialogAsync();
                if (umbresult == MessageBoxResult.Primary)
                {
                    AddPythonUv();
                }
            });
        }
        else
        {
            CommandText = $"uv 存在 : {str}";
        }
    }


    public async Task<(bool Success, string Output)> RunCommand(string command)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // 捕获错误输出 
                    CreateNoWindow = true // 不显示窗口 
                }
            };


            var outputBuilder = new StringBuilder();
            process.OutputDataReceived += (_, e) => outputBuilder.AppendLine(e.Data);
            process.ErrorDataReceived += (_, e) => outputBuilder.AppendLine("[ERROR] " + e.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            return (process.ExitCode == 0, outputBuilder.ToString());
        }
        catch (Exception ex)
        {
            return (false, $"Process failed: {ex.Message}");
        }
    }


    public async Task<(bool Success, string Output)> RunCommandCmd(string command)
    {
        try
        {
            var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            var machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            var fullPath = $"{userPath};{machinePath}";


            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}", // 注意：cmd需要 /C 参数执行命令
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    // StandardOutputEncoding = Encoding.UTF8,  // 显式指定编码
                    // StandardErrorEncoding = Encoding.UTF8

                    Environment = { ["PATH"] = fullPath } // 👈 强制设置 PATH 
                }
            };

            var outputBuilder = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                CommandText = $" {e}";
                var collectionView = CollectionViewSource.GetDefaultView(CommandText);
                collectionView.Refresh();
                
                if (!string.IsNullOrEmpty(e.Data))
                    outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    outputBuilder.AppendLine("[ERROR] " + e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            return (process.ExitCode == 0, outputBuilder.ToString());
        }
        catch (Exception ex)
        {
            return (false, $"Process failed: {ex.ToString()}"); // 返回完整异常信息 
        }
    }


    // [RelayCommand]
    // private async void RefreshPythonVersion()
    // {
    //     ProgressBar = true;
    //     Task.Run(() =>
    //     {
    //         using (var process = new Process())
    //         {
    //             process.StartInfo.FileName = @"Assets/GetPythonVersion.exe";
    //             process.EnableRaisingEvents = true;
    //             process.Exited += (sender, e) =>
    //                 Console.WriteLine("进程已退出，代码: " + process.ExitCode);
    //
    //             process.Start();
    //             // 主线程继续执行...
    //         }
    //
    //         ProgressBar = false;
    //
    //         Application.Current.Dispatcher.InvokeAsync(async () =>
    //         {
    //             UiMessageBox umb = new()
    //             {
    //                 Title = "提示",
    //                 Content = "版本文件更新完成",
    //                 MinWidth = 300
    //             };
    //             await umb.ShowDialogAsync();
    //         });
    //     });
    // }
    //
    // [RelayCommand]
    // private async void GetPageData()
    // {
    //     try
    //     {
    //         using FileStream stream = File.OpenRead(@"python_windows_versions.json");
    //         var data = await JsonSerializer.DeserializeAsync<PythonAppMain>(stream);
    //         PythonVersion = data;
    //     }
    //     catch (Exception e)
    //     {
    //         Console.WriteLine(e);
    //     }
    // }
    //
    // [RelayCommand]
    // private async void SelectItem(string param)
    // {
    //     Console.WriteLine($" SelectItem : {param}");
    //
    //
    //     SelectItemData = PythonVersion.DownLoads[param];
    //
    //     Task.Run(() =>
    //     {
    //         string filePath = @"plugin";
    //
    //
    //         for (int i = 0; i < SelectItemData.Count; i++)
    //         {
    //             string downFileName = SelectItemData[i].Href.Split("/").Last();
    //
    //             var hf = new PullicFunction();
    //
    //             var (exists, size, error) = hf.SafeCheckFile(filePath, downFileName);
    //             if (exists)
    //             {
    //                 SelectItemData[i].DownLoadPercent = "源文件已存在";
    //             }
    //         }
    //
    //         var collectionView = CollectionViewSource.GetDefaultView(SelectItemData);
    //         collectionView.Refresh();
    //     });
    //
    //     PythonVersionChecked = param;
    // }
    //
    // [RelayCommand]
    // private async void DownLoadItem(PythonAppChildren param)
    // {
    //     Console.WriteLine($" DownLoadItem.href : {param.Href}");
    //     // SelectItemChildren = param;
    //
    //     param.DownLoadPercent = "即将开始下载....";
    //     var collectionView = CollectionViewSource.GetDefaultView(SelectItemData);
    //     collectionView.Refresh();
    //
    //     await DownLoadWithProgressNodeJs(param.Href, param);
    // }
    //
    //
    // public async Task DownLoadWithProgressNodeJs(string uri, PythonAppChildren param,
    //     Action<long, long> progressCallback = null)
    // {
    //     string BaseUrl = "";
    //     string url = BaseUrl + uri;
    //     string filePath = @"plugin";
    //     string downFileName = uri.Split("/").Last();
    //
    //     // 确保目录存在 
    //     Directory.CreateDirectory(filePath);
    //
    //     string fileName = Path.GetFileName(new Uri(url).LocalPath);
    //     string fullPath = Path.Combine(filePath, fileName);
    //
    //     try
    //     {
    //         using (HttpClient httpClient = new HttpClient())
    //         {
    //             // 先获取文件总大小
    //             var headResponse = await httpClient.SendAsync(
    //                 new HttpRequestMessage(HttpMethod.Head, url));
    //             long? totalBytes = headResponse.Content.Headers.ContentLength;
    //
    //             Console.WriteLine($"开始下载 {fileName} (大小: {FormatBytes(totalBytes ?? 0)})");
    //             var fileBytes = FormatBytes2(totalBytes ?? 0);
    //
    //
    //             var hf = new PullicFunction();
    //
    //             var (exists, size, error) = hf.SafeCheckFile(filePath, downFileName);
    //             if (exists)
    //             {
    //                 if (size == fileBytes)
    //                 {
    //                     Console.WriteLine($" 该版本已存在 : ");
    //                     param.DownLoadPercent = $"该版本已存在";
    //
    //                     var collectionView = CollectionViewSource.GetDefaultView(SelectItemData);
    //                     collectionView.Refresh();
    //                     return;
    //                 }
    //             }
    //
    //             // 创建带进度处理的HttpContent 
    //             using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
    //             using (var stream = await response.Content.ReadAsStreamAsync())
    //             using (var fileStream = new FileStream(fullPath, FileMode.Create))
    //             {
    //                 var buffer = new byte[8192];
    //                 long bytesRead = 0;
    //                 int read;
    //
    //                 while ((read = await stream.ReadAsync(buffer)) > 0)
    //                 {
    //                     await fileStream.WriteAsync(buffer, 0, read);
    //                     bytesRead += read;
    //
    //                     // 报告进度
    //                     if (totalBytes.HasValue)
    //                     {
    //                         double percentage = (double)bytesRead / totalBytes.Value * 100;
    //                         Console.WriteLine(
    //                             $"下载进度: {percentage:F2}% ({FormatBytes(bytesRead)}/{FormatBytes(totalBytes.Value)})");
    //
    //                         param.DownLoadPercent =
    //                             $"{percentage:F2}% ({FormatBytes(bytesRead)}/{FormatBytes(totalBytes.Value)})";
    //
    //                         progressCallback?.Invoke(bytesRead, totalBytes.Value);
    //
    //                         var collectionView = CollectionViewSource.GetDefaultView(SelectItemData);
    //                         collectionView.Refresh();
    //                     }
    //                 }
    //             }
    //
    //             Console.WriteLine($"文件 {fileName} 下载完成");
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"下载失败: {ex.Message} {ex.Data}");
    //     }
    // }
    //
    // [RelayCommand]
    // private async void InstallPythonExe(string href)
    // {
    //     string exeName = href.Split("/").Last();
    //     Task.Run(() =>
    //     {
    //         string installerBashPath = @"plugin";
    //
    //         string installerPath = installerBashPath + "/" + exeName;
    //         string installDir = @"PythonVersion";
    //
    //         // 确保目录存在 
    //         Directory.CreateDirectory(installDir);
    //
    //         bool success = SilentInstaller.InstallSilently(installerPath, installDir);
    //         Console.WriteLine($"安装结果: {success}");
    //     });
    // }
    //
    // private static string FormatBytes(long bytes)
    // {
    //     string[] sizes = { "B", "KB", "MB", "GB" };
    //     int order = 0;
    //     double len = bytes;
    //     while (len >= 1024 && order < sizes.Length - 1)
    //     {
    //         order++;
    //         len /= 1024;
    //     }
    //
    //     return $"{len:0.##} {sizes[order]}";
    // }
    //
    // private static long FormatBytes2(long bytes)
    // {
    //     string[] sizes = { "B", "KB", "MB", "GB" };
    //     int order = 0;
    //     double len = bytes;
    //     while (len >= 1024 && order < sizes.Length - 1)
    //     {
    //         order++;
    //         len /= 1024;
    //     }
    //
    //     return bytes;
    // }
    //
    //
    // public class SilentInstaller
    // {
    //     /// <summary>
    //     /// 静默安装指定的exe文件 
    //     /// </summary>
    //     /// <param name="exePath">安装包路径</param>
    //     /// <param name="installDir">可选：自定义安装目录</param>
    //     /// <returns>是否安装成功</returns>
    //     public static bool InstallSilently(string exePath, string installDir = null)
    //     {
    //         if (!File.Exists(exePath))
    //         {
    //             Console.WriteLine($"错误：文件不存在 - {exePath}");
    //             return false;
    //         }
    //
    //         try
    //         {
    //             // 根据安装包类型选择静默参数（兼容NSIS、Inno Setup等）
    //             string arguments = BuildSilentArguments(exePath, installDir);
    //
    //             // 配置进程启动信息 
    //             var processInfo = new ProcessStartInfo
    //             {
    //                 FileName = exePath,
    //                 Arguments = arguments,
    //                 CreateNoWindow = true, // 强制不显示窗口（避免隐藏不完全）
    //                 UseShellExecute = false, // 必须为false才能重定向输出
    //                 RedirectStandardOutput = true,
    //                 RedirectStandardError = true,
    //                 Verb = "runas"
    //             };
    //
    //             // 启动进程并异步读取输出 
    //             using (var process = new Process { StartInfo = processInfo })
    //             {
    //                 // 绑定输出/错误流事件
    //                 process.OutputDataReceived += (sender, e) =>
    //                     Console.WriteLine($"[LOG] {e.Data}");
    //                 process.ErrorDataReceived += (sender, e) =>
    //                     Console.WriteLine($"[ERROR] {e.Data}");
    //
    //                 Console.WriteLine($"[开始安装] 正在启动进程...");
    //                 process.Start();
    //
    //                 // 开始异步读取输出
    //                 process.BeginOutputReadLine();
    //                 process.BeginErrorReadLine();
    //
    //                 // 超时控制（单位：毫秒）
    //                 bool exited = process.WaitForExit(300000); // 5分钟超时 
    //
    //                 if (!exited)
    //                 {
    //                     Console.WriteLine("[超时] 安装进程未在预期时间内完成，可能卡在用户交互环节");
    //                     process.Kill();
    //                     return false;
    //                 }
    //
    //                 Console.WriteLine($"[完成] 退出代码: {process.ExitCode}");
    //                 return process.ExitCode == 0;
    //             }
    //         }
    //         catch (Exception ex)
    //         {
    //             Console.WriteLine($"安装异常: {ex.Message}");
    //             return false;
    //         }
    //     }
    //
    //     /// <summary>
    //     /// 构建静默安装参数（根据常见安装包类型适配）
    //     /// </summary>
    //     private static string BuildSilentArguments(string exePath, string installDir)
    //     {
    //         string fileName = Path.GetFileName(exePath).ToLower();
    //
    //         // 常见安装包类型判断（可根据实际需求扩展）
    //         if (fileName.Contains("python") || fileName.Contains("inno"))
    //         {
    //             // Inno Setup或Python官方安装包 
    //             return
    //                 $"/quiet InstallAllUsers=1 PrependPath=1 {(string.IsNullOrEmpty(installDir) ? "" : $"TargetDir=\"{installDir}\"")}";
    //         }
    //         else if (fileName.Contains("nsis"))
    //         {
    //             // NSIS安装包 
    //             return $"/S {(string.IsNullOrEmpty(installDir) ? "" : $"/D={installDir}")}";
    //         }
    //         else
    //         {
    //             // 默认通用静默参数（可能不适用于所有安装包） 
    //             return $"/silent /norestart {(string.IsNullOrEmpty(installDir) ? "" : $"/DIR=\"{installDir}\"")}";
    //         }
    //     }
    // }


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

    public Task OnNavigatedFromAsync() => Task.CompletedTask;
}

public class Item
{
    public string Version { get; set; }
    public string Status { get; set; }
};
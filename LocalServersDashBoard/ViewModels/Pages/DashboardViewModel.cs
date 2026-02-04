using System.Diagnostics;
using LocalServersDashBoard.Helpers.Api;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using System.Management;
using System.Text;
using System.Windows.Data;
using LocalServersDashBoard.Properties;

namespace LocalServersDashBoard.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject, INavigationAware
    {
        [ObservableProperty] private INavigationService _navigationService;

        [ObservableProperty] private int _counter = 0;

        private bool _isInitialized = false;
        [ObservableProperty] private string _osVersion;
        [ObservableProperty] private string _osPlatform;
        [ObservableProperty] private string _machineName;
        [ObservableProperty] private string _userName;
        [ObservableProperty] private string _systemDirectory;
        [ObservableProperty] private int _processorCount;
        [ObservableProperty] private long _memorySize;
        [ObservableProperty] private string _systemArchitecture;
        [ObservableProperty] private string _nodeJsVersion;


        public DashboardViewModel(
            INavigationService navigationService,
            NodeAppApi pageApi
        )
        {
            _navigationService = navigationService;
        }

        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }


        private void Init()
        {
            // 您的函数的初始化应该写在这里管理

            // 操作系统信息 
            OsVersion = Environment.OSVersion.ToString();
            OsPlatform = Environment.OSVersion.Platform.ToString();
            MachineName = Environment.MachineName;
            UserName = Environment.UserName;

            // 系统目录 
            SystemDirectory = Environment.SystemDirectory;

            // 处理器和内存
            ProcessorCount = Environment.ProcessorCount;
            MemorySize = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024); // MB

            SystemArchitecture = GetCpuArchitecture();
            Settings.Default.SystemArchitecture = SystemArchitecture;

            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var (res, str) = await RunCommandCmd("node -v");
                NodeJsVersion = str;
            });
        }


        string GetCpuArchitecture()
        {
            var searcher = new ManagementObjectSearcher("SELECT Architecture FROM Win32_Processor");
            foreach (var item in searcher.Get())
            {
                int archCode = Convert.ToInt32(item["Architecture"]);
                return archCode switch
                {
                    0 => "x86",
                    1 => "MIPS",
                    2 => "Alpha",
                    3 => "PowerPC",
                    5 => "ARM",
                    6 => "ia64",
                    9 => "x64",
                    _ => $"未知架构（代码 {archCode}）"
                };
            }

            return "无法获取";
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
                    // var collectionView = CollectionViewSource.GetDefaultView(CommandText);
                    // collectionView.Refresh();

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
            return Task.CompletedTask;
        }
    }
}
using System.Net.Http.Headers;
using System.Windows.Threading;

namespace LocalServersDashBoard.Helpers;

using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LocalServersDashBoard.Properties;
using Wpf.Ui.Controls;

public partial class HttpRequests : ObservableObject
{
    [ObservableProperty] private string _baseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        // 其他配置项...
    };

    private readonly HttpClient _httpClient;

    public HttpRequests(HttpClient httpClient)
    {
        _httpClient = httpClient;
#if DEBUG
        BaseUrl = Settings.Default.BASE_URL_DEBUG;
#else
        BaseUrl = Settings.Default.BASE_URL;
#endif
    }

    public async Task<JsonNode?> HttpPost(object parameter, string url)
    {
        var postData = JsonSerializer.Serialize(parameter);
        var uri = BaseUrl + url;
        StringContent httpContent = new(postData, Encoding.UTF8, "application/json");
        try
        {
            // 清理历史头（需捕获异常避免 Key 不存在）
            _httpClient.DefaultRequestHeaders.Remove("sign");
            _httpClient.DefaultRequestHeaders.Remove("nostr");
            _httpClient.DefaultRequestHeaders.Remove("timestamp");
            _httpClient.DefaultRequestHeaders.Remove("openid");
            _httpClient.DefaultRequestHeaders.Remove("password");
        }
        catch (Exception e)
        {
        }


        var response = await _httpClient.PostAsync(uri, httpContent);
        if (response.IsSuccessStatusCode)
        {
            var responseHeader = response.Headers;
            var result = await response.Content.ReadAsStringAsync();

            JsonSerializerOptions options = JsonOptions;

            JsonNode? receiveData = JsonSerializer.Deserialize<JsonNode>(result, options);

            return receiveData;
        }

        return null;
    }


    public async Task<JsonNode?> HttpGet(object parameter, string url)
    {
        var uri = BaseUrl + url;


        var response = await _httpClient.GetAsync(uri);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();

            JsonSerializerOptions options = JsonOptions;

            JsonNode? receiveData = JsonSerializer.Deserialize<JsonNode>(result, options);

            return receiveData;
        }

        return null;
    }

    public async Task DownLoad(string uri)
    {
        string url = BaseUrl + uri;
        string filePath = @"plugin";

        Console.WriteLine($" DownLoad_uri : {uri}");

        // 确保目录存在 
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }

        // 从 URL 中提取文件名 
        string fileName = Path.GetFileName(new Uri(url).LocalPath);
        string fullPath = Path.Combine(filePath, fileName);

        try
        {
            using (HttpClient httpClient = new HttpClient())
            {
                Console.WriteLine($" 开始下载文件");
                // 发送 GET 请求并获取响应 
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // 获取响应内容的字节数组 
                byte[] content = await response.Content.ReadAsByteArrayAsync();

                // 将字节数组写入文件 
                await File.WriteAllBytesAsync(fullPath, content);

                Console.WriteLine($"文件 {fileName} 下载成功，保存路径: {fullPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下载文件时出现错误: {ex.Message}");
        }
    }

    
    public async Task DownLoadWithProgress(string uri, Action<long, long> progressCallback = null)
    {
        string url = BaseUrl + uri;
        string filePath = @"plugin";

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
                            progressCallback?.Invoke(bytesRead, totalBytes.Value);
                        }
                    }
                }

                Console.WriteLine($"文件 {fileName} 下载完成");
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


    private string DoMD5(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // 转换为十六进制字符串 
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2")); // x2 表示两位小写十六进制 
            }

            return sb.ToString();
        }
    }

    public async Task Uploads(string filePath, string url)
    {
        var uri = BaseUrl + url;

        using var client = new HttpClient();
        using var formData = new MultipartFormDataContent();

        // 1. 读取文件并显式设置Content-Type 
        var fileStream = File.OpenRead(filePath);
        var fileContent = new StreamContent(fileStream);

        // 根据文件扩展名动态设置Content-Type（关键修复点）
        string contentType = GetMimeType(Path.GetExtension(filePath));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        // 2. 添加到表单数据
        formData.Add(fileContent, "file", Path.GetFileName(filePath));

        // 3. 发送请求
        var response = await client.PostAsync(uri, formData);
        response.EnsureSuccessStatusCode();
    }

    // 根据文件扩展名获取MIME类型 
    private string GetMimeType(string extension) => extension.ToLower() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream" // 默认类型 
    };
}

public class MySignReturn
{
    public string? Sign { get; set; }
    public string? NoStr { get; set; }
    public string? Timestamp { get; set; }
}

public class PostResultMainData
{
    [JsonPropertyName("code")] public required string Code { get; set; }
    [JsonPropertyName("message")] public required string Message { get; set; }
    [JsonPropertyName("data")] public JsonNode Data { get; set; } = null!;

    [JsonPropertyName("config")] public JsonNode Config { get; set; } = null!;
}
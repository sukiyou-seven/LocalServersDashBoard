
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalServersDashBoard.Helpers.Api;

public class PythonAppApi
{

    private const string BaseUri = "";
    private static readonly string XXXX = BaseUri + "/";

    private readonly HttpRequests _httpRequests;

    public PythonAppApi(HttpRequests httpRequests)
    {
        _httpRequests = httpRequests;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        // 其他配置项...
    };



}


public class PythonAppMain
{ 
    [JsonPropertyName("last_updated")] public string LastUpdate{ get; set; }
    [JsonPropertyName("data")] public Dictionary<string, List<PythonAppChildren>> DownLoads{ get; set; }
    [JsonPropertyName("version_list")] public List<string> VersionList{ get; set; }
}


public class PythonAppChildren
{
    [JsonPropertyName("value")] public string Href{ get; set; }
    [JsonPropertyName("label")] public string Text{ get; set; }
    [JsonPropertyName("dlp")] public string DownLoadPercent { get; set; } = "未下载";
}
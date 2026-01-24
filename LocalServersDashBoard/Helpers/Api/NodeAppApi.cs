
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalServersDashBoard.Helpers.Api;

public class NodeAppApi
{

    private const string BaseUri = "";
    private static readonly string XXXX = BaseUri + "/";
    private static readonly string NodeVersionUri = BaseUri + "https://nodejs.org/dist/index.json";

    private readonly HttpRequests _httpRequests;

    public NodeAppApi(HttpRequests httpRequests)
    {
        _httpRequests = httpRequests;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        // 其他配置项...
    };
    
    
    public async Task<List<NodeVersion>?> GetNodeVersion()
    {
        try
        {
            var res = await _httpRequests.HttpGet(new{},NodeVersionUri);
            Console.WriteLine($" res : {res}");
            JsonSerializerOptions options = JsonOptions;
            List<NodeVersion>? send = res.Deserialize<List<NodeVersion>>(options);
            
            return send;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }


    public async Task DownLoadNodeJs(string uri)
    {
        try
        {
            await _httpRequests.DownLoadWithProgress(uri);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

}


// public class exp
// {
//     [JsonPropertyName("name")] public string Name { get; set; }
// }

public class NodeVersion
{
    [JsonPropertyName("version")] public string Version { get; set; }
    [JsonPropertyName("date")] public string Date { get; set; }
    [JsonPropertyName("files")] public List<string> Files { get; set; }
    [JsonPropertyName("npm")] public string Npm { get; set; }
    [JsonPropertyName("v8")] public string V8 { get; set; }
    [JsonPropertyName("uv")] public string Uv{ get; set; }
    [JsonPropertyName("zlib")] public string Zlib{ get; set; }
    [JsonPropertyName("openssl")] public string Openssl{ get; set; }
    [JsonPropertyName("modules")] public string Modules{ get; set; }
    [JsonPropertyName("lts")] public object Lts{ get; set; }
    [JsonPropertyName("security")] public bool Security{ get; set; }
    
    [JsonPropertyName("down_load_percent")] public string DownLoadPercent{ get; set; }
    public string IsDq { get; set; } = "#ffffff";
    public string AlreadyExist { get; set; }
    public bool CanUse { get; set; }
}

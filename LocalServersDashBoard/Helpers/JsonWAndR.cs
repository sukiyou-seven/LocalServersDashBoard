using System.Text.Json;
using System.IO;
using LocalServersDashBoard.Helpers.Api;

namespace LocalServersDashBoard.Helpers;

public class JsonWAndR
{
    public async Task WriteJson(string path, object obj)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        await using FileStream stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, obj, options);
    }
    
    public async Task<List<NodeVersion>?> ReadNodeJson(string path)
    { 
        using FileStream stream = File.OpenRead(path);
        var data = await JsonSerializer.DeserializeAsync <List<NodeVersion>?>(stream);
        return data;
    }
}
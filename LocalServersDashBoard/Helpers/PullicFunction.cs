using System.IO;

namespace LocalServersDashBoard.Helpers;

public class PullicFunction
{
    public (bool Exists, long Size, string Error) SafeCheckFile(string directoryPath, string fileName)
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
}
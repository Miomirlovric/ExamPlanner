using Application.Storage;

namespace ExamPlanner.Services;

public class StorageManager : IStorageManager
{
    private readonly string _baseDirectory;

    public StorageManager()
    {
        _baseDirectory = Path.Combine(FileSystem.AppDataDirectory, "GraphImages");
        Directory.CreateDirectory(_baseDirectory);
    }

    public async Task<string> SaveFileAsync(byte[] data, string fileName)
    {
        var path = Path.Combine(_baseDirectory, fileName);
        await File.WriteAllBytesAsync(path, data);
        return path;
    }

    public async Task<byte[]> LoadFileAsync(string filePath)
    {
        return await File.ReadAllBytesAsync(filePath);
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }
}

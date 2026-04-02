namespace Application.Storage;

public interface IStorageManager
{
    Task<string> SaveFileAsync(byte[] data, string fileName);
    Task<byte[]> LoadFileAsync(string filePath);
    Task DeleteFileAsync(string filePath);
}

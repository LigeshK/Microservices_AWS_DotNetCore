namespace HotelAdmin_Lambda.Interfaces;

public interface IFileStorageService
{
    Task UploadFileAsync(string bucketName, string fileName, Stream fileStream);
}

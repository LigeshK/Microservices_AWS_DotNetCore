using Amazon.S3;
using HotelAdmin_Lambda.Interfaces;

namespace HotelAdmin_Lambda.Repositories;

public class S3FileStorageService(AmazonS3Client s3Client) : IFileStorageService
{
    private readonly AmazonS3Client _s3Client = s3Client;

    public async Task UploadFileAsync(string bucketName, string fileName, Stream fileStream)
    {
        await _s3Client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
        {
            BucketName = bucketName,
            Key = fileName,
            InputStream = fileStream,
            AutoCloseStream = true
        });
    }
}

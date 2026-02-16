using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Tran.Api.Services;

/// <summary>
/// Azure Blob Storage 서비스 구현
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient? _containerClient;
    private readonly bool _isEnabled;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("AzureBlobStorage:ConnectionString");
        var containerName = configuration.GetValue<string>("AzureBlobStorage:ContainerName") ?? "tran-documents";

        if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("{your-account}"))
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                _containerClient.CreateIfNotExists(PublicAccessType.None);
                _isEnabled = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Azure Blob Storage initialization failed: {ex.Message}");
                _isEnabled = false;
            }
        }
        else
        {
            _isEnabled = false;
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        if (!_isEnabled || _containerClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        var blobClient = _containerClient.GetBlobClient(fileName);
        
        var blobHttpHeaders = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        await blobClient.UploadAsync(fileStream, new BlobUploadOptions
        {
            HttpHeaders = blobHttpHeaders
        });

        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadFileAsync(string fileName)
    {
        if (!_isEnabled || _containerClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        var blobClient = _containerClient.GetBlobClient(fileName);
        var response = await blobClient.DownloadAsync();
        return response.Value.Content;
    }

    public async Task DeleteFileAsync(string fileName)
    {
        if (!_isEnabled || _containerClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        var blobClient = _containerClient.GetBlobClient(fileName);
        await blobClient.DeleteIfExistsAsync();
    }

    public async Task<bool> FileExistsAsync(string fileName)
    {
        if (!_isEnabled || _containerClient == null)
        {
            return false;
        }

        var blobClient = _containerClient.GetBlobClient(fileName);
        return await blobClient.ExistsAsync();
    }

    public string GetFileUrl(string fileName)
    {
        if (!_isEnabled || _containerClient == null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        var blobClient = _containerClient.GetBlobClient(fileName);
        return blobClient.Uri.ToString();
    }
}

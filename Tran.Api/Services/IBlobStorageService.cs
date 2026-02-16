namespace Tran.Api.Services;

/// <summary>
/// Azure Blob Storage 서비스 인터페이스
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// 파일 업로드
    /// </summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// 파일 다운로드
    /// </summary>
    Task<Stream> DownloadFileAsync(string fileName);

    /// <summary>
    /// 파일 삭제
    /// </summary>
    Task DeleteFileAsync(string fileName);

    /// <summary>
    /// 파일 존재 여부 확인
    /// </summary>
    Task<bool> FileExistsAsync(string fileName);

    /// <summary>
    /// 파일 URL 조회
    /// </summary>
    string GetFileUrl(string fileName);
}

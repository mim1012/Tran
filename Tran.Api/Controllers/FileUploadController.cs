using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tran.Api.DTOs;
using Tran.Api.Services;

namespace Tran.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FileUploadController : ControllerBase
{
    private readonly IBlobStorageService _blobService;

    public FileUploadController(IBlobStorageService blobService)
    {
        _blobService = blobService;
    }

    /// <summary>
    /// 파일 업로드
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> Upload(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<string>.ErrorResult("파일이 없습니다."));
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            using var stream = file.OpenReadStream();
            var url = await _blobService.UploadFileAsync(stream, fileName, file.ContentType);

            return Ok(ApiResponse<string>.SuccessResult(url, "파일이 업로드되었습니다."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<string>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.ErrorResult($"파일 업로드 실패: {ex.Message}"));
        }
    }

    /// <summary>
    /// 파일 삭제
    /// </summary>
    [HttpDelete("{fileName}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(string fileName)
    {
        try
        {
            await _blobService.DeleteFileAsync(fileName);
            return Ok(ApiResponse<bool>.SuccessResult(true, "파일이 삭제되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResult(ex.Message));
        }
    }
}

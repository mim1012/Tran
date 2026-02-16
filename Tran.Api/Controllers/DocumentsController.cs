using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tran.Api.DTOs;
using Tran.Core.Models;
using Tran.Core.Services;
using Tran.Data;

namespace Tran.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly TranDbContext _context;
    private readonly IStateTransitionService _stateTransitionService;

    public DocumentsController(TranDbContext context, IStateTransitionService stateTransitionService)
    {
        _context = context;
        _stateTransitionService = stateTransitionService;
    }

    /// <summary>
    /// 회사별 거래명세표 목록 조회
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Document>>>> GetDocuments([FromQuery] string? companyId)
    {
        try
        {
            var query = _context.Documents
                .Include(d => d.Items)
                .AsQueryable();

            if (!string.IsNullOrEmpty(companyId))
            {
                query = query.Where(d => d.FromCompanyId == companyId || d.ToCompanyId == companyId);
            }

            var documents = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Ok(ApiResponse<List<Document>>.SuccessResult(documents));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<Document>>.ErrorResult(ex.Message));
        }
    }

    /// <summary>
    /// 거래명세표 상세 조회 (품목 포함)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Document>>> GetDocumentById(string id)
    {
        try
        {
            var document = await _context.Documents
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (document == null)
            {
                return NotFound(ApiResponse<Document>.ErrorResult("거래명세표를 찾을 수 없습니다."));
            }

            return Ok(ApiResponse<Document>.SuccessResult(document));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Document>.ErrorResult(ex.Message));
        }
    }

    /// <summary>
    /// 거래명세표 생성 (Draft 상태) + 품목 일괄 생성
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Document>>> CreateDocument([FromBody] CreateDocumentRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var companyId = User.FindFirst("CompanyId")?.Value ?? string.Empty;

            var document = new Document
            {
                DocumentId = Guid.NewGuid().ToString(),
                FromCompanyId = companyId,
                ToCompanyId = request.ToCompanyId,
                TransactionDate = request.TransactionDate,
                Memo = request.Memo,
                State = DocumentState.Draft,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
            };

            foreach (var itemDto in request.Items)
            {
                var item = new DocumentItem
                {
                    ItemId = Guid.NewGuid().ToString(),
                    DocumentId = document.DocumentId,
                    ItemName = itemDto.ItemName,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    OptionText = itemDto.OptionText,
                };
                item.CalculateLineAmount();
                document.Items.Add(item);
            }

            document.TotalAmount = document.Items.Sum(i => i.LineAmount);

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDocumentById), new { id = document.DocumentId },
                ApiResponse<Document>.SuccessResult(document, "거래명세표가 생성되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Document>.ErrorResult(ex.Message));
        }
    }

    /// <summary>
    /// Draft 문서의 품목 일괄 수정 (Replace-All 패턴)
    /// </summary>
    [HttpPut("{id}/items")]
    public async Task<ActionResult<ApiResponse<Document>>> UpdateDocumentItems(string id, [FromBody] UpdateDocumentItemsRequest request)
    {
        try
        {
            var document = await _context.Documents
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (document == null)
            {
                return NotFound(ApiResponse<Document>.ErrorResult("거래명세표를 찾을 수 없습니다."));
            }

            if (document.State != DocumentState.Draft)
            {
                return BadRequest(ApiResponse<Document>.ErrorResult("Draft 상태의 문서만 품목을 수정할 수 있습니다."));
            }

            // Replace-All: 기존 품목 삭제 후 새 품목 삽입
            _context.DocumentItems.RemoveRange(document.Items);

            var newItems = new List<DocumentItem>();
            foreach (var itemDto in request.Items)
            {
                var item = new DocumentItem
                {
                    ItemId = Guid.NewGuid().ToString(),
                    DocumentId = document.DocumentId,
                    ItemName = itemDto.ItemName,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    OptionText = itemDto.OptionText,
                };
                item.CalculateLineAmount();
                newItems.Add(item);
            }

            document.Items = newItems;
            document.TotalAmount = newItems.Sum(i => i.LineAmount);

            _context.DocumentItems.AddRange(newItems);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<Document>.SuccessResult(document, "품목이 수정되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Document>.ErrorResult(ex.Message));
        }
    }

    /// <summary>
    /// 상태전이: Draft → Sent
    /// </summary>
    [HttpPost("{id}/send")]
    public async Task<ActionResult<ApiResponse<Document>>> SendDocument(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            var document = await _context.Documents
                .Include(d => d.Items)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (document == null)
            {
                return NotFound(ApiResponse<Document>.ErrorResult("거래명세표를 찾을 수 없습니다."));
            }

            if (!_stateTransitionService.CanTransition(document.State, DocumentState.Sent))
            {
                return BadRequest(ApiResponse<Document>.ErrorResult(
                    $"현재 상태({document.State})에서 전송할 수 없습니다."));
            }

            var result = await _stateTransitionService.TransitionAsync(
                document, DocumentState.Sent, userId, "문서 전송");

            if (!result.Success)
            {
                return BadRequest(ApiResponse<Document>.ErrorResult(
                    result.ErrorMessage ?? "상태 전이에 실패했습니다."));
            }

            // 상태 로그 저장
            foreach (var log in result.StateLogs)
            {
                if (string.IsNullOrEmpty(log.LogId))
                    log.LogId = Guid.NewGuid().ToString();
                _context.DocumentStateLogs.Add(log);
            }

            document.SentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<Document>.SuccessResult(document, "거래명세표가 전송되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Document>.ErrorResult(ex.Message));
        }
    }
}

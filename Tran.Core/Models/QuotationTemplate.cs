namespace Tran.Core.Models;

/// <summary>
/// 견적서 양식 (Excel/PDF 파일 업로드)
/// </summary>
public class QuotationTemplate
{
    public int TemplateId { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public int? QuotationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Company? Company { get; set; }
}

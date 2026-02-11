using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Data.Services;

/// <summary>
/// 견적서 양식 관리 서비스 구현
/// </summary>
public class QuotationTemplateService : IQuotationTemplateService
{
    private readonly TranDbContext _context;

    public QuotationTemplateService(TranDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<QuotationTemplate> UploadAsync(QuotationTemplate template)
    {
        _context.QuotationTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<List<QuotationTemplate>> GetByCompanyAsync(string companyId)
    {
        return await _context.QuotationTemplates
            .Where(t => t.CompanyId == companyId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<QuotationTemplate?> GetByIdAsync(int templateId)
    {
        return await _context.QuotationTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId);
    }

    public async Task DeleteAsync(int templateId)
    {
        var template = await _context.QuotationTemplates
            .FirstOrDefaultAsync(t => t.TemplateId == templateId);

        if (template != null)
        {
            _context.QuotationTemplates.Remove(template);
            await _context.SaveChangesAsync();
        }
    }
}

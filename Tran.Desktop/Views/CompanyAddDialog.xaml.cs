using System.Windows;
using Microsoft.EntityFrameworkCore;
using Tran.Core.Models;
using Tran.Data;

namespace Tran.Desktop.Views;

/// <summary>
/// 거래처 등록/수정 다이얼로그
/// 생성 모드: CompanyAddDialog()
/// 수정 모드: CompanyAddDialog(existingCompany)
/// </summary>
public partial class CompanyAddDialog : Window
{
    private readonly Company? _editingCompany;
    private bool IsEditMode => _editingCompany != null;

    /// <summary>
    /// 등록/수정된 거래처 (DialogResult=true일 때 사용)
    /// </summary>
    public Company? ResultCompany { get; private set; }

    /// <summary>
    /// 생성 모드
    /// </summary>
    public CompanyAddDialog()
    {
        InitializeComponent();
        CompanyNameBox.Focus();
    }

    /// <summary>
    /// 수정 모드
    /// </summary>
    public CompanyAddDialog(Company company) : this()
    {
        _editingCompany = company;
        Title = "거래처 수정";
        SaveButton.Content = "저장";

        CompanyNameBox.Text = company.CompanyName;
        BusinessNumberBox.Text = company.BusinessNumber;
        BusinessNumberBox.IsEnabled = false; // 사업자번호는 수정 불가
        RepresentativeBox.Text = company.Representative ?? "";
        ContactBox.Text = company.Contact ?? "";
        AddressBox.Text = company.Address ?? "";
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var companyName = CompanyNameBox.Text.Trim();
        var businessNumber = BusinessNumberBox.Text.Trim();

        if (string.IsNullOrEmpty(companyName))
        {
            ShowError("거래처명을 입력해 주세요.");
            CompanyNameBox.Focus();
            return;
        }

        if (string.IsNullOrEmpty(businessNumber))
        {
            ShowError("사업자번호를 입력해 주세요.");
            BusinessNumberBox.Focus();
            return;
        }

        try
        {
            using var context = DbContextFactory.Create();

            if (IsEditMode)
            {
                // 수정 모드
                var existing = await context.Companies.FindAsync(_editingCompany!.CompanyId);
                if (existing == null)
                {
                    ShowError("거래처를 찾을 수 없습니다.");
                    return;
                }

                existing.CompanyName = companyName;
                existing.Representative = string.IsNullOrWhiteSpace(RepresentativeBox.Text) ? null : RepresentativeBox.Text.Trim();
                existing.Contact = string.IsNullOrWhiteSpace(ContactBox.Text) ? null : ContactBox.Text.Trim();
                existing.Address = string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim();

                await context.SaveChangesAsync();
                ResultCompany = existing;
            }
            else
            {
                // 생성 모드
                var exists = await context.Companies
                    .AnyAsync(c => c.BusinessNumber == businessNumber);
                if (exists)
                {
                    ShowError("이미 등록된 사업자번호입니다.");
                    BusinessNumberBox.Focus();
                    return;
                }

                var company = new Company
                {
                    CompanyId = Guid.NewGuid().ToString(),
                    CompanyName = companyName,
                    BusinessNumber = businessNumber,
                    Representative = string.IsNullOrWhiteSpace(RepresentativeBox.Text) ? null : RepresentativeBox.Text.Trim(),
                    Contact = string.IsNullOrWhiteSpace(ContactBox.Text) ? null : ContactBox.Text.Trim(),
                    Address = string.IsNullOrWhiteSpace(AddressBox.Text) ? null : AddressBox.Text.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Companies.Add(company);
                await context.SaveChangesAsync();
                ResultCompany = company;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowError($"저장 실패: {ex.Message}");
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}

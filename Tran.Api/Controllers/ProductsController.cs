using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tran.Api.DTOs;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Product>>>> GetAllProducts([FromQuery] bool includeInactive = false)
    {
        try
        {
            var products = await _productService.GetAllProductsAsync(includeInactive);
            return Ok(ApiResponse<List<Product>>.SuccessResult(products));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<Product>>.ErrorResult(ex.Message));
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<Product>>>> SearchProducts([FromQuery] string keyword)
    {
        try
        {
            var products = await _productService.SearchProductsAsync(keyword);
            return Ok(ApiResponse<List<Product>>.SuccessResult(products));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<Product>>.ErrorResult(ex.Message));
        }
    }

    [HttpGet("{productId}")]
    public async Task<ActionResult<ApiResponse<Product>>> GetProductById(int productId)
    {
        try
        {
            var product = await _productService.GetByIdAsync(productId);
            if (product == null)
            {
                return NotFound(ApiResponse<Product>.ErrorResult("품목을 찾을 수 없습니다."));
            }
            return Ok(ApiResponse<Product>.SuccessResult(product));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Product>.ErrorResult(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Product>>> CreateProduct([FromBody] Product product)
    {
        try
        {
            var created = await _productService.CreateAsync(product);
            return CreatedAtAction(nameof(GetProductById), new { productId = created.ProductId }, 
                ApiResponse<Product>.SuccessResult(created, "품목이 생성되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Product>.ErrorResult(ex.Message));
        }
    }

    [HttpPut("{productId}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateProduct(int productId, [FromBody] Product product)
    {
        try
        {
            if (productId != product.ProductId)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("품목 ID가 일치하지 않습니다."));
            }
            await _productService.UpdateAsync(product);
            return Ok(ApiResponse<bool>.SuccessResult(true, "품목이 수정되었습니다."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResult(ex.Message));
        }
    }
}

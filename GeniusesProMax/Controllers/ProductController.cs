using Azure;
using GeniusesProMax.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GeniusesProMax.Controllers
{
    [Route("api/Product")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        public readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(int pageNumber = 1, int pageSize = 4)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 4;
            if (pageSize > 10) pageSize = 10;

            var products = await _productService.GetProductsAsync(pageNumber, pageSize);

            if (products == null || products.Items.Count == 0) return NotFound("No products found (validate your query parameters)");

            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound("Product not found");
            return Ok(product);
        }
    }
}

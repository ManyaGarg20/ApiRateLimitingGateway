using Microsoft.AspNetCore.Mvc;
using BackendApi.DTOs;
using BackendApi.Models;
using BackendApi.Repositories;

namespace BackendApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;

    public ProductsController(IProductRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll()
    {
        var products = await _repository.GetAllAsync();
        var dtos = products.Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Stock));
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        return Ok(new ProductDto(product.Id, product.Name, product.Price, product.Stock));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            Stock = dto.Stock
        };

        var created = await _repository.AddAsync(product);
        var resultDto = new ProductDto(created.Id, created.Name, created.Price, created.Stock);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, resultDto);
    }
}
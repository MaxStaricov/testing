using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.DTOs;

namespace Domain.Interfaces;

public interface IProductProvider
{
    Task<ProductViewDto> CreateProductAsync(ProductCreateDto dto);
    Task<ProductViewDto?> GetProductAsync(Guid id);
    Task<List<ProductViewDto>> GetAllProductsAsync(
        string? searchQuery = null,
        List<string>? categories = null,
        List<string>? flags = null,
        string? necessity = null,
        string? sortBy = null);
    Task<ProductViewDto> UpdateProductAsync(Guid id, ProductUpdateDto dto);
    Task DeleteProductAsync(Guid id);
}

public interface IDishProvider
{
    Task<DishViewDto> CreateDishAsync(DishCreateDto dto);
    Task<DishViewDto?> GetDishAsync(Guid id);
    Task<List<DishViewDto>> GetAllDishesAsync(
        string? searchQuery = null,
        List<string>? categories = null,
        List<string>? flags = null);
    Task<DishViewDto> UpdateDishAsync(Guid id, DishUpdateDto dto);
    Task DeleteDishAsync(Guid id);
}

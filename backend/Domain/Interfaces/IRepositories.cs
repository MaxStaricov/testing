using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Models;

namespace Domain.Interfaces;

public interface IProductRepository
{
    Task<Product> CreateAsync(Product product);
    Task<Product?> GetByIdAsync(Guid id);
    Task<List<Product>> GetAllAsync();
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
    Task<List<string>> GetDishesUsingProductNamesAsync(Guid productId);
}

public interface IDishRepository
{
    Task<Dish> CreateAsync(Dish dish);
    Task<Dish?> GetByIdAsync(Guid id);
    Task<List<Dish>> GetAllAsync();
    Task UpdateAsync(Dish dish);
    Task DeleteAsync(Guid id);
    Task<List<Guid>> GetDishIdsByProductIdAsync(Guid productId);
    Task<List<string>> GetDishesUsingProductNamesAsync(Guid productId);
}

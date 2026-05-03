using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.DTOs;
using Domain.Interfaces;
using Domain.Models;

namespace Infrastructure.Services;

public class ProductProvider : IProductProvider
{
    private readonly IProductRepository _repository;
    private readonly IDishRepository _dishRepository;

    public ProductProvider(IProductRepository repository, IDishRepository dishRepository)
    {
        _repository = repository;
        _dishRepository = dishRepository;
    }

    private ProductViewDto MapToViewDto(Product p)
    {
        return new ProductViewDto
        {
            Id = p.Id,
            Title = p.Title,
            Photos = p.Photos,
            Calories = p.Calories,
            Proteins = p.Proteins,
            Fats = p.Fats,
            Carbohydrates = p.Carbohydrates,
            Description = p.Description,
            Category = p.Category,
            Necessity = p.Necessity,
            Flags = p.Flags,
            DateCreated = p.DateCreated,
            DateModified = p.DateModified
        };
    }

    private void ValidateProduct(ProductCreateDto dto)
    {
        if (dto.Proteins + dto.Fats + dto.Carbohydrates > 100)
            throw new Exception("Sum of proteins, fats, and carbohydrates cannot exceed 100g per 100g.");
    }

    public async Task<ProductViewDto> CreateProductAsync(ProductCreateDto dto)
    {
        ValidateProduct(dto);
        var p = new ProductBuilder()
            .WithTitle(dto.Title)
            .WithPhotos(dto.Photos)
            .WithCalories(dto.Calories)
            .WithProteins(dto.Proteins)
            .WithFats(dto.Fats)
            .WithCarbohydrates(dto.Carbohydrates)
            .WithDescription(dto.Description)
            .WithCategory(dto.Category)
            .WithNecessity(dto.Necessity)
            .WithFlags(dto.Flags)
            .Build();

        var created = await _repository.CreateAsync(p);
        return MapToViewDto(created);
    }

    public async Task<ProductViewDto?> GetProductAsync(Guid id)
    {
        var p = await _repository.GetByIdAsync(id);
        return p == null ? null : MapToViewDto(p);
    }

    public async Task<List<ProductViewDto>> GetAllProductsAsync(
        string? searchQuery = null,
        List<string>? categories = null,
        List<string>? flags = null,
        string? necessity = null,
        string? sortBy = null)
    {
        var all = await _repository.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            all = all.Where(x => x.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (categories != null && categories.Any())
        {
            var parsedCats = categories
                .Select(c => Enum.TryParse<ProductCategory>(c, true, out var res) ? (ProductCategory?)res : null)
                .Where(c => c.HasValue)
                .Select(c => c!.Value)
                .ToList();
            
            if (parsedCats.Any())
            {
                all = all.Where(x => parsedCats.Contains(x.Category)).ToList();
            }
        }

        if (!string.IsNullOrWhiteSpace(necessity))
        {
            if (Enum.TryParse<CookingNecessity>(necessity, true, out var r))
            {
                all = all.Where(x => x.Necessity == r).ToList();
            }
        }

        if (flags != null && flags.Any())
        {
            var parsedFlags = flags
                .Select(f => Enum.TryParse<DietaryFlags>(f, true, out var res) ? (DietaryFlags?)res : null)
                .Where(f => f.HasValue)
                .Select(f => f!.Value)
                .ToList();

            foreach (var flag in parsedFlags)
            {
                all = all.Where(x => x.Flags.HasFlag(flag)).ToList();
            }
        }

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            all = sortBy.ToLowerInvariant() switch
            {
                "title" => all.OrderBy(x => x.Title).ToList(),
                "calories" => all.OrderBy(x => x.Calories).ToList(),
                "proteins" => all.OrderBy(x => x.Proteins).ToList(),
                "fats" => all.OrderBy(x => x.Fats).ToList(),
                "carbohydrates" => all.OrderBy(x => x.Carbohydrates).ToList(),
                _ => all
            };
        }

        return all.Select(MapToViewDto).ToList();
    }

    public async Task<ProductViewDto> UpdateProductAsync(Guid id, ProductUpdateDto dto)
    {
        ValidateProduct(dto);
        var p = await _repository.GetByIdAsync(id);
        if (p == null) throw new Exception("Product not found");

        var updated = new ProductBuilder()
            .WithId(id)
            .WithTitle(dto.Title)
            .WithPhotos(dto.Photos)
            .WithCalories(dto.Calories)
            .WithProteins(dto.Proteins)
            .WithFats(dto.Fats)
            .WithCarbohydrates(dto.Carbohydrates)
            .WithDescription(dto.Description)
            .WithCategory(dto.Category)
            .WithNecessity(dto.Necessity)
            .WithFlags(dto.Flags)
            .WithDateCreated(p.DateCreated)
            .Build();

        await _repository.UpdateAsync(updated);
        return MapToViewDto(updated);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var usedInDishes = await _dishRepository.GetDishesUsingProductNamesAsync(id);
        if (usedInDishes != null && usedInDishes.Any())
        {
            var dishList = string.Join(", ", usedInDishes);
            throw new InvalidOperationException($"Невозможно удалить продукт, так как он используется в следующих блюдах: {dishList}");
        }
        await _repository.DeleteAsync(id);
    }
}

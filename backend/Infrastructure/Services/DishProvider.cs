using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.DTOs;
using Domain.Interfaces;
using Domain.Models;

namespace Infrastructure.Services;

public class DishProvider : IDishProvider
{
    private readonly IDishRepository _repository;
    private readonly IProductRepository _productRepository;

    public DishProvider(IDishRepository repository, IProductRepository productRepository)
    {
        _repository = repository;
        _productRepository = productRepository;
    }

    private DishViewDto MapToViewDto(Dish d)
    {
        return new DishViewDto
        {
            Id = d.Id,
            Title = d.Title,
            Photos = d.Photos,
            PortionSize = d.PortionSize,
            Calories = d.Calories,
            Proteins = d.Proteins,
            Fats = d.Fats,
            Carbohydrates = d.Carbohydrates,
            Category = d.Category,
            Flags = d.Flags,
            Ingredients = d.Ingredients.Select(i => new DishIngredientDto
            {
                ProductId = i.ProductId,
                AmountInGrams = i.AmountInGrams
            }).ToList(),
            DateCreated = d.DateCreated,
            DateModified = d.DateModified
        };
    }

    private void ValidateDish(DishUpdateDto dto)
    {
        if (dto.PortionSize <= 0) throw new Exception("Portion size must be greater than 0.");
        var sumBjuPer100g = (dto.Proteins + dto.Fats + dto.Carbohydrates) / dto.PortionSize * 100;
        if (sumBjuPer100g > 100)
            throw new Exception("Sum of proteins, fats, and carbohydrates per 100g cannot exceed 100g.");
    }

    private async Task<List<DishProductItem>> FetchIngredientsAsync(List<DishIngredientDto> dtoList)
    {
        var ingredients = new List<DishProductItem>();
        if (dtoList == null) return ingredients;

        var groupedIngredients = dtoList
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, AmountInGrams = g.Sum(x => x.AmountInGrams) });

        foreach (var ing in groupedIngredients)
        {
            var p = await _productRepository.GetByIdAsync(ing.ProductId);
            if (p != null)
            {
                ingredients.Add(new DishProductItem
                {
                    ProductId = ing.ProductId,
                    AmountInGrams = ing.AmountInGrams,
                    Product = p
                });
            }
        }
        return ingredients;
    }

    private static readonly Dictionary<string, DishCategory> MacroToCategory = new()
    {
        {"!десерт", DishCategory.Dessert},
        {"!первое", DishCategory.FirstCourse},
        {"!второе", DishCategory.SecondCourse},
        {"!напиток", DishCategory.Drink},
        {"!салат", DishCategory.Salad},
        {"!суп", DishCategory.Soup},
        {"!перекус", DishCategory.Snack}
    };

    private (string, DishCategory?) ParseMacroCategory(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return (title, null);
        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            var lower = word.ToLowerInvariant();
            if (MacroToCategory.TryGetValue(lower, out var cat))
            {
                var newTitle = string.Join(' ', words.Where(w => !string.Equals(w, word, StringComparison.OrdinalIgnoreCase)));
                return (newTitle, cat);
            }
        }
        return (title, null);
    }

    public async Task<DishViewDto> CreateDishAsync(DishCreateDto dto)
    {
        var (titleWithoutMacro, macroCategory) = ParseMacroCategory(dto.Title);
        var category = dto.Category;
        if (macroCategory.HasValue && (dto.Category == 0 || dto.Category == macroCategory.Value))
        {
            category = macroCategory.Value;
        }
        ValidateDish(new DishUpdateDto
        {
            PortionSize = dto.PortionSize,
            Calories = dto.Calories,
            Proteins = dto.Proteins,
            Fats = dto.Fats,
            Carbohydrates = dto.Carbohydrates
        });
        var ingredients = await FetchIngredientsAsync(dto.Ingredients);
        var builder = new DishBuilder()
            .WithTitle(titleWithoutMacro)
            .WithPhotos(dto.Photos)
            .WithPortionSize(dto.PortionSize)
            .WithCategory(category)
            .WithCalories(dto.Calories)
            .WithProteins(dto.Proteins)
            .WithFats(dto.Fats)
            .WithCarbohydrates(dto.Carbohydrates)
            .WithFlags(dto.Flags)
            .WithIngredients(ingredients);
        var dish = builder.Build();
        if (dish.Calories == 0 && dish.Proteins == 0)
        {
            dish.RecalculateMacrosFromIngredients();
        }
        dish.RecalculateFlagsFromIngredients();
        var created = await _repository.CreateAsync(dish);
        return MapToViewDto(created);
    }

    public async Task<DishViewDto?> GetDishAsync(Guid id)
    {
        var d = await _repository.GetByIdAsync(id);
        return d == null ? null : MapToViewDto(d);
    }

    public async Task<List<DishViewDto>> GetAllDishesAsync(
        string? searchQuery = null,
        List<string>? categories = null,
        List<string>? flags = null)
    {
        var all = await _repository.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            all = all.Where(x => x.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (categories != null && categories.Any())
        {
            var parsedCats = categories
                .Select(c => Enum.TryParse<DishCategory>(c, true, out var res) ? (DishCategory?)res : null)
                .Where(c => c.HasValue)
                .Select(c => c!.Value)
                .ToList();

            if (parsedCats.Any())
            {
                all = all.Where(x => parsedCats.Contains(x.Category)).ToList();
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

        return all.Select(MapToViewDto).ToList();
    }

    public async Task<DishViewDto> UpdateDishAsync(Guid id, DishUpdateDto dto)
    {
        var (titleWithoutMacro, macroCategory) = ParseMacroCategory(dto.Title);
        var category = dto.Category;
        if (macroCategory.HasValue && (dto.Category == 0 || dto.Category == macroCategory.Value))
        {
            category = macroCategory.Value;
        }
        ValidateDish(dto);
        var d = await _repository.GetByIdAsync(id);
        if (d == null) throw new Exception("Dish not found");
        var ingredients = await FetchIngredientsAsync(dto.Ingredients);
        var updated = new DishBuilder()
            .WithId(id)
            .WithTitle(titleWithoutMacro)
            .WithPhotos(dto.Photos)
            .WithPortionSize(dto.PortionSize)
            .WithCategory(category)
            .WithCalories(dto.Calories)
            .WithProteins(dto.Proteins)
            .WithFats(dto.Fats)
            .WithCarbohydrates(dto.Carbohydrates)
            .WithFlags(dto.Flags)
            .WithIngredients(ingredients)
            .WithDateCreated(d.DateCreated)
            .Build();
        updated.RecalculateFlagsFromIngredients();
        if (updated.Calories == 0 && updated.Proteins == 0)
        {
            updated.RecalculateMacrosFromIngredients();
        }
        await _repository.UpdateAsync(updated);
        return MapToViewDto(updated);
    }

    public async Task DeleteDishAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }
}

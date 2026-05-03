using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Models;

public class Dish
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<byte[]> Photos { get; set; } = new();
    
    public decimal PortionSize { get; set; }
    public DishCategory Category { get; set; }
    
    public decimal Calories { get; set; }
    public decimal Proteins { get; set; }
    public decimal Fats { get; set; }
    public decimal Carbohydrates { get; set; }

    public DietaryFlags Flags { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? DateModified { get; set; }

    public List<DishProductItem> Ingredients { get; set; } = new();

    public void RecalculateMacrosFromIngredients()
    {
        if (Ingredients == null || Ingredients.Count == 0)
        {
            Calories = 0; Proteins = 0; Fats = 0; Carbohydrates = 0;
            return;
        }

        decimal totalCal = 0, totalPro = 0, totalFat = 0, totalCarb = 0;
        foreach (var ing in Ingredients)
        {
            if (ing.Product == null) continue;
            if (ing.AmountInGrams <= 0) continue;
            var ratio = ing.AmountInGrams / 100m;
            totalCal += ing.Product.Calories * ratio;
            totalPro += ing.Product.Proteins * ratio;
            totalFat += ing.Product.Fats * ratio;
            totalCarb += ing.Product.Carbohydrates * ratio;
        }

        Calories = totalCal;
        Proteins = totalPro;
        Fats = totalFat;
        Carbohydrates = totalCarb;
    }

    public void RecalculateFlagsFromIngredients()
    {
        bool isVegan = true;
        bool isGlutenFree = true;
        bool isSugarFree = true;

        if (Ingredients != null && Ingredients.Any())
        {
            foreach (var ing in Ingredients)
            {
                if (ing.Product == null) continue;
                if (!ing.Product.Flags.HasFlag(DietaryFlags.Vegan)) isVegan = false;
                if (!ing.Product.Flags.HasFlag(DietaryFlags.GlutenFree)) isGlutenFree = false;
                if (!ing.Product.Flags.HasFlag(DietaryFlags.SugarFree)) isSugarFree = false;
            }
        }
        else
        {
            isVegan = isGlutenFree = isSugarFree = false;
        }

        DietaryFlags newFlags = DietaryFlags.None;
        if (isVegan && Flags.HasFlag(DietaryFlags.Vegan)) newFlags |= DietaryFlags.Vegan;
        if (isGlutenFree && Flags.HasFlag(DietaryFlags.GlutenFree)) newFlags |= DietaryFlags.GlutenFree;
        if (isSugarFree && Flags.HasFlag(DietaryFlags.SugarFree)) newFlags |= DietaryFlags.SugarFree;
        Flags = newFlags;
    }
}

public class DishProductItem
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public decimal AmountInGrams { get; set; }
}

public class DishBuilder
{
    private readonly Dish _dish = new();

    public DishBuilder WithId(Guid id) { _dish.Id = id; return this; }
    public DishBuilder WithTitle(string title)
    {
        _dish.Title = title;
        ProcessMacrosIntoCategory();
        return this;
    }
    public DishBuilder WithPhotos(List<byte[]> photos) { _dish.Photos = photos ?? new(); return this; }
    public DishBuilder WithPortionSize(decimal portionSize) { _dish.PortionSize = portionSize; return this; }
    public DishBuilder WithCategory(DishCategory category) { _dish.Category = category; return this; }
    public DishBuilder WithCalories(decimal value) { _dish.Calories = value; return this; }
    public DishBuilder WithProteins(decimal value) { _dish.Proteins = value; return this; }
    public DishBuilder WithFats(decimal value) { _dish.Fats = value; return this; }
    public DishBuilder WithCarbohydrates(decimal value) { _dish.Carbohydrates = value; return this; }
    public DishBuilder WithFlags(DietaryFlags flags) { _dish.Flags = flags; return this; }
    public DishBuilder WithIngredients(List<DishProductItem> ingredients) { _dish.Ingredients = ingredients ?? new(); return this; }
    public DishBuilder WithDateCreated(DateTime? dateCreated) { _dish.DateCreated = dateCreated; return this; }
    public DishBuilder WithDateModified(DateTime? dateModified) { _dish.DateModified = dateModified; return this; }

    private void ProcessMacrosIntoCategory()
    {
        if (string.IsNullOrEmpty(_dish.Title)) return;
        var macrosMap = new Dictionary<string, DishCategory>(StringComparer.OrdinalIgnoreCase)
        {
            {"!десерт", DishCategory.Dessert},
            {"!первое", DishCategory.FirstCourse},
            {"!второе", DishCategory.SecondCourse},
            {"!напиток", DishCategory.Drink},
            {"!салат", DishCategory.Salad},
            {"!суп", DishCategory.Soup},
            {"!перекус", DishCategory.Snack}
        };

        var words = _dish.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (macrosMap.TryGetValue(word, out var cat))
            {
                _dish.Category = cat;
                _dish.Title = _dish.Title.Replace(word, "", StringComparison.OrdinalIgnoreCase).Trim();
                _dish.Title = System.Text.RegularExpressions.Regex.Replace(_dish.Title, @"\s+", " ");
                break;
            }
        }
    }

    public Dish Build()
    {
        return _dish;
    }
}

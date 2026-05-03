using System;
using System.Collections.Generic;
using Domain.Models;

namespace Domain.DTOs;

public class ProductCreateDto
{
    public string Title { get; set; } = string.Empty;
    public List<byte[]> Photos { get; set; } = new();
    public decimal Calories { get; set; }
    public decimal Proteins { get; set; }
    public decimal Fats { get; set; }
    public decimal Carbohydrates { get; set; }
    public string? Description { get; set; }
    public ProductCategory Category { get; set; }
    public CookingNecessity Necessity { get; set; }
    public DietaryFlags Flags { get; set; }
}

public class ProductUpdateDto : ProductCreateDto
{
    public Guid Id { get; set; }
}

public class ProductViewDto : ProductUpdateDto
{
    public DateTime? DateCreated { get; set; }
    public DateTime? DateModified { get; set; }
}

public class DishIngredientDto
{
    public Guid ProductId { get; set; }
    public decimal AmountInGrams { get; set; }
}

public class DishCreateDto
{
    public string Title { get; set; } = string.Empty;
    public List<byte[]> Photos { get; set; } = new();
    public decimal PortionSize { get; set; }
    public DishCategory Category { get; set; }
    public List<DishIngredientDto> Ingredients { get; set; } = new();
    public decimal Calories { get; set; }
    public decimal Proteins { get; set; }
    public decimal Fats { get; set; }
    public decimal Carbohydrates { get; set; }
    public DietaryFlags Flags { get; set; }
}

public class DishUpdateDto : DishCreateDto
{
    public Guid Id { get; set; }
    public decimal Calories { get; set; }
    public decimal Proteins { get; set; }
    public decimal Fats { get; set; }
    public decimal Carbohydrates { get; set; }
    public DietaryFlags Flags { get; set; }
}

public class DishViewDto : DishUpdateDto
{
    public DateTime? DateCreated { get; set; }
    public DateTime? DateModified { get; set; }
}


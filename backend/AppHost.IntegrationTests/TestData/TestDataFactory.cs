using Domain.DTOs;
using Domain.Models;

namespace TestModule.Backend.IntegrationTests.TestData;

public static class ProductTestDataFactory
{
    public static ProductCreateDto CreateValidProduct(string title = "Test Product") =>
        new()
        {
            Title = title,
            Photos = new List<byte[]>(),
            Calories = 100m,
            Proteins = 10m,
            Fats = 5m,
            Carbohydrates = 15m,
            Description = "A standard test product.",
            Category = ProductCategory.Vegetables,
            Necessity = CookingNecessity.RequiresCooking,
            Flags = DietaryFlags.None
        };

    public static ProductCreateDto CreateMinimalProduct() =>
        new()
        {
            Title = "Minimal",
            Photos = new List<byte[]>(),
            Calories = 0,
            Proteins = 0,
            Fats = 0,
            Carbohydrates = 0,
            Description = null,
            Category = ProductCategory.Frozen,
            Necessity = CookingNecessity.ReadyToEat,
            Flags = DietaryFlags.None
        };

    public static ProductCreateDto CreateSpecialCharacterTitleProduct() =>
        new()
        {
            Title = "Яблоко & Груша / Тест #1 (ÄÖÜ)",
            Calories = 52m,
            Proteins = 0.3m,
            Fats = 0.2m,
            Carbohydrates = 14m,
            Category = ProductCategory.Vegetables,
            Necessity = CookingNecessity.ReadyToEat,
            Flags = DietaryFlags.Vegan
        };

    public static ProductCreateDto CreateProductInCategory(ProductCategory category) =>
        new()
        {
            Title = $"Product [{category}]",
            Calories = 100,
            Proteins = 10,
            Fats = 5,
            Carbohydrates = 15,
            Category = category,
            Necessity = CookingNecessity.RequiresCooking,
            Flags = DietaryFlags.None
        };

    public static ProductCreateDto CreateProductWithNecessity(CookingNecessity necessity) =>
        new()
        {
            Title = $"Product [necessity={necessity}]",
            Calories = 100,
            Proteins = 10,
            Fats = 5,
            Carbohydrates = 15,
            Category = ProductCategory.Meat,
            Necessity = necessity,
            Flags = DietaryFlags.None
        };

    public static ProductCreateDto CreateVeganProduct() =>
        new()
        {
            Title = "Vegan Product",
            Calories = 150,
            Proteins = 8,
            Fats = 3,
            Carbohydrates = 20,
            Description = "Plant-based.",
            Category = ProductCategory.Vegetables,
            Necessity = CookingNecessity.RequiresCooking,
            Flags = DietaryFlags.Vegan
        };

    public static ProductCreateDto CreateGlutenFreeProduct() =>
        new()
        {
            Title = "Gluten Free Product",
            Calories = 200,
            Proteins = 12,
            Fats = 4,
            Carbohydrates = 30,
            Category = ProductCategory.Cereals,
            Necessity = CookingNecessity.RequiresCooking,
            Flags = DietaryFlags.GlutenFree
        };

    public static ProductCreateDto CreateComplexDietaryFlagsProduct() =>
        new()
        {
            Title = "Complex Dietary Product",
            Calories = 120,
            Proteins = 6,
            Fats = 2,
            Carbohydrates = 15,
            Category = ProductCategory.Greens,
            Necessity = CookingNecessity.ReadyToEat,
            Flags = DietaryFlags.Vegan | DietaryFlags.GlutenFree | DietaryFlags.SugarFree
        };

    public static ProductCreateDto CreateZeroCalorieProduct() =>
        new()
        {
            Title = "Zero Calorie Product",
            Calories = 0m,
            Proteins = 0m,
            Fats = 0m,
            Carbohydrates = 0m,
            Category = ProductCategory.Liquid,
            Necessity = CookingNecessity.ReadyToEat,
            Flags = DietaryFlags.None
        };

    public static ProductCreateDto CreateJustAboveZeroCalorieProduct() =>
        new()
        {
            Title = "Nearly Zero Calorie Product",
            Calories = 0.01m,
            Proteins = 0m,
            Fats = 0m,
            Carbohydrates = 0m,
            Category = ProductCategory.Liquid,
            Necessity = CookingNecessity.ReadyToEat,
            Flags = DietaryFlags.None
        };

    public static ProductCreateDto CreateHighCalorieProduct() =>
        new()
        {
            Title = "High Calorie Product",
            Calories = 9000m,
            Proteins = 50m,
            Fats = 90m,
            Carbohydrates = 0m,
            Category = ProductCategory.Sweets,
            Necessity = CookingNecessity.ReadyToEat,
            Flags = DietaryFlags.None
        };

    public static ProductCreateDto CreateMinLengthTitleProduct() =>
        new()
        {
            Title = "X",
            Calories = 50m,
            Proteins = 5m,
            Fats = 2m,
            Carbohydrates = 8m,
            Category = ProductCategory.Greens,
            Necessity = CookingNecessity.ReadyToEat,
            Flags = DietaryFlags.None
        };

    public static ProductCreateDto CreateLongTitleProduct() =>
        new()
        {
            Title = "Very Long Product Title That Contains Many Characters " +
                    "And Tests The Boundary Of String Length Validation " +
                    "With Multiple Sentences And Punctuation!!!",
            Calories = 50m,
            Proteins = 5m,
            Fats = 2m,
            Carbohydrates = 8m,
            Category = ProductCategory.Greens,
            Necessity = CookingNecessity.ReadyToEat,
            Flags = DietaryFlags.None
        };
}

public static class DishTestDataFactory
{
    public static DishCreateDto CreateValidDish(string title = "Test Dish") =>
        new()
        {
            Title = title,
            Photos = new List<byte[]>(),
            PortionSize = 200m,
            Category = DishCategory.SecondCourse,
            Ingredients = new List<DishIngredientDto>(),
            Calories = 300m,
            Proteins = 25m,
            Fats = 10m,
            Carbohydrates = 40m,
            Flags = DietaryFlags.None
        };

    public static DishCreateDto CreateMinimalDish() =>
        new()
        {
            Title = "Minimal Dish",
            Photos = new List<byte[]>(),
            PortionSize = 100m,
            Category = DishCategory.Snack,
            Ingredients = new List<DishIngredientDto>(),
            Calories = 0m,
            Proteins = 0m,
            Fats = 0m,
            Carbohydrates = 0m,
            Flags = DietaryFlags.None
        };

    public static DishCreateDto CreateDishWithSaladMacroInTitle() =>
        new()
        {
            Title = "Летний !салат",
            PortionSize = 180m,
            Category = DishCategory.Snack,
            Ingredients = new List<DishIngredientDto>(),
            Calories = 150m,
            Proteins = 5m,
            Fats = 8m,
            Carbohydrates = 12m,
            Flags = DietaryFlags.Vegan
        };

    public static DishCreateDto CreateDishWithDessertMacroInTitle() =>
        new()
        {
            Title = "Шоколадный !десерт мусс",
            PortionSize = 120m,
            Category = DishCategory.Snack,
            Ingredients = new List<DishIngredientDto>(),
            Calories = 280m,
            Proteins = 6m,
            Fats = 15m,
            Carbohydrates = 32m,
            Flags = DietaryFlags.None
        };

    public static DishCreateDto CreateDishInCategory(DishCategory category) =>
        new()
        {
            Title = $"Dish [{category}]",
            PortionSize = 200m,
            Category = category,
            Ingredients = new List<DishIngredientDto>(),
            Calories = 300m,
            Proteins = 20m,
            Fats = 10m,
            Carbohydrates = 40m,
            Flags = DietaryFlags.None
        };

    public static DishCreateDto CreateVeganDish() =>
        new()
        {
            Title = "Vegan Dish",
            PortionSize = 250m,
            Category = DishCategory.Salad,
            Ingredients = new List<DishIngredientDto>(),
            Calories = 200m,
            Proteins = 8m,
            Fats = 5m,
            Carbohydrates = 30m,
            Flags = DietaryFlags.Vegan
        };

    public static DishCreateDto CreateLargePortionDish() =>
        new()
        {
            Title = "Large Portion Dish",
            PortionSize = 1000m,
            Category = DishCategory.FirstCourse,
            Ingredients = new List<DishIngredientDto>(),
            Calories = 500m,
            Proteins = 40m,
            Fats = 20m,
            Carbohydrates = 60m,
            Flags = DietaryFlags.None
        };

    public static DishCreateDto CreateSmallPortionDish() =>
        new()
        {
            Title = "Small Portion Dish",
            PortionSize = 0.1m,
            Category = DishCategory.Dessert,
            Ingredients = new List<DishIngredientDto>(),
            Calories = 0.3m,
            Proteins = 0.03m,
            Fats = 0.03m,
            Carbohydrates = 0.04m,
            Flags = DietaryFlags.None
        };

    public static DishCreateDto CreateZeroPortionDish() =>
        new()
        {
            Title = "Zero Portion Dish",
            PortionSize = 0m,
            Category = DishCategory.Snack,
            Ingredients = new List<DishIngredientDto>(),
            Calories = 100m,
            Proteins = 5m,
            Fats = 5m,
            Carbohydrates = 10m,
            Flags = DietaryFlags.None
        };

    public static DishCreateDto CreateMacrosTooHighDish() =>
        new()
        {
            Title = "Too High Macros Dish",
            PortionSize = 100m,
            Category = DishCategory.Snack,
            Ingredients = new List<DishIngredientDto>(),
            Calories = 700m,
            Proteins = 50m,
            Fats = 40m,
            Carbohydrates = 30m,
            Flags = DietaryFlags.None
        };

    public static DishCreateDto CreateMacrosAtExactLimitDish() =>
        new()
        {
            Title = "Exact Limit Macros Dish",
            PortionSize = 100m,
            Category = DishCategory.SecondCourse,
            Ingredients = new List<DishIngredientDto>(),
            Calories = 590m,
            Proteins = 40m,
            Fats = 30m,
            Carbohydrates = 30m,
            Flags = DietaryFlags.None
        };
}

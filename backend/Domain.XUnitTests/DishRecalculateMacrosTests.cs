using Domain.Models;
using FluentAssertions;
using Xunit;

namespace Domain.XUnitTests;

public class DishRecalculateMacrosTests
{
    private static Product CreateProduct(decimal calories, decimal proteins, decimal fats, decimal carbohydrates) =>
        new()
        {
            Calories = calories,
            Proteins = proteins,
            Fats = fats,
            Carbohydrates = carbohydrates
        };

    private static DishProductItem CreateIngredient(Product product, decimal amountInGrams) =>
        new()
        {
            Product = product,
            AmountInGrams = amountInGrams
        };

    [Fact]
    public void RecalculateMacros_WhenIngredientsIsNull_SetsAllMacrosToZero()
    {
        var dish = new Dish { Ingredients = null! };

        dish.RecalculateMacrosFromIngredients();

        dish.Calories.Should().Be(0m);
        dish.Proteins.Should().Be(0m);
        dish.Fats.Should().Be(0m);
        dish.Carbohydrates.Should().Be(0m);
    }

    [Fact]
    public void RecalculateMacros_WhenIngredientsIsEmpty_SetsAllMacrosToZero()
    {
        var dish = new Dish { Ingredients = [] };

        dish.RecalculateMacrosFromIngredients();

        dish.Calories.Should().Be(0m);
        dish.Proteins.Should().Be(0m);
        dish.Fats.Should().Be(0m);
        dish.Carbohydrates.Should().Be(0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void RecalculateMacros_WhenAmountIsZeroOrNegative_SkipsIngredient(decimal amountInGrams)
    {
        var product = CreateProduct(200m, 10m, 5m, 20m);
        var dish = new Dish
        {
            Ingredients = [CreateIngredient(product, amountInGrams)]
        };

        dish.RecalculateMacrosFromIngredients();

        dish.Calories.Should().Be(0m);
        dish.Proteins.Should().Be(0m);
        dish.Fats.Should().Be(0m);
        dish.Carbohydrates.Should().Be(0m);
    }

    [Fact]
    public void RecalculateMacros_WhenProductIsNull_SkipsIngredient()
    {
        var dish = new Dish
        {
            Ingredients =
            [
                new DishProductItem
                {
                    Product = null!,
                    AmountInGrams = 150m
                }
            ]
        };

        dish.RecalculateMacrosFromIngredients();

        dish.Calories.Should().Be(0m);
        dish.Proteins.Should().Be(0m);
        dish.Fats.Should().Be(0m);
        dish.Carbohydrates.Should().Be(0m);
    }

    [Fact]
    public void RecalculateMacros_With100gOfIngredient_EqualsProductMacros()
    {
        var product = CreateProduct(250.5m, 12.3m, 8.7m, 30.1m);
        var dish = new Dish
        {
            Ingredients = [CreateIngredient(product, 100m)]
        };

        dish.RecalculateMacrosFromIngredients();

        dish.Calories.Should().Be(250.5m);
        dish.Proteins.Should().Be(12.3m);
        dish.Fats.Should().Be(8.7m);
        dish.Carbohydrates.Should().Be(30.1m);
    }

    [Fact]
    public void RecalculateMacros_WithMinimalPositiveWeight_CalculatesCorrectProportion()
    {
        var product = CreateProduct(1000m, 50m, 50m, 50m);
        var dish = new Dish
        {
            Ingredients = [CreateIngredient(product, 0.01m)]
        };

        dish.RecalculateMacrosFromIngredients();

        var ratio = 0.01m / 100m;
        dish.Calories.Should().Be(product.Calories * ratio);
        dish.Proteins.Should().Be(product.Proteins * ratio);
        dish.Fats.Should().Be(product.Fats * ratio);
        dish.Carbohydrates.Should().Be(product.Carbohydrates * ratio);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(900, 100, 100, 100)]
    public void RecalculateMacros_WithBoundaryMacroValues_CalculatesCorrectly(
        decimal calories,
        decimal proteins,
        decimal fats,
        decimal carbohydrates)
    {
        var product = CreateProduct(calories, proteins, fats, carbohydrates);
        var dish = new Dish
        {
            Ingredients = [CreateIngredient(product, 100m)]
        };

        dish.RecalculateMacrosFromIngredients();

        dish.Calories.Should().Be(calories);
        dish.Proteins.Should().Be(proteins);
        dish.Fats.Should().Be(fats);
        dish.Carbohydrates.Should().Be(carbohydrates);
    }

    [Fact]
    public void RecalculateMacros_WithMultipleValidIngredients_SumsProportionalValues()
    {
        var firstProduct = CreateProduct(200m, 10m, 8m, 20m);
        var secondProduct = CreateProduct(150m, 12m, 5m, 25m);
        var dish = new Dish
        {
            Ingredients =
            [
                CreateIngredient(firstProduct, 150m),
                CreateIngredient(secondProduct, 200m)
            ]
        };

        dish.RecalculateMacrosFromIngredients();

        dish.Calories.Should().Be(600m);
        dish.Proteins.Should().Be(39m);
        dish.Fats.Should().Be(22m);
        dish.Carbohydrates.Should().Be(80m);
    }

    [Fact]
    public void RecalculateMacros_WithMixedValidAndInvalidIngredients_SumsOnlyValid()
    {
        var validProduct = CreateProduct(100m, 10m, 5m, 10m);
        var zeroAmountProduct = CreateProduct(200m, 20m, 10m, 20m);
        var negativeAmountProduct = CreateProduct(50m, 5m, 2m, 5m);
        var dish = new Dish
        {
            Ingredients =
            [
                CreateIngredient(validProduct, 100m),
                CreateIngredient(zeroAmountProduct, 0m),
                new DishProductItem { Product = null!, AmountInGrams = 50m },
                CreateIngredient(negativeAmountProduct, -10m)
            ]
        };

        dish.RecalculateMacrosFromIngredients();

        dish.Calories.Should().Be(100m);
        dish.Proteins.Should().Be(10m);
        dish.Fats.Should().Be(5m);
        dish.Carbohydrates.Should().Be(10m);
    }

    [Fact]
    public void RecalculateMacros_WithFractionalGrams_MaintainsDecimalPrecision()
    {
        var product = CreateProduct(123.456m, 10.111m, 5.222m, 20.333m);
        var dish = new Dish
        {
            Ingredients = [CreateIngredient(product, 33.333m)]
        };

        dish.RecalculateMacrosFromIngredients();

        var ratio = 33.333m / 100m;
        dish.Calories.Should().Be(product.Calories * ratio);
        dish.Proteins.Should().Be(product.Proteins * ratio);
        dish.Fats.Should().Be(product.Fats * ratio);
        dish.Carbohydrates.Should().Be(product.Carbohydrates * ratio);
    }
}

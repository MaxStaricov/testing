using Domain.Models;
using FluentAssertions;
using Xunit;

namespace Domain.XUnitTests;

/// <summary>
/// Unit-тесты для <see cref="Dish.RecalculateMacrosFromIngredients"/>.
///
/// Цель: проверить автоматический расчет КБЖУ блюда как суммы КБЖУ ингредиентов
/// пропорционально их весу в граммах.
///
/// </summary>
public class DishRecalculateMacrosTests
{
    private static DishProductItem Ingredient(Product? product, decimal amountInGrams) =>
        new()
        {
            Product = product!,
            AmountInGrams = amountInGrams
        };
    private static Product ProductWithMacros(
        decimal calories,
        decimal proteins,
        decimal fats,
        decimal carbohydrates) =>
        new()
        {
            Calories = calories,
            Proteins = proteins,
            Fats = fats,
            Carbohydrates = carbohydrates
        };

    private static void AssertMacros(
        Dish dish,
        decimal expectedCalories,
        decimal expectedProteins,
        decimal expectedFats,
        decimal expectedCarbohydrates)
    {
        dish.Calories.Should().Be(expectedCalories);
        dish.Proteins.Should().Be(expectedProteins);
        dish.Fats.Should().Be(expectedFats);
        dish.Carbohydrates.Should().Be(expectedCarbohydrates);
    }





    /// <summary>
    /// EP: список ингредиентов отсутствует.
    /// Ожидаем сброс всех рассчитанных значений в 0.
    /// </summary>
    [Fact]
    public void RecalculateMacros_WhenIngredientsIsNull_SetsAllMacrosToZero()
    {
        var dish = new Dish
        {
            Calories = 10m,
            Proteins = 20m,
            Fats = 30m,
            Carbohydrates = 40m,
            Ingredients = null!
        };

        dish.RecalculateMacrosFromIngredients();

        AssertMacros(dish, 0m, 0m, 0m, 0m);
    }

    /// <summary>
    /// EP: список ингредиентов есть, но он пустой.
    /// отдельный класс эквивалентности от null.
    /// </summary>
    [Fact]
    public void RecalculateMacros_WhenIngredientsIsEmpty_SetsAllMacrosToZero()
    {
        var dish = new Dish
        {
            Calories = 10m,
            Proteins = 20m,
            Fats = 30m,
            Carbohydrates = 40m,
            Ingredients = []
        };

        dish.RecalculateMacrosFromIngredients();

        AssertMacros(dish, 0m, 0m, 0m, 0m);
    }

    /// <summary>
    /// EP: один валидный ингредиент с весом больше 100 г.
    /// Проверяем стандартный рабочий сценарий с коэффициентом больше 1.
    /// </summary>
    [Fact]
    public void RecalculateMacros_WhenIngredientAmountIsMoreThan100g_MultipliesProductMacros()
    {
        var product = ProductWithMacros(100m, 10m, 5m, 20m);
        var dish = new Dish
        {
            Ingredients = [Ingredient(product, 250m)]
        };

        dish.RecalculateMacrosFromIngredients();

        AssertMacros(dish, 250m, 25m, 12.5m, 50m);
    }

    /// <summary>
    /// EP: один валидный ингредиент с весом меньше 100 г.
    /// Проверяем стандартный рабочий сценарий с коэффициентом меньше 1.
    /// </summary>
    [Fact]
    public void RecalculateMacros_WhenIngredientAmountIsLessThan100g_CalculatesFractionOfProductMacros()
    {
        var product = ProductWithMacros(100m, 10m, 6m, 20m);
        var dish = new Dish
        {
            Ingredients = [Ingredient(product, 50m)]
        };

        dish.RecalculateMacrosFromIngredients();

        AssertMacros(dish, 50m, 5m, 3m, 10m);
    }

    /// <summary>
    /// EP: несколько валидных ингредиентов.
    /// КБЖУ блюда должны быть суммой пропорционально пересчитанных КБЖУ продуктов.
    /// </summary>
    [Fact]
    public void RecalculateMacros_WhenDishHasMultipleValidIngredients_SumsProportionalMacros()
    {
        var meat = ProductWithMacros(250m, 26m, 15m, 0m);
        var potato = ProductWithMacros(77m, 2m, 0.4m, 17m);
        var dish = new Dish
        {
            Ingredients =
            [
                Ingredient(meat, 200m),
                Ingredient(potato, 150m)
            ]
        };

        dish.RecalculateMacrosFromIngredients();

        AssertMacros(
            dish,
            expectedCalories: 250m * 2m + 77m * 1.5m,
            expectedProteins: 26m * 2m + 2m * 1.5m,
            expectedFats: 15m * 2m + 0.4m * 1.5m,
            expectedCarbohydrates: 0m * 2m + 17m * 1.5m);
    }

    /// <summary>
    /// EP: смешанный список валидных и невалидных ингредиентов.
    /// В расчете должны участвовать только элементы с Product != null и AmountInGrams > 0.
    /// </summary>
    [Fact]
    public void RecalculateMacros_WhenDishHasMixedIngredients_SumsOnlyValidIngredients()
    {
        var validProduct = ProductWithMacros(100m, 10m, 5m, 10m);
        var zeroAmountProduct = ProductWithMacros(200m, 20m, 10m, 20m);
        var negativeAmountProduct = ProductWithMacros(50m, 5m, 2m, 5m);
        var dish = new Dish
        {
            Ingredients =
            [
                Ingredient(validProduct, 100m),
                Ingredient(zeroAmountProduct, 0m),
                Ingredient(null, 50m),
                Ingredient(negativeAmountProduct, -10m)
            ]
        };

        dish.RecalculateMacrosFromIngredients();

        AssertMacros(dish, 100m, 10m, 5m, 10m);
    }

    /// <summary>
    /// BVA: 0 г и отрицательный вес находятся на границе/за границей валидного диапазона.
    /// Такие ингредиенты должны быть проигнорированы.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void RecalculateMacros_WhenAmountIsZeroOrNegative_SkipsIngredient(int amountInGrams)
    {
        var product = ProductWithMacros(200m, 10m, 5m, 20m);
        var dish = new Dish
        {
            Ingredients = [Ingredient(product, amountInGrams)]
        };

        dish.RecalculateMacrosFromIngredients();

        AssertMacros(dish, 0m, 0m, 0m, 0m);
    }

    /// <summary>
    /// BVA: 100 г является базовой границей, при которой коэффициент ratio равен 1.
    /// КБЖУ блюда должны совпасть с КБЖУ продукта на 100 г.
    /// </summary>
    [Fact]
    public void RecalculateMacros_WhenIngredientAmountIs100g_EqualsProductMacros()
    {
        var product = ProductWithMacros(250.5m, 12.3m, 8.7m, 30.1m);
        var dish = new Dish
        {
            Ingredients = [Ingredient(product, 100m)]
        };

        dish.RecalculateMacrosFromIngredients();

        AssertMacros(dish, 250.5m, 12.3m, 8.7m, 30.1m);
    }

    /// <summary>
    /// BVA: минимальный положительный вес 0.01 г.
    /// Проверяем, что decimal-расчет сохраняет точность пропорции.
    /// </summary>
    [Fact]
    public void RecalculateMacros_WhenIngredientAmountIsMinimalPositiveValue_CalculatesProportion()
    {
        var product = ProductWithMacros(1000m, 50m, 25m, 75m);
        var dish = new Dish
        {
            Ingredients = [Ingredient(product, 0.01m)]
        };

        dish.RecalculateMacrosFromIngredients();

        var ratio = 0.01m / 100m;
        AssertMacros(
            dish,
            product.Calories * ratio,
            product.Proteins * ratio,
            product.Fats * ratio,
            product.Carbohydrates * ratio);
    }


    /// <summary>
    /// BVA: КБЖУ продукта равны 0.
    /// Это нижняя граница значений макронутриентов.
    /// </summary>
    [Fact]
    public void RecalculateMacros_WhenProductMacrosAreZero_ReturnsZeroMacros()
    {
        var product = ProductWithMacros(0m, 0m, 0m, 0m);
        var dish = new Dish
        {
            Ingredients = [Ingredient(product, 100m)]
        };

        dish.RecalculateMacrosFromIngredients();

        AssertMacros(dish, 0m, 0m, 0m, 0m);
    }

    /// <summary>
    /// BVA: дробный вес ингредиента.
    /// Проверяем точность decimal-арифметики без округления внутри доменной функции.
    /// </summary>
    [Fact]
    public void RecalculateMacros_WhenIngredientAmountIsFractional_MaintainsDecimalPrecision()
    {
        var product = ProductWithMacros(123.456m, 10.111m, 5.222m, 20.333m);
        var dish = new Dish
        {
            Ingredients = [Ingredient(product, 33.333m)]
        };

        dish.RecalculateMacrosFromIngredients();

        var ratio = 33.333m / 100m;
        AssertMacros(
            dish,
            product.Calories * ratio,
            product.Proteins * ratio,
            product.Fats * ratio,
            product.Carbohydrates * ratio);
    }
}

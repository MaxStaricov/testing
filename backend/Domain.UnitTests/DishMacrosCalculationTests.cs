using System;
using System.Collections.Generic;
using Domain.Models;
using NUnit.Framework;

namespace Domain.UnitTests;

/// <summary>
/// Unit tests for Dish macro calculation functionality.
///
/// Test Design Techniques:
/// - Equivalence Partitioning: Valid weights, boundary weights, zero/negative weights
/// - Boundary Value Analysis: Testing edge cases (0, -1, min/max decimals, fractions)
/// - Parametrized Tests: Testing multiple input scenarios
///
/// BDD Format: КОГДА (When) - ТОГДА (Then)
/// </summary>
[TestFixture]
public class DishMacrosCalculationTests
{
    private Dish _dish = null!;

    [SetUp]
    public void Setup()
    {
        _dish = new Dish();
    }

    /// <summary>
    /// Creates a test product with specified macronutrient values.
    /// Helper method for test setup.
    /// </summary>
    private Product CreateProductStub(decimal calories, decimal proteins, decimal fats, decimal carbs)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Calories = calories,
            Proteins = proteins,
            Fats = fats,
            Carbohydrates = carbs
        };
    }

    #region BVA - Boundary Value Analysis Tests

    /// <summary>
    /// КОГДА подаются граничные значения для веса ингредиента
    /// ТОГДА расчет КБЖУ блюда выполняется корректно
    ///
    /// Boundary Values tested:
    /// - BV1: Negative weight (-100, -1) -> Expected: 0 multiplier
    /// - BV2: Zero weight (0) -> Expected: 0 multiplier
    /// - BV3: Minimum positive weight (1) -> Expected: 1 multiplier
    /// - BV4: Normal weights (50, 100)
    /// - BV5: Large weight (10000)
    /// </summary>
    [TestCase(-100, 0)]
    [TestCase(-1, 0)]
    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(50, 50)]
    [TestCase(100, 100)]
    [TestCase(10000, 10000)]
    public void RecalculateMacros_BoundaryValuesForAmount_IsCorrect(int amountInGrams, int expectedMultiplierFactor)
    {
        // ARRANGE: Подготавливаем блюдо с одним ингредиентом заданного веса
        var productStub = CreateProductStub(100m, 10m, 5m, 20m);
        _dish.Ingredients = new List<DishProductItem>
        {
            new DishProductItem { Product = productStub, AmountInGrams = amountInGrams }
        };

        // ACT: Пересчитываем КБЖУ блюда
        _dish.RecalculateMacrosFromIngredients();

        // THEN: Проверяем корректность расчета
        var expectedRatio = expectedMultiplierFactor / 100m;
        Assert.That(_dish.Calories, Is.EqualTo(100m * expectedRatio), "Калории не совпадают");
        Assert.That(_dish.Proteins, Is.EqualTo(10m * expectedRatio), "Белки не совпадают");
        Assert.That(_dish.Fats, Is.EqualTo(5m * expectedRatio), "Жиры не совпадают");
        Assert.That(_dish.Carbohydrates, Is.EqualTo(20m * expectedRatio), "Углеводы не совпадают");
    }

    /// <summary>
    /// КОГДА ингредиент имеет экстремально малую дробную граммовку
    /// ТОГДА КБЖУ рассчитывается без ошибок
    ///
    /// BV6: Fractional boundary values (0.01g)
    /// Tests precision of decimal calculations
    /// </summary>
    [Test]
    public void RecalculateMacros_FractionalBoundaryAmount_IsCorrect()
    {
        // ARRANGE: Подготавливаем блюдо с дробным весом
        var productStub = CreateProductStub(100m, 10m, 5m, 20m);
        _dish.Ingredients = new List<DishProductItem>
        {
            new DishProductItem { Product = productStub, AmountInGrams = 0.01m }
        };

        // ACT: Пересчитываем КБЖУ
        _dish.RecalculateMacrosFromIngredients();

        // THEN: Проверяем корректность с дробной точностью
        var ratio = 0.01m / 100m;
        Assert.That(_dish.Calories, Is.EqualTo(100m * ratio).Within(0.0001m), "Калории с дробью не совпадают");
        Assert.That(_dish.Proteins, Is.EqualTo(10m * ratio).Within(0.0001m), "Белки с дробью не совпадают");
    }

    #endregion

    #region EP - Equivalence Partitioning Tests

    /// <summary>
    /// КОГДА блюдо содержит несколько ингредиентов с положительным весом
    /// ТОГДА КБЖУ блюда равно сумме КБЖУ ингредиентов
    ///
    /// EP1: Multiple valid ingredients
    /// Tests summation logic for ingredients
    /// </summary>
    [Test]
    public void RecalculateMacros_MultipleIngredients_CalculatesSumCorrectly()
    {
        // ARRANGE: Подготавливаем блюдо с несколькими ингредиентами
        var meatStub = CreateProductStub(250m, 26m, 15m, 0m);
        var potatoStub = CreateProductStub(77m, 2m, 0.4m, 17m);

        _dish.Ingredients = new List<DishProductItem>
        {
            new DishProductItem { Product = meatStub, AmountInGrams = 200m },
            new DishProductItem { Product = potatoStub, AmountInGrams = 150m }
        };

        // ACT: Пересчитываем КБЖУ
        _dish.RecalculateMacrosFromIngredients();

        // THEN: Проверяем что результат - сумма компонентов
        Assert.That(_dish.Calories, Is.EqualTo((250m * 2.0m) + (77m * 1.5m)), "Сумма калорий не совпадает");
        Assert.That(_dish.Proteins, Is.EqualTo((26m * 2.0m) + (2m * 1.5m)), "Сумма белков не совпадает");
        Assert.That(_dish.Fats, Is.EqualTo((15m * 2.0m) + (0.4m * 1.5m)), "Сумма жиров не совпадает");
        Assert.That(_dish.Carbohydrates, Is.EqualTo((0m * 2.0m) + (17m * 1.5m)), "Сумма углеводов не совпадает");
    }

    /// <summary>
    /// КОГДА список ингредиентов пуст или равен null
    /// ТОГДА все макронутриенты блюда равны 0
    ///
    /// EP2: Empty/Null ingredients - Invalid/Edge case partition
    /// Tests null-safety
    /// </summary>
    [Test]
    public void RecalculateMacros_EmptyOrNullIngredients_ReturnsZero()
    {
        // ARRANGE: Подготавливаем блюдо без ингредиентов
        _dish.Ingredients = null!;

        // ACT: Пересчитываем КБЖУ
        _dish.RecalculateMacrosFromIngredients();

        // THEN: Проверяем что результат - нули
        Assert.Multiple(() =>
        {
            Assert.That(_dish.Calories, Is.EqualTo(0m), "Калории должны быть 0");
            Assert.That(_dish.Proteins, Is.EqualTo(0m), "Белки должны быть 0");
            Assert.That(_dish.Fats, Is.EqualTo(0m), "Жиры должны быть 0");
            Assert.That(_dish.Carbohydrates, Is.EqualTo(0m), "Углеводы должны быть 0");
        });
    }

    /// <summary>
    /// КОГДА в блюде есть ингредиент без продукта (Product == null)
    /// ТОГДА этот ингредиент игнорируется при расчёте
    ///
    /// EP3: Invalid ingredient (null product) - should be ignored
    /// Tests robustness with corrupted data
    /// </summary>
    [Test]
    public void RecalculateMacros_IngredientWithNullProduct_IsIgnored()
    {
        // ARRANGE: Подготавливаем блюдо с валидным и невалидным ингредиентом
        var validProduct = CreateProductStub(100m, 10m, 10m, 10m);
        _dish.Ingredients = new List<DishProductItem>
        {
            new DishProductItem { Product = validProduct, AmountInGrams = 100m },
            new DishProductItem { Product = null!, AmountInGrams = 500m }
        };

        // ACT: Пересчитываем КБЖУ
        _dish.RecalculateMacrosFromIngredients();

        // THEN: Проверяем что только валидный ингредиент учтен
        Assert.That(_dish.Calories, Is.EqualTo(100m), "Калории от валидного ингредиента");
        Assert.That(_dish.Proteins, Is.EqualTo(10m), "Белки от валидного ингредиента");
        Assert.That(_dish.Fats, Is.EqualTo(10m), "Жиры от валидного ингредиента");
        Assert.That(_dish.Carbohydrates, Is.EqualTo(10m), "Углеводы от валидного ингредиента");
    }

    #endregion

    #region Edge Cases & Special Scenarios

    /// <summary>
    /// КОГДА список ингредиентов пуст (но не null)
    /// ТОГДА КБЖУ блюда равно 0
    ///
    /// Edge case: Empty list vs null list
    /// </summary>
    [Test]
    public void RecalculateMacros_EmptyIngredientsCollection_ReturnsZero()
    {
        // ARRANGE: Подготавливаем пустое список ингредиентов
        _dish.Ingredients = new List<DishProductItem>();

        // ACT: Пересчитываем КБЖУ
        _dish.RecalculateMacrosFromIngredients();

        // THEN: Проверяем что результат - нули
        Assert.Multiple(() =>
        {
            Assert.That(_dish.Calories, Is.EqualTo(0m));
            Assert.That(_dish.Proteins, Is.EqualTo(0m));
            Assert.That(_dish.Fats, Is.EqualTo(0m));
            Assert.That(_dish.Carbohydrates, Is.EqualTo(0m));
        });
    }

    #endregion
}


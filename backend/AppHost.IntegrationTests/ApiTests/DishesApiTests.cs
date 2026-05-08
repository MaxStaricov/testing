using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.DTOs;
using Domain.Models;
using TestModule.Backend.IntegrationTests.Fixtures;
using TestModule.Backend.IntegrationTests.TestData;
using Xunit;

namespace TestModule.Backend.IntegrationTests.ApiTests;

/// <summary>
/// Набор интеграционных тестов для проверки API управления блюдами.
/// Тесты сгруппированы по техникам тестирования: классы эквивалентности и пограничные значения.
/// </summary>
[Collection("ApiCollection")]
public class DishesApiTests
{
    private readonly ApiFixture _fixture;
    private const string DishUrl = "/api/dishes";
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public DishesApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }




    #region Классы эквивалентности: валидные сценарии создания

    /// <summary>
    /// Проверяет создание блюда с автоматическим определением категории по маркеру в названии.
    /// </summary>
    /// <remarks>
    /// Маркеры вида "!салат", "!десерт", "!напиток" в названии блюда должны автоматически 
    /// устанавливать соответствующую категорию и удаляться из итогового названия.
    /// </remarks>
    [Theory(DisplayName = "КОГДА в названии блюда есть макрос, ТОГДА категория устанавливается автоматически")]
    [InlineData("Весенний !салат", DishCategory.Salad)]
    [InlineData("Шоколадный !десерт", DishCategory.Dessert)]
    [InlineData("Вода !напиток", DishCategory.Drink)]
    public async Task CreateDish_WithMacroInTitle_SetsCorrectCategory(string title, DishCategory expectedCategory)
    {
        var client = _fixture.Client;
        var dto = DishTestDataFactory.CreateValidDish(title);
        dto.Category = 0;

        var response = await client.PostAsJsonAsync(DishUrl, dto);
        var result = await response.Content.ReadFromJsonAsync<DishViewDto>(_jsonOptions);

        Assert.Equal(expectedCategory, result!.Category);
        Assert.DoesNotContain("!", result.Title);
    }

    /// <summary>
    /// Проверяет успешное создание блюд для всех поддерживаемых категорий.
    /// </summary>
    /// <remarks>
    /// Тест покрывает классы эквивалентности по полю Category: каждое значение перечисления 
    /// должно корректно обрабатываться при создании блюда.
    /// </remarks>
    [Theory(DisplayName = "КОГДА создается блюдо любой категории, ТОГДА оно успешно сохраняется")]
    [InlineData(DishCategory.FirstCourse)]
    [InlineData(DishCategory.SecondCourse)]
    [InlineData(DishCategory.Dessert)]
    [InlineData(DishCategory.Salad)]
    [InlineData(DishCategory.Soup)]
    [InlineData(DishCategory.Snack)]
    [InlineData(DishCategory.Drink)]
    public async Task CreateDish_AllCategories_ReturnsCreated(DishCategory category)
    {
        var client = _fixture.Client;
        var dto = DishTestDataFactory.CreateDishInCategory(category);

        var response = await client.PostAsJsonAsync(DishUrl, dto);
        var result = await response.Content.ReadFromJsonAsync<DishViewDto>(_jsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(category, result!.Category);
    }

    /// <summary>
    /// Проверяет фильтрацию противоречивых диетических флагов при создании блюда.
    /// </summary>
    /// <remarks>
    /// Если блюдо помечено как веганское, но содержит ингредиенты животного происхождения, 
    /// флаг Vegan должен быть автоматически удалён из результата.
    /// </remarks>
    [Fact(DisplayName = "КОГДА флаги блюда противоречат ингредиентам, ТОГДА некорректные флаги фильтруются")]
    public async Task CreateDish_WithConflictingFlags_FlagsAreFiltered()
    {
        var client = _fixture.Client;
        var chickenId = new Guid("a1000000-0000-0000-0000-000000000004");
        var dto = DishTestDataFactory.CreateValidDish("Fake Vegan Dish");
        dto.Flags = DietaryFlags.Vegan;
        dto.Ingredients = new List<DishIngredientDto> { new() { ProductId = chickenId, AmountInGrams = 100 } };

        var response = await client.PostAsJsonAsync(DishUrl, dto);
        var result = await response.Content.ReadFromJsonAsync<DishViewDto>(_jsonOptions);

        Assert.False(result!.Flags.HasFlag(DietaryFlags.Vegan));
    }

    #endregion





    #region Пограничные значения: валидные граничные случаи создания

    /// <summary>
    /// Проверяет создание блюд с пограничными, но валидными значениями параметров.
    /// </summary>
    /// <remarks>
    /// Тест покрывает пограничные случаи:
    /// - Минимальный размер порции
    /// - Максимальный размер порции  
    /// - Сумма БЖУ, равная ровно 100г на 100г продукта (верхняя граница)
    /// </remarks>
    [Theory(DisplayName = "КОГДА передаются валидные граничные значения, ТОГДА блюдо успешно создается")]
    [MemberData(nameof(GetValidBoundaryDishTestData))]
    public async Task CreateDish_ValidBoundaryValues_ReturnsCreated(DishCreateDto dto)
    {
        var client = _fixture.Client;
        var response = await client.PostAsJsonAsync(DishUrl, dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Источник данных для теста пограничных значений при создании блюд.
    /// </summary>
    public static IEnumerable<object[]> GetValidBoundaryDishTestData()
    {
        yield return new object[] { DishTestDataFactory.CreateSmallPortionDish() };
        yield return new object[] { DishTestDataFactory.CreateLargePortionDish() };
        yield return new object[] { DishTestDataFactory.CreateMacrosAtExactLimitDish() };
    }

    #endregion






    #region Классы эквивалентности: невалидные сценарии создания

    /// <summary>
    /// Проверяет возврат ошибки валидации при передаче некорректных данных.
    /// </summary>
    /// <remarks>
    /// Тест покрывает классы эквивалентности невалидного ввода:
    /// - Нулевой или отрицательный размер порции
    /// - Превышение лимита суммы БЖУ
    /// </remarks>
    [Theory(DisplayName = "КОГДА передаются некорректные данные, ТОГДА возвращается ошибка валидации")]
    [MemberData(nameof(GetInvalidDishTestData))]
    public async Task CreateDish_InvalidData_ReturnsBadRequest(DishCreateDto dto, string expectedError)
    {
        var client = _fixture.Client;

        var response = await client.PostAsJsonAsync(DishUrl, dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedError, error);
    }

    /// <summary>
    /// Источник данных для теста невалидного ввода.
    /// </summary>
    public static IEnumerable<object[]> GetInvalidDishTestData()
    {
        yield return new object[] { DishTestDataFactory.CreateZeroPortionDish(), "Portion size must be greater than 0" };
        yield return new object[] { DishTestDataFactory.CreateMacrosTooHighDish(), "Sum of proteins, fats, and carbohydrates per 100g cannot exceed 100g" };
    }

    #endregion






    #region Чтение и фильтрация: классы эквивалентности

    /// <summary>
    /// Проверяет корректность фильтрации блюд по категории при получении списка.
    /// </summary>
    /// <remarks>
    /// Запрос с параметром категории должен возвращать только блюда, соответствующие 
    /// указанному значению — класс эквивалентности по полю фильтрации.
    /// </remarks>
    [Fact(DisplayName = "КОГДА запрашивается список блюд с фильтрами, ТОГДА возвращаются только подходящие блюда")]
    public async Task GetDishes_WithFilters_ReturnsCorrectDishes()
    {
        var client = _fixture.Client;
        var url = $"{DishUrl}?category=Dessert";

        var response = await client.GetAsync(url);
        var result = await response.Content.ReadFromJsonAsync<List<DishViewDto>>(_jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(result!, d => Assert.Equal(DishCategory.Dessert, d.Category));
    }

    #endregion




    #region Обновление: валидные и невалидные сценарии

    /// <summary>
    /// Проверяет успешное обновление существующего блюда валидными данными.
    /// </summary>
    /// <remarks>
    /// Класс эквивалентности: обновление с корректными значениями всех полей 
    /// должно завершаться успешно и сохранять изменения.
    /// </remarks>
    [Fact(DisplayName = "КОГДА обновляются данные существующего блюда, ТОГДА изменения успешно сохраняются")]
    public async Task UpdateDish_ValidUpdate_StoredCorrectly()
    {
        var client = _fixture.Client;
        var createdDto = DishTestDataFactory.CreateValidDish("Before Update Dish");
        var createResponse = await client.PostAsJsonAsync(DishUrl, createdDto);
        var created = await createResponse.Content.ReadFromJsonAsync<DishViewDto>(_jsonOptions);

        var updateDto = new DishUpdateDto 
        { 
            Id = created!.Id, 
            Title = "After Update Dish", 
            PortionSize = 333,
            Category = DishCategory.Dessert 
        };

        var updateResponse = await client.PutAsJsonAsync($"{DishUrl}/{created.Id}", updateDto);
        var updated = await updateResponse.Content.ReadFromJsonAsync<DishViewDto>(_jsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("After Update Dish", updated!.Title);
        Assert.Equal(333, updated.PortionSize);
    }

    /// <summary>
    /// Проверяет отклонение обновления с невалидными данными (пограничное значение).
    /// </summary>
    /// <remarks>
    /// Пограничное значение: размер порции = 0 (нижняя граница, недопустимая).
    /// Система должна вернуть ошибку валидации.
    /// </remarks>
    [Fact(DisplayName = "КОГДА блюдо обновляется невалидными данными, ТОГДА возвращается ошибка")]
    public async Task UpdateDish_InvalidPortion_ReturnsBadRequest()
    {
        var client = _fixture.Client;
        var createdDto = DishTestDataFactory.CreateValidDish("To Update Invalid Dish");
        var createResponse = await client.PostAsJsonAsync(DishUrl, createdDto);
        var created = await createResponse.Content.ReadFromJsonAsync<DishViewDto>(_jsonOptions);

        var updateDto = new DishUpdateDto 
        { 
            Id = created!.Id, 
            Title = created.Title,
            PortionSize = 0
        };

        var response = await client.PutAsJsonAsync($"{DishUrl}/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion





    #region Удаление: позитивный сценарий

    /// <summary>
    /// Проверяет полный цикл удаления блюда: создание → удаление → проверка отсутствия.
    /// </summary>
    /// <remarks>
    /// Позитивный сценарий: удаление существующего блюда должно завершаться успешно, 
    /// а последующий запрос к удалённому ресурсу — возвращать 404.
    /// </remarks>
    [Fact(DisplayName = "КОГДА блюдо удаляется, ТОГДА оно больше не доступно в системе")]
    public async Task DeleteDish_Exists_RemovedSuccessfully()
    {
        var client = _fixture.Client;
        var createdDto = DishTestDataFactory.CreateValidDish("To Delete Dish");
        var createResponse = await client.PostAsJsonAsync(DishUrl, createdDto);
        var created = await createResponse.Content.ReadFromJsonAsync<DishViewDto>(_jsonOptions);

        var deleteResponse = await client.DeleteAsync($"{DishUrl}/{created!.Id}");
        var getResponse = await client.GetAsync($"{DishUrl}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

}
#endregion
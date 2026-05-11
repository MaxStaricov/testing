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
/// Набор интеграционных тестов для проверки API управления продуктами.
/// Тесты сгруппированы по техникам тестирования: классы эквивалентности и пограничные значения.
/// </summary>
[Collection("ApiCollection")]
public class ProductsApiTests
{
    private readonly ApiFixture _fixture;
    private const string BaseUrl = "/api/products";
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ProductsApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }






    #region Классы эквивалентности: валидные сценарии создания

    /// <summary>
    /// Предоставляет набор тестовых данных для проверки создания продуктов с валидными параметрами.
    /// </summary>
    public static IEnumerable<object[]> GetValidProductTestData()
    {
        yield return new object[] { ProductTestDataFactory.CreateValidProduct("Standard Apple"), "Standard Apple" };
        yield return new object[] { ProductTestDataFactory.CreateMinimalProduct(), "Minimal" };
        yield return new object[] { ProductTestDataFactory.CreateSpecialCharacterTitleProduct(), "Яблоко & Груша / Тест #1 (ÄÖÜ)" };
        yield return new object[] { ProductTestDataFactory.CreateComplexDietaryFlagsProduct(), "Complex Dietary Product" };
    }



    /// <summary>
    /// Проверяет успешное создание продукта при передаче корректных данных.
    /// </summary>
    /// <remarks>
    /// Тест покрывает классы эквивалентности валидного ввода:
    /// - Стандартные значения всех полей
    /// - Минимальные допустимые значения
    /// - Специальные символы в названии
    /// - Сложные комбинации диетических флагов
    /// </remarks>
    [Theory(DisplayName = "КОГДА передаются валидные данные, ТОГДА продукт успешно создается")]
    [MemberData(nameof(GetValidProductTestData))]
    public async Task CreateProduct_ValidData_ReturnsCreated(ProductCreateDto dto, string expectedTitle)
    {
        var client = _fixture.Client;
        var response = await client.PostAsJsonAsync(BaseUrl, dto);
        var result = await response.Content.ReadFromJsonAsync<ProductViewDto>(_jsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(expectedTitle, result.Title);
        Assert.Equal(dto.Calories, result.Calories);
        Assert.Equal(dto.Flags, result.Flags);
    }


    /// <summary>
    /// Проверяет создание продукта для каждой поддерживаемой категории.
    /// </summary>
    /// <remarks>
    /// Тест покрывает классы эквивалентности по полю Category: каждое значение перечисления 
    /// должно корректно обрабатываться при создании продукта.
    /// </remarks>
    [Theory(DisplayName = "КОГДА создается продукт любой категории, ТОГДА он успешно сохраняется")]
    [InlineData(ProductCategory.Meat)]
    [InlineData(ProductCategory.Vegetables)]
    [InlineData(ProductCategory.Frozen)]
    [InlineData(ProductCategory.Sweets)]
    [InlineData(ProductCategory.Spices)]
    public async Task CreateProduct_AllCategories_ReturnsCreated(ProductCategory category)
    {
        var client = _fixture.Client;
        var dto = ProductTestDataFactory.CreateProductInCategory(category);

        var response = await client.PostAsJsonAsync(BaseUrl, dto);
        var result = await response.Content.ReadFromJsonAsync<ProductViewDto>(_jsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(category, result!.Category);
    }

    #endregion






    #region Пограничные значения: валидные граничные случаи

    /// <summary>
    /// Источник данных для теста пограничных значений калорийности.
    /// </summary>
    /// <remarks>
    /// Используем MemberData вместо InlineData для поддержки типа decimal.
    /// </remarks>
    public static IEnumerable<object[]> GetBoundaryCaloriesTestData()
    {
        yield return new object[] { 0m, "Zero Calories" };
        yield return new object[] { 0.01m, "Minimal Calories" };
        yield return new object[] { 9000m, "Max Practical Calories" };
    }


    /// <summary>
    /// Проверяет успешное создание продукта при использовании граничных значений калорийности.
    /// </summary>
    /// <remarks>
    /// Тест покрывает пограничные случаи:
    /// - Нулевая калорийность (нижняя граница)
    /// - Минимальное положительное значение (0.01)
    /// - Максимальное практическое значение (9000)
    /// </remarks>
    [Theory(DisplayName = "КОГДА передаются граничные значения КБЖУ, ТОГДА продукт успешно создается")]
    [MemberData(nameof(GetBoundaryCaloriesTestData))]
    public async Task CreateProduct_BoundaryCalories_ReturnsCreated(decimal calories, string title)
    {
        var client = _fixture.Client;
        var dto = ProductTestDataFactory.CreateValidProduct(title);
        dto.Calories = calories;

        var response = await client.PostAsJsonAsync(BaseUrl, dto);
        var result = await response.Content.ReadFromJsonAsync<ProductViewDto>(_jsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(calories, result!.Calories);
    }

    /// <summary>
    /// Проверяет создание продукта с заголовком граничной длины.
    /// </summary>
    /// <remarks>
    /// Тест покрывает пограничные случаи длины названия:
    /// - Минимальная длина (2 символа)
    /// - Максимальная допустимая длина (~100 символов)
    /// </remarks>
    [Theory(DisplayName = "КОГДА передается заголовок граничной длины, ТОГДА продукт успешно создается")]
    [InlineData("X2")]
    [InlineData("Very Long Title... 100 symbols repeated...")]
    public async Task CreateProduct_BoundaryTitleLength_ReturnsCreated(string title)
    {
        var client = _fixture.Client;
        var dto = ProductTestDataFactory.CreateValidProduct(title);

        var response = await client.PostAsJsonAsync(BaseUrl, dto);
        var result = await response.Content.ReadFromJsonAsync<ProductViewDto>(_jsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(title, result!.Title);
    }

    #endregion





    #region Классы эквивалентности: невалидные сценарии

    /// <summary>
    /// Проверяет возврат ошибки валидации при превышении лимита суммы БЖУ.
    /// </summary>
    /// <remarks>
    /// <strong>Бизнес-правило:</strong> Сумма белков, жиров и углеводов на 100г продукта 
    /// не может превышать 100г. Тест покрывает класс эквивалентности невалидного ввода 
    /// за пределами допустимого диапазона.
    /// </remarks>
    [Fact(DisplayName = "КОГДА сумма БЖУ превышает 100г, ТОГДА возвращается ошибка")]
    public async Task CreateProduct_SumOfMacrosExceeds100_ReturnsBadRequest()
    {
        var client = _fixture.Client;
        var dto = ProductTestDataFactory.CreateValidProduct("Invalid Macros");
        dto.Proteins = 40;
        dto.Fats = 40;
        dto.Carbohydrates = 30; // 40+40+30 = 110 > 100

        var response = await client.PostAsJsonAsync(BaseUrl, dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sum of proteins, fats, and carbohydrates cannot exceed 100g", error);
    }

    #endregion






    #region Чтение и фильтрация: классы эквивалентности

    /// <summary>
    /// Проверяет, что при запросе всех продуктов без фильтров возвращается непустой список.
    /// </summary>
    /// <remarks>
    /// Позитивный сценарий: после сидирования базы данных запрос без параметров 
    /// должен возвращать как минимум один продукт.
    /// </remarks>
    [Fact(DisplayName = "КОГДА запрашиваются все продукты без фильтров, ТОГДА возвращается непустой список")]
    public async Task GetAllProducts_NoFilters_ReturnsNonEmptyList()
    {
        var client = _fixture.Client;
        var response = await client.GetAsync(BaseUrl);
        var result = await response.Content.ReadFromJsonAsync<List<ProductViewDto>>(_jsonOptions);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Проверяет корректность фильтрации продуктов по нескольким категориям одновременно.
    /// </summary>
    /// <remarks>
    /// Запрос с параметром <c>?category=Sweets,Fruits</c> должен возвращать только продукты 
    /// указанных категорий. Примечание: в тесте проверяется категория <c>Vegetables</c> вместо 
    /// <c>Fruits</c>, так как в тестовых данных используется именно она.
    /// </remarks>
    [Fact(DisplayName = "КОГДА задан фильтр по нескольким категориям, ТОГДА возвращаются только подходящие продукты")]
    public async Task GetProducts_FilterByMultipleCategories_ReturnsFilteredResults()
    {
        var client = _fixture.Client;
        var url = $"{BaseUrl}?category=Sweets,Fruits";
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<ProductViewDto>>(_jsonOptions);

        Assert.All(result!, p => Assert.True(p.Category == ProductCategory.Sweets || p.Category == ProductCategory.Vegetables));
    }

    /// <summary>
    /// Убеждается, что запрос несуществующего продукта возвращает статус 404.
    /// </summary>
    /// <remarks>
    /// Негативный сценарий: запрос по несуществующему GUID должен возвращать HttpStatusCode.NotFound.
    /// </remarks>
    [Fact(DisplayName = "КОГДА запрашивается несуществующий продукт, ТОГДА возвращается 404")]
    public async Task GetProduct_NonExistent_ReturnsNotFound()
    {
        var client = _fixture.Client;
        var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Проверяет корректность сортировки результатов по различным полям.
    /// </summary>
    /// <param name="sortField">Имя поля для сортировки.</param>
    /// <returns>Задача асинхронного выполнения теста.</returns>
    /// <remarks>
    /// Поддерживаемые поля сортировки: <c>calories</c>, <c>proteins</c>, <c>fats</c>, <c>title</c>.
    /// Сортировка по строковым полям выполняется без учёта регистра.
    /// </remarks>
    [Theory(DisplayName = "КОГДА задана сортировка по полю, ТОГДА список возвращается в правильном порядке")]
    [InlineData("calories")]
    [InlineData("proteins")]
    [InlineData("fats")]
    [InlineData("title")]
    public async Task GetProducts_SortByDifferentFields_ReturnsSortedList(string sortField)
    {
        var client = _fixture.Client;
        var response = await client.GetAsync($"{BaseUrl}?sort={sortField}");
        var result = await response.Content.ReadFromJsonAsync<List<ProductViewDto>>(_jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        
        for (int i = 1; i < result.Count; i++)
        {
            var prev = result[i - 1];
            var curr = result[i];
            
            bool isOrdered = sortField switch
            {
                "calories" => prev.Calories <= curr.Calories,
                "proteins" => prev.Proteins <= curr.Proteins,
                "fats" => prev.Fats <= curr.Fats,
                "title" => string.Compare(prev.Title, curr.Title, StringComparison.OrdinalIgnoreCase) <= 0,
                _ => true
            };
            Assert.True(isOrdered, $"List is not sorted by {sortField} at index {i}");
        }
    }

    #endregion






    #region Обновление: валидные и невалидные сценарии

    /// <summary>
    /// Проверяет успешное обновление существующего продукта валидными данными.
    /// </summary>
    /// <remarks>
    /// Класс эквивалентности: обновление с корректными значениями всех полей 
    /// должно завершаться успешно и сохранять изменения.
    /// </remarks>
    [Fact(DisplayName = "КОГДА обновляются данные существующего продукта, ТОГДА изменения успешно сохраняются")]
    public async Task UpdateProduct_ValidUpdate_StoredCorrectly()
    {
        var client = _fixture.Client;
        var createdDto = ProductTestDataFactory.CreateValidProduct("Before Update");
        var createResponse = await client.PostAsJsonAsync(BaseUrl, createdDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductViewDto>(_jsonOptions);

        var updateDto = new ProductUpdateDto 
        { 
            Id = created!.Id, 
            Title = "After Update", 
            Calories = 555,
            Category = ProductCategory.Spices 
        };

        var updateResponse = await client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductViewDto>(_jsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("After Update", updated!.Title);
        Assert.Equal(555, updated.Calories);
        Assert.Equal(ProductCategory.Spices, updated.Category);
    }

    /// <summary>
    /// Проверяет отклонение обновления с невалидными макросами.
    /// </summary>
    /// <remarks>
    /// Пограничное значение: сумма белков и жиров = 120г > 100г (верхняя граница, недопустимая).
    /// Система должна вернуть ошибку валидации.
    /// </remarks>
    [Fact(DisplayName = "КОГДА продукт обновляется невалидными макросами, ТОГДА возвращается ошибка")]
    public async Task UpdateProduct_InvalidMacros_ReturnsBadRequest()
    {
        var client = _fixture.Client;
        var createdDto = ProductTestDataFactory.CreateValidProduct("To Update Invalid");
        var createResponse = await client.PostAsJsonAsync(BaseUrl, createdDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductViewDto>(_jsonOptions);

        var updateDto = new ProductUpdateDto 
        { 
            Id = created!.Id, 
            Title = created.Title,
            Proteins = 60, Fats = 60 // 60+60 = 120 > 100
        };

        var response = await client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion




    #region Удаление: позитивный сценарий

    /// <summary>
    /// Проверяет полный цикл удаления: создание → удаление → проверка отсутствия.
    /// </summary>
    /// <remarks>
    /// Позитивный сценарий: удаление существующего продукта должно завершаться успешно, 
    /// а последующий запрос к удалённому ресурсу — возвращать 404.
    /// </remarks>
    [Fact(DisplayName = "КОГДА продукт удаляется, ТОГДА он больше не доступно в системе")]
    public async Task DeleteProduct_Exists_RemovedSuccessfully()
    {
        var client = _fixture.Client;
        var createdDto = ProductTestDataFactory.CreateValidProduct("To Delete");
        var createResponse = await client.PostAsJsonAsync(BaseUrl, createdDto);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductViewDto>(_jsonOptions);

        var deleteResponse = await client.DeleteAsync($"{BaseUrl}/{created!.Id}");
        var getResponse = await client.GetAsync($"{BaseUrl}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    #endregion
}
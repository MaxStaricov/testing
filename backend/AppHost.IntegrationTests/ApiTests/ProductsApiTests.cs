using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.DTOs;
using Domain.Models;
using TestModule.Backend.IntegrationTests.Fixtures;
using TestModule.Backend.IntegrationTests.TestData;
using Xunit;

namespace TestModule.Backend.IntegrationTests.ApiTests;

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

    [Theory(DisplayName = "КОГДА передаются граничные значения КБЖУ, ТОГДА продукт успешно создается")]
    [InlineData(0, "Zero Calories")]
    [InlineData(0.01, "Minimal Calories")]
    [InlineData(9000, "Max Practical Calories")]
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

    [Fact(DisplayName = "КОГДА сумма БЖУ превышает 100г, ТОГДА возвращается ошибка")]
    public async Task CreateProduct_SumOfMacrosExceeds100_ReturnsBadRequest()
    {
        var client = _fixture.Client;
        var dto = ProductTestDataFactory.CreateValidProduct("Invalid Macros");
        dto.Proteins = 40;
        dto.Fats = 40;
        dto.Carbohydrates = 30;

        var response = await client.PostAsJsonAsync(BaseUrl, dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sum of proteins, fats, and carbohydrates cannot exceed 100g", error);
    }

    public static IEnumerable<object[]> GetValidProductTestData()
    {
        yield return new object[] { ProductTestDataFactory.CreateValidProduct("Standard Apple"), "Standard Apple" };
        yield return new object[] { ProductTestDataFactory.CreateMinimalProduct(), "Minimal" };
        yield return new object[] { ProductTestDataFactory.CreateSpecialCharacterTitleProduct(), "Яблоко & Груша / Тест #1 (ÄÖÜ)" };
        yield return new object[] { ProductTestDataFactory.CreateComplexDietaryFlagsProduct(), "Complex Dietary Product" };
    }

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

    [Fact(DisplayName = "КОГДА запрашивается несуществующий продукт, ТОГДА возвращается 404")]
    public async Task GetProduct_NonExistent_ReturnsNotFound()
    {
        var client = _fixture.Client;
        var response = await client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

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
            Proteins = 60, Fats = 60
        };

        var response = await client.PutAsJsonAsync($"{BaseUrl}/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

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
}
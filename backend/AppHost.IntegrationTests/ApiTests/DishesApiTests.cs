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
public class DishesApiTests
{
    private readonly ApiFixture _fixture;
    private const string DishUrl = "/api/dishes";
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public DishesApiTests(ApiFixture fixture)
    {
        _fixture = fixture;
    }

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

    [Theory(DisplayName = "КОГДА передаются валидные граничные значения, ТОГДА блюдо успешно создается")]
    [MemberData(nameof(GetValidBoundaryDishTestData))]
    public async Task CreateDish_ValidBoundaryValues_ReturnsCreated(DishCreateDto dto)
    {
        var client = _fixture.Client;
        var response = await client.PostAsJsonAsync(DishUrl, dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    public static IEnumerable<object[]> GetValidBoundaryDishTestData()
    {
        yield return new object[] { DishTestDataFactory.CreateSmallPortionDish() };
        yield return new object[] { DishTestDataFactory.CreateLargePortionDish() };
        yield return new object[] { DishTestDataFactory.CreateMacrosAtExactLimitDish() };
    }

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

    public static IEnumerable<object[]> GetInvalidDishTestData()
    {
        yield return new object[] { DishTestDataFactory.CreateZeroPortionDish(), "Portion size must be greater than 0" };
        yield return new object[] { DishTestDataFactory.CreateMacrosTooHighDish(), "Sum of proteins, fats, and carbohydrates per 100g cannot exceed 100g" };
    }

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

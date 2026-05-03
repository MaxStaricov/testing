using System;
using System.Collections.Generic;

namespace Domain.Models;

public class Product
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public List<byte[]> Photos { get; set; } = new();
    public decimal Calories { get; set; }
    public decimal Proteins { get; set; }
    public decimal Fats { get; set; }
    public decimal Carbohydrates { get; set; }
    public string? Description { get; set; }
    public ProductCategory Category { get; set; }
    public CookingNecessity Necessity { get; set; }
    public DietaryFlags Flags { get; set; }
    public DateTime? DateCreated { get; set; }
    public DateTime? DateModified { get; set; }
}

public class ProductBuilder
{
    private readonly Product _product = new();

    public ProductBuilder WithId(Guid id) { _product.Id = id; return this; }
    public ProductBuilder WithTitle(string title) { _product.Title = title; return this; }
    public ProductBuilder WithPhotos(List<byte[]> photos) { _product.Photos = photos ?? new List<byte[]>(); return this; }
    public ProductBuilder WithCalories(decimal calories) { _product.Calories = calories; return this; }
    public ProductBuilder WithProteins(decimal proteins) { _product.Proteins = proteins; return this; }
    public ProductBuilder WithFats(decimal fats) { _product.Fats = fats; return this; }
    public ProductBuilder WithCarbohydrates(decimal carbohydrates) { _product.Carbohydrates = carbohydrates; return this; }
    public ProductBuilder WithDescription(string? description) { _product.Description = description; return this; }
    public ProductBuilder WithCategory(ProductCategory category) { _product.Category = category; return this; }
    public ProductBuilder WithNecessity(CookingNecessity necessity) { _product.Necessity = necessity; return this; }
    public ProductBuilder WithFlags(DietaryFlags flags) { _product.Flags = flags; return this; }
    public ProductBuilder WithDateCreated(DateTime? dateCreated) { _product.DateCreated = dateCreated; return this; }
    public ProductBuilder WithDateModified(DateTime? dateModified) { _product.DateModified = dateModified; return this; }

    public Product Build() => _product;
}

using System;
using System.Linq;
using DB.ModelsDb;
using Domain.Models;

namespace Infrastructure.Mappers;

public static class ProductMapper
{
    public static ProductDb ToDb(this Product domain)
    {
        return new ProductDb
        {
            Id = domain.Id,
            Title = domain.Title,
            Calories = domain.Calories,
            Proteins = domain.Proteins,
            Fats = domain.Fats,
            Carbohydrates = domain.Carbohydrates,
            Description = domain.Description,
            Category = (int)domain.Category,
            Necessity = (int)domain.Necessity,
            Flags = (int)domain.Flags,
            DateCreated = domain.DateCreated,
            DateModified = domain.DateModified
        };
    }

    public static Product ToDomain(this ProductDb db, System.Collections.Generic.List<ProductPhotoDb> photos = null)
    {
        var builder = new ProductBuilder()
            .WithId(db.Id)
            .WithTitle(db.Title)
            .WithCalories(db.Calories)
            .WithProteins(db.Proteins)
            .WithFats(db.Fats)
            .WithCarbohydrates(db.Carbohydrates)
            .WithDescription(db.Description)
            .WithCategory((ProductCategory)db.Category)
            .WithNecessity((CookingNecessity)db.Necessity)
            .WithFlags((DietaryFlags)db.Flags)
            .WithDateCreated(db.DateCreated)
            .WithDateModified(db.DateModified);

        if (photos != null && photos.Any())
        {
            builder.WithPhotos(photos.Select(p => p.Content).ToList());
        }

        return builder.Build();
    }
}

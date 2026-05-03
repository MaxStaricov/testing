using System.Linq;
using DB.ModelsDb;
using Domain.Models;
using System.Collections.Generic;

namespace Infrastructure.Mappers;

public static class DishMapper
{
    public static DishesDb ToDb(this Dish domain)
    {
        return new DishesDb
        {
            Id = domain.Id,
            Title = domain.Title,
            PortionSize = domain.PortionSize,
            Calories = domain.Calories,
            Proteins = domain.Proteins,
            Fats = domain.Fats,
            Carbohydrates = domain.Carbohydrates,
            Category = (int)domain.Category,
            Flags = (int)domain.Flags,
            DateCreated = domain.DateCreated,
            DateModified = domain.DateModified
        };
    }

    public static Dish ToDomain(this DishesDb db, List<DishProductItem> ingredients, List<ProductPhotoDb> photos = null)
    {
        var builder = new DishBuilder()
            .WithId(db.Id)
            .WithTitle(db.Title)
            .WithPortionSize(db.PortionSize)
            .WithCalories(db.Calories)
            .WithProteins(db.Proteins)
            .WithFats(db.Fats)
            .WithCarbohydrates(db.Carbohydrates)
            .WithCategory((DishCategory)db.Category)
            .WithFlags((DietaryFlags)db.Flags)
            .WithIngredients(ingredients)
            .WithDateCreated(db.DateCreated)
            .WithDateModified(db.DateModified);

        if (photos != null && photos.Any())
        {
            builder.WithPhotos(photos.Select(p => p.Content).ToList());
        }

        return builder.Build();
    }
}

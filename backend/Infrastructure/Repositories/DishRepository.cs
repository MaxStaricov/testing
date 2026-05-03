using DB;
using DB.ModelsDb;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Mappers;
using LinqToDB;
using LinqToDB.Async;

namespace Infrastructure.Repositories;

public class DishRepository : IDishRepository
{
    private readonly AppDbContext _db;
    private readonly IProductRepository _productRepository;

    public DishRepository(AppDbContext db, IProductRepository productRepository)
    {
        _db = db;
        _productRepository = productRepository;
    }

    public async Task<Dish> CreateAsync(Dish dish)
    {
        dish.Id = Guid.NewGuid();
        dish.DateCreated = DateTime.UtcNow;

        var dbEntity = dish.ToDb();
        await _db.InsertAsync(dbEntity);

        if (dish.Photos != null && dish.Photos.Any())
        {
            foreach (var photoContent in dish.Photos)
            {
                var pBase = new ProductPhotoDb
                {
                    Id = Guid.NewGuid(),
                    Content = photoContent,
                    DishId = dish.Id
                };
                await _db.InsertAsync(pBase);
            }
        }

        if (dish.Ingredients != null && dish.Ingredients.Any())
        {
            foreach (var ing in dish.Ingredients)
            {
                var dp = new DishProductDb
                {
                    DishId = dish.Id,
                    ProductId = ing.ProductId,
                    AmountInGrams = ing.AmountInGrams
                };
                await _db.InsertAsync(dp);
            }
        }

        return dish;
    }

    public async Task<Dish?> GetByIdAsync(Guid id)
    {
        var dbEntity = await _db.Dishes.FirstOrDefaultAsync(d => d.Id == id);
        if (dbEntity == null) return null;

        var dbPhotos = await _db.Photos.Where(p => p.DishId == id).ToListAsync();
        var dbIngredients = await _db.DishProducts.Where(dp => dp.DishId == id).ToListAsync();
        
        var ingredients = new List<DishProductItem>();
        foreach (var dbIng in dbIngredients)
        {
            var product = await _productRepository.GetByIdAsync(dbIng.ProductId);
            if (product != null)
            {
                ingredients.Add(new DishProductItem
                {
                    ProductId = dbIng.ProductId,
                    AmountInGrams = dbIng.AmountInGrams,
                    Product = product
                });
            }
        }

        return dbEntity.ToDomain(ingredients, dbPhotos);
    }

    public async Task<List<Dish>> GetAllAsync()
    {
        var dishesDb = await _db.Dishes.ToListAsync();
        var photosDb = await _db.Photos.Where(p => p.DishId != null).ToListAsync();
        var ingredientsDb = await _db.DishProducts.ToListAsync();
        
        var allProducts = await _productRepository.GetAllAsync();
        var productsDict = allProducts.ToDictionary(p => p.Id);

        var result = new List<Dish>();
        foreach (var dDb in dishesDb)
        {
            var dPhotos = photosDb.Where(p => p.DishId == dDb.Id).ToList();
            var dIngDb = ingredientsDb.Where(dp => dp.DishId == dDb.Id).ToList();
            
            var dIngredients = new List<DishProductItem>();
            foreach (var ingDb in dIngDb)
            {
                if (productsDict.TryGetValue(ingDb.ProductId, out var product))
                {
                    dIngredients.Add(new DishProductItem
                    {
                        ProductId = ingDb.ProductId,
                        AmountInGrams = ingDb.AmountInGrams,
                        Product = product
                    });
                }
            }
            
            result.Add(dDb.ToDomain(dIngredients, dPhotos));
        }

        return result;
    }

    public async Task UpdateAsync(Dish dish)
    {
        dish.DateModified = DateTime.UtcNow;
        var dbEntity = dish.ToDb();
        await _db.UpdateAsync(dbEntity);

        await _db.Photos.Where(p => p.DishId == dish.Id).DeleteAsync();
        if (dish.Photos != null && dish.Photos.Any())
        {
            foreach (var photoContent in dish.Photos)
            {
                var pBase = new ProductPhotoDb
                {
                    Id = Guid.NewGuid(),
                    Content = photoContent,
                    DishId = dish.Id
                };
                await _db.InsertAsync(pBase);
            }
        }

        await _db.DishProducts.Where(dp => dp.DishId == dish.Id).DeleteAsync();
        if (dish.Ingredients != null && dish.Ingredients.Any())
        {
            foreach (var ing in dish.Ingredients)
            {
                var dp = new DishProductDb
                {
                    DishId = dish.Id,
                    ProductId = ing.ProductId,
                    AmountInGrams = ing.AmountInGrams
                };
                await _db.InsertAsync(dp);
            }
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        await _db.Photos.Where(p => p.DishId == id).DeleteAsync();
        await _db.DishProducts.Where(dp => dp.DishId == id).DeleteAsync();
        await _db.Dishes.Where(d => d.Id == id).DeleteAsync();
    }

    public async Task<List<Guid>> GetDishIdsByProductIdAsync(Guid productId)
    {
        return await _db.DishProducts
            .Where(dp => dp.ProductId == productId)
            .Select(dp => dp.DishId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<string>> GetDishesUsingProductNamesAsync(Guid productId)
    {
        return await _db.DishProducts
            .Where(dp => dp.ProductId == productId)
            .Join(_db.Dishes, dp => dp.DishId, d => d.Id, (dp, d) => d.Title)
            .Distinct()
            .ToListAsync();
    }
}

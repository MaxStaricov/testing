using DB;
using DB.ModelsDb;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.Mappers;
using LinqToDB;
using LinqToDB.Async;

namespace Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Product> CreateAsync(Product product)
    {
        product.Id = Guid.NewGuid();
        product.DateCreated = DateTime.UtcNow;

        var dbEntity = product.ToDb();
        await _db.InsertAsync(dbEntity);

        if (product.Photos != null && product.Photos.Any())
        {
            foreach (var photoContent in product.Photos)
            {
                var pBase = new ProductPhotoDb
                {
                    Id = Guid.NewGuid(),
                    Content = photoContent,
                    ProductId = product.Id
                };
                await _db.InsertAsync(pBase);
            }
        }

        return product;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        var dbEntity = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (dbEntity == null) return null;

        var photos = await _db.Photos.Where(p => p.ProductId == id).ToListAsync();
        return dbEntity.ToDomain(photos);
    }

    public async Task<List<Product>> GetAllAsync()
    {
        var productsDb = await _db.Products.ToListAsync();
        var photosDb = await _db.Photos.Where(p => p.ProductId != null).ToListAsync();

        var result = new List<Product>();
        foreach (var p in productsDb)
        {
            var pPhotos = photosDb.Where(ph => ph.ProductId == p.Id).ToList();
            result.Add(p.ToDomain(pPhotos));
        }
        return result;
    }

    public async Task UpdateAsync(Product product)
    {
        product.DateModified = DateTime.UtcNow;
        var dbEntity = product.ToDb();
        await _db.UpdateAsync(dbEntity);

        await _db.Photos.Where(p => p.ProductId == product.Id).DeleteAsync();
        
        if (product.Photos != null && product.Photos.Any())
        {
            foreach (var photoContent in product.Photos)
            {
                var pBase = new ProductPhotoDb
                {
                    Id = Guid.NewGuid(),
                    Content = photoContent,
                    ProductId = product.Id
                };
                await _db.InsertAsync(pBase);
            }
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        await _db.Photos.Where(p => p.ProductId == id).DeleteAsync();
        await _db.Products.Where(p => p.Id == id).DeleteAsync();
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

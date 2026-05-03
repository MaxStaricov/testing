using DB.ModelsDb;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Configuration;

namespace DB;

public class AppDbContext : DataConnection
{
    public AppDbContext(DataOptions<AppDbContext> options) : base(options.Options) { }

    public ITable<ProductDb> Products => this.GetTable<ProductDb>();
    public ITable<DishesDb> Dishes => this.GetTable<DishesDb>();
    public ITable<ProductPhotoDb> Photos => this.GetTable<ProductPhotoDb>();
    public ITable<DishProductDb> DishProducts => this.GetTable<DishProductDb>();
}

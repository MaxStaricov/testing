using LinqToDB.Mapping;

namespace DB.ModelsDb;

[Table("DishProducts")]
public class DishProductDb
{
    [Column("DishId"), NotNull]
    public Guid DishId { get; set; }
    
    [Column("ProductId"), NotNull]
    public Guid ProductId { get; set; }
    
    [Column("AmountInGrams"), NotNull]
    public decimal AmountInGrams { get; set; }
}

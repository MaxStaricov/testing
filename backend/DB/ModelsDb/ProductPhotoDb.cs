using LinqToDB.Mapping;

namespace DB.ModelsDb;

[Table("ProductPhotos")]
public class ProductPhotoDb
{
    [PrimaryKey, Column("Id")]
    public Guid Id { get; set; }
    
    [Column("Content"), NotNull]
    public byte[] Content { get; set; } = Array.Empty<byte>();
    
    [Column("ProductId")]
    public Guid? ProductId { get; set; }
    
    [Column("DishId")]
    public Guid? DishId { get; set; }
}
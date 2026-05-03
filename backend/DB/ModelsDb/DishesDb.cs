using Domain.Models;
using LinqToDB.Mapping;

namespace DB.ModelsDb;

[Table("Dishes")]
public class DishesDb
{
    [PrimaryKey, Column("Id")]
    public Guid Id { get; set; }
    
    [Column("Title"), NotNull]
    public string Title { get; set; } = string.Empty;
    
    [Column("PortionSize"), NotNull]
    public decimal PortionSize { get; set; }
    
    [Column("Calories"), NotNull]
    public decimal Calories { get; set; }
    
    [Column("Proteins"), NotNull]
    public decimal Proteins { get; set; }
    
    [Column("Fats"), NotNull]
    public decimal Fats { get; set; }
    
    [Column("Carbohydrates"), NotNull]
    public decimal Carbohydrates { get; set; }
    
    [Column("Category"), NotNull]
    public int Category { get; set; }
    
    [Column("Flags"), NotNull]
    public int Flags { get; set; }
    
    [Column("DateCreated")]
    public DateTime? DateCreated { get; set; }
    
    [Column("DateModified")]
    public DateTime? DateModified { get; set; }
}
using System.ComponentModel;

namespace Domain.Models;

public enum ProductCategory
{
    Frozen, Meat, Vegetables, Greens, Spices, Cereals, Canned, Liquid, Sweets
}

public enum CookingNecessity
{
    ReadyToEat, SemiFinished, RequiresCooking
}

public enum DishCategory
{
    [Description("Десерт")]
    Dessert,
    [Description("Первое")]
    FirstCourse,
    [Description("Второе")]
    SecondCourse,
    [Description("Напиток")]
    Drink,
    [Description("Салат")]
    Salad,
    [Description("Суп")]
    Soup,
    [Description("Перекус")]
    Snack
}

[Flags]
public enum DietaryFlags
{
    None = 0,
    Vegan = 1 << 0,
    GlutenFree = 1 << 1,
    SugarFree = 1 << 2
}

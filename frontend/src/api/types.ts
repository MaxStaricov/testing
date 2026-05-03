
export const CookingNecessity = {
  ReadyToEat: 0,
  SemiFinished: 1,
  RequiresCooking: 2,
} as const;
export type CookingNecessity = (typeof CookingNecessity)[keyof typeof CookingNecessity];

export const DietaryFlags = {
  None: 0,
  Vegan: 1,
  GlutenFree: 2,
  SugarFree: 4,
} as const;
export type DietaryFlags = (typeof DietaryFlags)[keyof typeof DietaryFlags];

export const DishCategory = {
  Dessert: 0,
  FirstCourse: 1,
  SecondCourse: 2,
  Drink: 3,
  Salad: 4,
  Soup: 5,
  Snack: 6,
} as const;
export type DishCategory = (typeof DishCategory)[keyof typeof DishCategory];

export const ProductCategory = {
  Frozen: 0,
  Meat: 1,
  Vegetables: 2,
  Greens: 3,
  Spices: 4,
  Cereals: 5,
  Canned: 6,
  Liquid: 7,
  Sweets: 8,
} as const;
export type ProductCategory = (typeof ProductCategory)[keyof typeof ProductCategory];

export const ProductCategoryLabels: Record<ProductCategory, string> = {
  [ProductCategory.Frozen]: 'Замороженный',
  [ProductCategory.Meat]: 'Мясной',
  [ProductCategory.Vegetables]: 'Овощи',
  [ProductCategory.Greens]: 'Зелень',
  [ProductCategory.Spices]: 'Специи',
  [ProductCategory.Cereals]: 'Крупы',
  [ProductCategory.Canned]: 'Консервы',
  [ProductCategory.Liquid]: 'Жидкость',
  [ProductCategory.Sweets]: 'Сладости'
};

export const DishCategoryLabels: Record<DishCategory, string> = {
  [DishCategory.Dessert]: 'Десерт',
  [DishCategory.FirstCourse]: 'Первое',
  [DishCategory.SecondCourse]: 'Второе',
  [DishCategory.Drink]: 'Напиток',
  [DishCategory.Salad]: 'Салат',
  [DishCategory.Soup]: 'Суп',
  [DishCategory.Snack]: 'Перекус'
};

export interface DishIngredientDto {
  productId: string;
  amountInGrams: number;
}

export interface DishCreateDto {
  title?: string | null;
  photos?: string[] | null;
  portionSize: number;
  category: DishCategory;
  ingredients?: DishIngredientDto[] | null;
  calories: number;
  proteins: number;
  fats: number;
  carbohydrates: number;
  flags: DietaryFlags;
}

export interface DishUpdateDto extends DishCreateDto {
  id: string;
}

export interface ProductCreateDto {
  title?: string | null;
  photos?: string[] | null;
  calories: number;
  proteins: number;
  fats: number;
  carbohydrates: number;
  description?: string | null;
  category: ProductCategory;
  necessity: CookingNecessity;
  flags: DietaryFlags;
}

export interface ProductUpdateDto extends ProductCreateDto {
  id: string;
}

export interface ProductDto extends ProductUpdateDto {
  dateCreated?: string;
  dateModified?: string;
}

export interface DishDto extends DishUpdateDto {
  dateCreated?: string;
  dateModified?: string;
}


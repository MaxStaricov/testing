INSERT INTO "Products" ("Id","Title","Calories","Proteins","Fats","Carbohydrates","Description","Category","Necessity","Flags","DateCreated")
VALUES
    ('a1000000-0000-0000-0000-000000000001',
     'Seed Шоколадный торт', 420, 6, 22, 55, 'Seed product – sweets category', 8, 0, 0, NOW()),

    ('a1000000-0000-0000-0000-000000000002',
     'Seed Веганский овощной микс', 80, 3, 2, 14, 'Seed product – vegan flag', 2, 0, 1, NOW()),

    ('a1000000-0000-0000-0000-000000000003',
     'Seed Минеральная вода', 0, 0, 0, 0, 'Seed product – ready to eat', 7, 0, 0, NOW()),

    ('a1000000-0000-0000-0000-000000000004',
     'Seed Куриная грудка', 165, 31, 3, 0, 'Seed product – requires cooking', 1, 2, 0, NOW()),

    ('a1000000-0000-0000-0000-000000000005',
     'Seed Macro Source Product', 100, 10, 5, 15, 'Known macros: 100kcal per 100g', 2, 0, 0, NOW())

ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Dishes" ("Id","Title","PortionSize","Calories","Proteins","Fats","Carbohydrates","Category","Flags","DateCreated")
VALUES
    ('b1000000-0000-0000-0000-000000000001',
     'Seed Тирамису', 150, 350, 7, 18, 40, 0, 0, NOW()),

    ('b1000000-0000-0000-0000-000000000002',
     'Seed Веганский салат', 200, 120, 4, 5, 18, 4, 1, NOW()),

    ('b1000000-0000-0000-0000-000000000003',
     'Seed Борщ', 300, 180, 9, 6, 22, 5, 0, NOW())

ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "DishProducts" ("DishId","ProductId","AmountInGrams")
VALUES
    ('b1000000-0000-0000-0000-000000000002',
     'a1000000-0000-0000-0000-000000000002', 100),

    ('b1000000-0000-0000-0000-000000000003',
     'a1000000-0000-0000-0000-000000000004', 200)

ON CONFLICT DO NOTHING;

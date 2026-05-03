CREATE TABLE IF NOT EXISTS "Products" (
    "Id" UUID PRIMARY KEY,
    "Title" VARCHAR(255) NOT NULL,
    "Calories" DECIMAL NOT NULL,
    "Proteins" DECIMAL NOT NULL,
    "Fats" DECIMAL NOT NULL,
    "Carbohydrates" DECIMAL NOT NULL,
    "Description" TEXT,
    "Category" INT NOT NULL,
    "Necessity" INT NOT NULL,
    "Flags" INT NOT NULL,
    "DateCreated" TIMESTAMP,
    "DateModified" TIMESTAMP
);

CREATE TABLE IF NOT EXISTS "Dishes" (
    "Id" UUID PRIMARY KEY,
    "Title" VARCHAR(255) NOT NULL,
    "PortionSize" DECIMAL NOT NULL,
    "Calories" DECIMAL NOT NULL,
    "Proteins" DECIMAL NOT NULL,
    "Fats" DECIMAL NOT NULL,
    "Carbohydrates" DECIMAL NOT NULL,
    "Category" INT NOT NULL,
    "Flags" INT NOT NULL,
    "DateCreated" TIMESTAMP,
    "DateModified" TIMESTAMP
);

CREATE TABLE IF NOT EXISTS "ProductPhotos" (
    "Id" UUID PRIMARY KEY,
    "Content" BYTEA NOT NULL,
    "ProductId" UUID,
    "DishId" UUID
);

CREATE TABLE IF NOT EXISTS "DishProducts" (
    "DishId" UUID NOT NULL,
    "ProductId" UUID NOT NULL,
    "AmountInGrams" DECIMAL NOT NULL,
    PRIMARY KEY ("DishId", "ProductId")
);

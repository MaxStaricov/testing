import { expect, test } from '@playwright/test';
import type { ProductCreateDto, ProductDto, DishCreateDto } from '../src/api/types';

const API_BASE = process.env.API_BASE_URL || 'http://localhost:8080/api';

const clickSave = (page: any) => page.getByRole('button', { name: /Сохранить/ }).click();

const fillMacro = (page: any, label: RegExp, value: number) => page.getByLabel(label).fill(String(value));

const fillProduct = async (page: any, title = 'Аб') => {
  await page.getByLabel('Название').fill(title);
  for (const l of [/Калории/, /Белки/, /Жиры/, /Углеводы/] as const) await fillMacro(page, l, 0);
};

async function createProduct(request: any, overrides?: Partial<ProductCreateDto>): Promise<ProductDto> {
  const data: ProductCreateDto = {
    title: 'Тестовый продукт',
    photos: [],
    calories: 100,
    proteins: 10,
    fats: 5,
    carbohydrates: 15,
    description: '',
    category: 2,
    necessity: 0,
    flags: 0,
    ...overrides,
  };
  const res = await request.post(`${API_BASE}/products`, { data });
  return res.json();
}

async function createMinimalProduct(request: any): Promise<ProductDto> {
  return createProduct(request, {
    title: 'Минимальный',
    calories: 0, proteins: 0, fats: 0, carbohydrates: 0,
    category: 0, necessity: 0, flags: 0,
  });
}

async function deleteProduct(request: any, id: string) {
  await request.delete(`${API_BASE}/products/${id}`);
}

async function deleteDish(request: any, id: string) {
  await request.delete(`${API_BASE}/dishes/${id}`);
}






test.describe('Форма продукта', () => {

  test('[EP] Пустое название — ошибка', async ({ page }) => {
    await page.goto('/products/new');
    await clickSave(page);
    await expect(page.getByText('Название обязательно')).toBeVisible();
  });

  test('[BVA] 1 символ названия — ошибка', async ({ page }) => {
    await page.goto('/products/new');
    await page.getByLabel('Название').fill('А');
    await clickSave(page);
    await expect(page.getByText('Минимум 2 символа')).toBeVisible();
  });

  test('[BVA] 2 символа название — корректно', async ({ page }) => {
    await page.goto('/products/new');
    await fillProduct(page, 'Аб');
    await clickSave(page);
    await expect(page).toHaveURL('/products');
  });

  test('-1 для КБЖУ — ошибка', async ({ page }) => {
    await page.goto('/products/new');
    await fillProduct(page);

    for (const l of [/Калории/, /Белки/, /Жиры/, /Углеводы/] as const) {
      await fillMacro(page, l, -1);
      await clickSave(page);
      await expect(page.getByText('Не может быть отрицательным')).toBeVisible();
      await fillMacro(page, l, 0);
    }
  });

  test('[BVA] 101 для Б/Ж/У — ошибка', async ({ page }) => {
    await page.goto('/products/new');
    await fillProduct(page);

    for (const l of [/Белки/, /Жиры/, /Углеводы/] as const) {
      await fillMacro(page, l, 101);
      await clickSave(page);
      await expect(page.getByText('Не может превышать 100')).toBeVisible();
      await fillMacro(page, l, 0);
    }
  });
});




test.describe('Редактирование продукта', () => {
  test('[EP] Предзаполнение названия', async ({ page, request }) => {
    const product = await createMinimalProduct(request);

    await page.goto(`/products/${product.id}/edit`);
    await expect(page.getByLabel('Название')).toHaveValue('Минимальный');
    await expect(page.getByLabel('Калории (ккал/100г)')).toHaveValue('0');

    await deleteProduct(request, product.id);
  });

  test('[EP] Сохранение изменений с переходом на галвную страницу', async ({ page, request }) => {
    const product = await createMinimalProduct(request);

    await page.goto(`/products/${product.id}/edit`);
    await page.getByLabel('Название').fill('Минимальный зелёный');
    await clickSave(page);
    await expect(page).toHaveURL('/products');

    await deleteProduct(request, product.id);
  });
});




test.describe('Создание блюда: продукты', () => {
  let testProducts: ProductDto[];

  test.beforeAll(async ({ request }) => {
    testProducts = [
      await createProduct(request, { title: 'Продукт А', category: 2, flags: 1 | 2 | 4 }),
      await createProduct(request, { title: 'Продукт Б', category: 1, flags: 0 }),
    ];
  });

  test.afterAll(async ({ request }) => {
    if (testProducts) {
      for (const p of testProducts) {
        await deleteProduct(request, p.id);
      }
    }
  });

  test(' сохранить без продуктов — ошибка', async ({ page }) => {
    await page.goto('/dishes/new');
    await page.getByLabel('Название').fill('Тест');
    await page.getByLabel(/Размер порции/).fill('1');
    await page.locator('tbody tr').first().getByRole('spinbutton').fill('1');
    await clickSave(page);
    await expect(page.getByText('Продукт обязателен')).toBeVisible();
  });

  test(' сохранить с несколькими продуктами', async ({ page }) => {
    await page.goto('/dishes/new');
    await page.getByLabel('Название').fill('Микс');

    const rows = page.locator('tbody tr');
    await rows.nth(0).getByLabel('Выберите продукт').click();
    await page.getByRole('option', { name: 'Продукт А' }).first().click();
    await rows.nth(0).getByRole('spinbutton').fill('50');

    await page.getByRole('button', { name: /Добавить ингредиент/ }).click();
    await rows.nth(1).getByLabel('Выберите продукт').click();
    await page.getByRole('option', { name: 'Продукт Б' }).first().click();
    await rows.nth(1).getByRole('spinbutton').fill('100');

    await clickSave(page);
    await expect(page).toHaveURL('/dishes');
  });
});


test.describe('Форма блюда: ручные возможности', () => {
  let testProducts: ProductDto[];
  let createdDishIds: string[];

  test.beforeAll(async ({ request }) => {
    testProducts = [
      await createProduct(request, { title: 'Продукт А', calories: 100, proteins: 10, fats: 5, carbohydrates: 20, flags: 1 | 2 | 4 }),
      await createProduct(request, { title: 'Продукт Б', calories: 50, proteins: 2, fats: 1, carbohydrates: 10, flags: 0 }),
    ];
  });

  test.beforeEach(() => {
    createdDishIds = [];
  });

  test.afterAll(async ({ request }) => {
    if (testProducts) {
      for (const p of testProducts) {
        await deleteProduct(request, p.id);
      }
    }
  });

  test.afterEach(async ({ request }) => {
    for (const id of createdDishIds) {
      await deleteDish(request, id);
    }
  });

  test('Автокатегория по макросу', async ({ page, request }) => {
    const cases = [
      { kw: '!суп', cat: 5, searchTitle: 'Мой суп' },
      { kw: '!десерт', cat: 0, searchTitle: 'Мой десерт' },
      { kw: '!салат', cat: 4, searchTitle: 'Мой салат' },
      { kw: '!напиток', cat: 3, searchTitle: 'Мой напиток' },
    ];
    for (const { kw, cat, searchTitle } of cases) {
      const fullTitle = `${searchTitle} ${kw}`;
      await page.goto('/dishes/new');
      await page.getByLabel('Название').fill(fullTitle);
      await page.locator('.MuiAutocomplete-root input').first().click();
      await page.getByRole('option', { name: 'Продукт А' }).first().click();
      await page.locator('tbody tr').first().getByRole('spinbutton').fill('50');
      await clickSave(page);
      await expect(page).toHaveURL('/dishes');

      const dishes = await (await request.get(`${API_BASE}/dishes`)).json();
      const created = dishes.find((d: any) => d.title === searchTitle);
      expect(created).toBeTruthy();
      expect(created.category).toBe(cat);
      createdDishIds.push(created.id);
    }
  });

  test('Добавление/удаление строк продуктов', async ({ page }) => {
    await page.goto('/dishes/new');
    await page.getByRole('button', { name: /Добавить ингредиент/ }).click();
    await expect(page.locator('tbody tr')).toHaveCount(2);
    await page.locator('tbody tr').first().locator('svg.lucide-trash2').click();
    await expect(page.locator('tbody tr')).toHaveCount(1);
  });
});

test.describe('фото', () => {
  test('[BVA] 0 фото — счётчик отображается', async ({ page }) => {
    await page.goto('/products/new');
    await expect(page.getByText('Фотографии (0/5)')).toBeVisible();
  });

  test('[BVA] 5 фото — счетчик обновился', async ({ page }) => {
    await page.goto('/products/new');
    const fakeFile = { name: 'photo.jpg', mimeType: 'image/jpeg', buffer: Buffer.from('fake') };

    for (let i = 0; i < 5; i++) {
      const [fileChooser] = await Promise.all([
        page.waitForEvent('filechooser'),
        page.locator('label[class]')
          .filter({ has: page.locator('svg.lucide-plus') })
          .first().click()
      ]);
      await fileChooser.setFiles(fakeFile);
    }
    await expect(page.getByText('Фотографии (5/5)')).toBeVisible();
  });
});

test.describe('Описание', () => {
  test('[EP] Описание с текстом сохраняется корректно', async ({ page }) => {

    await page.goto('/products/new');
    await fillProduct(page, 'Описание тест');
    await page.getByLabel('Состав').fill('Описание продукта');
    await clickSave(page);
    await expect(page).toHaveURL('/products');
  });

  test('[BVA] Пустое описание — успешно сохраняется', async ({ page }) => {
    await page.goto('/products/new');
    await fillProduct(page, 'Без описания');
    await clickSave(page);
    await expect(page).toHaveURL('/products');
  });
});

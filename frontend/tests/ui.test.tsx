import { expect, test, type Page, type Route } from '@playwright/test';
import type { ProductCreateDto, ProductDto, DishCreateDto, DishDto } from '../src/api/types';




const PRODUCTS: ProductDto[] = [
  { id: 'p1', title: 'Чечевица', photos: [], calories: 120, proteins: 16, fats: 2, carbohydrates: 20, description: 'Крупа', category: 5, necessity: 2, flags: 1 | 2 },
  { id: 'p2', title: 'Масло', photos: [], calories: 899, proteins: 0, fats: 99.9, carbohydrates: 0, description: '', category: 7, necessity: 0, flags: 4 },
  { id: 'p3', title: 'Курица', photos: [], calories: 165, proteins: 31, fats: 3.6, carbohydrates: 0, description: 'Филе', category: 1, necessity: 2, flags: 0 },
];

const DISHES: DishDto[] = [
  { id: 'd1', title: 'Суп', photos: [], calories: 250, proteins: 12, fats: 5, carbohydrates: 30, portionSize: 300, category: 5, ingredients: [{ productId: 'p1', amountInGrams: 100 }], flags: 1 | 2 },
  { id: 'd2', title: 'Салат', photos: [], calories: 350, proteins: 8, fats: 28, carbohydrates: 8, portionSize: 200, category: 4, ingredients: [{ productId: 'p2', amountInGrams: 50 }], flags: 4 },
];

const DISH_PRODUCTS = [
  { id: 'dp1', title: 'Продукт А', photos: [], calories: 100, proteins: 10, fats: 5, carbohydrates: 20, category: 0, necessity: 0, flags: 1 | 2 | 4 },
  { id: 'dp2', title: 'Продукт Б', photos: [], calories: 50, proteins: 2, fats: 1, carbohydrates: 10, category: 1, necessity: 1, flags: 0 },
];






const fulfill = (route: Route, body: unknown, status = 200) =>
  route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) });

const mockProducts = async (page: Page, prods = PRODUCTS) => {
  await page.route(/\/api\/products(?:\/|$|\?)/, async (route) => {
    const url = new URL(route.request().url());
    const m = route.request().method();
    const p = url.pathname;
    const id = p.replace('/api/products/', '');
    if (p === '/api/products' && m === 'GET') await fulfill(route, prods);
    else if (p === '/api/products' && m === 'POST') await fulfill(route, { id: 'np', ...route.request().postDataJSON() });
    else if (id && m === 'GET') {
      const f = prods.find(x => x.id === id);
      if (f) await fulfill(route, f);
      else await fulfill(route, { message: 'Not found' }, 404);
    } else if (id && m === 'DELETE') await fulfill(route, {});
    else if (id && m === 'PUT') await fulfill(route, { id, ...route.request().postDataJSON() });
  });
};

const mockDishes = async (page: Page, dishes = DISHES) => {
  await page.route(/\/api\/dishes(?:\/|$|\?)/, async (route) => {
    const url = new URL(route.request().url());
    const m = route.request().method();
    const p = url.pathname;
    const id = p.replace('/api/dishes/', '');
    if (p === '/api/dishes' && m === 'GET') await fulfill(route, dishes);
    else if (p === '/api/dishes' && m === 'POST') await fulfill(route, { id: 'nd', ...route.request().postDataJSON() });
    else if (id && m === 'GET') {
      const f = dishes.find(x => x.id === id);
      if (f) await fulfill(route, f);
      else await fulfill(route, { message: 'Not found' }, 404);
    } else if (id && m === 'DELETE') await fulfill(route, {});
    else if (id && m === 'PUT') await fulfill(route, { id, ...route.request().postDataJSON() });
  });
};

const mockBoth = async (page: Page) => { await mockProducts(page); await mockDishes(page); };

const clickSave = (page: Page) => page.getByRole('button', { name: /Сохранить/ }).click();

const fillMacro = (page: Page, label: RegExp, value: number) => page.getByLabel(label).fill(String(value));

const fillProduct = async (page: Page, title = 'Аб') => {
  await page.getByLabel('Название').fill(title);
  for (const l of [/Калории/, /Белки/, /Жиры/, /Углеводы/] as const) await fillMacro(page, l, 0);
};






test.describe('Форма продукта', () => {

  test('[EP] Пустое название — ошибка', async ({ page }) => {
    const creates: ProductCreateDto[] = [];
    await page.route(/\/api\/products(?:\?|$)/, async (route) => {
      if (route.request().method() === 'POST') {
        creates.push(route.request().postDataJSON());
        await fulfill(route, { id: 'np', ...creates[creates.length - 1] });
      }
    });
    await page.goto('/products/new');
    await clickSave(page);
    await expect(page.getByText('Название обязательно')).toBeVisible();
    expect(creates).toHaveLength(0);
  });

  test('[BVA] 1 символ — ошибка', async ({ page }) => {
    await page.route(/\/api\/products(?:\?|$)/, async (route) => {
      if (route.request().method() === 'POST') await fulfill(route, { id: 'np' });
    });
    await page.goto('/products/new');

    await page.getByLabel('Название').fill('А');
    await clickSave(page);
    await expect(page.getByText('Минимум 2 символа')).toBeVisible();
  });

  test('[BVA] 2 символа — корректно', async ({ page }) => {
    const creates: ProductCreateDto[] = [];
    await page.route(/\/api\/products(?:\?|$)/, async (route) => {
      if (route.request().method() === 'POST') {
        creates.push(route.request().postDataJSON());
        await fulfill(route, { id: 'np', ...creates[creates.length - 1] });
      }
    });
    await page.goto('/products/new');
    await fillProduct(page, 'Аб');
    await clickSave(page);
    await expect.poll(() => creates.length).toBe(1);
    expect(creates[0].title).toBe('Аб');
    expect(creates[0].calories).toBe(0);
  });

  test('[BVA] -1 для КБЖУ — ошибка', async ({ page }) => {
    const creates: ProductCreateDto[] = [];
    await page.route(/\/api\/products(?:\?|$)/, async (route) => {
      if (route.request().method() === 'POST') { creates.push(route.request().postDataJSON()); await fulfill(route, { id: 'np' }); }
    });
    await page.goto('/products/new');
    await fillProduct(page);

    for (const l of [/Калории/, /Белки/, /Жиры/, /Углеводы/] as const) {
      await fillMacro(page, l, -1);
      await clickSave(page);
      await expect(page.getByText('Не может быть отрицательным')).toBeVisible();
      await fillMacro(page, l, 0);
    }
    expect(creates).toHaveLength(0);
  });

  test('[BVA] 101 для Б/Ж/У — выше границы', async ({ page }) => {
    const creates: ProductCreateDto[] = [];
    await page.route(/\/api\/products(?:\?|$)/, async (route) => {
      if (route.request().method() === 'POST') { creates.push(route.request().postDataJSON()); await fulfill(route, { id: 'np' }); }
    });
    await page.goto('/products/new');
    await fillProduct(page);

    for (const l of [/Белки/, /Жиры/, /Углеводы/] as const) {
      await fillMacro(page, l, 101);
      await clickSave(page);
      await expect(page.getByText('Не может превышать 100')).toBeVisible();
      await fillMacro(page, l, 0);
    }
    expect(creates).toHaveLength(0);
  });

  test('[EP] Комбинация флагов', async ({ page }) => {
    const creates: ProductCreateDto[] = [];
    await page.route(/\/api\/products(?:\?|$)/, async (route) => {
      if (route.request().method() === 'POST') {
        creates.push(route.request().postDataJSON());
        await fulfill(route, { id: 'np', ...creates[creates.length - 1] });
      }
    });
    await page.goto('/products/new');
    await fillProduct(page);
    await page.getByText('Веган').click();
    await page.getByText('Без сахара').click();
    await clickSave(page);
    await expect.poll(() => creates.length).toBe(1);
    expect(creates[0].flags).toBe(1 | 4);
  });
});






test.describe('Редактирование продукта', () => {
  test('Предзаполнение названия', async ({ page }) => {
    await mockProducts(page);
    await page.goto('/products/p1/edit');
    await expect(page.getByLabel('Название')).toHaveValue('Чечевица');
    await expect(page.getByLabel('Калории (ккал/100г)')).toHaveValue('120');
  });

  test('Сохранение изменений с переходом на галвную страницу', async ({ page }) => {
    await mockProducts(page);
    await page.goto('/products/p1/edit');
    await page.getByLabel('Название').fill('Чечевица зелёная');
    await clickSave(page);
    await expect(page).toHaveURL('/products');
  });
});





test.describe('Форма блюда: продукты', () => {
  test('[EP] Продукт не выбран — ошибка', async ({ page }) => {
    const dc: DishCreateDto[] = [];
    await page.route(/\/api\/products(?:\?|$)/, (r) => fulfill(r, DISH_PRODUCTS));
    await page.route(/\/api\/dishes(?:\?|$)/, async (r) => {
      if (r.request().method() === 'POST') { dc.push(r.request().postDataJSON()); await fulfill(r, { id: 'nd' }); }
      else await fulfill(r, []);
    });
    await page.goto('/dishes/new');
    await page.getByLabel('Название').fill('Тест');
    await page.getByLabel(/Размер порции/).fill('1');
    await page.locator('tbody tr').first().getByRole('spinbutton').fill('1');
    await clickSave(page);
    await expect(page.getByText('Продукт обязателен')).toBeVisible();
    expect(dc).toHaveLength(0);
  });

  test('[EP] Несколько ингредиентов', async ({ page }) => {
    const dc: DishCreateDto[] = [];
    await page.route(/\/api\/products(?:\?|$)/, (r) => fulfill(r, DISH_PRODUCTS));
    await page.route(/\/api\/dishes(?:\?|$)/, async (r) => {
      if (r.request().method() === 'POST') { dc.push(r.request().postDataJSON()); await fulfill(r, { id: 'nd', ...dc[dc.length - 1] }); }
      else await fulfill(r, []);
    });
    await page.goto('/dishes/new');
    await page.getByLabel('Название').fill('Микс');

    const rows = page.locator('tbody tr');
    await rows.nth(0).getByLabel('Выберите продукт').click();
    await page.getByRole('option', { name: 'Продукт А' }).click();
    await rows.nth(0).getByRole('spinbutton').fill('50');

    await page.getByRole('button', { name: /Добавить ингредиент/ }).click();
    await rows.nth(1).getByLabel('Выберите продукт').click();
    await page.getByRole('option', { name: 'Продукт Б' }).click();
    await rows.nth(1).getByRole('spinbutton').fill('100');

    await clickSave(page);
    await expect.poll(() => dc.length).toBe(1);
    expect(dc[0].ingredients).toHaveLength(2);
  });
});

test.describe('Вес ингредиента - BVA', () => {
  let dc: DishCreateDto[];
  test.beforeEach(async ({ page }) => {
    dc = [];
    await page.route(/\/api\/products(?:\?|$)/, (r) => fulfill(r, DISH_PRODUCTS));
    await page.route(/\/api\/dishes(?:\?|$)/, async (r) => {
      if (r.request().method() === 'POST') {
        dc.push(r.request().postDataJSON());
        await fulfill(r, { id: 'nd', ...dc[dc.length - 1] });
      } else {
        await fulfill(r, []);
      }
    });

    await page.goto('/dishes/new');
    await page.getByLabel('Название').fill('Тест');
    await page.getByLabel(/Размер порции/).fill('1');
    await page.getByLabel('Выберите продукт').click();
    await page.getByRole('option', { name: 'Продукт А' }).click();
  });

  test('[BVA] Вес -1 - ошибка отрицательного значения', async ({ page }) => {
    const w = page.locator('tbody tr').first().getByRole('spinbutton');
    await w.fill('-1');
    await clickSave(page);
    await expect(page.getByText('Не может быть отрицательным')).toBeVisible();
  });

  test('[BVA] Вес 0 - ошибка отсутствия ингредиента', async ({ page }) => {
    const w = page.locator('tbody tr').first().getByRole('spinbutton');
    await w.fill('0');
    await clickSave(page);
    await expect(page.getByText('Добавьте хотя бы один ингредиент')).toBeVisible();
    expect(dc).toHaveLength(0);
  });

  test('[BVA] Вес 1 - корректное создание', async ({ page }) => {
    const w = page.locator('tbody tr').first().getByRole('spinbutton');
    await w.fill('1');
    await clickSave(page);
    await expect.poll(() => dc.length).toBe(1);
    expect(dc[0].ingredients![0].amountInGrams).toBe(1);
  });
});


test.describe('Форма блюда: ручные возможности', () => {
  test('5Ручной режим КБЖУ: переключение', async ({ page }) => {
    await page.route(/\/api\/products/, (r) => fulfill(r, DISH_PRODUCTS));
    await page.route(/\/api\/dishes/, (r) => fulfill(r, { id: 'nd' }));
    await page.goto('/dishes/new');
    await expect(page.getByText(/КБЖУ.*🤖/)).toBeVisible();
    await page.getByRole('button', { name: 'Редактировать' }).click();
    await expect(page.getByText(/КБЖУ.*✏️/)).toBeVisible();
    await page.getByRole('button', { name: 'Вернуть авто' }).click();
    await expect(page.getByText(/КБЖУ.*🤖/)).toBeVisible();
  });

  test('Ручные КБЖУ сохраняются', async ({ page }) => {
    let cap: any = null;
    await page.route(/\/api\/products/, (r) => fulfill(r, DISH_PRODUCTS));
    await page.route(/\/api\/dishes/, async (r) => {
      if (r.request().method() === 'GET') await fulfill(r, []);
      else { cap = r.request().postDataJSON(); await fulfill(r, { id: 'nd', ...cap }); }
    });
    await page.goto('/dishes/new');
    await page.getByLabel('Название').fill('Блюдо');
    await page.locator('.MuiAutocomplete-root input').first().click();
    await page.getByRole('option', { name: 'Продукт А' }).click();
    await page.locator('tbody tr').first().getByRole('spinbutton').fill('100');
    await page.getByRole('button', { name: 'Редактировать' }).click();
    await page.getByLabel('🔥 Калории').fill('777');
    await page.getByLabel('🥩 Белки').fill('40');
    await page.getByLabel('🧈 Жиры').fill('20');
    await page.getByLabel('🍞 Углеводы').fill('10');
    await clickSave(page);
    await expect.poll(() => cap).toBeTruthy();
    expect(cap.calories).toBe(777);
    expect(cap.proteins).toBe(40);
  });

  test('Ручные флаги ставятся', async ({ page }) => {
    let cap: any = null;
    await page.route(/\/api\/products/, (r) => fulfill(r, DISH_PRODUCTS));
    await page.route(/\/api\/dishes/, async (r) => {
      if (r.request().method() === 'GET') await fulfill(r, []);
      else { cap = r.request().postDataJSON(); await fulfill(r, { id: 'nd', ...cap }); }
    });
    await page.goto('/dishes/new');
    await page.getByLabel('Название').fill('Блюдо');
    await page.locator('.MuiAutocomplete-root input').first().click();
    await page.getByRole('option', { name: 'Продукт Б' }).click();
    await page.locator('tbody tr').first().getByRole('spinbutton').fill('100');
    await page.getByText('🌱 Веган').click();
    await page.getByText('🍬 Без сахара').click();
    await clickSave(page);
    await expect.poll(() => cap).toBeTruthy();
    expect(cap.flags & 1).toBe(1);
    expect(cap.flags & 4).toBe(4);
    expect(cap.flags & 2).toBe(0);
  });

  test('Автокатегория по макросу', async ({ page }) => {
    let cap: any = null;
    await page.route(/\/api\/products/, (r) => fulfill(r, DISH_PRODUCTS));
    await page.route(/\/api\/dishes/, async (r) => {
      if (r.request().method() === 'GET') await fulfill(r, []);
      else { cap = r.request().postDataJSON(); await fulfill(r, { id: 'nd', ...cap }); }
    });
    const cases = [
      { kw: '!суп', cat: 5 }, { kw: '!десерт', cat: 0 },
      { kw: '!салат', cat: 4 }, { kw: '!напиток', cat: 3 },
    ];
    for (const { kw, cat } of cases) {
      await page.goto('/dishes/new');
      await page.getByLabel('Название').fill(kw);
      await page.locator('.MuiAutocomplete-root input').first().click();
      await page.getByRole('option', { name: 'Продукт А' }).click();
      await page.locator('tbody tr').first().getByRole('spinbutton').fill('50');
      await clickSave(page);
      await expect.poll(() => cap).toBeTruthy();
      expect(cap.category).toBe(cat);
      cap = null;
    }
  });

  test('Добавление/удаление строк продуктов', async ({ page }) => {
    await page.route(/\/api\/products/, (r) => fulfill(r, DISH_PRODUCTS));
    await page.route(/\/api\/dishes/, (r) => fulfill(r, { id: 'nd' }));
    await page.goto('/dishes/new');
    await page.getByRole('button', { name: /Добавить ингредиент/ }).click();
    await expect(page.locator('tbody tr')).toHaveCount(2);
    await page.locator('tbody tr').first().locator('svg.lucide-trash2').click();
    await expect(page.locator('tbody tr')).toHaveCount(1);
  });
});


test.describe('фото', () => {
    test.beforeEach(async ({ page }) => {
      await page.route(/\/api\/products/, (r) => 
        r.request().method() === 'GET' 
          ? fulfill(r, []) 
          : fulfill(r, { id: 'np' })
      );
      await page.goto('/products/new');
    });

    test('[BVA] 0 фото — счётчик отображается', async ({ page }) => {
      await expect(page.getByText('Фотографии (0/5)')).toBeVisible();
      
      await page.locator('label[class]')
        .filter({ has: page.locator('svg.lucide-plus') })
        .first().click();
      await expect(page.getByText('Фотографии (0/5)')).toBeVisible();
    });

    test('[BVA] 5 фото — максимальное допустимое количество', async ({ page }) => {
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
    let creates: ProductCreateDto[];

    test.beforeEach(async ({ page }) => {
      creates = [];
      await page.route(/\/api\/products(?:\?|$)/, async (route) => {
        if (route.request().method() === 'POST') {
          creates.push(route.request().postDataJSON());
          await fulfill(route, { id: 'np' });
        }
      });
      await page.goto('/products/new');
      await fillProduct(page);
    });

    test('[EP] Описание с текстом сохраняется корректно', async ({ page }) => {
      await page.getByLabel('Состав').fill('Описание продукта');
      await clickSave(page);
      
      await expect.poll(() => creates.length).toBe(1);
      expect(creates[0].description).toBe('Описание продукта');
    });

    test('[BVA] Пустое описание — успешно', async ({ page }) => {
      await clickSave(page);
      
      await expect.poll(() => creates.length).toBe(1);
      expect(creates[0].description).toBe('');
    });
  });
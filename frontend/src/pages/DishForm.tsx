import { useState, useEffect, useCallback, useRef } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useForm, Controller, useFieldArray } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import {
  Box, Button, TextField, FormControl, InputLabel, Select, MenuItem,
  Typography, Checkbox, FormControlLabel, Card, CardContent, Alert,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Paper, IconButton, Autocomplete, CircularProgress,
} from '@mui/material';
import { ArrowLeft, Save, Plus, Trash2, Edit, RefreshCw } from 'lucide-react';
import { createDish, updateDish, getDishById, getProducts } from '../api';
import type { ProductDto, DishCategory, DietaryFlags, DishIngredientDto } from '../api/types';
import type { Resolver } from 'react-hook-form';
import { DishCategoryLabels } from '../api/types';
import PhotoUploader from '../components/PhotoUploader';


const schema = yup.object({
  title: yup.string().required('Название обязательно').min(2, 'Минимум 2 символа'),
  portionSize: yup.number().required('Размер порции обязателен').positive('Должен быть положительным'),
  category: yup.number().required('Категория обязательна'),
  ingredients: yup.array().of(
    yup.object({
      productId: yup.string().required('Продукт обязателен'),
      amountInGrams: yup.number().min(0, 'Не может быть отрицательным').required('Количество обязательно'),
    })
  ).min(1, 'Должен быть хотя бы один ингредиент'),
  calories: yup.number().required('Калории обязательны').min(0),
  proteins: yup.number().required('Белки обязательны').min(0),
  fats: yup.number().required('Жиры обязательны').min(0),
  carbohydrates: yup.number().required('Углеводы обязательны').min(0),
  flags: yup.number().required(),
});

type FormData = yup.InferType<typeof schema>;

const DishForm: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [allProducts, setAllProducts] = useState<ProductDto[]>([]);
  const [loadingProducts, setLoadingProducts] = useState(false);
  const [photos, setPhotos] = useState<string[]>([]);
  
  const [manualFlagsMode, setManualFlagsMode] = useState(false);
  const [manualMacrosMode, setManualMacrosMode] = useState(false);

  const manualFlagsModeRef = useRef(false);
  const manualMacrosModeRef = useRef(false);
  
  useEffect(() => { manualFlagsModeRef.current = manualFlagsMode; }, [manualFlagsMode]);
  useEffect(() => { manualMacrosModeRef.current = manualMacrosMode; }, [manualMacrosMode]);

  const { control, handleSubmit, formState: { errors }, reset, watch, setValue } = useForm<FormData>({
    resolver: yupResolver(schema) as Resolver<FormData>,
    defaultValues: {
      title: '', portionSize: 100, category: 0,
      ingredients: [{ productId: '', amountInGrams: 0 }],
      calories: 0, proteins: 0, fats: 0, carbohydrates: 0, flags: 0,
    },
  });

  const { fields, append, remove } = useFieldArray({ control, name: 'ingredients' });
  const watchedIngredients = watch('ingredients');
  const watchedTitle = watch('title');

  const getProductById = useCallback((productId: string | undefined) => {
    if (!productId || !allProducts.length) return null;
    return allProducts.find(p => p.id === productId) || null;
  }, [allProducts]);

  const calculateMacrosAndFlags = useCallback(() => {
    if (!allProducts.length) return;
    const ingredients = watch('ingredients');
    if (!ingredients) return;

    let totalCalories = 0, totalProteins = 0, totalFats = 0, totalCarbs = 0;
    const validIngredients = ingredients.filter(ing => 
      ing?.productId && (ing?.amountInGrams ?? 0) > 0
    );
    const productCache = new Map<string, ProductDto>();
    
    validIngredients.forEach(ingredient => {
      let product = productCache.get(ingredient.productId!);
      if (!product) {
        product = allProducts.find(p => p.id === ingredient.productId);
        if (product) productCache.set(ingredient.productId!, product);
      }
      if (product && ingredient.amountInGrams) {
        const factor = ingredient.amountInGrams / 100;
        totalCalories += product.calories * factor;
        totalProteins += product.proteins * factor;
        totalFats += product.fats * factor;
        totalCarbs += product.carbohydrates * factor;
      }
    });

    if (!manualMacrosModeRef.current) {
      setValue('calories', Math.round(totalCalories), { shouldValidate: true });
      setValue('proteins', Math.round(totalProteins * 100) / 100, { shouldValidate: true });
      setValue('fats', Math.round(totalFats * 100) / 100, { shouldValidate: true });
      setValue('carbohydrates', Math.round(totalCarbs * 100) / 100, { shouldValidate: true });
    }


    if (!manualFlagsModeRef.current && validIngredients.length > 0) {
      let newFlags = 0;
      if (validIngredients.every(ing => {
        const p = getProductById(ing.productId);
        return p && (p.flags & 1);
      })) newFlags |= 1;
      if (validIngredients.every(ing => {
        const p = getProductById(ing.productId);
        return p && (p.flags & 2);
      })) newFlags |= 2;
      if (validIngredients.every(ing => {
        const p = getProductById(ing.productId);
        return p && (p.flags & 4);
      })) newFlags |= 4;
      
      const currentFlags = control._formValues.flags ?? 0;
      if (currentFlags !== newFlags) {
        setValue('flags', newFlags, { shouldValidate: true });
      }
    }
  }, [allProducts, setValue, getProductById, control, watch]); 
  useEffect(() => {
    calculateMacrosAndFlags();
  }, [watchedIngredients, calculateMacrosAndFlags]);

  const debounceRef = useRef<number | null>(null);
  const triggerRecalculation = useCallback(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = window.setTimeout(() => {
      calculateMacrosAndFlags();
    }, 100);
  }, [calculateMacrosAndFlags]);

  const handleAmountChange = useCallback((index: number, value: number | '') => {
    const numericValue = value === '' ? 0 : value;
    setValue(`ingredients.${index}.amountInGrams`, numericValue, { shouldValidate: true });
    triggerRecalculation();
  }, [setValue, triggerRecalculation]);

  const handleProductChange = useCallback((index: number, productId: string) => {
    setValue(`ingredients.${index}.productId`, productId, { shouldValidate: true });
    triggerRecalculation();
  }, [setValue, triggerRecalculation]);

  useEffect(() => {
    const loadDish = async () => {
      if (id) {
        try {
          const dish = await getDishById(id);
          reset({
            title: dish.title || '', portionSize: dish.portionSize, category: dish.category,
            ingredients: dish.ingredients?.length 
              ? dish.ingredients.map(ing => ({ productId: ing.productId ?? '', amountInGrams: ing.amountInGrams ?? 0 }))
              : [{ productId: '', amountInGrams: 0 }],
            calories: dish.calories, proteins: dish.proteins, fats: dish.fats,
            carbohydrates: dish.carbohydrates, flags: dish.flags,
          });
          setPhotos(dish.photos || []);
          
          const validIng = (dish.ingredients || []).filter(ing => ing?.productId && (ing?.amountInGrams ?? 0) > 0);
          if (validIng.length > 0 && allProducts.length > 0) {
            let autoFlags = 0;
            if (validIng.every(ing => { const p = allProducts.find(x => x.id === ing.productId); return p && (p.flags & 1); })) autoFlags |= 1;
            if (validIng.every(ing => { const p = allProducts.find(x => x.id === ing.productId); return p && (p.flags & 2); })) autoFlags |= 2;
            if (validIng.every(ing => { const p = allProducts.find(x => x.id === ing.productId); return p && (p.flags & 4); })) autoFlags |= 4;
            
            if (dish.flags !== autoFlags) setManualFlagsMode(true);
          }
        } catch { setError('Не удалось загрузить данные'); }
      }
    };
    loadDish();
  }, [id, reset, allProducts]);

  useEffect(() => {
    const loadProducts = async () => {
      setLoadingProducts(true);
      try { setAllProducts(await getProducts({})); } 
      catch (err) { console.error(err); setError('Не удалось загрузить продукты'); } 
      finally { setLoadingProducts(false); }
    };
    loadProducts();
  }, []);

  useEffect(() => {
    if (watchedTitle) {
      const t = watchedTitle.toLowerCase();
      if (t.includes('!десерт')) setValue('category', 0);
      else if (t.includes('!первое')) setValue('category', 1);
      else if (t.includes('!второе')) setValue('category', 2);
      else if (t.includes('!напиток')) setValue('category', 3);
      else if (t.includes('!салат')) setValue('category', 4);
      else if (t.includes('!суп')) setValue('category', 5);
      else if (t.includes('!перекус')) setValue('category', 6);
    }
  }, [watchedTitle, setValue]);

  const toggleFlag = useCallback((flagBit: number) => {
    const currentFlags = control._formValues.flags ?? 0;
    const newFlags = currentFlags ^ flagBit;
    setValue('flags', newFlags, { shouldValidate: true, shouldTouch: true, shouldDirty: true });
    setManualFlagsMode(true);
    manualFlagsModeRef.current = true;
  }, [control, setValue]);

  const resetFlagsToAuto = useCallback(() => {
    setManualFlagsMode(false);
    setTimeout(() => calculateMacrosAndFlags(), 0);
  }, [calculateMacrosAndFlags]);

  const toggleMacrosMode = useCallback(() => setManualMacrosMode(prev => !prev), []);
  const resetMacrosToAuto = useCallback(() => {
    setManualMacrosMode(false);
    calculateMacrosAndFlags();
  }, [calculateMacrosAndFlags]);

  const addIngredient = () => append({ productId: '', amountInGrams: 0 });

  const removeIngredient = (index: number) => {
    if (fields.length > 1) { remove(index); triggerRecalculation(); } 
    else {
      setValue(`ingredients.${index}.productId`, '', { shouldValidate: true });
      setValue(`ingredients.${index}.amountInGrams`, 0, { shouldValidate: true });
      triggerRecalculation();
    }
  };

  const onSubmit = async (data: FormData) => {
    setLoading(true); setError(null);
    try {
      const cleanIngredients: DishIngredientDto[] = (data.ingredients || [])
        .filter((ing: any) => ing?.productId && (ing?.amountInGrams ?? 0) > 0)
        .map((ing: any) => ({ productId: ing.productId!, amountInGrams: ing.amountInGrams! }));
      
      if (cleanIngredients.length === 0) { setError('Добавьте хотя бы один ингредиент'); setLoading(false); return; }

      const dishData = { 
        ...data, 
        photos, 
        category: data.category as DishCategory, 
        flags: data.flags as DietaryFlags, 
        ingredients: cleanIngredients 
      };
      
      if (id) await updateDish(id, { ...dishData, id } as any);
      else await createDish(dishData);
      navigate('/dishes');
    } catch (err: unknown) {
      console.error(err); setError(err instanceof Error ? err.message : 'Ошибка сохранения');
    } finally { setLoading(false); }
  };

  const productOptions = allProducts.map(p => ({ ...p, title: p.title ?? 'Без названия' }));
  const currentFlags = watch('flags') || 0;

  return (
    <Box sx={{ p: { xs: 2, md: 3 } }}>
      <Box display="flex" alignItems="center" mb={3}>
        <Button startIcon={<ArrowLeft />} component={Link} to="/dishes">Назад к блюдам</Button>
        <Typography variant="h4" sx={{ ml: 2 }}>{id ? 'Редактировать блюдо' : 'Создать блюдо'}</Typography>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Card sx={{ overflow: 'visible' }}>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)}>
            <Box display="grid" gridTemplateColumns="repeat(12, minmax(0, 1fr))" gap={3}>
              
              <Box gridColumn="span 12">
                <Controller name="title" control={control} render={({ field }) => (
                  <TextField {...field} label="Название" fullWidth error={!!errors.title} helperText={errors.title?.message} />
                )} />
              </Box>

              <Box gridColumn={{ xs: 'span 12', sm: 'span 6' }}>
                <Controller name="portionSize" control={control} render={({ field }) => (
                  <TextField {...field} label="Размер порции (г)" type="number" fullWidth error={!!errors.portionSize} helperText={errors.portionSize?.message} />
                )} />
              </Box>
              <Box gridColumn={{ xs: 'span 12', sm: 'span 6' }}>
                <Controller name="category" control={control} render={({ field }) => (
                  <FormControl fullWidth error={!!errors.category}>
                    <InputLabel>Категория</InputLabel>
                    <Select {...field} value={field.value ?? ''}>
                      {Object.entries(DishCategoryLabels).map(([key, label]) => <MenuItem key={key} value={Number(key)}>{label}</MenuItem>)}
                    </Select>
                  </FormControl>
                )} />
              </Box>

              {/* Ингредиенты */}
              <Box gridColumn="span 12" sx={{ position: 'relative', zIndex: 10 }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, p: 1.5, bgcolor: 'action.hover', borderRadius: 1 }}>
                  <Typography variant="h6">Ингредиенты</Typography>
                  <Button type="button" variant="contained" color="primary" startIcon={<Plus size={18} />} onClick={addIngredient} disabled={loading} sx={{ minWidth: 190, fontWeight: 600, textTransform: 'none', boxShadow: 3 }}>
                    Добавить ингредиент
                  </Button>
                </Box>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 1.5 }}>Добавлено: <strong>{fields?.length || 0}</strong></Typography>
                
                {fields.length === 0 ? (
                  <Alert severity="info">Нажмите кнопку выше, чтобы добавить первый ингредиент</Alert>
                ) : (
                  <TableContainer component={Paper}>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell><strong>Продукт</strong></TableCell>
                          <TableCell align="right"><strong>Вес (г)</strong></TableCell>
                          <TableCell align="right" width={60}><strong>✕</strong></TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {fields.map((field, index) => (
                          <TableRow key={field.id} hover>
                            <TableCell>
                              <Controller name={`ingredients.${index}.productId`} control={control} render={({ field: ctrlField }) => (
                                <Autocomplete options={productOptions} getOptionLabel={(opt) => opt?.title || ''} value={getProductById(ctrlField.value)} onChange={(_, val) => { const newId = val?.id || ''; ctrlField.onChange(newId); handleProductChange(index, newId); }} loading={loadingProducts} isOptionEqualToValue={(opt, val) => opt?.id === val?.id} renderInput={(params) => (
                                  <TextField {...params} label="Выберите продукт" size="small" fullWidth error={!!errors.ingredients?.[index]?.productId} helperText={errors.ingredients?.[index]?.productId?.message} InputProps={{ ...params.InputProps, endAdornment: <>{loadingProducts && <CircularProgress size={16} sx={{ mr: 1 }} />}{params.InputProps.endAdornment}</> }} />
                                )} />
                              )} />
                            </TableCell>
                            <TableCell align="right">
                              <Controller name={`ingredients.${index}.amountInGrams`} control={control} render={({ field: ctrlField }) => (
                                <TextField {...ctrlField} type="number" size="small" sx={{ width: { xs: 80, sm: 100 } }} inputProps={{ min: 0, step: 1 }} error={!!errors.ingredients?.[index]?.amountInGrams} helperText={errors.ingredients?.[index]?.amountInGrams?.message} value={ctrlField.value ?? 0} onChange={(e) => { const val = e.target.value === '' ? 0 : Number(e.target.value); handleAmountChange(index, val); }} />
                              )} />
                            </TableCell>
                            <TableCell align="right">
                              <IconButton size="small" color="error" onClick={() => removeIngredient(index)} disabled={fields.length <= 1 || loading}><Trash2 size={16} /></IconButton>
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                )}
              </Box>

              {/* ✅ КБЖУ — С РУЧНЫМ РЕЖИМОМ */}
              <Box gridColumn="span 12">
                <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                  <Typography variant="h6">КБЖУ {manualMacrosMode ? '✏️ (ручной)' : '🤖 (авто)'}</Typography>
                  <Box display="flex" gap={1}>
                    {manualMacrosMode && (
                      <Button size="small" variant="outlined" color="primary" onClick={resetMacrosToAuto} startIcon={<RefreshCw size={14} />} sx={{ textTransform: 'none' }}>Авто</Button>
                    )}
                    <Button size="small" variant={manualMacrosMode ? 'contained' : 'outlined'} color={manualMacrosMode ? 'warning' : 'primary'} onClick={toggleMacrosMode} startIcon={manualMacrosMode ? <RefreshCw size={14} /> : <Edit size={14} />} sx={{ textTransform: 'none' }}>
                      {manualMacrosMode ? 'Вернуть авто' : 'Редактировать'}
                    </Button>
                  </Box>
                </Box>
                <Typography variant="body2" color="textSecondary" sx={{ mb: 1.5 }}>
                  {manualMacrosMode ? 'Вы редактируете значения вручную. Изменения ингредиентов не будут влиять на КБЖУ.' : 'Рассчитываются автоматически на основе ингредиентов'}
                </Typography>

                <Box display="grid" gridTemplateColumns="repeat(4, 1fr)" gap={2}>
                  {(['calories', 'proteins', 'fats', 'carbohydrates'] as const).map((field) => {
                    const labels = {
                      calories: { label: '🔥 Калории', unit: 'ккал', color: 'error.main' },
                      proteins: { label: '🥩 Белки', unit: 'г', color: 'primary.main' },
                      fats: { label: '🧈 Жиры', unit: 'г', color: 'warning.main' },
                      carbohydrates: { label: '🍞 Углеводы', unit: 'г', color: 'info.main' },
                    };
                    const { label, unit, color } = labels[field];
                    
                    return (
                      <Box key={field}>
                        <Controller name={field} control={control} render={({ field: { onChange, value, ...rest } }) => (
                          <TextField
                            {...rest}
                            type="number"
                            fullWidth
                            label={label}
                            value={value ?? 0}
                            // 🔥 onChange ВСЕГДА есть (нужно react-hook-form)
                            onChange={(e) => onChange(e.target.value === '' ? 0 : Number(e.target.value))}
                            // 🔥 readOnly блокирует ввод ТОЛЬКО в авто-режиме
                            InputProps={{ readOnly: !manualMacrosMode }}
                            helperText={unit}
                            sx={{ 
                              '& .MuiInputBase-input': { fontWeight: 600, color },
                              '& .MuiInputBase-root': { bgcolor: manualMacrosMode ? 'action.hover' : 'transparent' }
                            }}
                          />
                        )} />
                      </Box>
                    );
                  })}
                </Box>
              </Box>

              {/* Флаги */}
              <Box gridColumn="span 12">
                <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
                  <Typography variant="subtitle1">Флаги {manualFlagsMode ? '✏️ (ручной)' : '🤖 (авто)'}</Typography>
                  {manualFlagsMode && <Button size="small" variant="outlined" color="primary" onClick={resetFlagsToAuto} sx={{ textTransform: 'none', fontSize: '0.75rem' }}>🔄 Сбросить</Button>}
                </Box>
                <Box display="flex" gap={2} flexWrap="wrap">
                  <FormControlLabel control={<Checkbox checked={!!(currentFlags & 1)} onChange={() => toggleFlag(1)} color="success" disabled={loading} />} label="🌱 Веган" />
                  <FormControlLabel control={<Checkbox checked={!!(currentFlags & 2)} onChange={() => toggleFlag(2)} color="warning" disabled={loading} />} label="🌾 Без глютена" />
                  <FormControlLabel control={<Checkbox checked={!!(currentFlags & 4)} onChange={() => toggleFlag(4)} color="error" disabled={loading} />} label="🍬 Без сахара" />
                </Box>
              </Box>

              <Box gridColumn="span 12"><PhotoUploader photos={photos} onPhotosChange={setPhotos} /></Box>

              <Box gridColumn="span 12" sx={{ mt: 1 }}>
                <Button type="submit" variant="contained" color="success" startIcon={<Save />} disabled={loading} size="large" sx={{ minWidth: 160, fontWeight: 600 }}>
                  {loading ? 'Сохранение...' : '💾 Сохранить'}
                </Button>
              </Box>
            </Box>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
};

export default DishForm;

// import { useState, useEffect, useCallback, useRef } from 'react';
// import { useParams, useNavigate, Link } from 'react-router-dom';
// import { useForm, Controller, useFieldArray } from 'react-hook-form';
// import { yupResolver } from '@hookform/resolvers/yup';
// import * as yup from 'yup';
// import {
//   Box, Button, TextField, FormControl, InputLabel, Select, MenuItem,
//   Typography, Checkbox, FormControlLabel, Card, CardContent, Alert,
//   Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
//   Paper, IconButton, Autocomplete, CircularProgress,
// } from '@mui/material';
// import { ArrowLeft, Save, Plus, Trash2 } from 'lucide-react';
// import { createDish, updateDish, getDishById, getProducts } from '../api';
// import type { ProductDto, DishCategory, DietaryFlags, DishIngredientDto } from '../api/types';
// import type { Resolver } from 'react-hook-form';
// import { DishCategoryLabels } from '../api/types';
// import PhotoUploader from '../components/PhotoUploader';

// // ✅ Схема валидации
// const schema = yup.object({
//   title: yup.string().required('Название обязательно').min(2, 'Минимум 2 символа'),
//   portionSize: yup.number().required('Размер порции обязателен').positive('Должен быть положительным'),
//   category: yup.number().required('Категория обязательна'),
//   ingredients: yup.array().of(
//     yup.object({
//       productId: yup.string().required('Продукт обязателен'),
//       amountInGrams: yup.number().min(0, 'Не может быть отрицательным').required('Количество обязательно'),
//     })
//   ).min(1, 'Должен быть хотя бы один ингредиент'),
//   calories: yup.number().required('Калории обязательны').min(0),
//   proteins: yup.number().required('Белки обязательны').min(0),
//   fats: yup.number().required('Жиры обязательны').min(0),
//   carbohydrates: yup.number().required('Углеводы обязательны').min(0),
//   flags: yup.number().required(),
// });

// type FormData = yup.InferType<typeof schema>;

// const DishForm: React.FC = () => {
//   const { id } = useParams<{ id: string }>();
//   const navigate = useNavigate();
//   const [loading, setLoading] = useState(false);
//   const [error, setError] = useState<string | null>(null);
//   const [allProducts, setAllProducts] = useState<ProductDto[]>([]);
//   const [loadingProducts, setLoadingProducts] = useState(false);
//   const [photos, setPhotos] = useState<string[]>([]);
//   const [manualFlagsMode, setManualFlagsMode] = useState(false);

//   const { control, handleSubmit, formState: { errors }, reset, watch, setValue } = useForm<FormData>({
//     resolver: yupResolver(schema) as Resolver<FormData>,
//     defaultValues: {
//       title: '', portionSize: 100, category: 0,
//       ingredients: [{ productId: '', amountInGrams: 0 }],
//       calories: 0, proteins: 0, fats: 0, carbohydrates: 0, flags: 0,
//     },
//   });

//   const { fields, append, remove } = useFieldArray({ control, name: 'ingredients' });
//   const watchedIngredients = watch('ingredients');
//   const watchedTitle = watch('title');

//   // ✅ Мемоизированный поиск продукта
//   const getProductById = useCallback((productId: string | undefined) => {
//     if (!productId || !allProducts.length) return null;
//     return allProducts.find(p => p.id === productId) || null;
//   }, [allProducts]);

//   // ✅ Расчёт БЖУ и флагов
//   const calculateMacrosAndFlags = useCallback(() => {
//     if (!allProducts.length) return;
//     const ingredients = watch('ingredients');
//     if (!ingredients) return;

//     let totalCalories = 0, totalProteins = 0, totalFats = 0, totalCarbs = 0;
//     const validIngredients = ingredients.filter(ing => 
//       ing?.productId && (ing?.amountInGrams ?? 0) > 0
//     );
//     const productCache = new Map<string, ProductDto>();
    
//     validIngredients.forEach(ingredient => {
//       let product = productCache.get(ingredient.productId!);
//       if (!product) {
//         product = allProducts.find(p => p.id === ingredient.productId);
//         if (product) productCache.set(ingredient.productId!, product);
//       }
//       if (product && ingredient.amountInGrams) {
//         const factor = ingredient.amountInGrams / 100;
//         totalCalories += product.calories * factor;
//         totalProteins += product.proteins * factor;
//         totalFats += product.fats * factor;
//         totalCarbs += product.carbohydrates * factor;
//       }
//     });

//     setValue('calories', Math.round(totalCalories), { shouldValidate: true });
//     setValue('proteins', Math.round(totalProteins * 100) / 100, { shouldValidate: true });
//     setValue('fats', Math.round(totalFats * 100) / 100, { shouldValidate: true });
//     setValue('carbohydrates', Math.round(totalCarbs * 100) / 100, { shouldValidate: true });

//     // 🔥 Пересчёт флагов ТОЛЬКО в авто-режиме
//     if (!manualFlagsMode && validIngredients.length > 0) {
//       let newFlags = 0;
//       if (validIngredients.every(ing => {
//         const p = getProductById(ing.productId);
//         return p && (p.flags & 1);
//       })) newFlags |= 1;
//       if (validIngredients.every(ing => {
//         const p = getProductById(ing.productId);
//         return p && (p.flags & 2);
//       })) newFlags |= 2;
//       if (validIngredients.every(ing => {
//         const p = getProductById(ing.productId);
//         return p && (p.flags & 4);
//       })) newFlags |= 4;
      
//       const currentFlags = control._formValues.flags ?? 0;
//       if (currentFlags !== newFlags) {
//         setValue('flags', newFlags, { shouldValidate: true });
//       }
//     }
//   }, [allProducts, setValue, getProductById, manualFlagsMode, control, watch]);

//   // ✅ Фоновый пересчёт
//   useEffect(() => {
//     calculateMacrosAndFlags();
//   }, [watchedIngredients, calculateMacrosAndFlags]);

//   // ✅ Debounce
//   const debounceRef = useRef<number | null>(null);
//   const triggerRecalculation = useCallback(() => {
//     if (debounceRef.current) clearTimeout(debounceRef.current);
//     debounceRef.current = window.setTimeout(() => calculateMacrosAndFlags(), 100);
//   }, [calculateMacrosAndFlags]);

//   // ✅ Обработчики изменений
//   const handleAmountChange = useCallback((index: number, value: number | '') => {
//     const numericValue = value === '' ? 0 : value;
//     setValue(`ingredients.${index}.amountInGrams`, numericValue, { shouldValidate: true });
//     triggerRecalculation();
//   }, [setValue, triggerRecalculation]);

//   const handleProductChange = useCallback((index: number, productId: string) => {
//     setValue(`ingredients.${index}.productId`, productId, { shouldValidate: true });
//     triggerRecalculation();
//   }, [setValue, triggerRecalculation]);

//   // ✅ Загрузка блюда (ЕДИНСТВЕННЫЙ useEffect!)
//   useEffect(() => {
//     const loadDish = async () => {
//       if (id) {
//         try {
//           const dish = await getDishById(id);
//           reset({
//             title: dish.title || '', portionSize: dish.portionSize, category: dish.category,
//             ingredients: dish.ingredients?.length 
//               ? dish.ingredients.map(ing => ({ productId: ing.productId ?? '', amountInGrams: ing.amountInGrams ?? 0 }))
//               : [{ productId: '', amountInGrams: 0 }],
//             calories: dish.calories, proteins: dish.proteins, fats: dish.fats,
//             carbohydrates: dish.carbohydrates, flags: dish.flags,
//           });
//           setPhotos(dish.photos || []);
          
//           // 🔥 Определяем ручной режим по несовпадению флагов
//           const validIng = (dish.ingredients || []).filter(
//             ing => ing?.productId && (ing?.amountInGrams ?? 0) > 0
//           );
//           if (validIng.length > 0 && allProducts.length > 0) {
//             let autoFlags = 0;
//             if (validIng.every(ing => {
//               const p = allProducts.find(x => x.id === ing.productId);
//               return p && (p.flags & 1);
//             })) autoFlags |= 1;
//             if (validIng.every(ing => {
//               const p = allProducts.find(x => x.id === ing.productId);
//               return p && (p.flags & 2);
//             })) autoFlags |= 2;
//             if (validIng.every(ing => {
//               const p = allProducts.find(x => x.id === ing.productId);
//               return p && (p.flags & 4);
//             })) autoFlags |= 4;
            
//             if (dish.flags !== autoFlags) {
//               setManualFlagsMode(true);
//             }
//           }
//         } catch {
//           setError('Не удалось загрузить данные');
//         }
//       }
//     };
//     loadDish();
//   }, [id, reset, allProducts]);

//   // ✅ Загрузка продуктов
//   useEffect(() => {
//     const loadProducts = async () => {
//       setLoadingProducts(true);
//       try {
//         const data = await getProducts({});
//         setAllProducts(data);
//       } catch (err) {
//         console.error('Failed to load products:', err);
//         setError('Не удалось загрузить список продуктов');
//       } finally {
//         setLoadingProducts(false);
//       }
//     };
//     loadProducts();
//   }, []);

//   // ✅ Авто-категория
//   useEffect(() => {
//     if (watchedTitle) {
//       const t = watchedTitle.toLowerCase();
//       if (t.includes('!десерт')) setValue('category', 0);
//       else if (t.includes('!первое')) setValue('category', 1);
//       else if (t.includes('!второе')) setValue('category', 2);
//       else if (t.includes('!напиток')) setValue('category', 3);
//       else if (t.includes('!салат')) setValue('category', 4);
//       else if (t.includes('!суп')) setValue('category', 5);
//       else if (t.includes('!перекус')) setValue('category', 6);
//     }
//   }, [watchedTitle, setValue]);

//   // ✅ Переключение флагов
//   const toggleFlag = useCallback((flagBit: number) => {
//     const currentFlags = control._formValues.flags ?? 0;
//     const newFlags = currentFlags ^ flagBit;
//     setValue('flags', newFlags, { shouldValidate: true, shouldTouch: true });
//     setManualFlagsMode(true);
//   }, [control, setValue]);

//   const resetFlagsToAuto = useCallback(() => {
//     setManualFlagsMode(false);
//     setTimeout(() => calculateMacrosAndFlags(), 0);
//   }, [calculateMacrosAndFlags]);

//   const addIngredient = () => append({ productId: '', amountInGrams: 0 });

//   const removeIngredient = (index: number) => {
//     if (fields.length > 1) {
//       remove(index);
//       triggerRecalculation();
//     } else {
//       setValue(`ingredients.${index}.productId`, '', { shouldValidate: true });
//       setValue(`ingredients.${index}.amountInGrams`, 0, { shouldValidate: true });
//       triggerRecalculation();
//     }
//   };

//   // ✅ Отправка формы
//   const onSubmit = async (data: FormData) => {
//     setLoading(true);
//     setError(null);
//     try {
//       const cleanIngredients: DishIngredientDto[] = (data.ingredients || [])
//         .filter(ing => ing?.productId && (ing?.amountInGrams ?? 0) > 0)
//         .map(ing => ({ productId: ing.productId!, amountInGrams: ing.amountInGrams! }));
      
//       if (cleanIngredients.length === 0) {
//         setError('Добавьте хотя бы один ингредиент с количеством > 0');
//         setLoading(false);
//         return;
//       }

//       const dishData = { 
//         ...data, photos, 
//         category: data.category as DishCategory, 
//         flags: data.flags as DietaryFlags,
//         ingredients: cleanIngredients
//       };
      
//       if (id) await updateDish(id, { ...dishData, id } as any);
//       else await createDish(dishData);
//       navigate('/dishes');
//     } catch (err: unknown) {
//       console.error('Ошибка сохранения:', err);
//       setError(err instanceof Error ? err.message : 'Не удалось сохранить блюдо');
//     } finally {
//       setLoading(false);
//     }
//   };

//   const productOptions = allProducts.map(p => ({ ...p, title: p.title ?? 'Без названия' }));
//   const currentFlags = watch('flags') || 0;

//   return (
//     <Box sx={{ p: { xs: 2, md: 3 } }}>
//       <Box display="flex" alignItems="center" mb={3}>
//         <Button startIcon={<ArrowLeft />} component={Link} to="/dishes">Назад к блюдам</Button>
//         <Typography variant="h4" sx={{ ml: 2 }}>{id ? 'Редактировать блюдо' : 'Создать блюдо'}</Typography>
//       </Box>

//       {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

//       <Card sx={{ overflow: 'visible' }}>
//         <CardContent>
//           <form onSubmit={handleSubmit(onSubmit)}>
//             <Box display="grid" gridTemplateColumns="repeat(12, minmax(0, 1fr))" gap={3}>
              
//               {/* Название */}
//               <Box gridColumn="span 12">
//                 <Controller name="title" control={control} render={({ field }) => (
//                   <TextField {...field} label="Название" fullWidth error={!!errors.title} helperText={errors.title?.message} />
//                 )} />
//               </Box>

//               {/* Порция и категория */}
//               <Box gridColumn={{ xs: 'span 12', sm: 'span 6' }}>
//                 <Controller name="portionSize" control={control} render={({ field }) => (
//                   <TextField {...field} label="Размер порции (г)" type="number" fullWidth error={!!errors.portionSize} helperText={errors.portionSize?.message} />
//                 )} />
//               </Box>
//               <Box gridColumn={{ xs: 'span 12', sm: 'span 6' }}>
//                 <Controller name="category" control={control} render={({ field }) => (
//                   <FormControl fullWidth error={!!errors.category}>
//                     <InputLabel>Категория</InputLabel>
//                     <Select {...field} value={field.value ?? ''}>
//                       {Object.entries(DishCategoryLabels).map(([key, label]) => (
//                         <MenuItem key={key} value={Number(key)}>{label}</MenuItem>
//                       ))}
//                     </Select>
//                   </FormControl>
//                 )} />
//               </Box>

//               {/* Ингредиенты */}
//               <Box gridColumn="span 12" sx={{ position: 'relative', zIndex: 10 }}>
//                 <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, p: 1.5, bgcolor: 'action.hover', borderRadius: 1 }}>
//                   <Typography variant="h6">Ингредиенты</Typography>
//                   <Button type="button" variant="contained" color="primary" startIcon={<Plus size={18} />} onClick={addIngredient} disabled={loading} sx={{ minWidth: 190, fontWeight: 600, textTransform: 'none', boxShadow: 3 }}>
//                     Добавить ингредиент
//                   </Button>
//                 </Box>
//                 <Typography variant="body2" color="textSecondary" sx={{ mb: 1.5 }}>Добавлено: <strong>{fields?.length || 0}</strong></Typography>
                
//                 {fields.length === 0 ? (
//                   <Alert severity="info">Нажмите кнопку выше, чтобы добавить первый ингредиент</Alert>
//                 ) : (
//                   <TableContainer component={Paper}>
//                     <Table size="small">
//                       <TableHead>
//                         <TableRow>
//                           <TableCell><strong>Продукт</strong></TableCell>
//                           <TableCell align="right"><strong>Вес (г)</strong></TableCell>
//                           <TableCell align="right" width={60}><strong>✕</strong></TableCell>
//                         </TableRow>
//                       </TableHead>
//                       <TableBody>
//                         {fields.map((field, index) => (
//                           <TableRow key={field.id} hover>
//                             <TableCell>
//                               <Controller name={`ingredients.${index}.productId`} control={control} render={({ field: ctrlField }) => (
//                                 <Autocomplete options={productOptions} getOptionLabel={(opt) => opt?.title || ''} value={getProductById(ctrlField.value)} onChange={(_, val) => { const newId = val?.id || ''; ctrlField.onChange(newId); handleProductChange(index, newId); }} loading={loadingProducts} isOptionEqualToValue={(opt, val) => opt?.id === val?.id} renderInput={(params) => (
//                                   <TextField {...params} label="Выберите продукт" size="small" fullWidth error={!!errors.ingredients?.[index]?.productId} helperText={errors.ingredients?.[index]?.productId?.message} InputProps={{ ...params.InputProps, endAdornment: <>{loadingProducts && <CircularProgress size={16} sx={{ mr: 1 }} />}{params.InputProps.endAdornment}</> }} />
//                                 )} />
//                               )} />
//                             </TableCell>
//                             <TableCell align="right">
//                               <Controller name={`ingredients.${index}.amountInGrams`} control={control} render={({ field: ctrlField }) => (
//                                 <TextField {...ctrlField} type="number" size="small" sx={{ width: { xs: 80, sm: 100 } }} inputProps={{ min: 0, step: 1 }} error={!!errors.ingredients?.[index]?.amountInGrams} helperText={errors.ingredients?.[index]?.amountInGrams?.message} value={ctrlField.value ?? 0} onChange={(e) => { const val = e.target.value === '' ? 0 : Number(e.target.value); handleAmountChange(index, val); }} />
//                               )} />
//                             </TableCell>
//                             <TableCell align="right">
//                               <IconButton size="small" color="error" onClick={() => removeIngredient(index)} disabled={fields.length <= 1 || loading}><Trash2 size={16} /></IconButton>
//                             </TableCell>
//                           </TableRow>
//                         ))}
//                       </TableBody>
//                     </Table>
//                   </TableContainer>
//                 )}
//               </Box>

//               {/* БЖУ */}
//               {(['calories', 'proteins', 'fats', 'carbohydrates'] as const).map((field) => (
//                 <Box key={field} gridColumn={{ xs: 'span 12', sm: 'span 6', md: 'span 3' }}>
//                   <Controller name={field} control={control} render={({ field: ctrlField }) => (
//                     <TextField {...ctrlField} label={field === 'calories' ? '🔥 Калории' : field === 'proteins' ? '🥩 Белки' : field === 'fats' ? '🧈 Жиры' : '🍞 Углеводы'} type="number" fullWidth value={ctrlField.value ?? 0} InputProps={{ readOnly: true }} helperText={field !== 'calories' ? 'г / порция' : 'ккал / порция'} sx={{ '& .MuiInputBase-input': { fontWeight: 600, color: field === 'calories' ? 'error.main' : 'primary.main' }}} />
//                   )} />
//                 </Box>
//               ))}

//               {/* Флаги */}
//               <Box gridColumn="span 12">
//                 <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
//                   <Typography variant="subtitle1">Флаги {manualFlagsMode ? '✏️ (ручной)' : '🤖 (авто)'}</Typography>
//                   {manualFlagsMode && <Button size="small" variant="outlined" color="primary" onClick={resetFlagsToAuto} sx={{ textTransform: 'none', fontSize: '0.75rem' }}>🔄 Сбросить</Button>}
//                 </Box>
//                 <Box display="flex" gap={2} flexWrap="wrap">
//                   <FormControlLabel control={<Checkbox checked={!!(currentFlags & 1)} onChange={() => toggleFlag(1)} color="success" disabled={loading} />} label="🌱 Веган" />
//                   <FormControlLabel control={<Checkbox checked={!!(currentFlags & 2)} onChange={() => toggleFlag(2)} color="warning" disabled={loading} />} label="🌾 Без глютена" />
//                   <FormControlLabel control={<Checkbox checked={!!(currentFlags & 4)} onChange={() => toggleFlag(4)} color="error" disabled={loading} />} label="🍬 Без сахара" />
//                 </Box>
//               </Box>

//               {/* Фото */}
//               <Box gridColumn="span 12"><PhotoUploader photos={photos} onPhotosChange={setPhotos} /></Box>

//               {/* Сохранить */}
//               <Box gridColumn="span 12" sx={{ mt: 1 }}>
//                 <Button type="submit" variant="contained" color="success" startIcon={<Save />} disabled={loading} size="large" sx={{ minWidth: 160, fontWeight: 600 }}>
//                   {loading ? 'Сохранение...' : '💾 Сохранить'}
//                 </Button>
//               </Box>
//             </Box>
//           </form>
//         </CardContent>
//       </Card>
//     </Box>
//   );
// };

// export default DishForm;
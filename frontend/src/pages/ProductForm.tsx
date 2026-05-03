import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import {
  Box,
  Button,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Typography,
  Checkbox,
  FormControlLabel,
  Card,
  CardContent,
  Alert,
} from '@mui/material';
import { ArrowLeft, Save } from 'lucide-react';
import { createProduct, updateProduct, getProductById } from '../api';
import type { ProductCategory, CookingNecessity, DietaryFlags } from '../api/types';
import type { Resolver } from 'react-hook-form';
import { ProductCategoryLabels } from '../api/types';
import PhotoUploader from '../components/PhotoUploader';

const schema = yup.object({
  title: yup.string().required('Название обязательно').min(2, 'Минимум 2 символа'),
  calories: yup.number().required('Калории обязательны').min(0, 'Не может быть отрицательным'),
  proteins: yup.number().required('Белки обязательны').min(0, 'Не может быть отрицательным').max(100, 'Не может превышать 100'),
  fats: yup.number().required('Жиры обязательны').min(0, 'Не может быть отрицательным').max(100, 'Не может превышать 100'),
  carbohydrates: yup.number().required('Углеводы обязательны').min(0, 'Не может быть отрицательным').max(100, 'Не может превышать 100'),
  description: yup.string(),
  category: yup.number().required('Категория обязательна'),
  necessity: yup.number().required('Необходимость готовки обязательна'),
  flags: yup.number(),
}).test('macros-sum', 'Сумма БЖУ не может превышать 100', function(value) {
  const { proteins, fats, carbohydrates } = value;
  return (proteins || 0) + (fats || 0) + (carbohydrates || 0) <= 100;
});

type FormData = yup.InferType<typeof schema>;

const ProductForm: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [photos, setPhotos] = useState<string[]>([]);

  const { control, handleSubmit, formState: { errors }, reset, watch } = useForm<FormData>({
    resolver: yupResolver(schema) as Resolver<FormData>,
    defaultValues: {
      title: '',
      calories: 0,
      proteins: 0,
      fats: 0,
      carbohydrates: 0,
      description: '',
      category: 0,
      necessity: 0,
      flags: 0,
    },
  });

  const flags = watch('flags') || 0;

  useEffect(() => {
    if (id) {
      const loadProduct = async () => {
        try {
          const product = await getProductById(id);
          reset({
            title: product.title || '',
            calories: product.calories,
            proteins: product.proteins,
            fats: product.fats,
            carbohydrates: product.carbohydrates,
            description: product.description || '',
            category: product.category,
            necessity: product.necessity,
            flags: product.flags,
          });
          setPhotos(product.photos || []);
        } catch {
          setError('Не удалось загрузить продукт');
        }
      };
      loadProduct();
    }
  }, [id, reset]);

  const onSubmit = async (data: FormData) => {
    setLoading(true);
    setError(null);
    try {
      const productData = { ...data, photos, category: data.category as ProductCategory, necessity: data.necessity as CookingNecessity, flags: data.flags as DietaryFlags };
      if (id) {
        await updateProduct(id, { ...productData, id });
      } else {
        await createProduct(productData);
      }
      navigate('/products');
    } catch (error: any) {
      if (error instanceof Error) {
        setError(error.message);
      } else {
        setError('Не удалось сохранить продукт');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleFlagChange = (flag: number, checked: boolean) => {
    const newFlags = checked ? flags | flag : flags & ~flag;
    reset({ ...watch(), flags: newFlags });
  };

  return (
    <Box>
      <Box display="flex" alignItems="center" mb={3}>
        <Button startIcon={<ArrowLeft />} component={Link} to="/products">
          Назад к продуктам
        </Button>
        <Typography variant="h4" sx={{ ml: 2 }}>
          {id ? 'Редактировать продукт' : 'Создать продукт'}
        </Typography>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Card>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)}>
            <Box display="grid" gridTemplateColumns="repeat(12, minmax(0, 1fr))" gap={3}>
              <Box gridColumn="span 12">
                <Controller
                  name="title"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Название"
                      fullWidth
                      error={!!errors.title}
                      helperText={errors.title?.message}
                    />
                  )}
                />
              </Box>

              <Box gridColumn={{ xs: 'span 12', sm: 'span 6' }}>
                <Controller
                  name="category"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth error={!!errors.category}>
                      <InputLabel>Категория</InputLabel>
                      <Select {...field}>
                        {Object.entries(ProductCategoryLabels).map(([key, label]) => (
                          <MenuItem key={key} value={Number(key)}>{label}</MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  )}
                />
              </Box>

              <Box gridColumn={{ xs: 'span 12', sm: 'span 6' }}>
                <Controller
                  name="necessity"
                  control={control}
                  render={({ field }) => (
                    <FormControl fullWidth error={!!errors.necessity}>
                      <InputLabel>Необходимость готовки</InputLabel>
                      <Select {...field}>
                        <MenuItem value={0}>Готовый к употреблению</MenuItem>
                        <MenuItem value={1}>Полуфабрикат</MenuItem>
                        <MenuItem value={2}>Требует приготовления</MenuItem>
                      </Select>
                    </FormControl>
                  )}
                />
              </Box>

              <Box gridColumn={{ xs: 'span 12', sm: 'span 6', md: 'span 3' }}>
                <Controller
                  name="calories"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Калории (ккал/100г)"
                      type="number"
                      fullWidth
                      error={!!errors.calories}
                      helperText={errors.calories?.message}
                    />
                  )}
                />
              </Box>

              <Box gridColumn={{ xs: 'span 12', sm: 'span 6', md: 'span 3' }}>
                <Controller
                  name="proteins"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Белки (г/100г)"
                      type="number"
                      fullWidth
                      error={!!errors.proteins}
                      helperText={errors.proteins?.message}
                    />
                  )}
                />
              </Box>

              <Box gridColumn={{ xs: 'span 12', sm: 'span 6', md: 'span 3' }}>
                <Controller
                  name="fats"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Жиры (г/100г)"
                      type="number"
                      fullWidth
                      error={!!errors.fats}
                      helperText={errors.fats?.message}
                    />
                  )}
                />
              </Box>

              <Box gridColumn={{ xs: 'span 12', sm: 'span 6', md: 'span 3' }}>
                <Controller
                  name="carbohydrates"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Углеводы (г/100г)"
                      type="number"
                      fullWidth
                      error={!!errors.carbohydrates}
                      helperText={errors.carbohydrates?.message}
                    />
                  )}
                />
              </Box>

              <Box gridColumn="span 12">
                <Controller
                  name="description"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Состав"
                      multiline
                      rows={3}
                      fullWidth
                      error={!!errors.description}
                      helperText={errors.description?.message}
                    />
                  )}
                />
              </Box>

              <Box gridColumn="span 12">
                <Typography variant="h6">Дополнительные флаги</Typography>
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={!!(flags & 1)}
                      onChange={(e) => handleFlagChange(1, e.target.checked)}
                    />
                  }
                  label="Веган"
                />
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={!!(flags & 2)}
                      onChange={(e) => handleFlagChange(2, e.target.checked)}
                    />
                  }
                  label="Без глютена"
                />
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={!!(flags & 4)}
                      onChange={(e) => handleFlagChange(4, e.target.checked)}
                    />
                  }
                  label="Без сахара"
                />
              </Box>

              <Box gridColumn="span 12">
                <PhotoUploader photos={photos} onPhotosChange={setPhotos} />
              </Box>

              <Box gridColumn="span 12">
                <Button
                  type="submit"
                  variant="contained"
                  startIcon={<Save />}
                  disabled={loading}
                >
                  {loading ? 'Сохранение...' : 'Сохранить'}
                </Button>
              </Box>
            </Box>
          </form>
        </CardContent>
      </Card>
    </Box>
  );
};

export default ProductForm;
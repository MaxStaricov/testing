import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  Box,
  Button,
  Typography,
  Card,
  CardContent,
  CardMedia,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
} from '@mui/material';
import { ArrowLeft, Edit } from 'lucide-react';
import { getDishById, getProducts } from '../api';
import type { DishDto, ProductDto } from '../api/types';
import { DishCategoryLabels } from '../api/types';

const DishDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [dish, setDish] = useState<DishDto | null>(null);
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadDish = async () => {
      if (!id) return;
      setLoading(true);
      try {
        const dishData = await getDishById(id);
        setDish(dishData);

        // Load products for ingredient names
        const allProducts = await getProducts({});
        setProducts(allProducts);
      } catch (error) {
        console.error('Failed to load dish');
      } finally {
        setLoading(false);
      }
    };
    loadDish();
  }, [id]);

// Для ProductDetail.tsx и DishDetail.tsx
const getPhotoSrc = (photo: string) => {
  // Предполагаем, что все фото - JPEG. Для поддержки других форматов нужна более сложная логика.
  return `data:image/jpeg;base64,${photo}`;
};


  const getFlagsArray = (flags: number) => {
    const flagsArray = [];
    if (flags & 1) flagsArray.push('Веган');
    if (flags & 2) flagsArray.push('Без глютена');
    if (flags & 4) flagsArray.push('Без сахара');
    return flagsArray;
  };

  const getProductName = (productId: string) => {
    const product = products.find(p => p.id === productId);
    return product?.title || 'Неизвестный продукт';
  };

  if (loading) return <Typography>Загрузка...</Typography>;
  if (!dish) return <Typography>Блюдо не найдено</Typography>;

  return (
    <Box>
      <Box display="flex" alignItems="center" mb={3}>
        <Button startIcon={<ArrowLeft />} component={Link} to="/dishes">
          Назад к блюдам
        </Button>
        <Button
          startIcon={<Edit />}
          component={Link}
          to={`/dishes/${dish.id}/edit`}
          sx={{ ml: 2 }}
        >
          Редактировать
        </Button>
      </Box>

      <Card>
        <CardContent>
          <Typography variant="h4" gutterBottom>
            {dish.title}
          </Typography>

          <Box display="flex" flexWrap="wrap" gap={2} mb={2}>
            <Typography><strong>Категория:</strong> {DishCategoryLabels[dish.category]}</Typography>
            <Typography><strong>Размер порции:</strong> {dish.portionSize} г</Typography>
          </Box>

          <Typography variant="h6" gutterBottom>Пищевая ценность на порцию</Typography>
          <Box display="flex" flexWrap="wrap" gap={2} mb={2}>
            <Typography><strong>Калории:</strong> {dish.calories} ккал</Typography>
            <Typography><strong>Белки:</strong> {dish.proteins} г</Typography>
            <Typography><strong>Жиры:</strong> {dish.fats} г</Typography>
            <Typography><strong>Углеводы:</strong> {dish.carbohydrates} г</Typography>
          </Box>

          <Box mt={2} mb={2}>
            <Typography variant="subtitle1">Флаги:</Typography>
            <Box mt={1}>
              {getFlagsArray(dish.flags).map((flag) => (
                <Chip key={flag} label={flag} sx={{ mr: 1, mb: 1 }} />
              ))}
            </Box>
          </Box>

          <Typography variant="h6" gutterBottom>Состав</Typography>
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Продукт</TableCell>
                  <TableCell align="right">Количество (г)</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {dish.ingredients?.map((ingredient, index) => (
                  <TableRow key={index}>
                    <TableCell>{getProductName(ingredient.productId)}</TableCell>
                    <TableCell align="right">{ingredient.amountInGrams}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>

{dish.photos && dish.photos.length > 0 && (
  <Box mt={3}>
    <Typography variant="h6">Фотографии</Typography>
    <Box display="flex" flexWrap="wrap" gap={2} mt={1}>
      {dish.photos.map((photo, index) => (
        <CardMedia
          key={index}
          component="img"
          height="140"
          image={getPhotoSrc(photo)}  // ✅ ИСПРАВЛЕНО
          alt={`Фото ${index + 1}`}
          sx={{ width: 140, objectFit: 'cover' }}
        />
      ))}
    </Box>
  </Box>
)}

          <Box mt={2}>
            {dish.dateCreated && (
              <Typography><strong>Дата создания:</strong> {new Date(dish.dateCreated).toLocaleString()}</Typography>
            )}
            {dish.dateModified && (
              <Typography><strong>Дата изменения:</strong> {new Date(dish.dateModified).toLocaleString()}</Typography>
            )}
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
};

export default DishDetail;
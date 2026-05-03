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
} from '@mui/material';
import { ArrowLeft, Edit } from 'lucide-react';
import { getProductById } from '../api';
import type { ProductDto } from '../api/types';
import { ProductCategoryLabels } from '../api/types';

// Для ProductDetail.tsx и DishDetail.tsx
const getPhotoSrc = (photo: string) => {
  // Предполагаем, что все фото - JPEG. Для поддержки других форматов нужна более сложная логика.
  return `data:image/jpeg;base64,${photo}`;
};

const ProductDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [product, setProduct] = useState<ProductDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadProduct = async () => {
      if (!id) return;
      setLoading(true);
      try {
        const data = await getProductById(id);
        setProduct(data);
      } catch {
        console.error('Failed to load product');
      } finally {
        setLoading(false);
      }
    };
    loadProduct();
  }, [id]);

  const getFlagsArray = (flags: number) => {
    const flagsArray = [];
    if (flags & 1) flagsArray.push('Веган');
    if (flags & 2) flagsArray.push('Без глютена');
    if (flags & 4) flagsArray.push('Без сахара');
    return flagsArray;
  };

  const getNecessityLabel = (necessity: number) => {
    switch (necessity) {
      case 0: return 'Готовый к употреблению';
      case 1: return 'Полуфабрикат';
      case 2: return 'Требует приготовления';
      default: return '';
    }
  };

  if (loading) return <Typography>Загрузка...</Typography>;
  if (!product) return <Typography>Продукт не найден</Typography>;

  return (
    <Box>
      <Box display="flex" alignItems="center" mb={3}>
        <Button startIcon={<ArrowLeft />} component={Link} to="/products">
          Назад к продуктам
        </Button>
        <Button
          startIcon={<Edit />}
          component={Link}
          to={`/products/${product.id}/edit`}
          sx={{ ml: 2 }}
        >
          Редактировать
        </Button>
      </Box>

      <Card>
        <CardContent>
          <Typography variant="h4" gutterBottom>
            {product.title}
          </Typography>

          <Box display="grid" gridTemplateColumns="repeat(12, minmax(0, 1fr))" gap={3}>
            <Box gridColumn={{ xs: 'span 12', md: 'span 6' }}>
              <Typography variant="h6">Основная информация</Typography>
              <Typography><strong>Категория:</strong> {ProductCategoryLabels[product.category]}</Typography>
              <Typography><strong>Необходимость готовки:</strong> {getNecessityLabel(product.necessity)}</Typography>
              <Typography><strong>Калорийность:</strong> {product.calories} ккал/100г</Typography>
              <Typography><strong>Белки:</strong> {product.proteins} г/100г</Typography>
              <Typography><strong>Жиры:</strong> {product.fats} г/100г</Typography>
              <Typography><strong>Углеводы:</strong> {product.carbohydrates} г/100г</Typography>
            </Box>

            <Box gridColumn={{ xs: 'span 12', md: 'span 6' }}>
              <Typography variant="h6">Дополнительно</Typography>
              {product.description && (
                <Typography><strong>Состав:</strong> {product.description}</Typography>
              )}
              <Box mt={2}>
                <Typography variant="subtitle1">Флаги:</Typography>
                <Box mt={1}>
                  {getFlagsArray(product.flags).map((flag) => (
                    <Chip key={flag} label={flag} sx={{ mr: 1, mb: 1 }} />
                  ))}
                </Box>
              </Box>
              {product.dateCreated && (
                <Typography><strong>Дата создания:</strong> {new Date(product.dateCreated).toLocaleString()}</Typography>
              )}
              {product.dateModified && (
                <Typography><strong>Дата изменения:</strong> {new Date(product.dateModified).toLocaleString()}</Typography>
              )}
            </Box>
          </Box>

          {product.photos && product.photos.length > 0 && (
            <Box mt={3}>
              <Typography variant="h6">Фотографии</Typography>
              <Box display="grid" gridTemplateColumns="repeat(12, minmax(0, 1fr))" gap={2} mt={1}>
                {product.photos.map((photo, index) => (
                  <Box gridColumn={{ xs: 'span 6', sm: 'span 4', md: 'span 3' }} key={index}>
                    <CardMedia
                      component="img"
                      height="140"
                      image={getPhotoSrc(photo)}
                      alt={`Фото ${index + 1}`}
                    />
                  </Box>
                ))}
              </Box>
            </Box>
          )}
        </CardContent>
      </Card>
    </Box>
  );
};

export default ProductDetail;
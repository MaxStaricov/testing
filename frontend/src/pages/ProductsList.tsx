import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  CardActions,
  CardMedia, 
  Typography,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Chip,
  IconButton,
} from '@mui/material';
import { Edit, Trash2, Plus } from 'lucide-react';
import { getProducts, deleteProduct } from '../api';
import type { ProductDto, DietaryFlags } from '../api/types';
import { ProductCategoryLabels } from '../api/types';

const getPhotoSrc = (photo: string) => {
  return `data:image/jpeg;base64,${photo}`;
};

const ProductsList: React.FC = () => {
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState('');
  const [category, setCategory] = useState('');
  const [necessity, setNecessity] = useState('');
  const [flags, setFlags] = useState('');
  const [sort, setSort] = useState('');

  const loadProducts = useCallback(async () => {
    setLoading(true);
    try {
      const data = await getProducts({ query, category, necessity, flags, sort });
      setProducts(data);
    } catch (error) {
      console.error('Failed to load products:', error);
    } finally {
      setLoading(false);
    }
  }, [query, category, necessity, flags, sort]);

  useEffect(() => {
    loadProducts();
  }, [loadProducts]);

  const handleDelete = async (id: string) => {
    if (window.confirm('Вы уверены, что хотите удалить этот продукт?')) {
      try {
        await deleteProduct(id);
        loadProducts();
      } catch {
        alert('Не удалось удалить продукт');
      }
    }
  };

  const getFlagsArray = (flags: DietaryFlags) => {
    const flagsArray: string[] = [];
    if (flags & 1) flagsArray.push('Веган');
    if (flags & 2) flagsArray.push('Без глютена');
    if (flags & 4) flagsArray.push('Без сахара');
    return flagsArray;
  };

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4">Продукты</Typography>
        <Button variant="contained" startIcon={<Plus />} component={Link} to="/products/new">
          Добавить продукт
        </Button>
      </Box>

      <Box display="flex" gap={2} mb={3} flexWrap="wrap">
        <TextField
          label="Поиск"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          size="small"
        />
        <FormControl size="small" sx={{ minWidth: 120 }}>
          <InputLabel>Категория</InputLabel>
          <Select value={category} onChange={(e) => setCategory(e.target.value)}>
            <MenuItem value="">Все</MenuItem>
            {Object.entries(ProductCategoryLabels).map(([key, label]) => (
              <MenuItem key={key} value={key}>{label}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 120 }}>
          <InputLabel>Необходимость готовки</InputLabel>
          <Select value={necessity} onChange={(e) => setNecessity(e.target.value)}>
            <MenuItem value="">Все</MenuItem>
            <MenuItem value="0">Готовый к употреблению</MenuItem>
            <MenuItem value="1">Полуфабрикат</MenuItem>
            <MenuItem value="2">Требует приготовления</MenuItem>
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 120 }}>
          <InputLabel>Флаги</InputLabel>
          <Select value={flags} onChange={(e) => setFlags(e.target.value)}>
            <MenuItem value="">Все</MenuItem>
            <MenuItem value="1">Веган</MenuItem>
            <MenuItem value="2">Без глютена</MenuItem>
            <MenuItem value="4">Без сахара</MenuItem>
          </Select>
        </FormControl>
        <FormControl size="small" sx={{ minWidth: 120 }}>
          <InputLabel>Сортировка</InputLabel>
          <Select value={sort} onChange={(e) => setSort(e.target.value)}>
            <MenuItem value="">По умолчанию</MenuItem>
            <MenuItem value="title">Название</MenuItem>
            <MenuItem value="calories">Калорийность</MenuItem>
            <MenuItem value="proteins">Белки</MenuItem>
            <MenuItem value="fats">Жиры</MenuItem>
            <MenuItem value="carbohydrates">Углеводы</MenuItem>
          </Select>
        </FormControl>
      </Box>

      {loading ? (
        <Typography>Загрузка...</Typography>
      ) : (
        <Box display="flex" flexWrap="wrap" gap={3}>
          {products.map((product) => (
            <Box key={product.id} width={{ xs: '100%', sm: '48%', md: '30%' }}>
              <Card>
                {}
                {product.photos && product.photos.length > 0 && (
                  <CardMedia
                    component="img"
                    height="140"
                    image={getPhotoSrc(product.photos[0])}
                    alt={product.title ?? 'Продукт'}  
                    sx={{ objectFit: 'cover' }}
                  />
                )}
                
                <CardContent>
                  <Typography variant="h6">{product.title}</Typography>
                  <Typography color="textSecondary">
                    Категория: {ProductCategoryLabels[product.category]}
                  </Typography>
                  <Typography>Калории: {product.calories} ккал/100г</Typography>
                  <Typography>
                    БЖУ: {product.proteins}/{product.fats}/{product.carbohydrates}
                  </Typography>
                  <Box mt={1}>
                    {getFlagsArray(product.flags).map((flag) => (
                      <Chip key={flag} label={flag} size="small" sx={{ mr: 1 }} />
                    ))}
                  </Box>
                </CardContent>
                <CardActions>
                  <Button size="small" component={Link} to={`/products/${product.id}`}>
                    Просмотр
                  </Button>
                  <IconButton size="small" component={Link} to={`/products/${product.id}/edit`}>
                    <Edit />
                  </IconButton>
                  <IconButton size="small" onClick={() => handleDelete(product.id)}>
                    <Trash2 />
                  </IconButton>
                </CardActions>
              </Card>
            </Box>
          ))}
        </Box>
      )}
    </Box>
  );
};

export default ProductsList;
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
import { getDishes, deleteDish } from '../api';
import type { DishDto } from '../api/types';
import { DishCategoryLabels } from '../api/types';


const getPhotoSrc = (photo: string) => {
  return `data:image/jpeg;base64,${photo}`;
};

const DishesList: React.FC = () => {
  const [dishes, setDishes] = useState<DishDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState('');
  const [category, setCategory] = useState('');
  const [flags, setFlags] = useState('');

  const loadDishes = useCallback(async () => {
    setLoading(true);
    try {
      const data = await getDishes({ query, category, flags });
      setDishes(data);
    } catch (error) {
      console.error('Failed to load dishes:', error);
    } finally {
      setLoading(false);
    }
  }, [query, category, flags]);

  useEffect(() => {
    loadDishes();
  }, [loadDishes]);

  const handleDelete = async (id: string) => {
    if (window.confirm('Вы уверены, что хотите удалить это блюдо?')) {
      try {
        await deleteDish(id);
        loadDishes();
      } catch {
        alert('Не удалось удалить блюдо');
      }
    }
  };

  const getFlagsArray = (flags: number) => {
    const flagsArray: string[] = [];
    if (flags & 1) flagsArray.push('Веган');
    if (flags & 2) flagsArray.push('Без глютена');
    if (flags & 4) flagsArray.push('Без сахара');
    return flagsArray;
  };

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4">Блюда</Typography>
        <Button variant="contained" startIcon={<Plus />} component={Link} to="/dishes/new">
          Добавить блюдо
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
            {Object.entries(DishCategoryLabels).map(([key, label]) => (
              <MenuItem key={key} value={key}>{label}</MenuItem>
            ))}
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
      </Box>

      {loading ? (
        <Typography>Загрузка...</Typography>
      ) : (
        <Box display="flex" flexWrap="wrap" gap={3}>
          {dishes.map((dish) => (
            <Box key={dish.id} width={{ xs: '100%', sm: '48%', md: '30%' }}>
              <Card>
                {}
                {dish.photos && dish.photos.length > 0 && (
                  <CardMedia
                    component="img"
                    height="140"
                    image={getPhotoSrc(dish.photos[0])}
                    alt={dish.title ?? 'Блюдо'} 
                    sx={{ objectFit: 'cover' }}
                  />
                )}
                
                <CardContent>
                  <Typography variant="h6">{dish.title}</Typography>
                  <Typography color="textSecondary">
                    Категория: {DishCategoryLabels[dish.category]}
                  </Typography>
                  <Typography>Калории: {dish.calories} ккал/порция</Typography>
                  <Typography>
                    БЖУ: {dish.proteins}/{dish.fats}/{dish.carbohydrates}
                  </Typography>
                  <Typography>Размер порции: {dish.portionSize} г</Typography>
                  <Box mt={1}>
                    {getFlagsArray(dish.flags).map((flag) => (
                      <Chip key={flag} label={flag} size="small" sx={{ mr: 1 }} />
                    ))}
                  </Box>
                </CardContent>
                <CardActions>
                  <Button size="small" component={Link} to={`/dishes/${dish.id}`}>
                    Просмотр
                  </Button>
                  <IconButton size="small" component={Link} to={`/dishes/${dish.id}/edit`}>
                    <Edit />
                  </IconButton>
                  <IconButton size="small" onClick={() => handleDelete(dish.id)}>
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

export default DishesList;
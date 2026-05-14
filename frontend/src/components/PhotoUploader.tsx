import React, { useState } from 'react';
import {
  Box,
  Card,
  IconButton,
  Typography,
  Alert,
} from '@mui/material';
import { Trash2, Plus } from 'lucide-react';

interface PhotoUploaderProps {
  photos: string[];
  onPhotosChange: (photos: string[]) => void;
  maxPhotos?: number;
}

// Для PhotoUploader.tsx
const getPhotoSrc = (photo: string) => {
  return `data:image/jpeg;base64,${photo}`;
};

// const getPhotoSrc = (photo: string) =>
//   photo.startsWith('')
//     ? photo
//     : `image/jpeg;base64,${photo}`;

const PhotoUploader: React.FC<PhotoUploaderProps> = ({
  photos,
  onPhotosChange,
  maxPhotos = 5,
}) => {
  const [error, setError] = useState<string | null>(null);

  const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setError(null);

    if (photos.length >= maxPhotos) {
      setError(`Максимум ${maxPhotos} фотографий`);
      event.target.value = '';
      return;
    }

    if (!file.type.startsWith('image/')) {
      setError('Пожалуйста, выберите изображение');
      event.target.value = '';
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      setError('Размер файла не должен превышать 5MB');
      event.target.value = '';
      return;
    }

    const reader = new FileReader();

    reader.onload = () => {
      const result = reader.result as string;
      const base64 = result.split(',')[1];
      onPhotosChange([...photos, base64]);
    };

    reader.onerror = () => {
      setError('Ошибка чтения файла');
    };

    reader.readAsDataURL(file);
    event.target.value = '';
  };

  const handleRemovePhoto = (index: number) => {
    onPhotosChange(photos.filter((_, i) => i !== index));
  };

  return (
    <Box>
      <Typography variant="h6">
        Фотографии ({photos.length}/{maxPhotos})
      </Typography>

      {error && <Alert severity="error">{error}</Alert>}

      <Box display="grid" gridTemplateColumns="repeat(auto-fill, minmax(150px, 1fr))" gap={2}>
        {photos.map((photo, index) => (
          <Card key={index} sx={{ position: 'relative' }}>
            <Box
              component="img"
              src={getPhotoSrc(photo)}
              alt={`photo-${index}`}
              sx={{ width: '100%', height: 150, objectFit: 'cover', display: 'block' }}
            />
            <IconButton
              onClick={() => handleRemovePhoto(index)}
              sx={{ position: 'absolute', top: 4, right: 4, bgcolor: 'rgba(255,255,255,0.8)' }}
            >
              <Trash2 size={16} />
            </IconButton>
          </Card>
        ))}

          <Card
            component="label"
            sx={{
              height: 150,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              cursor: 'pointer',
            }}
          >
            <input type="file" accept="image/*" hidden onChange={handleFileUpload} />
            {photos.length < maxPhotos && <Plus />}
          </Card>
      </Box>
    </Box>
  );
};

export default PhotoUploader;

import axios from 'axios';
import type {
  ProductCreateDto,
  ProductUpdateDto,
  ProductDto,
  DishCreateDto,
  DishUpdateDto,
  DishDto,
} from './types';

const apiClient = axios.create({
  baseURL: '/api',
});

export const getProducts = async (params: { query?: string; category?: string; necessity?: string; flags?: string; sort?: string }) => {
  const { data } = await apiClient.get<ProductDto[]>('/products', { params });
  return data;
};

export const getProductById = async (id: string) => {
  const { data } = await apiClient.get<ProductDto>(`/products/${id}`);
  return data;
};

export const createProduct = async (product: ProductCreateDto) => {
  const { data } = await apiClient.post<ProductDto>('/products', product);
  return data;
};

export const updateProduct = async (id: string, product: ProductUpdateDto) => {
  const { data } = await apiClient.put<ProductDto>(`/products/${id}`, product);
  return data;
};

export const deleteProduct = async (id: string) => {
  const { data } = await apiClient.delete(`/products/${id}`);
  return data;
};

export const getDishes = async (params: { query?: string; category?: string; flags?: string }) => {
  const { data } = await apiClient.get<DishDto[]>('/dishes', { params });
  return data;
};

export const getDishById = async (id: string) => {
  const { data } = await apiClient.get<DishDto>(`/dishes/${id}`);
  return data;
};

export const createDish = async (dish: DishCreateDto) => {
  const { data } = await apiClient.post<DishDto>('/dishes', dish);
  return data;
};

export const updateDish = async (id: string, dish: DishUpdateDto) => {
  const { data } = await apiClient.put<DishDto>(`/dishes/${id}`, dish);
  return data;
};

export const deleteDish = async (id: string) => {
  const { data } = await apiClient.delete(`/dishes/${id}`);
  return data;
};

import api from './api';

export const authService = {
  register: (data) => api.post('/auth/register', data),
  login: (data) => api.post('/auth/login', data),
  verifyOtp: (data) => api.post('/auth/verify-otp', data),
  getProfile: () => api.get('/auth/profile'),
  getAllUsers: (page = 1, pageSize = 10) =>
    api.get(`/auth/users?page=${page}&pageSize=${pageSize}`),
  updateUserStatus: (userId, data) =>
    api.put(`/auth/users/${userId}/status`, data),
};

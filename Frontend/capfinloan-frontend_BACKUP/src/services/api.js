import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL
  || 'http://localhost:5000/gateway';

const api = axios.create({
  baseURL: API_URL,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

// Attach JWT token to every request
api.interceptors.request.use(
  (config) => {
    const token = sessionStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Handle responses globally
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.code === 'ECONNABORTED') {
      error.message = 'Request timed out. Please try again.';
    } else if (!error.response) {
      error.message = 'Cannot connect to server. ' +
        'Please check if all services are running.';
    } else if (error.response.status === 401) {
      sessionStorage.clear();
      window.location.href = '/auth/login';
    }
    return Promise.reject(error);
  }
);

export default api;

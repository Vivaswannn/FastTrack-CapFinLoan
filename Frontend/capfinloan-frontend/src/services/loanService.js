import api from './api';

export const loanService = {
  createDraft: (data) => api.post('/applications', data),
  updateDraft: (id, data) => api.put(`/applications/${id}`, data),
  submit: (id) => api.post(`/applications/${id}/submit`),
  getMyApplications: (page = 1, pageSize = 10) =>
    api.get(`/applications/my?page=${page}&pageSize=${pageSize}`),
  getById: (id) => api.get(`/applications/${id}`),
  getStatusHistory: (id) => api.get(`/applications/${id}/status`),
  getAllApplications: (page = 1, pageSize = 10, status = null) => {
    let url = `/admin/applications?page=${page}&pageSize=${pageSize}`;
    if (status) url += `&status=${status}`;
    return api.get(url);
  },
  updateStatus: (id, data) =>
    api.put(`/admin/applications/${id}/status`, data),
};

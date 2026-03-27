import api from './api';

export const adminService = {
  makeDecision: (appId, data) =>
    api.post(`/admin/applications/${appId}/decision`, data),
  getDecision: async (appId) => {
    try { return await api.get(`/admin/decisions/${appId}`); }
    catch (err) {
      if (err.response && err.response.status === 404) return { data: { data: null } };
      throw err;
    }
  },
  getDashboardStats: () => api.get('/admin/reports/dashboard'),
  getMonthlyTrend: (months = 6) =>
    api.get(`/admin/reports/monthly?months=${months}`),
  exportCsv: (startDate, endDate) => {
    let url = '/admin/reports/export';
    const params = [];
    if (startDate) params.push(`startDate=${startDate}`);
    if (endDate) params.push(`endDate=${endDate}`);
    if (params.length) url += '?' + params.join('&');
    return api.get(url, { responseType: 'blob' });
  },
};

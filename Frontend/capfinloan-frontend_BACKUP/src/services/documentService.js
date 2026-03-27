import api from './api';

export const documentService = {
  upload: (formData) => api.post('/documents/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  }),
  getByApplication: async (appId) => {
    try { return await api.get(`/documents/${appId}`); }
    catch (err) {
      if (err.response && (err.response.status === 404 || err.response.status === 400)) return { data: { data: [] } };
      throw err;
    }
  },
  downloadFile: (docId) =>
    api.get(`/documents/file/${docId}`, { responseType: 'blob' }),
  verifyDocument: (docId, data) =>
    api.put(`/admin/documents/${docId}/verify`, data),
  getAdminDocuments: async (appId) => {
    try { return await api.get(`/admin/documents/${appId}`); }
    catch (err) {
      if (err.response && (err.response.status === 404 || err.response.status === 400)) return { data: { data: [] } };
      throw err;
    }
  },
};

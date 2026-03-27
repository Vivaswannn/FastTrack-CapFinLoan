import { useState, useEffect, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useDropzone } from 'react-dropzone';
import toast from 'react-hot-toast';
import PageLayout from '../../components/layout/PageLayout';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import { documentService } from '../../services/documentService';
import { loanService } from '../../services/loanService';
import { formatDate, formatFileSize } from '../../utils/formatters';
import { DOCUMENT_TYPES } from '../../utils/constants';

export default function Documents() {
  const [searchParams] = useSearchParams();
  const appId = searchParams.get('appId');
  const [documents, setDocuments] = useState([]);
  const [applications, setApplications] = useState([]);
  const [selectedApp, setSelectedApp] = useState(
    appId && appId !== 'undefined' && appId !== 'null' ? appId : ''
  );
  const [docType, setDocType] = useState('AadhaarCard');
  const [file, setFile] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    fetchApplications();
  }, []);

  useEffect(() => {
    if (selectedApp) fetchDocuments();
  }, [selectedApp]);

  const fetchApplications = async () => {
    try {
      const res = await loanService.getMyApplications(1, 100);
      setApplications(res.data.data.items || []);
    } catch (err) {
      const msg = err.response?.data?.message || err.message || 'Failed to load applications';
      toast.error(msg);
    }
  };

  const fetchDocuments = async () => {
    if (!selectedApp) return;
    setLoading(true);
    try {
      const res = await documentService.getByApplication(selectedApp);
      setDocuments(res.data.data || []);
    } catch (err) {
      const msg = err.response?.data?.message || err.message || 'Failed to load documents';
      toast.error(msg);
    }
    finally { setLoading(false); }
  };

  const onDrop = useCallback((accepted, rejected) => {
    if (rejected.length > 0) {
      toast.error('File rejected. Check type (PDF/JPG/PNG) and size (max 5MB)');
      return;
    }
    if (accepted.length > 0) setFile(accepted[0]);
  }, []);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'application/pdf': ['.pdf'],
      'image/jpeg': ['.jpg', '.jpeg'],
      'image/png': ['.png'],
    },
    maxSize: 5 * 1024 * 1024,
    multiple: false,
  });

  const handleUpload = async () => {
    if (!file) { toast.error('Select a file first'); return; }
    if (!selectedApp) { toast.error('Select an application'); return; }
    setUploading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);
      formData.append('applicationId', selectedApp);
      formData.append('documentType', docType);
      await documentService.upload(formData);
      toast.success('Document uploaded successfully!');
      setFile(null);
      fetchDocuments();
    } catch (err) {
      const msg = err.response?.data?.message || 'Upload failed';
      toast.error(msg);
    } finally { setUploading(false); }
  };

  const handleDownload = async (doc) => {
    try {
      const res = await documentService.downloadFile(doc.documentId);
      const blob = new Blob([res.data]);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = doc.fileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      const msg = err.response?.data?.message || err.message || 'Download failed';
      toast.error(msg);
    }
  };

  return (
    <PageLayout
      title="My Documents"
      subtitle="Upload and manage your KYC and income documents">

      {/* Application selector */}
      {!appId && (
        <div className="card mb-6">
          <label className="label">Select Application</label>
          <select className="input-field max-w-sm"
            value={selectedApp}
            onChange={e => setSelectedApp(e.target.value)}>
            <option value="">-- Select an application --</option>
            {applications.map(a => (
              <option key={a.applicationId} value={a.applicationId}>
                {a.loanType} Loan — ₹{a.loanAmount?.toLocaleString()} ({a.status})
              </option>
            ))}
          </select>
        </div>
      )}

      {selectedApp && (
        <>
          {/* Upload Section */}
          <div className="card mb-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-5">Upload Document</h2>
            <div className="grid md:grid-cols-2 gap-6">
              <div>
                <label className="label">Document Type</label>
                <select className="input-field"
                  value={docType} onChange={e => setDocType(e.target.value)}>
                  {DOCUMENT_TYPES.map(t => (
                    <option key={t.value} value={t.value}>{t.label}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="label">File (PDF, JPG, PNG — max 5MB)</label>
                <div {...getRootProps()}
                  className={`border-2 border-dashed rounded-xl p-6 text-center cursor-pointer transition-colors
                    ${isDragActive
                      ? 'border-primary-500 bg-primary-50'
                      : file
                      ? 'border-green-400 bg-green-50'
                      : 'border-gray-300 hover:border-primary-400'}`}>
                  <input {...getInputProps()} />
                  {file ? (
                    <div>
                      <div className="text-2xl mb-2">📄</div>
                      <p className="font-medium text-green-700 text-sm">{file.name}</p>
                      <p className="text-xs text-gray-500 mt-1">{formatFileSize(file.size)}</p>
                      <button
                        onClick={e => { e.stopPropagation(); setFile(null); }}
                        className="text-xs text-red-500 mt-2 hover:underline">
                        Remove
                      </button>
                    </div>
                  ) : (
                    <div>
                      <div className="text-3xl mb-2">☁️</div>
                      <p className="text-sm text-gray-600 font-medium">
                        {isDragActive ? 'Drop file here' : 'Drag & Drop or Click to Upload'}
                      </p>
                      <p className="text-xs text-gray-400 mt-1">PDF, JPG, PNG up to 5MB</p>
                    </div>
                  )}
                </div>
              </div>
            </div>
            <div className="mt-4 flex justify-end">
              <button onClick={handleUpload}
                disabled={!file || uploading}
                className="btn-primary flex items-center gap-2">
                {uploading ? (
                  <>
                    <div className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                    Uploading...
                  </>
                ) : '📤 Upload Document'}
              </button>
            </div>
          </div>

          {/* Documents List */}
          <div className="card">
            <div className="flex justify-between items-center mb-5">
              <h2 className="text-lg font-semibold text-gray-900">Uploaded Documents</h2>
              <button onClick={fetchDocuments} className="text-sm text-primary-600 hover:underline">
                Refresh
              </button>
            </div>

            {loading ? <LoadingSpinner /> : documents.length === 0 ? (
              <div className="text-center py-10 text-gray-400">
                <div className="text-4xl mb-2">📂</div>
                <p>No documents uploaded yet</p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-100">
                      {['Document', 'File', 'Size', 'Status', 'Uploaded', 'Actions'].map(h => (
                        <th key={h} className="text-left py-3 px-3 text-xs font-medium text-gray-500 uppercase">
                          {h}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {documents.map(doc => (
                      <tr key={doc.documentId} className="hover:bg-gray-50">
                        <td className="py-3 px-3 font-medium text-gray-900">
                          {doc.documentType?.replace(/([A-Z])/g, ' $1').trim()}
                        </td>
                        <td className="py-3 px-3 text-gray-500 max-w-xs truncate">{doc.fileName}</td>
                        <td className="py-3 px-3 text-gray-500">
                          {doc.fileSizeFormatted || formatFileSize(doc.fileSizeBytes)}
                        </td>
                        <td className="py-3 px-3">
                          {doc.isReplaced ? (
                            <span className="text-xs bg-gray-100 text-gray-500 px-2 py-1 rounded-full">Replaced</span>
                          ) : doc.isVerified ? (
                            <span className="text-xs bg-green-100 text-green-700 px-2 py-1 rounded-full">✓ Verified</span>
                          ) : doc.verificationRemarks ? (
                            <span className="text-xs bg-red-100 text-red-700 px-2 py-1 rounded-full"
                              title={doc.verificationRemarks}>
                              ✗ Rejected
                            </span>
                          ) : (
                            <span className="text-xs bg-yellow-100 text-yellow-700 px-2 py-1 rounded-full">
                              Pending Review
                            </span>
                          )}
                        </td>
                        <td className="py-3 px-3 text-gray-500">{formatDate(doc.uploadedAt)}</td>
                        <td className="py-3 px-3">
                          <button onClick={() => handleDownload(doc)}
                            className="text-xs text-primary-600 hover:underline font-medium">
                            Download
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}
    </PageLayout>
  );
}

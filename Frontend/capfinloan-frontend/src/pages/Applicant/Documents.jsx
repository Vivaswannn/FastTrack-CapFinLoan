import { useState, useEffect, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useDropzone } from 'react-dropzone';
import toast from 'react-hot-toast';
import { CloudUpload, FileText, Upload, FolderOpen, CheckCircle2, XCircle, Download, RefreshCw } from 'lucide-react';
import PageLayout from '../../components/layout/PageLayout';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import { documentService } from '../../services/documentService';
import { loanService } from '../../services/loanService';
import { formatDate, formatFileSize } from '../../utils/formatters';
import { DOCUMENT_TYPES } from '../../utils/constants';

const labelCls = 'block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1.5';
const selectCls = 'w-full bg-white px-4 py-2.5 border border-slate-200 hover:border-slate-300 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 rounded-xl text-slate-800 text-sm focus:outline-none transition-all';

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

  useEffect(() => { fetchApplications(); }, []);
  useEffect(() => { if (selectedApp) fetchDocuments(); }, [selectedApp]);

  const fetchApplications = async () => {
    try {
      const res = await loanService.getMyApplications(1, 100);
      setApplications(res.data.data.items || []);
    } catch (err) {
      toast.error(err.response?.data?.message || err.message || 'Failed to load applications');
    }
  };

  const fetchDocuments = async () => {
    if (!selectedApp) return;
    setLoading(true);
    try {
      const res = await documentService.getByApplication(selectedApp);
      setDocuments(res.data.data || []);
    } catch (err) {
      toast.error(err.response?.data?.message || err.message || 'Failed to load documents');
    } finally { setLoading(false); }
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
      toast.error(err.response?.data?.message || 'Upload failed');
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
      toast.error(err.response?.data?.message || err.message || 'Download failed');
    }
  };

  return (
    <PageLayout
      title="My Documents"
      subtitle="Upload and manage your KYC and income documents">

      {/* Application selector */}
      {!appId && (
        <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-5 mb-6">
          <label className={labelCls}>Select Application</label>
          <select className={`${selectCls} max-w-sm`}
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
          <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6 mb-6">
            <h2 className="text-base font-bold text-slate-800 mb-5">Upload Document</h2>
            <div className="grid md:grid-cols-2 gap-6">
              <div>
                <label className={labelCls}>Document Type</label>
                <select className={selectCls}
                  value={docType} onChange={e => setDocType(e.target.value)}>
                  {DOCUMENT_TYPES.map(t => (
                    <option key={t.value} value={t.value}>{t.label}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className={labelCls}>File (PDF, JPG, PNG — max 5MB)</label>
                <div {...getRootProps()}
                  className={`border-2 border-dashed rounded-xl p-6 text-center cursor-pointer transition-all
                    ${isDragActive
                      ? 'border-teal-400 bg-teal-50'
                      : file
                      ? 'border-green-400 bg-green-50'
                      : 'border-slate-200 hover:border-teal-300 hover:bg-teal-50/30'}`}>
                  <input {...getInputProps()} />
                  {file ? (
                    <div>
                      <div className="flex justify-center mb-2"><FileText size={28} className="text-green-600" /></div>
                      <p className="font-semibold text-green-700 text-sm">{file.name}</p>
                      <p className="text-xs text-slate-400 mt-1">{formatFileSize(file.size)}</p>
                      <button
                        onClick={e => { e.stopPropagation(); setFile(null); }}
                        className="text-xs text-red-500 mt-2 hover:underline font-medium">
                        Remove
                      </button>
                    </div>
                  ) : (
                    <div>
                      <div className="flex justify-center mb-2">
                        <CloudUpload size={32} className={isDragActive ? 'text-teal-500' : 'text-slate-300'} />
                      </div>
                      <p className="text-sm text-slate-500 font-medium">
                        {isDragActive ? 'Drop file here' : 'Drag & Drop or Click to Upload'}
                      </p>
                      <p className="text-xs text-slate-300 mt-1">PDF, JPG, PNG up to 5MB</p>
                    </div>
                  )}
                </div>
              </div>
            </div>
            <div className="mt-5 flex justify-end">
              <button onClick={handleUpload}
                disabled={!file || uploading}
                className="inline-flex items-center gap-2 bg-teal-600 hover:bg-teal-700 text-white text-sm font-bold px-5 py-2.5 rounded-xl shadow-[0_4px_14px_rgba(20,184,166,0.25)] hover:-translate-y-0.5 transition-all duration-200 disabled:opacity-50 disabled:pointer-events-none">
                {uploading ? (
                  <>
                    <div className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                    Uploading...
                  </>
                ) : <><Upload size={15} /> Upload Document</>}
              </button>
            </div>
          </div>

          {/* Documents List */}
          <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="flex justify-between items-center px-6 py-5 border-b border-slate-100">
              <h2 className="text-base font-bold text-slate-800">Uploaded Documents</h2>
              <button onClick={fetchDocuments}
                className="inline-flex items-center gap-1.5 text-sm font-semibold text-teal-600 hover:text-teal-800 transition-colors">
                <RefreshCw size={13} /> Refresh
              </button>
            </div>

            {loading ? (
              <div className="p-8"><LoadingSpinner /></div>
            ) : documents.length === 0 ? (
              <div className="text-center py-12 text-slate-400">
                <div className="flex justify-center mb-3"><FolderOpen size={36} className="text-slate-200" /></div>
                <p className="text-sm">No documents uploaded yet</p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-slate-50/60">
                      {['Document', 'File', 'Size', 'Status', 'Uploaded', 'Actions'].map(h => (
                        <th key={h} className="text-left py-3 px-6 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                          {h}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {documents.map(doc => (
                      <tr key={doc.documentId} className="hover:bg-slate-50/50 transition-colors">
                        <td className="py-4 px-6 font-semibold text-slate-800">
                          {doc.documentType?.replace(/([A-Z])/g, ' $1').trim()}
                        </td>
                        <td className="py-4 px-6 text-slate-400 max-w-xs truncate">{doc.fileName}</td>
                        <td className="py-4 px-6 text-slate-400">
                          {doc.fileSizeFormatted || formatFileSize(doc.fileSizeBytes)}
                        </td>
                        <td className="py-4 px-6">
                          {doc.isReplaced ? (
                            <span className="text-xs bg-slate-100 text-slate-500 px-2.5 py-1 rounded-full font-medium">Replaced</span>
                          ) : doc.isVerified ? (
                            <span className="inline-flex items-center gap-1 text-xs bg-green-100 text-green-700 px-2.5 py-1 rounded-full font-semibold">
                              <CheckCircle2 size={11} /> Verified
                            </span>
                          ) : doc.verificationRemarks ? (
                            <span className="inline-flex items-center gap-1 text-xs bg-red-100 text-red-600 px-2.5 py-1 rounded-full font-semibold"
                              title={doc.verificationRemarks}>
                              <XCircle size={11} /> Rejected
                            </span>
                          ) : (
                            <span className="text-xs bg-amber-100 text-amber-700 px-2.5 py-1 rounded-full font-medium">
                              Pending
                            </span>
                          )}
                        </td>
                        <td className="py-4 px-6 text-slate-400">{formatDate(doc.uploadedAt)}</td>
                        <td className="py-4 px-6">
                          <button onClick={() => handleDownload(doc)}
                            className="inline-flex items-center gap-1.5 text-xs font-semibold text-teal-600 hover:text-teal-800 transition-colors">
                            <Download size={12} /> Download
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

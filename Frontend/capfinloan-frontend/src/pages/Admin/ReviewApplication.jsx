import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import PageLayout from '../../components/layout/PageLayout';
import StatusBadge from '../../components/common/StatusBadge';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import Modal from '../../components/common/Modal';
import { loanService } from '../../services/loanService';
import { documentService } from '../../services/documentService';
import { adminService } from '../../services/adminService';
import { formatCurrency, formatDate } from '../../utils/formatters';

const calculateEmi = (p, rate, n) => {
  if (!p || !n) return 0;
  const r = rate / 12 / 100;
  const f = Math.pow(1 + r, n);
  return Math.round((p * r * f) / (f - 1));
};

const VALID_TRANSITIONS = {
  Submitted: ['DocsPending'],
  DocsPending: ['DocsVerified'],
  DocsVerified: ['UnderReview'],
  UnderReview: ['Approved', 'Rejected'],
  Approved: ['Closed'],
  Rejected: ['Closed'],
};

export default function ReviewApplication() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [app, setApp] = useState(null);
  const [docs, setDocs] = useState([]);
  const [decision, setDecision] = useState(null);
  const [loading, setLoading] = useState(true);

  const [newStatus, setNewStatus] = useState('');
  const [statusRemarks, setStatusRemarks] = useState('');
  const [updatingStatus, setUpdatingStatus] = useState(false);

  const [decisionType, setDecisionType] = useState('Approved');
  const [decisionForm, setDecisionForm] = useState({
    loanAmountApproved: '',
    interestRate: '10.5',
    tenureMonths: '36',
    sanctionTerms: '',
    remarks: '',
  });
  const [showDecisionModal, setShowDecisionModal] = useState(false);
  const [submittingDecision, setSubmittingDecision] = useState(false);

  const [rejectRemarks, setRejectRemarks] = useState({});
  const [verifying, setVerifying] = useState({});

  useEffect(() => { fetchAll(); }, [id]);

  const fetchAll = async () => {
    setLoading(true);
    try {
      const [appRes, docsRes] = await Promise.allSettled([
        loanService.getById(id),
        documentService.getAdminDocuments(id),
      ]);
      if (appRes.status === 'fulfilled') {
        const appData = appRes.value.data.data;
        setApp(appData);
        setDecisionForm(p => ({
          ...p,
          loanAmountApproved: appData.loanAmount || '',
          tenureMonths: appData.tenureMonths || '36',
        }));
      }
      if (docsRes.status === 'fulfilled') {
        setDocs(docsRes.value.data.data || []);
      }
      try {
        const decRes = await adminService.getDecision(id);
        if (decRes.data?.data) setDecision(decRes.data.data);
      } catch (err) {
        if (err.response?.status !== 404 && err.response?.status !== 401) {
          console.warn('Decision fetch error:', err.response?.status);
        }
      }
    } catch (err) {
      const msg = err.response?.data?.message || err.message || 'Failed to load application';
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  const handleStatusUpdate = async () => {
    if (!newStatus) { toast.error('Select a status'); return; }
    setUpdatingStatus(true);
    try {
      await loanService.updateStatus(id, { newStatus, remarks: statusRemarks });
      toast.success(`Status updated to ${newStatus}`);
      setNewStatus('');
      setStatusRemarks('');
      fetchAll();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Status update failed');
    } finally {
      setUpdatingStatus(false);
    }
  };

  const handleVerify = async (docId, isVerified) => {
    const remarks = rejectRemarks[docId] || '';
    if (!isVerified && !remarks) {
      toast.error('Enter a reason for rejection');
      return;
    }
    setVerifying(p => ({ ...p, [docId]: true }));
    try {
      await documentService.verifyDocument(docId, {
        isVerified,
        verificationRemarks: remarks,
      });
      toast.success(isVerified ? 'Document verified!' : 'Document rejected');
      fetchAll();
    } catch (err) {
      const msg = err.response?.data?.message || err.message || 'Verification failed';
      toast.error(msg);
    } finally {
      setVerifying(p => ({ ...p, [docId]: false }));
    }
  };

  const handleDownload = async (doc) => {
    try {
      const res = await documentService.downloadFile(doc.documentId);
      const blob = new Blob([res.data]);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = doc.fileName;
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      const msg = err.response?.data?.message || err.message || 'Download failed';
      toast.error(msg);
    }
  };

  const handleDecisionSubmit = async () => {
    setSubmittingDecision(true);
    try {
      const payload = decisionType === 'Approved' ? {
        decisionType: 'Approved',
        remarks: decisionForm.remarks || 'Approved',
        sanctionTerms: decisionForm.sanctionTerms,
        loanAmountApproved: Number(decisionForm.loanAmountApproved),
        interestRate: Number(decisionForm.interestRate),
        tenureMonths: Number(decisionForm.tenureMonths),
      } : {
        decisionType: 'Rejected',
        remarks: decisionForm.remarks,
      };
      await adminService.makeDecision(id, payload);
      toast.success(`Application ${decisionType.toLowerCase()} successfully!`);
      setShowDecisionModal(false);
      fetchAll();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Decision submission failed');
    } finally {
      setSubmittingDecision(false);
    }
  };

  const emi = calculateEmi(
    Number(decisionForm.loanAmountApproved),
    Number(decisionForm.interestRate),
    Number(decisionForm.tenureMonths)
  );

  const validNextStatuses = app ? VALID_TRANSITIONS[app.status] || [] : [];

  if (loading) return <PageLayout title="Review Application"><LoadingSpinner /></PageLayout>;
  if (!app) return <PageLayout title="Not Found"><p className="text-gray-500">Application not found.</p></PageLayout>;

  return (
    <PageLayout
      title="Review Application"
      subtitle={`ID: ${id.slice(0, 8)}...`}
      action={
        <button onClick={() => navigate('/admin/applications')} className="btn-secondary text-sm">
          ← Back to Queue
        </button>
      }>

      <div className="grid lg:grid-cols-3 gap-6">

        {/* Column 1 — Applicant Details */}
        <div className="space-y-4">
          <div className="card">
            <h3 className="font-semibold text-gray-900 mb-4">Applicant Information</h3>
            <div className="space-y-2 text-sm">
              {[
                ['Full Name', app.fullName],
                ['Email', app.email],
                ['Phone', app.phone],
                ['Date of Birth', formatDate(app.dateOfBirth)],
                ['Address', app.address || '—'],
              ].map(([k, v]) => (
                <div key={k} className="flex justify-between py-1.5 border-b border-gray-50 last:border-0">
                  <span className="text-gray-500">{k}</span>
                  <span className="font-medium text-gray-900 text-right max-w-[180px] break-words">{v}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="card">
            <h3 className="font-semibold text-gray-900 mb-4">Employment Details</h3>
            <div className="space-y-2 text-sm">
              {[
                ['Type', app.employmentType],
                ['Employer', app.employerName || '—'],
                ['Job Title', app.jobTitle || '—'],
                ['Monthly Income', formatCurrency(app.monthlyIncome)],
                ['Experience', `${app.yearsOfExperience || 0} years`],
              ].map(([k, v]) => (
                <div key={k} className="flex justify-between py-1.5 border-b border-gray-50 last:border-0">
                  <span className="text-gray-500">{k}</span>
                  <span className="font-medium text-gray-900 text-right">{v}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="card">
            <h3 className="font-semibold text-gray-900 mb-4">Loan Details</h3>
            <div className="space-y-2 text-sm">
              {[
                ['Loan Type', app.loanType],
                ['Amount', formatCurrency(app.loanAmount)],
                ['Tenure', `${app.tenureMonths} months`],
                ['Purpose', app.purpose || '—'],
                ['Applied', formatDate(app.createdAt)],
                ['Submitted', formatDate(app.submittedAt)],
              ].map(([k, v]) => (
                <div key={k} className="flex justify-between py-1.5 border-b border-gray-50 last:border-0">
                  <span className="text-gray-500">{k}</span>
                  <span className="font-medium text-gray-900 text-right">{v}</span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Column 2 — Documents */}
        <div className="card h-fit">
          <h3 className="font-semibold text-gray-900 mb-4">Documents ({docs.length})</h3>
          {docs.length === 0 ? (
            <p className="text-gray-400 text-sm text-center py-6">No documents uploaded</p>
          ) : (
            <div className="space-y-4">
              {docs.map(doc => (
                <div key={doc.documentId} className="border border-gray-100 rounded-xl p-4">
                  <div className="flex justify-between items-start mb-2">
                    <div>
                      <div className="font-medium text-gray-900 text-sm">
                        {doc.documentType?.replace(/([A-Z])/g, ' $1').trim()}
                      </div>
                      <div className="text-xs text-gray-400 mt-0.5">{doc.fileName}</div>
                    </div>
                    {doc.isVerified ? (
                      <span className="text-xs bg-green-100 text-green-700 px-2 py-1 rounded-full">✓ Verified</span>
                    ) : doc.verificationRemarks ? (
                      <span className="text-xs bg-red-100 text-red-700 px-2 py-1 rounded-full">✗ Rejected</span>
                    ) : (
                      <span className="text-xs bg-yellow-100 text-yellow-700 px-2 py-1 rounded-full">Pending</span>
                    )}
                  </div>

                  {doc.verificationRemarks && !doc.isVerified && (
                    <p className="text-xs text-red-600 mb-2 bg-red-50 rounded px-2 py-1">
                      {doc.verificationRemarks}
                    </p>
                  )}

                  <div className="flex gap-2 mt-3">
                    <button onClick={() => handleDownload(doc)}
                      className="text-xs text-primary-600 hover:underline">
                      Download
                    </button>
                    {!doc.isVerified && (
                      <>
                        <button
                          disabled={verifying[doc.documentId]}
                          onClick={() => handleVerify(doc.documentId, true)}
                          className="text-xs bg-green-600 text-white px-2 py-1 rounded hover:bg-green-700 disabled:opacity-50">
                          Verify
                        </button>
                        <button
                          disabled={verifying[doc.documentId]}
                          onClick={() => handleVerify(doc.documentId, false)}
                          className="text-xs bg-red-600 text-white px-2 py-1 rounded hover:bg-red-700 disabled:opacity-50">
                          Reject
                        </button>
                      </>
                    )}
                  </div>

                  {!doc.isVerified && (
                    <input
                      className="input-field text-xs mt-2"
                      placeholder="Rejection reason..."
                      value={rejectRemarks[doc.documentId] || ''}
                      onChange={e => setRejectRemarks(p => ({ ...p, [doc.documentId]: e.target.value }))} />
                  )}
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Column 3 — Status + Decision */}
        <div className="space-y-4">

          {/* Current Status */}
          <div className="card">
            <h3 className="font-semibold text-gray-900 mb-3">Current Status</h3>
            <div className="flex items-center gap-3">
              <StatusBadge status={app.status} />
              <span className="text-sm text-gray-500">{formatDate(app.submittedAt)}</span>
            </div>
          </div>

          {/* Update Status */}
          {validNextStatuses.length > 0 && !decision && (
            <div className="card">
              <h3 className="font-semibold text-gray-900 mb-4">Update Status</h3>
              <div className="space-y-3">
                <div>
                  <label className="label">New Status</label>
                  <select className="input-field" value={newStatus}
                    onChange={e => setNewStatus(e.target.value)}>
                    <option value="">Select status...</option>
                    {validNextStatuses.map(s => <option key={s} value={s}>{s}</option>)}
                  </select>
                </div>
                <div>
                  <label className="label">Remarks</label>
                  <textarea className="input-field" rows={2}
                    placeholder="Add notes..."
                    value={statusRemarks}
                    onChange={e => setStatusRemarks(e.target.value)} />
                </div>
                <button
                  onClick={handleStatusUpdate}
                  disabled={updatingStatus || !newStatus}
                  className="btn-primary w-full flex items-center justify-center gap-2">
                  {updatingStatus && (
                    <div className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                  )}
                  Update Status
                </button>
              </div>
            </div>
          )}

          {/* Decision Form */}
          {app.status === 'UnderReview' && !decision && (
            <div className="card border-2 border-primary-100">
              <h3 className="font-semibold text-gray-900 mb-4">Make Decision</h3>

              <div className="flex rounded-xl overflow-hidden border border-gray-200 mb-4">
                {['Approved', 'Rejected'].map(type => (
                  <button key={type}
                    onClick={() => setDecisionType(type)}
                    className={`flex-1 py-2.5 text-sm font-medium transition-colors ${
                      decisionType === type
                        ? type === 'Approved' ? 'bg-green-600 text-white' : 'bg-red-600 text-white'
                        : 'bg-white text-gray-600'}`}>
                    {type === 'Approved' ? '✅ Approve' : '❌ Reject'}
                  </button>
                ))}
              </div>

              {decisionType === 'Approved' ? (
                <div className="space-y-3">
                  <div>
                    <label className="label">Approved Amount (₹)</label>
                    <input type="number" className="input-field"
                      value={decisionForm.loanAmountApproved}
                      onChange={e => setDecisionForm(p => ({ ...p, loanAmountApproved: e.target.value }))} />
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="label">Interest Rate (% p.a.)</label>
                      <input type="number" step="0.1" min="1" max="36" className="input-field"
                        value={decisionForm.interestRate}
                        onChange={e => setDecisionForm(p => ({ ...p, interestRate: e.target.value }))} />
                    </div>
                    <div>
                      <label className="label">Tenure (months)</label>
                      <input type="number" min="6" max="360" className="input-field"
                        value={decisionForm.tenureMonths}
                        onChange={e => setDecisionForm(p => ({ ...p, tenureMonths: e.target.value }))} />
                    </div>
                  </div>

                  {decisionForm.loanAmountApproved && (
                    <div className="bg-green-50 rounded-xl p-3 text-center">
                      <div className="text-xl font-bold text-green-700">{formatCurrency(emi)}/month</div>
                      <div className="text-xs text-green-600 mt-1">Estimated Monthly EMI</div>
                    </div>
                  )}

                  <div>
                    <label className="label">Sanction Terms *</label>
                    <textarea className="input-field" rows={3}
                      placeholder="Enter loan sanction terms..."
                      value={decisionForm.sanctionTerms}
                      onChange={e => setDecisionForm(p => ({ ...p, sanctionTerms: e.target.value }))} />
                  </div>
                  <div>
                    <label className="label">Remarks</label>
                    <input className="input-field" placeholder="Additional notes..."
                      value={decisionForm.remarks}
                      onChange={e => setDecisionForm(p => ({ ...p, remarks: e.target.value }))} />
                  </div>
                </div>
              ) : (
                <div>
                  <label className="label">
                    Rejection Reason *
                    <span className="text-gray-400 font-normal ml-1">(min 10 characters)</span>
                  </label>
                  <textarea className="input-field" rows={4}
                    placeholder="Explain why this application is being rejected..."
                    value={decisionForm.remarks}
                    onChange={e => setDecisionForm(p => ({ ...p, remarks: e.target.value }))} />
                </div>
              )}

              <button
                onClick={() => {
                  if (decisionType === 'Approved' && !decisionForm.sanctionTerms.trim()) {
                    toast.error('Please enter sanction terms');
                    return;
                  }
                  if (decisionType === 'Rejected' && (!decisionForm.remarks || decisionForm.remarks.trim().length < 10)) {
                    toast.error('Please enter a rejection reason (min 10 characters)');
                    return;
                  }
                  setShowDecisionModal(true);
                }}
                className={`w-full py-3 rounded-xl font-medium mt-4 transition-colors ${
                  decisionType === 'Approved'
                    ? 'bg-green-600 hover:bg-green-700 text-white'
                    : 'bg-red-600 hover:bg-red-700 text-white'}`}>
                Submit Decision
              </button>
            </div>
          )}

          {/* Existing Decision */}
          {decision && (
            <div className={`card border-l-4 ${decision.decisionType === 'Approved' ? 'border-green-500' : 'border-red-500'}`}>
              <h3 className="font-semibold text-gray-900 mb-3">Decision Made</h3>
              <div className="space-y-2 text-sm">
                <div className={`text-lg font-bold ${decision.decisionType === 'Approved' ? 'text-green-700' : 'text-red-700'}`}>
                  {decision.decisionType === 'Approved' ? '✅ Approved' : '❌ Rejected'}
                </div>
                {decision.decisionType === 'Approved' && (
                  <>
                    <div className="flex justify-between">
                      <span className="text-gray-500">Amount</span>
                      <span className="font-medium">{formatCurrency(decision.loanAmountApproved)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-500">EMI</span>
                      <span className="font-medium">{formatCurrency(decision.monthlyEmi)}/mo</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-500">Rate</span>
                      <span className="font-medium">{decision.interestRate}% p.a.</span>
                    </div>
                  </>
                )}
                <div className="pt-2 text-xs text-gray-500">
                  By {decision.decidedBy} on {formatDate(decision.decidedAt)}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Confirmation Modal */}
      <Modal
        isOpen={showDecisionModal}
        onClose={() => setShowDecisionModal(false)}
        title={`Confirm ${decisionType}`}>
        <p className="text-gray-600 mb-2">
          Are you sure you want to <strong>{decisionType.toLowerCase()}</strong> this application?
          This action cannot be undone.
        </p>
        {decisionType === 'Approved' && (
          <div className="bg-green-50 rounded-xl p-3 mb-4 text-sm">
            <div className="font-medium text-green-800">
              Approving: {formatCurrency(Number(decisionForm.loanAmountApproved))}
            </div>
            <div className="text-green-600 mt-1">
              EMI: {formatCurrency(emi)}/month at {decisionForm.interestRate}% for {decisionForm.tenureMonths} months
            </div>
          </div>
        )}
        <div className="flex gap-3 justify-end mt-4">
          <button onClick={() => setShowDecisionModal(false)} className="btn-secondary">Cancel</button>
          <button
            onClick={handleDecisionSubmit}
            disabled={submittingDecision}
            className={`px-4 py-2 rounded-lg font-medium text-white flex items-center gap-2 transition-colors ${
              decisionType === 'Approved' ? 'bg-green-600 hover:bg-green-700' : 'bg-red-600 hover:bg-red-700'}`}>
            {submittingDecision && (
              <div className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />
            )}
            Confirm {decisionType}
          </button>
        </div>
      </Modal>

    </PageLayout>
  );
}

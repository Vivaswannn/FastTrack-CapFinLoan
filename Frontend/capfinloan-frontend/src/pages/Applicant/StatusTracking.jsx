import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import { CheckCircle2, XCircle } from 'lucide-react';
import PageLayout from '../../components/layout/PageLayout';
import StatusBadge from '../../components/common/StatusBadge';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import { loanService } from '../../services/loanService';
import { adminService } from '../../services/adminService';
import { formatCurrency, formatDate, formatDateTime } from '../../utils/formatters';

const statusColors = {
  Draft: 'bg-gray-400',
  Submitted: 'bg-blue-500',
  DocsPending: 'bg-yellow-500',
  DocsVerified: 'bg-indigo-500',
  UnderReview: 'bg-purple-500',
  Approved: 'bg-green-500',
  Rejected: 'bg-red-500',
  Closed: 'bg-gray-500',
};

export default function StatusTracking() {
  const { id } = useParams();
  const [application, setApplication] = useState(null);
  const [history, setHistory] = useState([]);
  const [decision, setDecision] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => { fetchAll(); }, [id]);

  const fetchAll = async () => {
    setLoading(true);
    try {
      const [appRes, histRes] = await Promise.allSettled([
        loanService.getById(id),
        loanService.getStatusHistory(id),
      ]);
      if (appRes.status === 'fulfilled') {
        setApplication(appRes.value.data.data);
      }
      if (histRes.status === 'fulfilled') {
        setHistory(histRes.value.data.data || []);
      }

      try {
        const decRes = await adminService.getDecision(id);
        if (decRes.data?.data) setDecision(decRes.data.data);
      } catch (err) {
        // 404 = no decision yet (expected), any other error = log it
        if (err.response?.status !== 404 && err.response?.status !== 401) {
          console.warn('Decision fetch error:', err.response?.status);
        }
      }
    } catch (err) {
      const msg = err.response?.data?.message || err.message || 'Failed to load application details';
      toast.error(msg);
    } finally { setLoading(false); }
  };

  if (loading) return (
    <PageLayout title="Tracking Status"><LoadingSpinner /></PageLayout>
  );

  if (!application) return (
    <PageLayout title="Not Found">
      <div className="text-center py-16">
        <p className="text-gray-500">Application not found.</p>
        <Link to="/applicant/dashboard" className="text-primary-600 hover:underline mt-4 block">
          Back to Dashboard
        </Link>
      </div>
    </PageLayout>
  );

  return (
    <PageLayout
      title="Application Status"
      subtitle={`Tracking: ${id.slice(0, 8)}...`}
      action={
        <Link to="/applicant/dashboard" className="btn-secondary text-sm">← Back</Link>
      }>

      <div className="grid lg:grid-cols-5 gap-6">

        {/* Left — Application Details (2/5) */}
        <div className="lg:col-span-2 space-y-4">
          {/* Summary Card */}
          <div className="card">
            <div className="flex justify-between items-start mb-4">
              <h2 className="font-semibold text-gray-900">Application Summary</h2>
              <StatusBadge status={application.status} />
            </div>
            <div className="space-y-3 text-sm">
              {[
                ['Loan Type', application.loanType],
                ['Requested Amount', formatCurrency(application.loanAmount)],
                ['Tenure', `${application.tenureMonths} months`],
                ['Applied On', formatDate(application.createdAt)],
                ['Submitted On', formatDate(application.submittedAt)],
                ['Applicant', application.fullName],
                ['Purpose', application.purpose || '—'],
              ].map(([k, v]) => (
                <div key={k} className="flex justify-between py-2 border-b border-gray-50 last:border-0">
                  <span className="text-gray-500">{k}</span>
                  <span className="font-medium text-gray-900 text-right">{v}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Decision Card */}
          {decision && (
            <div className={`card border-l-4 ${decision.decisionType === 'Approved' ? 'border-green-500' : 'border-red-500'}`}>
              <div className="flex items-center gap-2 mb-4">
                {decision.decisionType === 'Approved'
                  ? <CheckCircle2 size={22} className="text-green-500" />
                  : <XCircle size={22} className="text-red-500" />}
                <h2 className="font-semibold text-gray-900">Loan {decision.decisionType}</h2>
              </div>
              {decision.decisionType === 'Approved' ? (
                <div className="space-y-3 text-sm">
                  <div className="bg-green-50 rounded-lg p-3 text-center mb-3">
                    <div className="text-2xl font-bold text-green-700">
                      {formatCurrency(decision.loanAmountApproved)}
                    </div>
                    <div className="text-xs text-green-600 mt-1">Approved Amount</div>
                  </div>
                  {[
                    ['Monthly EMI', formatCurrency(decision.monthlyEmi)],
                    ['Interest Rate', `${decision.interestRate}% p.a.`],
                    ['Tenure', `${decision.tenureMonths} months`],
                    ['Decided By', decision.decidedBy],
                    ['Decision Date', formatDate(decision.decidedAt)],
                  ].map(([k, v]) => (
                    <div key={k} className="flex justify-between text-sm">
                      <span className="text-gray-500">{k}</span>
                      <span className="font-medium">{v}</span>
                    </div>
                  ))}
                  {decision.sanctionTerms && (
                    <div className="mt-3 pt-3 border-t border-gray-100">
                      <p className="text-xs text-gray-500 mb-1">Sanction Terms</p>
                      <p className="text-sm text-gray-700">{decision.sanctionTerms}</p>
                    </div>
                  )}
                </div>
              ) : (
                <div>
                  <p className="text-sm text-gray-600 mb-2">Reason for rejection:</p>
                  <p className="text-sm text-red-700 bg-red-50 rounded-lg p-3">{decision.remarks}</p>
                  <p className="text-xs text-gray-500 mt-3">
                    Decided by {decision.decidedBy} on {formatDate(decision.decidedAt)}
                  </p>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Right — Status Timeline (3/5) */}
        <div className="lg:col-span-3">
          <div className="card">
            <h2 className="font-semibold text-gray-900 mb-6">Status Timeline</h2>
            {history.length === 0 ? (
              <p className="text-gray-400 text-center py-8">No history available</p>
            ) : (
              <div className="relative">
                <div className="absolute left-4 top-0 bottom-0 w-0.5 bg-gray-100" />
                <div className="space-y-0">
                  {history.map((item, i) => {
                    const color = statusColors[item.toStatus] || 'bg-gray-400';
                    const isLast = i === history.length - 1;
                    return (
                      <div key={item.historyId} className="relative flex gap-4 pb-6 last:pb-0">
                        <div className={`relative z-10 w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 ${color} shadow-sm`}>
                          <span className="text-white text-xs font-bold">{i + 1}</span>
                        </div>
                        <div className={`flex-1 pb-6 ${isLast ? '' : 'border-b border-gray-50'}`}>
                          <div className="flex justify-between items-start">
                            <div>
                              <div className="font-semibold text-gray-900 text-sm">
                                {item.toStatus?.replace(/([A-Z])/g, ' $1').trim()}
                              </div>
                              {item.fromStatus !== item.toStatus && (
                                <div className="text-xs text-gray-400 mt-0.5">
                                  From: {item.fromStatus}
                                </div>
                              )}
                            </div>
                            <div className="text-right">
                              <div className="text-xs text-gray-500">{formatDateTime(item.changedAt)}</div>
                              <div className="text-xs text-gray-400 mt-0.5">by {item.changedBy}</div>
                            </div>
                          </div>
                          {item.remarks && (
                            <div className="mt-2 text-xs text-gray-500 bg-gray-50 rounded-lg px-3 py-2">
                              {item.remarks}
                            </div>
                          )}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </PageLayout>
  );
}

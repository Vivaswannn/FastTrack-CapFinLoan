import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { FileText, Clock, CheckCircle2, XCircle, Plus, ArrowRight, Sparkles } from 'lucide-react';
import PageLayout from '../../components/layout/PageLayout';
import StatusBadge from '../../components/common/StatusBadge';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import EmptyState from '../../components/common/EmptyState';
import Pagination from '../../components/common/Pagination';
import { loanService } from '../../services/loanService';
import { formatCurrency, formatDate } from '../../utils/formatters';
import { useAuth } from '../../context/AuthContext';

const getGreeting = () => {
  const h = new Date().getHours();
  if (h < 12) return 'Good morning';
  if (h < 17) return 'Good afternoon';
  return 'Good evening';
};

export default function ApplicantDashboard() {
  const [applications, setApplications] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [stats, setStats] = useState({ total: 0, pending: 0, approved: 0, rejected: 0 });
  const { user } = useAuth();
  const navigate = useNavigate();

  useEffect(() => { fetchApplications(); }, [page]);

  const fetchApplications = async () => {
    setLoading(true);
    try {
      const res = await loanService.getMyApplications(page, 10);
      const data = res.data.data;
      setApplications(data.items || []);
      setTotalPages(data.totalPages || 1);
      const all = data.items || [];
      setStats({
        total: data.totalCount || 0,
        pending: all.filter(a =>
          ['Submitted', 'DocsPending', 'DocsVerified', 'UnderReview'].includes(a.status)).length,
        approved: all.filter(a => a.status === 'Approved').length,
        rejected: all.filter(a => a.status === 'Rejected').length,
      });
    } catch (err) {
      toast.error(err.message || 'Failed to load applications');
      setApplications([]);
    } finally { setLoading(false); }
  };

  const kpiCards = [
    {
      label: 'Total Applications', value: stats.total,
      border: 'border-blue-200', iconBg: 'bg-blue-50', iconColor: 'text-blue-500',
      numColor: 'text-blue-700', icon: <FileText size={18} />,
    },
    {
      label: 'In Progress', value: stats.pending,
      border: 'border-amber-200', iconBg: 'bg-amber-50', iconColor: 'text-amber-500',
      numColor: 'text-amber-700', icon: <Clock size={18} />,
    },
    {
      label: 'Approved', value: stats.approved,
      border: 'border-teal-200', iconBg: 'bg-teal-50', iconColor: 'text-teal-600',
      numColor: 'text-teal-700', icon: <CheckCircle2 size={18} />,
    },
    {
      label: 'Rejected', value: stats.rejected,
      border: 'border-red-200', iconBg: 'bg-red-50', iconColor: 'text-red-400',
      numColor: 'text-red-700', icon: <XCircle size={18} />,
    },
  ];

  return (
    <PageLayout
      title={`Welcome, ${user?.fullName?.split(' ')[0]}`}
      subtitle="Manage and track all your loan applications"
      action={
        <Link to="/applicant/apply"
          className="inline-flex items-center gap-2 bg-teal-600 hover:bg-teal-700 text-white text-sm font-bold px-5 py-2.5 rounded-xl shadow-[0_4px_14px_rgba(20,184,166,0.25)] hover:shadow-[0_6px_20px_rgba(20,184,166,0.35)] hover:-translate-y-0.5 transition-all duration-200">
          <Plus size={15} />
          Apply for Loan
        </Link>
      }>

      {/* Welcome Hero Banner */}
      <div className="relative bg-gradient-to-br from-teal-600 via-teal-600 to-teal-700 rounded-2xl p-6 mb-6 overflow-hidden shadow-lg">
        <div className="absolute top-0 right-0 w-64 h-64 bg-white/5 rounded-full -translate-y-1/2 translate-x-1/4" />
        <div className="absolute bottom-0 left-1/3 w-40 h-40 bg-white/5 rounded-full translate-y-1/2" />
        <div className="relative z-10 flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <Sparkles size={14} className="text-teal-200" />
              <p className="text-teal-100 text-sm font-medium">{getGreeting()}, {user?.fullName?.split(' ')[0]}!</p>
            </div>
            <h2 className="text-white font-extrabold text-2xl tracking-tight leading-tight">
              Your Loan Journey
            </h2>
            <p className="text-teal-100/75 text-sm mt-1 max-w-xs">
              Track applications, upload documents, and stay updated on every step.
            </p>
          </div>
          <Link to="/applicant/apply"
            className="inline-flex items-center gap-2 bg-white text-teal-700 hover:bg-teal-50 text-sm font-bold px-5 py-2.5 rounded-xl shadow-sm hover:-translate-y-0.5 transition-all duration-200 shrink-0">
            <Plus size={15} />
            New Application
          </Link>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {kpiCards.map(card => (
          <div key={card.label}
            className={`bg-white rounded-xl border ${card.border} p-5 shadow-sm`}>
            <div className="flex justify-between items-start mb-3">
              <div className={`w-9 h-9 ${card.iconBg} rounded-lg flex items-center justify-center`}>
                <span className={card.iconColor}>{card.icon}</span>
              </div>
            </div>
            <div className={`text-2xl font-bold ${card.numColor} mb-0.5`}>{card.value}</div>
            <div className="text-sm text-slate-500">{card.label}</div>
          </div>
        ))}
      </div>

      {/* Applications Table */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="flex justify-between items-center px-6 py-5 border-b border-slate-100">
          <h2 className="text-base font-bold text-slate-800">My Applications</h2>
          <button onClick={fetchApplications}
            className="text-sm font-semibold text-teal-600 hover:text-teal-800 transition-colors">
            Refresh
          </button>
        </div>

        {loading ? <div className="p-8"><LoadingSpinner /></div> : applications.length === 0 ? (
          <EmptyState
            title="No applications yet"
            description="Start your loan journey today"
            action={
              <Link to="/applicant/apply"
                className="inline-flex items-center gap-2 bg-teal-600 hover:bg-teal-700 text-white text-sm font-bold px-5 py-2.5 rounded-xl shadow-[0_4px_14px_rgba(20,184,166,0.25)] hover:-translate-y-0.5 transition-all duration-200">
                <Plus size={15} /> Apply for Loan
              </Link>
            } />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-slate-50/60">
                    {['Loan Type', 'Amount', 'Status', 'Applied', 'Actions'].map(h => (
                      <th key={h} className="text-left py-3 px-6 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                        {h}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {applications.map(app => (
                    <tr key={app.applicationId} className="hover:bg-slate-50/50 transition-colors">
                      <td className="py-4 px-6">
                        <div className="font-semibold text-slate-800">{app.loanType}</div>
                        <div className="text-xs text-slate-400 font-mono mt-0.5">{app.applicationId.slice(0, 8)}...</div>
                      </td>
                      <td className="py-4 px-6 font-bold text-slate-800">
                        {formatCurrency(app.loanAmount)}
                      </td>
                      <td className="py-4 px-6"><StatusBadge status={app.status} /></td>
                      <td className="py-4 px-6 text-slate-400">{formatDate(app.createdAt)}</td>
                      <td className="py-4 px-6">
                        <div className="flex gap-3">
                          <button
                            onClick={() => navigate(`/applicant/status/${app.applicationId}`)}
                            className="inline-flex items-center gap-1 text-xs font-bold text-teal-600 hover:text-teal-800 transition-colors">
                            Track <ArrowRight size={11} />
                          </button>
                          <button
                            onClick={() => navigate(`/applicant/documents?appId=${app.applicationId}`)}
                            className="text-xs font-medium text-slate-400 hover:text-slate-600 transition-colors">
                            Docs
                          </button>
                          {app.status === 'Draft' && (
                            <button
                              onClick={() => navigate(`/applicant/apply/${app.applicationId}`)}
                              className="text-xs font-bold text-amber-600 hover:text-amber-800 transition-colors">
                              Continue
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="px-6 py-3 border-t border-slate-100">
              <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
            </div>
          </>
        )}
      </div>
    </PageLayout>
  );
}

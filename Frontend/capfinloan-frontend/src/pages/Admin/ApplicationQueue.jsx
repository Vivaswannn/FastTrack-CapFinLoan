import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { Search, RefreshCw } from 'lucide-react';
import PageLayout from '../../components/layout/PageLayout';
import StatusBadge from '../../components/common/StatusBadge';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import Pagination from '../../components/common/Pagination';
import EmptyState from '../../components/common/EmptyState';
import { loanService } from '../../services/loanService';
import { formatCurrency, formatDate } from '../../utils/formatters';

const STATUSES = [
  'All', 'Submitted', 'DocsPending', 'DocsVerified',
  'UnderReview', 'Approved', 'Rejected', 'Closed',
];

const STATUS_PILL = {
  All:          'bg-slate-100 text-slate-600',
  Submitted:    'bg-blue-100 text-blue-700',
  DocsPending:  'bg-amber-100 text-amber-700',
  DocsVerified: 'bg-indigo-100 text-indigo-700',
  UnderReview:  'bg-purple-100 text-purple-700',
  Approved:     'bg-teal-100 text-teal-700',
  Rejected:     'bg-red-100 text-red-700',
  Closed:       'bg-slate-100 text-slate-500',
};

const getDaysPending = (submittedAt) => {
  if (!submittedAt) return null;
  return Math.floor((new Date() - new Date(submittedAt)) / (1000 * 60 * 60 * 24));
};

export default function ApplicationQueue() {
  const [applications, setApplications] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [statusFilter, setStatusFilter] = useState('All');
  const [search, setSearch] = useState('');
  const [pageSize, setPageSize] = useState(10);
  const navigate = useNavigate();

  useEffect(() => { setPage(1); }, [statusFilter, pageSize]);
  useEffect(() => { fetchApplications(); }, [page, statusFilter, pageSize]);

  const fetchApplications = async () => {
    setLoading(true);
    try {
      const status = statusFilter === 'All' ? null : statusFilter;
      const res = await loanService.getAllApplications(page, pageSize, status);
      const data = res.data.data;
      setApplications(data.items || []);
      setTotalPages(data.totalPages || 1);
      setTotalCount(data.totalCount || 0);
    } catch (err) {
      toast.error(err.response?.data?.message || err.message || 'Failed to load applications');
    } finally {
      setLoading(false);
    }
  };

  const filtered = applications.filter(app => {
    if (!search) return true;
    const q = search.toLowerCase();
    return (
      app.fullName?.toLowerCase().includes(q) ||
      app.email?.toLowerCase().includes(q) ||
      app.applicationId?.toLowerCase().includes(q)
    );
  });

  return (
    <PageLayout
      title="Application Queue"
      subtitle="Review and manage all loan applications">

      {/* Filters */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-5 mb-6">
        <div className="flex flex-wrap gap-4 items-end">
          <div className="flex-1 min-w-48">
            <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1.5">Search</label>
            <div className="relative">
              <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-300" />
              <input
                className="w-full bg-white pl-9 pr-4 py-2.5 border border-slate-200 hover:border-slate-300 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 rounded-xl text-slate-800 text-sm placeholder:text-slate-300 focus:outline-none transition-all"
                placeholder="Search by name or email..."
                value={search}
                onChange={e => setSearch(e.target.value)} />
            </div>
          </div>
          <div>
            <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1.5">Per Page</label>
            <select
              className="bg-white px-4 py-2.5 border border-slate-200 hover:border-slate-300 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 rounded-xl text-slate-700 text-sm focus:outline-none transition-all"
              value={pageSize}
              onChange={e => setPageSize(Number(e.target.value))}>
              {[10, 25, 50].map(n => <option key={n} value={n}>{n}</option>)}
            </select>
          </div>
          <button onClick={fetchApplications}
            className="inline-flex items-center gap-2 bg-teal-600 hover:bg-teal-700 text-white text-sm font-bold px-4 py-2.5 rounded-xl shadow-sm hover:-translate-y-0.5 transition-all duration-200">
            <RefreshCw size={14} /> Refresh
          </button>
        </div>

        {/* Status filter pills */}
        <div className="flex flex-wrap gap-2 mt-4">
          {STATUSES.map(s => (
            <button key={s} onClick={() => setStatusFilter(s)}
              className={`text-xs font-semibold px-3 py-1.5 rounded-full transition-all ${
                statusFilter === s
                  ? 'bg-teal-600 text-white shadow-sm'
                  : `${STATUS_PILL[s]} hover:opacity-80`
              }`}>
              {s}
            </button>
          ))}
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="flex justify-between items-center px-6 py-4 border-b border-slate-100">
          <p className="text-sm font-medium text-slate-500">
            {totalCount > 0
              ? <>Showing <span className="font-bold text-slate-700">{((page - 1) * pageSize) + 1}–{Math.min(page * pageSize, totalCount)}</span> of <span className="font-bold text-slate-700">{totalCount}</span> applications</>
              : 'No applications found'}
          </p>
        </div>

        {loading ? (
          <div className="p-8"><LoadingSpinner /></div>
        ) : filtered.length === 0 ? (
          <EmptyState title="No applications found" description="Try adjusting your filters" />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-slate-50/60">
                    {['Applicant', 'Loan', 'Status', 'Submitted', 'Days Pending', 'Action'].map(h => (
                      <th key={h} className="text-left py-3 px-6 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                        {h}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {filtered.map(app => {
                    const days = getDaysPending(app.submittedAt);
                    return (
                      <tr key={app.applicationId} className="hover:bg-slate-50/50 transition-colors">
                        <td className="py-4 px-6">
                          <div className="font-semibold text-slate-800">{app.fullName || '—'}</div>
                          <div className="text-xs text-slate-400 mt-0.5">{app.email}</div>
                        </td>
                        <td className="py-4 px-6">
                          <div className="font-semibold text-slate-800">{app.loanType}</div>
                          <div className="text-xs text-slate-400 mt-0.5">{formatCurrency(app.loanAmount)}</div>
                        </td>
                        <td className="py-4 px-6">
                          <StatusBadge status={app.status} />
                        </td>
                        <td className="py-4 px-6 text-slate-400">
                          {formatDate(app.submittedAt) || '—'}
                        </td>
                        <td className="py-4 px-6">
                          {days !== null ? (
                            <span className={`font-semibold ${
                              days > 14 ? 'text-red-500' :
                              days > 7  ? 'text-amber-500' :
                                          'text-slate-500'}`}>
                              {days}d
                            </span>
                          ) : <span className="text-slate-300">—</span>}
                        </td>
                        <td className="py-4 px-6">
                          <button
                            onClick={() => navigate(`/admin/review/${app.applicationId}`)}
                            className="text-xs bg-teal-600 hover:bg-teal-700 text-white font-bold px-3 py-1.5 rounded-lg transition-colors shadow-sm">
                            Review →
                          </button>
                        </td>
                      </tr>
                    );
                  })}
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

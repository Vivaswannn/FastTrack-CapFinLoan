import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
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

  useEffect(() => {
    setPage(1);
  }, [statusFilter, pageSize]);

  useEffect(() => {
    fetchApplications();
  }, [page, statusFilter, pageSize]);

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
      const msg = err.response?.data?.message || err.message || 'Failed to load applications';
      toast.error(msg);
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
      <div className="card mb-6">
        <div className="flex flex-wrap gap-4 items-end">
          <div className="flex-1 min-w-48">
            <label className="label">Search</label>
            <input
              className="input-field"
              placeholder="Search by name or email..."
              value={search}
              onChange={e => setSearch(e.target.value)} />
          </div>
          <div>
            <label className="label">Status</label>
            <select className="input-field" value={statusFilter}
              onChange={e => setStatusFilter(e.target.value)}>
              {STATUSES.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>
          <div>
            <label className="label">Per Page</label>
            <select className="input-field" value={pageSize}
              onChange={e => setPageSize(Number(e.target.value))}>
              {[10, 25, 50].map(n => <option key={n} value={n}>{n}</option>)}
            </select>
          </div>
          <button onClick={fetchApplications} className="btn-primary h-10">
            Refresh
          </button>
        </div>
      </div>

      {/* Table */}
      <div className="card">
        <div className="flex justify-between items-center mb-4">
          <p className="text-sm text-gray-500">
            Showing {((page - 1) * pageSize) + 1}–{Math.min(page * pageSize, totalCount)} of {totalCount}
          </p>
        </div>

        {loading ? <LoadingSpinner /> :
          filtered.length === 0 ? (
            <EmptyState
              title="No applications found"
              description="Try adjusting your filters" />
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-100">
                      {['Applicant', 'Loan', 'Status', 'Submitted', 'Days Pending', 'Action'].map(h => (
                        <th key={h} className="text-left py-3 px-4 text-xs font-medium text-gray-500 uppercase">
                          {h}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {filtered.map(app => {
                      const days = getDaysPending(app.submittedAt);
                      return (
                        <tr key={app.applicationId} className="hover:bg-gray-50 transition-colors">
                          <td className="py-4 px-4">
                            <div className="font-medium text-gray-900">{app.fullName || '—'}</div>
                            <div className="text-xs text-gray-400 mt-0.5">{app.email}</div>
                          </td>
                          <td className="py-4 px-4">
                            <div className="font-medium text-gray-800">{app.loanType}</div>
                            <div className="text-xs text-gray-500 mt-0.5">{formatCurrency(app.loanAmount)}</div>
                          </td>
                          <td className="py-4 px-4">
                            <StatusBadge status={app.status} />
                          </td>
                          <td className="py-4 px-4 text-gray-500">
                            {formatDate(app.submittedAt) || '—'}
                          </td>
                          <td className="py-4 px-4">
                            {days !== null ? (
                              <span className={`font-medium ${
                                days > 14 ? 'text-red-600' :
                                days > 7 ? 'text-orange-500' :
                                'text-gray-600'}`}>
                                {days}d
                              </span>
                            ) : '—'}
                          </td>
                          <td className="py-4 px-4">
                            <button
                              onClick={() => navigate(`/admin/review/${app.applicationId}`)}
                              className="text-xs bg-primary-600 text-white px-3 py-1.5 rounded-lg hover:bg-primary-700 transition-colors font-medium">
                              Review →
                            </button>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
              <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
            </>
          )
        }
      </div>
    </PageLayout>
  );
}

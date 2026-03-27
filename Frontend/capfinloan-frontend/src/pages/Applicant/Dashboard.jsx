import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import PageLayout from '../../components/layout/PageLayout';
import StatusBadge from '../../components/common/StatusBadge';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import EmptyState from '../../components/common/EmptyState';
import Pagination from '../../components/common/Pagination';
import { loanService } from '../../services/loanService';
import { formatCurrency, formatDate } from '../../utils/formatters';
import { useAuth } from '../../context/AuthContext';

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
    } finally {
      setLoading(false);
    }
  };

  const kpiCards = [
    { label: 'Total Applications', value: stats.total,    color: 'border-blue-500',   text: 'text-blue-700' },
    { label: 'In Progress',        value: stats.pending,  color: 'border-yellow-500', text: 'text-yellow-700' },
    { label: 'Approved',           value: stats.approved, color: 'border-green-500',  text: 'text-green-700' },
    { label: 'Rejected',           value: stats.rejected, color: 'border-red-500',    text: 'text-red-700' },
  ];

  return (
    <PageLayout
      title={`Welcome, ${user?.fullName?.split(' ')[0]} 👋`}
      subtitle="Manage your loan applications"
      action={
        <Link to="/applicant/apply" className="btn-primary">
          + Apply for Loan
        </Link>
      }>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {kpiCards.map(card => (
          <div key={card.label} className={`card border-l-4 ${card.color}`}>
            <div className={`text-3xl font-bold ${card.text} mb-1`}>{card.value}</div>
            <div className="text-sm text-gray-500">{card.label}</div>
          </div>
        ))}
      </div>

      {/* Applications Table */}
      <div className="card">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-lg font-semibold text-gray-900">My Applications</h2>
          <button onClick={fetchApplications} className="text-sm text-primary-600 hover:underline">
            Refresh
          </button>
        </div>

        {loading ? <LoadingSpinner /> : applications.length === 0 ? (
          <EmptyState
            title="No applications yet"
            description="Start your loan journey today"
            action={
              <Link to="/applicant/apply" className="btn-primary">
                Apply for Loan
              </Link>
            } />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-100">
                    {['Loan Type', 'Amount', 'Status', 'Applied', 'Actions'].map(h => (
                      <th key={h} className="text-left py-3 px-4 text-xs font-medium text-gray-500 uppercase">
                        {h}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {applications.map(app => (
                    <tr key={app.applicationId} className="hover:bg-gray-50 transition-colors">
                      <td className="py-4 px-4">
                        <div className="font-medium text-gray-900">{app.loanType}</div>
                        <div className="text-xs text-gray-400 mt-0.5">{app.applicationId.slice(0, 8)}...</div>
                      </td>
                      <td className="py-4 px-4 font-semibold text-gray-900">
                        {formatCurrency(app.loanAmount)}
                      </td>
                      <td className="py-4 px-4">
                        <StatusBadge status={app.status} />
                      </td>
                      <td className="py-4 px-4 text-gray-500">{formatDate(app.createdAt)}</td>
                      <td className="py-4 px-4">
                        <div className="flex gap-2">
                          <button
                            onClick={() => navigate(`/applicant/status/${app.applicationId}`)}
                            className="text-xs text-primary-600 hover:underline font-medium">
                            Track
                          </button>
                          <button
                            onClick={() => navigate(`/applicant/documents?appId=${app.applicationId}`)}
                            className="text-xs text-gray-500 hover:underline">
                            Docs
                          </button>
                          {app.status === 'Draft' && (
                            <button
                              onClick={() => navigate(`/applicant/apply/${app.applicationId}`)}
                              className="text-xs text-orange-600 hover:underline font-medium">
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
            <Pagination page={page} totalPages={totalPages} onPageChange={setPage} />
          </>
        )}
      </div>
    </PageLayout>
  );
}

import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import {
  PieChart, Pie, Cell, Legend, Tooltip,
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
  ResponsiveContainer
} from 'recharts';
import PageLayout from '../../components/layout/PageLayout';
import StatusBadge from '../../components/common/StatusBadge';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import { adminService } from '../../services/adminService';
import { loanService } from '../../services/loanService';
import { formatCurrency, formatDate } from '../../utils/formatters';

const STATUS_COLORS = {
  Draft: '#6b7280',
  Submitted: '#3b82f6',
  DocsPending: '#f59e0b',
  DocsVerified: '#6366f1',
  UnderReview: '#8b5cf6',
  Approved: '#10b981',
  Rejected: '#ef4444',
  Closed: '#9ca3af',
};

export default function AdminDashboard() {
  const [stats, setStats] = useState(null);
  const [trend, setTrend] = useState([]);
  const [recentApps, setRecentApps] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => { fetchAll(); }, []);

  const fetchAll = async () => {
    setLoading(true);
    try {
      const [statsRes, trendRes, appsRes] =
        await Promise.allSettled([
          adminService.getDashboardStats(),
          adminService.getMonthlyTrend(6),
          loanService.getAllApplications(1, 5),
        ]);

      if (statsRes.status === 'fulfilled') {
        setStats(statsRes.value.data.data);
      }
      if (trendRes.status === 'fulfilled') {
        setTrend(trendRes.value.data.data || []);
      }
      if (appsRes.status === 'fulfilled') {
        setRecentApps(appsRes.value.data.data?.items || []);
      }
    } catch (err) {
      toast.error('Failed to load dashboard');
    } finally {
      setLoading(false);
    }
  };

  const pieData = stats ? [
    { name: 'Submitted', value: stats.submittedCount || 0 },
    { name: 'Under Review', value: stats.underReviewCount || 0 },
    { name: 'Approved', value: stats.approvedCount || 0 },
    { name: 'Rejected', value: stats.rejectedCount || 0 },
    { name: 'Pending', value: stats.pendingCount || 0 },
  ].filter(d => d.value > 0) : [];

  const kpiCards = stats ? [
    {
      label: 'Total Decisions',
      value: (stats.approvedCount || 0) + (stats.rejectedCount || 0),
      color: 'border-blue-500', text: 'text-blue-700', icon: '📋'
    },
    {
      label: 'Approved',
      value: stats.approvedCount || 0,
      color: 'border-green-500', text: 'text-green-700', icon: '✅'
    },
    {
      label: 'Rejected',
      value: stats.rejectedCount || 0,
      color: 'border-red-500', text: 'text-red-700', icon: '❌'
    },
    {
      label: 'Approval Rate',
      value: `${stats.approvalRate || 0}%`,
      color: 'border-purple-500', text: 'text-purple-700', icon: '📊'
    },
  ] : [];

  if (loading) return (
    <PageLayout title="Admin Dashboard"><LoadingSpinner /></PageLayout>
  );

  return (
    <PageLayout
      title="Admin Dashboard"
      subtitle="Overview of all loan applications and decisions">

      {/* KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {kpiCards.map(card => (
          <div key={card.label} className={`card border-l-4 ${card.color}`}>
            <div className="flex justify-between items-start">
              <div>
                <div className={`text-3xl font-bold ${card.text} mb-1`}>{card.value}</div>
                <div className="text-sm text-gray-500">{card.label}</div>
              </div>
              <span className="text-2xl">{card.icon}</span>
            </div>
          </div>
        ))}
      </div>

      {/* Charts */}
      <div className="grid lg:grid-cols-5 gap-6 mb-8">

        {/* Pie Chart */}
        <div className="lg:col-span-2 card">
          <h3 className="font-semibold text-gray-900 mb-4">Applications by Status</h3>
          {pieData.length > 0 ? (
            <ResponsiveContainer width="100%" height={240}>
              <PieChart>
                <Pie
                  data={pieData}
                  cx="50%"
                  cy="50%"
                  innerRadius={60}
                  outerRadius={90}
                  paddingAngle={3}
                  dataKey="value">
                  {pieData.map((entry) => (
                    <Cell
                      key={entry.name}
                      fill={STATUS_COLORS[entry.name.replace(' ', '')] || '#6b7280'}
                    />
                  ))}
                </Pie>
                <Tooltip formatter={(v, n) => [v, n]} />
                <Legend
                  iconType="circle"
                  iconSize={8}
                  formatter={(v) => <span className="text-xs text-gray-600">{v}</span>}
                />
              </PieChart>
            </ResponsiveContainer>
          ) : (
            <div className="flex items-center justify-center h-60 text-gray-400">
              No data yet
            </div>
          )}
        </div>

        {/* Bar Chart */}
        <div className="lg:col-span-3 card">
          <h3 className="font-semibold text-gray-900 mb-4">Monthly Decisions (Last 6 Months)</h3>
          {trend.length > 0 ? (
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={trend} margin={{ top: 5, right: 10, left: 0, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f3f4f6" />
                <XAxis dataKey="month" tick={{ fontSize: 11, fill: '#9ca3af' }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 11, fill: '#9ca3af' }} axisLine={false} tickLine={false} />
                <Tooltip contentStyle={{ borderRadius: '8px', border: '1px solid #e5e7eb', fontSize: '12px' }} />
                <Legend
                  iconType="circle"
                  iconSize={8}
                  formatter={(v) => <span className="text-xs text-gray-600">{v}</span>}
                />
                <Bar dataKey="approvedCount" name="Approved" fill="#10b981" radius={[4, 4, 0, 0]} />
                <Bar dataKey="rejectedCount" name="Rejected" fill="#ef4444" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : (
            <div className="flex items-center justify-center h-60 text-gray-400">
              No trend data yet
            </div>
          )}
        </div>
      </div>

      {/* Recent Applications */}
      <div className="card">
        <div className="flex justify-between items-center mb-5">
          <h3 className="font-semibold text-gray-900">Recent Applications</h3>
          <button onClick={() => navigate('/admin/applications')}
            className="text-sm text-primary-600 hover:underline">
            View All →
          </button>
        </div>

        {recentApps.length === 0 ? (
          <p className="text-center text-gray-400 py-8">No applications yet</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100">
                  {['Applicant', 'Loan Type', 'Amount', 'Status', 'Submitted', 'Action'].map(h => (
                    <th key={h} className="text-left py-3 px-4 text-xs font-medium text-gray-500 uppercase">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {recentApps.map(app => (
                  <tr key={app.applicationId} className="hover:bg-gray-50 transition-colors">
                    <td className="py-3 px-4">
                      <div className="font-medium text-gray-900">{app.fullName || 'N/A'}</div>
                      <div className="text-xs text-gray-400">{app.email}</div>
                    </td>
                    <td className="py-3 px-4 text-gray-700">{app.loanType}</td>
                    <td className="py-3 px-4 font-medium">{formatCurrency(app.loanAmount)}</td>
                    <td className="py-3 px-4"><StatusBadge status={app.status} /></td>
                    <td className="py-3 px-4 text-gray-500">
                      {formatDate(app.submittedAt || app.createdAt)}
                    </td>
                    <td className="py-3 px-4">
                      <button
                        onClick={() => navigate(`/admin/review/${app.applicationId}`)}
                        className="text-xs bg-primary-600 text-white px-3 py-1 rounded-lg hover:bg-primary-700 transition-colors">
                        Review
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

    </PageLayout>
  );
}

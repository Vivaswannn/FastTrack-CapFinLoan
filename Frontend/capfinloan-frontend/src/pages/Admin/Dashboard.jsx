import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { ClipboardList, CheckCircle2, XCircle, BarChart2, ShieldCheck } from 'lucide-react';
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
import { useAuth } from '../../context/AuthContext';

const getGreeting = () => {
  const h = new Date().getHours();
  if (h < 12) return 'Good morning';
  if (h < 17) return 'Good afternoon';
  return 'Good evening';
};

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
  const { user } = useAuth();

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

      if (statsRes.status === 'fulfilled') setStats(statsRes.value.data.data);
      if (trendRes.status === 'fulfilled') setTrend(trendRes.value.data.data || []);
      if (appsRes.status === 'fulfilled') setRecentApps(appsRes.value.data.data?.items || []);
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
      border: 'border-blue-200', iconBg: 'bg-blue-50', iconColor: 'text-blue-500',
      numColor: 'text-blue-700', icon: <ClipboardList size={18} />,
    },
    {
      label: 'Approved',
      value: stats.approvedCount || 0,
      border: 'border-teal-200', iconBg: 'bg-teal-50', iconColor: 'text-teal-600',
      numColor: 'text-teal-700', icon: <CheckCircle2 size={18} />,
    },
    {
      label: 'Rejected',
      value: stats.rejectedCount || 0,
      border: 'border-red-200', iconBg: 'bg-red-50', iconColor: 'text-red-400',
      numColor: 'text-red-700', icon: <XCircle size={18} />,
    },
    {
      label: 'Approval Rate',
      value: `${stats.approvalRate || 0}%`,
      border: 'border-purple-200', iconBg: 'bg-purple-50', iconColor: 'text-purple-500',
      numColor: 'text-purple-700', icon: <BarChart2 size={18} />,
    },
  ] : [];

  if (loading) return (
    <PageLayout title="Admin Dashboard"><LoadingSpinner /></PageLayout>
  );

  return (
    <PageLayout
      title="Admin Dashboard"
      subtitle="Overview of all loan applications and decisions">

      {/* Welcome Hero Banner */}
      <div className="relative bg-gradient-to-br from-slate-800 via-slate-800 to-slate-900 rounded-2xl p-6 mb-6 overflow-hidden shadow-lg">
        <div className="absolute top-0 right-0 w-72 h-72 bg-teal-500/10 rounded-full -translate-y-1/2 translate-x-1/4" />
        <div className="absolute bottom-0 left-1/4 w-48 h-48 bg-teal-500/5 rounded-full translate-y-1/2" />
        <div className="relative z-10 flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <ShieldCheck size={14} className="text-teal-400" />
              <p className="text-slate-300 text-sm font-medium">{getGreeting()}, {user?.fullName?.split(' ')[0]}!</p>
            </div>
            <h2 className="text-white font-extrabold text-2xl tracking-tight leading-tight">
              Admin Control Centre
            </h2>
            <p className="text-slate-400 text-sm mt-1 max-w-xs">
              Review applications, verify documents, and manage loan decisions.
            </p>
          </div>
          <div className="flex gap-3 shrink-0">
            <button onClick={() => navigate('/admin/applications')}
              className="inline-flex items-center gap-2 bg-teal-600 hover:bg-teal-500 text-white text-sm font-bold px-5 py-2.5 rounded-xl shadow-sm hover:-translate-y-0.5 transition-all duration-200">
              <ClipboardList size={15} />
              View Queue
            </button>
          </div>
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

      {/* Charts */}
      <div className="grid lg:grid-cols-5 gap-6 mb-6">

        {/* Pie Chart */}
        <div className="lg:col-span-2 bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <h3 className="font-bold text-slate-800 mb-4">Applications by Status</h3>
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
                <Tooltip
                  contentStyle={{ borderRadius: '10px', border: '1px solid #e2e8f0', fontSize: '12px' }}
                  formatter={(v, n) => [v, n]}
                />
                <Legend
                  iconType="circle"
                  iconSize={8}
                  formatter={(v) => <span className="text-xs text-slate-500">{v}</span>}
                />
              </PieChart>
            </ResponsiveContainer>
          ) : (
            <div className="flex items-center justify-center h-60 text-slate-300 text-sm">
              No data yet
            </div>
          )}
        </div>

        {/* Bar Chart */}
        <div className="lg:col-span-3 bg-white rounded-xl border border-slate-200 shadow-sm p-6">
          <h3 className="font-bold text-slate-800 mb-4">Monthly Decisions — Last 6 Months</h3>
          {trend.length > 0 ? (
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={trend} margin={{ top: 5, right: 10, left: 0, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                <XAxis dataKey="month" tick={{ fontSize: 11, fill: '#94a3b8' }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fontSize: 11, fill: '#94a3b8' }} axisLine={false} tickLine={false} />
                <Tooltip contentStyle={{ borderRadius: '10px', border: '1px solid #e2e8f0', fontSize: '12px' }} />
                <Legend
                  iconType="circle"
                  iconSize={8}
                  formatter={(v) => <span className="text-xs text-slate-500">{v}</span>}
                />
                <Bar dataKey="approvedCount" name="Approved" fill="#0d9488" radius={[4, 4, 0, 0]} />
                <Bar dataKey="rejectedCount" name="Rejected" fill="#ef4444" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : (
            <div className="flex items-center justify-center h-60 text-slate-300 text-sm">
              No trend data yet
            </div>
          )}
        </div>
      </div>

      {/* Recent Applications */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="flex justify-between items-center px-6 py-5 border-b border-slate-100">
          <h3 className="font-bold text-slate-800">Recent Applications</h3>
          <button onClick={() => navigate('/admin/applications')}
            className="text-sm font-semibold text-teal-600 hover:text-teal-800 transition-colors">
            View All →
          </button>
        </div>

        {recentApps.length === 0 ? (
          <p className="text-center text-slate-300 py-10 text-sm">No applications yet</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-slate-50/60">
                  {['Applicant', 'Loan Type', 'Amount', 'Status', 'Submitted', 'Action'].map(h => (
                    <th key={h} className="text-left py-3 px-6 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {recentApps.map(app => (
                  <tr key={app.applicationId} className="hover:bg-slate-50/50 transition-colors">
                    <td className="py-4 px-6">
                      <div className="font-semibold text-slate-800">{app.fullName || 'N/A'}</div>
                      <div className="text-xs text-slate-400 mt-0.5">{app.email}</div>
                    </td>
                    <td className="py-4 px-6 text-slate-600 font-medium">{app.loanType}</td>
                    <td className="py-4 px-6 font-bold text-slate-800">{formatCurrency(app.loanAmount)}</td>
                    <td className="py-4 px-6"><StatusBadge status={app.status} /></td>
                    <td className="py-4 px-6 text-slate-400">
                      {formatDate(app.submittedAt || app.createdAt)}
                    </td>
                    <td className="py-4 px-6">
                      <button
                        onClick={() => navigate(`/admin/review/${app.applicationId}`)}
                        className="text-xs bg-teal-600 hover:bg-teal-700 text-white font-bold px-3 py-1.5 rounded-lg transition-colors shadow-sm">
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

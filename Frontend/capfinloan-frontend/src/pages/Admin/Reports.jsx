import { useState, useEffect, useRef } from 'react';
import toast from 'react-hot-toast';
import { ClipboardList, IndianRupee, BarChart2, CheckCircle2, Download, RefreshCw } from 'lucide-react';
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, Legend, ResponsiveContainer
} from 'recharts';
import PageLayout from '../../components/layout/PageLayout';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import { adminService } from '../../services/adminService';
import { formatCurrency } from '../../utils/formatters';

export default function Reports() {
  const [stats, setStats] = useState(null);
  const [trend, setTrend] = useState([]);
  const [loading, setLoading] = useState(true);
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [exporting, setExporting] = useState(false);
  const mountedRef = useRef(true);

  useEffect(() => {
    mountedRef.current = true;
    fetchData();
    return () => { mountedRef.current = false; };
  }, []);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [statsRes, trendRes] = await Promise.allSettled([
        adminService.getDashboardStats(),
        adminService.getMonthlyTrend(12),
      ]);
      if (!mountedRef.current) return;
      if (statsRes.status === 'fulfilled') setStats(statsRes.value.data.data);
      if (trendRes.status === 'fulfilled') setTrend(trendRes.value.data.data || []);
      if (statsRes.status === 'rejected' && trendRes.status === 'rejected') {
        toast.error('Failed to load report data');
      }
    } catch (err) {
      if (!mountedRef.current) return;
      toast.error(err.response?.data?.message || err.message || 'Failed to load report data');
    } finally {
      if (mountedRef.current) setLoading(false);
    }
  };

  const handleExport = async () => {
    setExporting(true);
    try {
      const res = await adminService.exportCsv(startDate || null, endDate || null);
      const blob = new Blob([res.data], { type: 'text/csv' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `decisions_${new Date().toISOString().split('T')[0]}.csv`;
      a.click();
      URL.revokeObjectURL(url);
      toast.success('Report exported successfully!');
    } catch (err) {
      toast.error(err.response?.data?.message || err.message || 'Export failed');
    } finally {
      setExporting(false);
    }
  };

  const totalDecisions = stats ? (stats.approvedCount || 0) + (stats.rejectedCount || 0) : 0;

  const metricCards = stats ? [
    {
      label: 'Total Decisions',        value: totalDecisions,
      border: 'border-blue-200',   iconBg: 'bg-blue-50',   iconColor: 'text-blue-500',   numColor: 'text-blue-700',
      icon: <ClipboardList size={18} />,
    },
    {
      label: 'Total Approved Amount',  value: formatCurrency(stats.totalLoanAmountApproved || 0),
      border: 'border-teal-200',   iconBg: 'bg-teal-50',   iconColor: 'text-teal-600',   numColor: 'text-teal-700',
      icon: <IndianRupee size={18} />,
    },
    {
      label: 'Average Loan Amount',    value: formatCurrency(stats.averageLoanAmount || 0),
      border: 'border-purple-200', iconBg: 'bg-purple-50', iconColor: 'text-purple-500', numColor: 'text-purple-700',
      icon: <BarChart2 size={18} />,
    },
    {
      label: 'Approval Rate',          value: `${stats.approvalRate || 0}%`,
      border: 'border-amber-200',  iconBg: 'bg-amber-50',  iconColor: 'text-amber-500',  numColor: 'text-amber-700',
      icon: <CheckCircle2 size={18} />,
    },
  ] : [];

  if (loading) return <PageLayout title="Reports"><LoadingSpinner /></PageLayout>;

  return (
    <PageLayout
      title="Reports & Analytics"
      subtitle="Decision trends and performance metrics">

      {/* Filters + Export */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-5 mb-6">
        <div className="flex flex-wrap gap-4 items-end justify-between">
          <div className="flex gap-4 items-end flex-wrap">
            <div>
              <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1.5">Start Date</label>
              <input type="date"
                className="bg-white px-4 py-2.5 border border-slate-200 hover:border-slate-300 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 rounded-xl text-slate-700 text-sm focus:outline-none transition-all"
                value={startDate} onChange={e => setStartDate(e.target.value)} />
            </div>
            <div>
              <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1.5">End Date</label>
              <input type="date"
                className="bg-white px-4 py-2.5 border border-slate-200 hover:border-slate-300 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 rounded-xl text-slate-700 text-sm focus:outline-none transition-all"
                value={endDate} onChange={e => setEndDate(e.target.value)} />
            </div>
            <button
              onClick={() => { setStartDate(''); setEndDate(''); }}
              className="inline-flex items-center gap-2 px-4 py-2.5 border border-slate-200 hover:border-slate-300 text-slate-600 text-sm font-semibold rounded-xl transition-all hover:bg-slate-50">
              <RefreshCw size={14} /> Reset
            </button>
          </div>
          <button onClick={handleExport} disabled={exporting}
            className="inline-flex items-center gap-2 bg-teal-600 hover:bg-teal-700 text-white text-sm font-bold px-5 py-2.5 rounded-xl shadow-sm hover:-translate-y-0.5 transition-all duration-200 disabled:opacity-60 disabled:pointer-events-none">
            {exporting ? (
              <div className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />
            ) : <Download size={15} />}
            Export CSV
          </button>
        </div>
      </div>

      {/* Metric Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {metricCards.map(card => (
          <div key={card.label}
            className={`bg-white rounded-xl border ${card.border} p-5 shadow-sm`}>
            <div className="flex justify-between items-start mb-3">
              <div className={`w-9 h-9 ${card.iconBg} rounded-lg flex items-center justify-center`}>
                <span className={card.iconColor}>{card.icon}</span>
              </div>
            </div>
            <div className={`text-2xl font-bold ${card.numColor} mb-0.5 leading-tight`}>{card.value}</div>
            <div className="text-sm text-slate-500">{card.label}</div>
          </div>
        ))}
      </div>

      {/* Line Chart */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
        <h3 className="font-bold text-slate-800 mb-6">Monthly Decision Trend — Last 12 Months</h3>
        {trend.length > 0 ? (
          <ResponsiveContainer width="100%" height={320}>
            <LineChart data={trend} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
              <XAxis dataKey="month" tick={{ fontSize: 11, fill: '#94a3b8' }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fontSize: 11, fill: '#94a3b8' }} axisLine={false} tickLine={false} />
              <Tooltip contentStyle={{ borderRadius: '10px', border: '1px solid #e2e8f0', fontSize: '12px' }} />
              <Legend
                iconType="circle"
                iconSize={8}
                formatter={v => <span className="text-xs text-slate-500">{v}</span>}
              />
              <Line
                type="monotone"
                dataKey="approvedCount"
                name="Approved"
                stroke="#0d9488"
                strokeWidth={2.5}
                dot={{ fill: '#0d9488', r: 4 }}
                activeDot={{ r: 6 }}
              />
              <Line
                type="monotone"
                dataKey="rejectedCount"
                name="Rejected"
                stroke="#ef4444"
                strokeWidth={2.5}
                dot={{ fill: '#ef4444', r: 4 }}
                activeDot={{ r: 6 }}
              />
            </LineChart>
          </ResponsiveContainer>
        ) : (
          <div className="flex items-center justify-center h-60 text-slate-300 text-sm">
            No trend data available yet
          </div>
        )}
      </div>

    </PageLayout>
  );
}

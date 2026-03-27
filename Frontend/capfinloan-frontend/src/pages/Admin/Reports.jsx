import { useState, useEffect, useRef } from 'react';
import toast from 'react-hot-toast';
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
      if (statsRes.status === 'fulfilled') {
        setStats(statsRes.value.data.data);
      }
      if (trendRes.status === 'fulfilled') {
        setTrend(trendRes.value.data.data || []);
      }
      if (statsRes.status === 'rejected' && trendRes.status === 'rejected') {
        toast.error('Failed to load report data');
      }
    } catch (err) {
      if (!mountedRef.current) return;
      const msg = err.response?.data?.message || err.message || 'Failed to load report data';
      toast.error(msg);
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
      const msg = err.response?.data?.message || err.message || 'Export failed';
      toast.error(msg);
    } finally {
      setExporting(false);
    }
  };

  const totalDecisions = stats ? (stats.approvedCount || 0) + (stats.rejectedCount || 0) : 0;

  const metricCards = stats ? [
    { label: 'Total Decisions',       value: totalDecisions,                              icon: '📋', color: 'text-blue-700',   border: 'border-blue-200' },
    { label: 'Total Approved Amount', value: formatCurrency(stats.totalLoanAmountApproved || 0), icon: '💰', color: 'text-green-700',  border: 'border-green-200' },
    { label: 'Average Loan Amount',   value: formatCurrency(stats.averageLoanAmount || 0),       icon: '📊', color: 'text-purple-700', border: 'border-purple-200' },
    { label: 'Approval Rate',         value: `${stats.approvalRate || 0}%`,              icon: '✅', color: 'text-orange-700', border: 'border-orange-200' },
  ] : [];

  if (loading) return <PageLayout title="Reports"><LoadingSpinner /></PageLayout>;

  return (
    <PageLayout
      title="Reports & Analytics"
      subtitle="Decision trends and performance metrics">

      {/* Filters + Export */}
      <div className="card mb-6">
        <div className="flex flex-wrap gap-4 items-end justify-between">
          <div className="flex gap-4 items-end flex-wrap">
            <div>
              <label className="label">Start Date</label>
              <input type="date" className="input-field"
                value={startDate} onChange={e => setStartDate(e.target.value)} />
            </div>
            <div>
              <label className="label">End Date</label>
              <input type="date" className="input-field"
                value={endDate} onChange={e => setEndDate(e.target.value)} />
            </div>
            <button onClick={() => { setStartDate(''); setEndDate(''); }} className="btn-secondary h-10">
              Reset
            </button>
          </div>
          <button onClick={handleExport} disabled={exporting}
            className="btn-primary flex items-center gap-2">
            {exporting ? (
              <div className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />
            ) : '📥'}
            Export CSV
          </button>
        </div>
      </div>

      {/* Metric Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {metricCards.map(card => (
          <div key={card.label} className={`card border ${card.border}`}>
            <div className="flex justify-between items-start">
              <div>
                <div className={`text-2xl font-bold ${card.color} mb-1`}>{card.value}</div>
                <div className="text-xs text-gray-500">{card.label}</div>
              </div>
              <span className="text-2xl">{card.icon}</span>
            </div>
          </div>
        ))}
      </div>

      {/* Line Chart */}
      <div className="card">
        <h3 className="font-semibold text-gray-900 mb-6">Monthly Decision Trend (Last 12 Months)</h3>
        {trend.length > 0 ? (
          <ResponsiveContainer width="100%" height={320}>
            <LineChart data={trend} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f3f4f6" />
              <XAxis dataKey="month" tick={{ fontSize: 11, fill: '#9ca3af' }} axisLine={false} tickLine={false} />
              <YAxis tick={{ fontSize: 11, fill: '#9ca3af' }} axisLine={false} tickLine={false} />
              <Tooltip contentStyle={{ borderRadius: '10px', border: '1px solid #e5e7eb', fontSize: '12px' }} />
              <Legend
                iconType="circle"
                iconSize={8}
                formatter={v => <span className="text-xs text-gray-600">{v}</span>}
              />
              <Line
                type="monotone"
                dataKey="approvedCount"
                name="Approved"
                stroke="#10b981"
                strokeWidth={2.5}
                dot={{ fill: '#10b981', r: 4 }}
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
          <div className="flex items-center justify-center h-60 text-gray-400">
            No trend data available yet
          </div>
        )}
      </div>

    </PageLayout>
  );
}

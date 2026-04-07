import { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
import { Search, Users, UserCheck, UserX, RefreshCw } from 'lucide-react';
import PageLayout from '../../components/layout/PageLayout';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import Pagination from '../../components/common/Pagination';
import EmptyState from '../../components/common/EmptyState';
import { authService } from '../../services/authService';
import { formatDate } from '../../utils/formatters';

export default function UserManagement() {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState('');
  const [toggling, setToggling] = useState({});

  useEffect(() => { fetchUsers(); }, [page]);

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const res = await authService.getAllUsers(page, 10);
      const data = res.data.data;
      setUsers(data.items || []);
      setTotalPages(data.totalPages || 1);
    } catch (err) {
      toast.error(err.response?.data?.message || err.message || 'Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleStatus = async (user) => {
    if (user.email === 'admin@capfinloan.com') {
      toast.error('Cannot deactivate the system admin account');
      return;
    }
    const newStatus = !user.isActive;
    const action = newStatus ? 'activate' : 'deactivate';
    if (!window.confirm(`Are you sure you want to ${action} ${user.fullName}?`)) return;

    setToggling(p => ({ ...p, [user.userId]: true }));
    try {
      await authService.updateUserStatus(user.userId, { isActive: newStatus });
      toast.success(`${user.fullName} ${newStatus ? 'activated' : 'deactivated'} successfully`);
      fetchUsers();
    } catch (err) {
      toast.error(err.response?.data?.message || 'Status update failed');
    } finally {
      setToggling(p => ({ ...p, [user.userId]: false }));
    }
  };

  const filtered = users.filter(u => {
    if (!search) return true;
    const q = search.toLowerCase();
    return u.fullName?.toLowerCase().includes(q) || u.email?.toLowerCase().includes(q);
  });

  const statCards = [
    { label: 'Total Users',  value: users.length,                          icon: <Users size={18} />,      iconBg: 'bg-blue-50',   iconColor: 'text-blue-500',   numColor: 'text-blue-700',   border: 'border-blue-200' },
    { label: 'Active',       value: users.filter(u => u.isActive).length,  icon: <UserCheck size={18} />,  iconBg: 'bg-teal-50',   iconColor: 'text-teal-600',   numColor: 'text-teal-700',   border: 'border-teal-200' },
    { label: 'Inactive',     value: users.filter(u => !u.isActive).length, icon: <UserX size={18} />,      iconBg: 'bg-red-50',    iconColor: 'text-red-400',    numColor: 'text-red-700',    border: 'border-red-200' },
  ];

  return (
    <PageLayout
      title="User Management"
      subtitle="Manage applicant and admin accounts">

      {/* Search */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-5 mb-6">
        <div className="flex gap-4 items-end">
          <div className="flex-1 max-w-sm">
            <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1.5">Search Users</label>
            <div className="relative">
              <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-300" />
              <input
                className="w-full bg-white pl-9 pr-4 py-2.5 border border-slate-200 hover:border-slate-300 focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20 rounded-xl text-slate-800 text-sm placeholder:text-slate-300 focus:outline-none transition-all"
                placeholder="Search by name or email..."
                value={search}
                onChange={e => setSearch(e.target.value)} />
            </div>
          </div>
          <button onClick={fetchUsers}
            className="inline-flex items-center gap-2 bg-teal-600 hover:bg-teal-700 text-white text-sm font-bold px-4 py-2.5 rounded-xl shadow-sm hover:-translate-y-0.5 transition-all duration-200">
            <RefreshCw size={14} /> Refresh
          </button>
        </div>
      </div>

      {/* Stat Cards */}
      <div className="grid grid-cols-3 gap-4 mb-6">
        {statCards.map(card => (
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

      {/* Users Table */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        {loading ? (
          <div className="p-8"><LoadingSpinner /></div>
        ) : filtered.length === 0 ? (
          <EmptyState title="No users found" description="Try adjusting your search" />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-slate-50/60">
                    {['User', 'Role', 'Status', 'Joined', 'Actions'].map(h => (
                      <th key={h} className="text-left py-3 px-6 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                        {h}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {filtered.map(user => (
                    <tr key={user.userId} className="hover:bg-slate-50/50 transition-colors">
                      <td className="py-4 px-6">
                        <div className="flex items-center gap-3">
                          <div className={`w-9 h-9 rounded-full flex items-center justify-center text-sm font-bold flex-shrink-0 ${
                            user.role === 'Admin' ? 'bg-teal-100 text-teal-700' : 'bg-slate-100 text-slate-600'}`}>
                            {user.fullName?.[0]?.toUpperCase()}
                          </div>
                          <div>
                            <div className="font-semibold text-slate-800">{user.fullName}</div>
                            <div className="text-xs text-slate-400 mt-0.5">{user.email}</div>
                          </div>
                        </div>
                      </td>
                      <td className="py-4 px-6">
                        <span className={`text-xs px-2.5 py-1 rounded-full font-semibold ${
                          user.role === 'Admin'
                            ? 'bg-teal-100 text-teal-700'
                            : 'bg-slate-100 text-slate-600'}`}>
                          {user.role}
                        </span>
                      </td>
                      <td className="py-4 px-6">
                        <span className={`inline-flex items-center gap-1 text-xs px-2.5 py-1 rounded-full font-semibold ${
                          user.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-600'}`}>
                          <span className={`w-1.5 h-1.5 rounded-full ${user.isActive ? 'bg-green-500' : 'bg-red-400'}`} />
                          {user.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="py-4 px-6 text-slate-400">{formatDate(user.createdAt)}</td>
                      <td className="py-4 px-6">
                        <button
                          onClick={() => handleToggleStatus(user)}
                          disabled={toggling[user.userId]}
                          className={`text-xs px-3 py-1.5 rounded-lg font-semibold transition-colors disabled:opacity-50 ${
                            user.isActive
                              ? 'bg-red-50 text-red-600 hover:bg-red-100'
                              : 'bg-teal-50 text-teal-600 hover:bg-teal-100'}`}>
                          {toggling[user.userId] ? '...' : user.isActive ? 'Deactivate' : 'Activate'}
                        </button>
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

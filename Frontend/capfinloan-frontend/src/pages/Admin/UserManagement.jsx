import { useState, useEffect } from 'react';
import toast from 'react-hot-toast';
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
      const msg = err.response?.data?.message || err.message || 'Failed to load users';
      toast.error(msg);
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

  return (
    <PageLayout
      title="User Management"
      subtitle="Manage applicant and admin accounts">

      {/* Search */}
      <div className="card mb-6">
        <div className="flex gap-4 items-end">
          <div className="flex-1 max-w-sm">
            <label className="label">Search Users</label>
            <input className="input-field"
              placeholder="Search by name or email..."
              value={search}
              onChange={e => setSearch(e.target.value)} />
          </div>
          <button onClick={fetchUsers} className="btn-primary h-10">Refresh</button>
        </div>
      </div>

      {/* Stats row */}
      <div className="grid grid-cols-3 gap-4 mb-6">
        {[
          ['Total Users',  users.length,                          'text-blue-700',  'border-blue-200'],
          ['Active',       users.filter(u => u.isActive).length,  'text-green-700', 'border-green-200'],
          ['Inactive',     users.filter(u => !u.isActive).length, 'text-red-700',   'border-red-200'],
        ].map(([label, val, text, border]) => (
          <div key={label} className={`card border ${border} text-center`}>
            <div className={`text-2xl font-bold ${text}`}>{val}</div>
            <div className="text-xs text-gray-500 mt-1">{label}</div>
          </div>
        ))}
      </div>

      {/* Users Table */}
      <div className="card">
        {loading ? <LoadingSpinner /> :
          filtered.length === 0 ? (
            <EmptyState title="No users found" description="Try adjusting your search" />
          ) : (
            <>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-100">
                      {['User', 'Role', 'Status', 'Joined', 'Actions'].map(h => (
                        <th key={h} className="text-left py-3 px-4 text-xs font-medium text-gray-500 uppercase">
                          {h}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {filtered.map(user => (
                      <tr key={user.userId} className="hover:bg-gray-50 transition-colors">
                        <td className="py-4 px-4">
                          <div className="flex items-center gap-3">
                            <div className={`w-9 h-9 rounded-full flex items-center justify-center text-sm font-semibold flex-shrink-0 ${
                              user.role === 'Admin' ? 'bg-primary-100 text-primary-700' : 'bg-gray-100 text-gray-600'}`}>
                              {user.fullName?.[0]?.toUpperCase()}
                            </div>
                            <div>
                              <div className="font-medium text-gray-900">{user.fullName}</div>
                              <div className="text-xs text-gray-400">{user.email}</div>
                            </div>
                          </div>
                        </td>
                        <td className="py-4 px-4">
                          <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${
                            user.role === 'Admin'
                              ? 'bg-primary-100 text-primary-700'
                              : 'bg-gray-100 text-gray-600'}`}>
                            {user.role}
                          </span>
                        </td>
                        <td className="py-4 px-4">
                          <span className={`text-xs px-2.5 py-1 rounded-full font-medium ${
                            user.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                            {user.isActive ? '● Active' : '● Inactive'}
                          </span>
                        </td>
                        <td className="py-4 px-4 text-gray-500">{formatDate(user.createdAt)}</td>
                        <td className="py-4 px-4">
                          <button
                            onClick={() => handleToggleStatus(user)}
                            disabled={toggling[user.userId]}
                            className={`text-xs px-3 py-1.5 rounded-lg font-medium transition-colors disabled:opacity-50 ${
                              user.isActive
                                ? 'bg-red-50 text-red-600 hover:bg-red-100'
                                : 'bg-green-50 text-green-600 hover:bg-green-100'}`}>
                            {toggling[user.userId] ? '...' : user.isActive ? 'Deactivate' : 'Activate'}
                          </button>
                        </td>
                      </tr>
                    ))}
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

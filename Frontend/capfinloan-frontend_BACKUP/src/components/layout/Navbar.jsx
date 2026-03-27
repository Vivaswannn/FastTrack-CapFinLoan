import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import toast from 'react-hot-toast';

export default function Navbar() {
  const { user, logout, isAdmin } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    toast.success('Logged out successfully');
    navigate('/auth/login');
  };

  const dashboardPath = isAdmin() ? '/admin/dashboard' : '/applicant/dashboard';

  return (
    <nav className="bg-white border-b border-gray-200 sticky top-0 z-40">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          <Link to={dashboardPath} className="flex items-center gap-2">
            <div className="w-8 h-8 bg-primary-600 rounded-lg flex items-center justify-center">
              <span className="text-white font-bold text-sm">CF</span>
            </div>
            <span className="font-semibold text-gray-900">CapFinLoan</span>
          </Link>

          <div className="flex items-center gap-4">
            {isAdmin() ? (
              <div className="hidden md:flex gap-1">
                {[
                  ['/admin/dashboard', 'Dashboard'],
                  ['/admin/applications', 'Applications'],
                  ['/admin/reports', 'Reports'],
                  ['/admin/users', 'Users'],
                ].map(([path, label]) => (
                  <Link key={path} to={path}
                    className="px-3 py-2 text-sm text-gray-600 hover:text-primary-600 hover:bg-gray-50 rounded-lg transition-colors">
                    {label}
                  </Link>
                ))}
              </div>
            ) : (
              <div className="hidden md:flex gap-1">
                {[
                  ['/applicant/dashboard', 'Dashboard'],
                  ['/applicant/apply', 'Apply'],
                  ['/applicant/documents', 'Documents'],
                ].map(([path, label]) => (
                  <Link key={path} to={path}
                    className="px-3 py-2 text-sm text-gray-600 hover:text-primary-600 hover:bg-gray-50 rounded-lg transition-colors">
                    {label}
                  </Link>
                ))}
              </div>
            )}

            <div className="flex items-center gap-3 pl-4 border-l border-gray-200">
              <div className="text-right hidden sm:block">
                <p className="text-sm font-medium text-gray-900">{user?.fullName}</p>
                <p className="text-xs text-gray-500">{user?.role}</p>
              </div>
              <div className="w-9 h-9 bg-primary-100 rounded-full flex items-center justify-center">
                <span className="text-primary-700 font-semibold text-sm">
                  {user?.fullName?.[0]?.toUpperCase()}
                </span>
              </div>
              <button onClick={handleLogout}
                className="text-sm text-gray-500 hover:text-red-600 transition-colors px-2 py-1 rounded">
                Logout
              </button>
            </div>
          </div>
        </div>
      </div>
    </nav>
  );
}

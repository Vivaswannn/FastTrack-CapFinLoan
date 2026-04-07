import { Link, useLocation, useNavigate } from 'react-router-dom';
import { LogOut, LayoutDashboard, FileText, FolderOpen, Users, ClipboardList, BarChart2, Building2, Calculator } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import toast from 'react-hot-toast';

export default function Navbar() {
  const { user, logout, isAdmin } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    toast.success('Logged out successfully');
    navigate('/auth/login');
  };

  const dashboardPath = isAdmin() ? '/admin/dashboard' : '/applicant/dashboard';

  const adminLinks = [
    { path: '/admin/dashboard',    label: 'Dashboard',    icon: <LayoutDashboard size={15} /> },
    { path: '/admin/applications', label: 'Applications', icon: <ClipboardList size={15} /> },
    { path: '/admin/reports',      label: 'Reports',      icon: <BarChart2 size={15} /> },
    { path: '/admin/users',        label: 'Users',        icon: <Users size={15} /> },
  ];

  const applicantLinks = [
    { path: '/applicant/dashboard',  label: 'Dashboard',  icon: <LayoutDashboard size={15} /> },
    { path: '/applicant/apply',      label: 'Apply',      icon: <FileText size={15} /> },
    { path: '/applicant/documents',  label: 'Documents',  icon: <FolderOpen size={15} /> },
    { path: '/applicant/calculator', label: 'Calculator', icon: <Calculator size={15} /> },
  ];

  const links = isAdmin() ? adminLinks : applicantLinks;

  // High-quality real face placeholders
  const avatarUrl = isAdmin()
    ? 'https://images.unsplash.com/photo-1560250097-0b93528c311a?q=80&w=150&auto=format&fit=crop'
    : 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?q=80&w=150&auto=format&fit=crop';

  return (
    <nav className="bg-white border-b border-slate-200/80 sticky top-0 z-40 shadow-[0_1px_3px_rgb(0,0,0,0.04)]">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">

          {/* Logo */}
          <Link to={dashboardPath} className="flex items-center gap-2.5 group">
            <div className="w-8 h-8 bg-teal-600 rounded-lg flex items-center justify-center shadow-sm group-hover:bg-teal-700 transition-colors">
              <Building2 size={16} className="text-white" />
            </div>
            <span className="font-extrabold text-slate-800 tracking-tight">CapFinLoan</span>
            <span className="hidden sm:inline text-[10px] font-bold text-teal-600 bg-teal-50 border border-teal-100 px-1.5 py-0.5 rounded-full uppercase tracking-wider">
              {isAdmin() ? 'Admin' : 'Portal'}
            </span>
          </Link>

          <div className="flex items-center gap-4">
            {/* Nav links */}
            <div className="hidden md:flex gap-0.5">
              {links.map(({ path, label, icon }) => {
                const isActive = location.pathname === path;
                return (
                  <Link key={path} to={path}
                    className={`flex items-center gap-1.5 px-3 py-2 text-sm rounded-lg font-medium transition-all ${
                      isActive
                        ? 'bg-teal-50 text-teal-700 shadow-[0_1px_3px_rgb(0,0,0,0.04)]'
                        : 'text-slate-500 hover:text-slate-800 hover:bg-slate-50'
                    }`}>
                    <span className={isActive ? 'text-teal-600' : 'text-slate-400'}>{icon}</span>
                    {label}
                  </Link>
                );
              })}
            </div>

            {/* User profile */}
            <div className="flex items-center gap-3 pl-4 border-l border-slate-200">
              <div className="text-right hidden sm:block">
                <p className="text-sm font-semibold text-slate-800 leading-tight">{user?.fullName}</p>
                <p className="text-xs text-slate-400 font-medium">{user?.role}</p>
              </div>
              <img
                src={avatarUrl}
                alt={user?.fullName}
                className="w-9 h-9 rounded-full object-cover ring-2 ring-teal-100 shadow-sm"
              />
              <button onClick={handleLogout}
                className="flex items-center gap-1.5 text-sm text-slate-400 hover:text-red-500 hover:bg-red-50 transition-all px-2 py-1.5 rounded-lg">
                <LogOut size={15} />
                <span className="hidden sm:inline font-medium">Logout</span>
              </button>
            </div>
          </div>

        </div>
      </div>
    </nav>
  );
}

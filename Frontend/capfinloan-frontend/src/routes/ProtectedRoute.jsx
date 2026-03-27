import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export const ProtectedRoute = ({ children }) => {
  const { isAuthenticated, isAdmin, loading } = useAuth();
  if (loading) return (
    <div className="flex items-center justify-center h-screen">
      <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600" />
    </div>
  );
  
  if (!isAuthenticated()) return <Navigate to="/auth/login" replace />;
  if (isAdmin()) return <Navigate to="/admin/dashboard" replace />;
  
  return children;
};

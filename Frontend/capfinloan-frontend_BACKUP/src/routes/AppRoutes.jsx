import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ProtectedRoute } from './ProtectedRoute';
import { AdminRoute } from './AdminRoute';

import Landing from '../pages/Landing/Landing';
import Login from '../pages/Auth/Login';
import Register from '../pages/Auth/Register';
import ApplicantDashboard from '../pages/Applicant/Dashboard';
import ApplyLoan from '../pages/Applicant/ApplyLoan';
import Documents from '../pages/Applicant/Documents';
import StatusTracking from '../pages/Applicant/StatusTracking';
import AdminDashboard from '../pages/Admin/Dashboard';
import ApplicationQueue from '../pages/Admin/ApplicationQueue';
import ReviewApplication from '../pages/Admin/ReviewApplication';
import Reports from '../pages/Admin/Reports';
import UserManagement from '../pages/Admin/UserManagement';

export default function AppRoutes() {
  const { isAuthenticated, isAdmin } = useAuth();

  return (
    <Routes>
      <Route path="/" element={<Landing />} />
      <Route path="/auth/login" element={
        isAuthenticated()
          ? <Navigate to={isAdmin() ? '/admin/dashboard' : '/applicant/dashboard'} replace />
          : <Login />
      } />
      <Route path="/auth/register" element={
        isAuthenticated()
          ? <Navigate to="/applicant/dashboard" replace />
          : <Register />
      } />

      <Route path="/applicant/dashboard" element={
        <ProtectedRoute><ApplicantDashboard /></ProtectedRoute>
      } />
      <Route path="/applicant/apply" element={
        <ProtectedRoute><ApplyLoan /></ProtectedRoute>
      } />
      <Route path="/applicant/apply/:id" element={
        <ProtectedRoute><ApplyLoan /></ProtectedRoute>
      } />
      <Route path="/applicant/documents" element={
        <ProtectedRoute><Documents /></ProtectedRoute>
      } />
      <Route path="/applicant/status/:id" element={
        <ProtectedRoute><StatusTracking /></ProtectedRoute>
      } />

      <Route path="/admin/dashboard" element={
        <AdminRoute><AdminDashboard /></AdminRoute>
      } />
      <Route path="/admin/applications" element={
        <AdminRoute><ApplicationQueue /></AdminRoute>
      } />
      <Route path="/admin/review/:id" element={
        <AdminRoute><ReviewApplication /></AdminRoute>
      } />
      <Route path="/admin/reports" element={
        <AdminRoute><Reports /></AdminRoute>
      } />
      <Route path="/admin/users" element={
        <AdminRoute><UserManagement /></AdminRoute>
      } />

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

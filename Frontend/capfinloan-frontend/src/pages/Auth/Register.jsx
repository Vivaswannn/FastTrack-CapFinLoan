import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { useAuth } from '../../context/AuthContext';
import { authService } from '../../services/authService';

const getPasswordStrength = (password) => {
  let score = 0;
  if (password.length >= 8) score++;
  if (/[A-Z]/.test(password)) score++;
  if (/[a-z]/.test(password)) score++;
  if (/[0-9]/.test(password)) score++;
  if (/[^A-Za-z0-9]/.test(password)) score++;
  if (score <= 2) return { label: 'Weak', color: 'bg-red-400', width: '33%' };
  if (score <= 3) return { label: 'Fair', color: 'bg-yellow-400', width: '66%' };
  return { label: 'Strong', color: 'bg-emerald-500', width: '100%' };
};

export default function Register() {
  const [form, setForm] = useState({
    fullName: '', email: '', phone: '',
    password: '', confirmPassword: '',
  });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const strength = getPasswordStrength(form.password);

  const validate = () => {
    const errs = {};
    if (!form.fullName.trim()) errs.fullName = 'Full name is required';
    else if (form.fullName.trim().length < 2) errs.fullName = 'Name must be at least 2 characters';
    if (!form.email) errs.email = 'Email is required';
    else if (!/\S+@\S+\.\S+/.test(form.email)) errs.email = 'Enter a valid email address';
    if (!form.phone) errs.phone = 'Phone number is required';
    else if (!/^[6-9]\d{9}$/.test(form.phone)) errs.phone = 'Enter a valid 10-digit mobile number';
    if (!form.password) errs.password = 'Password is required';
    else if (form.password.length < 8) errs.password = 'Password must be at least 8 characters';
    else if (!/[A-Z]/.test(form.password)) errs.password = 'Must include an uppercase letter';
    else if (!/[0-9]/.test(form.password)) errs.password = 'Must include a number';
    else if (!/[^A-Za-z0-9]/.test(form.password)) errs.password = 'Must include a special character';
    if (!form.confirmPassword) errs.confirmPassword = 'Please confirm your password';
    else if (form.password !== form.confirmPassword) errs.confirmPassword = 'Passwords do not match';
    return errs;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const errs = validate();
    if (Object.keys(errs).length) {
      setErrors(errs);
      toast.error('Please fix the errors below');
      return;
    }
    setLoading(true);
    try {
      const res = await authService.register(form);
      const d = res.data.data;
      login({
        fullName: d.fullName,
        email: d.email,
        role: d.role,
        userId: d.userId
      }, d.token);
      toast.success('Account created! Welcome to CapFinLoan.');
      navigate('/applicant/dashboard');
    } catch (err) {
      const msg = err.response?.data?.message
        || err.message
        || 'Registration failed';
      if (msg.toLowerCase().includes('email') ||
          msg.toLowerCase().includes('exists')) {
        setErrors(p => ({ ...p, email: msg }));
      } else {
        setErrors(p => ({ ...p, general: msg }));
      }
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  const update = (field) => (e) => {
    setForm(p => ({ ...p, [field]: e.target.value }));
    setErrors(p => ({ ...p, [field]: '', general: '' }));
  };

  return (
    <div className="min-h-screen flex">
      {/* Left - Brand Panel */}
      <div className="hidden lg:flex lg:w-5/12 bg-gradient-to-br
        from-blue-900 via-blue-800 to-blue-600 flex-col justify-between p-12">
        <Link to="/" className="flex items-center gap-3">
          <div className="w-10 h-10 bg-white/20 rounded-xl
            flex items-center justify-center backdrop-blur-sm">
            <span className="text-white font-bold text-lg">₹</span>
          </div>
          <span className="text-white font-bold text-xl">CapFinLoan</span>
        </Link>
        <div>
          <h2 className="text-4xl font-bold text-white mb-4 leading-tight">
            Start Your<br/>Journey
          </h2>
          <p className="text-blue-200 text-lg mb-8 leading-relaxed">
            Join thousands of applicants who got their loans approved through CapFinLoan.
          </p>
          <div className="bg-white/10 rounded-2xl p-6 backdrop-blur-sm">
            <div className="text-white font-semibold mb-4">What you can do:</div>
            <div className="space-y-3 text-blue-100 text-sm">
              {[
                { icon: '📝', text: 'Apply for loans up to ₹1 Crore' },
                { icon: '📎', text: 'Upload documents digitally' },
                { icon: '⏱️', text: 'Track real-time application status' },
                { icon: '💰', text: 'Get EMI details instantly' },
              ].map(item => (
                <div key={item.text} className="flex items-center gap-2">
                  <span>{item.icon}</span>
                  <span>{item.text}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
        <p className="text-blue-300 text-sm">
          © 2025 CapFinLoan Financial Services
        </p>
      </div>

      {/* Right - Register Form */}
      <div className="flex-1 flex items-center justify-center bg-gray-50 p-6 overflow-y-auto">
        <div className="w-full max-w-lg py-8">
          {/* Mobile logo */}
          <div className="lg:hidden flex items-center gap-2 mb-8">
            <div className="w-8 h-8 bg-blue-600 rounded-lg
              flex items-center justify-center">
              <span className="text-white font-bold">₹</span>
            </div>
            <span className="font-bold text-gray-900 text-lg">CapFinLoan</span>
          </div>

          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-8">
            <h1 className="text-2xl font-bold text-gray-900 mb-1">Create Account</h1>
            <p className="text-gray-500 text-sm mb-6">
              Already have an account?{' '}
              <Link to="/auth/login"
                className="text-blue-600 hover:text-blue-700 font-medium hover:underline">
                Sign in
              </Link>
            </p>

            {errors.general && (
              <div className="bg-red-50 border border-red-200
                rounded-xl px-4 py-3 mb-5 flex items-start gap-2">
                <span className="text-red-500 text-lg leading-none mt-0.5">⚠</span>
                <p className="text-red-700 text-sm">{errors.general}</p>
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="label">Full Name</label>
                <input
                  className={`input-field ${errors.fullName ? 'input-error' : ''}`}
                  placeholder="Rahul Sharma"
                  value={form.fullName}
                  onChange={update('fullName')} />
                {errors.fullName && <p className="error-text">⚠ {errors.fullName}</p>}
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="label">Email Address</label>
                  <input type="email"
                    className={`input-field ${errors.email ? 'input-error' : ''}`}
                    placeholder="rahul@example.com"
                    value={form.email}
                    onChange={update('email')} />
                  {errors.email && <p className="error-text">⚠ {errors.email}</p>}
                </div>
                <div>
                  <label className="label">Mobile Number</label>
                  <input
                    className={`input-field ${errors.phone ? 'input-error' : ''}`}
                    placeholder="9876543210"
                    maxLength={10}
                    value={form.phone}
                    onChange={update('phone')} />
                  {errors.phone && <p className="error-text">⚠ {errors.phone}</p>}
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="label">Password</label>
                  <div className="relative">
                    <input
                      type={showPassword ? 'text' : 'password'}
                      className={`input-field pr-12 ${errors.password ? 'input-error' : ''}`}
                      placeholder="Min. 8 characters"
                      value={form.password}
                      onChange={update('password')} />
                    <button type="button"
                      onClick={() => setShowPassword(p => !p)}
                      className="absolute right-3 top-1/2
                        -translate-y-1/2 text-gray-400
                        hover:text-gray-600 text-lg transition-colors">
                      {showPassword ? '🙈' : '👁️'}
                    </button>
                  </div>
                  {form.password && (
                    <div className="mt-1.5">
                      <div className="h-1.5 bg-gray-200 rounded-full overflow-hidden">
                        <div className={`h-full rounded-full transition-all duration-300 ${strength.color}`}
                          style={{ width: strength.width }} />
                      </div>
                      <p className="text-xs mt-0.5 text-gray-500">
                        Strength: {strength.label}
                      </p>
                    </div>
                  )}
                  {errors.password && <p className="error-text">⚠ {errors.password}</p>}
                </div>
                <div>
                  <label className="label">Confirm Password</label>
                  <input type="password"
                    className={`input-field ${errors.confirmPassword ? 'input-error' : ''}`}
                    placeholder="Repeat password"
                    value={form.confirmPassword}
                    onChange={update('confirmPassword')} />
                  {errors.confirmPassword && (
                    <p className="error-text">⚠ {errors.confirmPassword}</p>
                  )}
                </div>
              </div>

              <button type="submit" disabled={loading}
                className="btn-primary w-full py-3 text-base
                  flex items-center justify-center gap-2 mt-2">
                {loading ? (
                  <><div className="spinner" />Creating Account...</>
                ) : 'Create Account →'}
              </button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}

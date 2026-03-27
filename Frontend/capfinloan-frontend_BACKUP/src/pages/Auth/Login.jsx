import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { useAuth } from '../../context/AuthContext';
import { authService } from '../../services/authService';

export default function Login() {
  const [form, setForm] = useState({ email: '', password: '' });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [requiresOtp, setRequiresOtp] = useState(false);
  const [otpCode, setOtpCode] = useState('');
  const { login } = useAuth();
  const navigate = useNavigate();

  const validate = () => {
    const errs = {};
    if (!form.email) errs.email = 'Email is required';
    else if (!/\S+@\S+\.\S+/.test(form.email))
      errs.email = 'Enter a valid email address';
    
    if (!requiresOtp) {
      if (!form.password) errs.password = 'Password is required';
    } else {
      if (!otpCode || otpCode.length !== 6) errs.otpCode = 'Enter a valid 6-digit OTP';
    }
    return errs;
  };

  const processLoginSuccess = (d) => {
    login({
      fullName: d.fullName,
      email: d.email,
      role: d.role,
      userId: d.userId
    }, d.token);
    toast.success(`Welcome back, ${d.fullName}!`);
    navigate(d.role === 'Admin'
      ? '/admin/dashboard'
      : '/applicant/dashboard');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const errs = validate();
    if (Object.keys(errs).length) {
      setErrors(errs);
      return;
    }
    setLoading(true);
    try {
      if (!requiresOtp) {
        const res = await authService.login(form);
        const d = res.data.data;
        if (d.requiresOtp) {
          setRequiresOtp(true);
          toast.success('OTP sent to your email!');
          return;
        }
        processLoginSuccess(d);
      } else {
        const res = await authService.verifyOtp({ email: form.email, otpCode });
        processLoginSuccess(res.data.data);
      }
    } catch (err) {
      const msg = err.response?.data?.message
        || err.message
        || 'Login failed. Please try again.';
      setErrors({ general: msg });
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex">
      {/* Left - Brand Panel */}
      <div className="hidden lg:flex lg:w-1/2 bg-gradient-to-br
        from-blue-900 via-blue-800 to-blue-600 p-12
        flex-col justify-between">
        <Link to="/" className="flex items-center gap-3">
          <div className="w-10 h-10 bg-white/20 rounded-xl
            flex items-center justify-center backdrop-blur-sm">
            <span className="text-white font-bold text-lg">₹</span>
          </div>
          <span className="text-white font-bold text-xl">
            CapFinLoan
          </span>
        </Link>
        <div>
          <h2 className="text-4xl font-bold text-white mb-4
            leading-tight">
            Your Financial<br/>Partner
          </h2>
          <p className="text-blue-200 text-lg mb-10 leading-relaxed">
            Fast loan approvals, transparent process,
            and real-time tracking — all in one place.
          </p>
          <div className="space-y-4">
            {[
              { icon: '⚡', text: 'Decisions within 24-48 hours' },
              { icon: '🔒', text: 'Bank-grade security' },
              { icon: '📊', text: 'Real-time status tracking' },
              { icon: '💰', text: 'Competitive interest rates' },
            ].map(item => (
              <div key={item.text}
                className="flex items-center gap-3
                  text-blue-100">
                <span className="text-xl">{item.icon}</span>
                <span>{item.text}</span>
              </div>
            ))}
          </div>
        </div>
        <p className="text-blue-300 text-sm">
          © 2025 CapFinLoan Financial Services
        </p>
      </div>

      {/* Right - Login Form */}
      <div className="flex-1 flex items-center justify-center
        bg-gray-50 p-6">
        <div className="w-full max-w-md">
          {/* Mobile logo */}
          <div className="lg:hidden flex items-center gap-2 mb-8">
            <div className="w-8 h-8 bg-blue-600 rounded-lg
              flex items-center justify-center">
              <span className="text-white font-bold">₹</span>
            </div>
            <span className="font-bold text-gray-900 text-lg">
              CapFinLoan
            </span>
          </div>

          <div className="bg-white rounded-2xl shadow-sm
            border border-gray-100 p-8">
            <h1 className="text-2xl font-bold text-gray-900 mb-1">
              Sign In
            </h1>
            <p className="text-gray-500 text-sm mb-7">
              New to CapFinLoan?{' '}
              <Link to="/auth/register"
                className="text-blue-600 hover:text-blue-700
                  font-medium hover:underline">
                Create an account
              </Link>
            </p>

            {errors.general && (
              <div className="bg-red-50 border border-red-200
                rounded-xl px-4 py-3 mb-5 flex items-start gap-2">
                <span className="text-red-500 text-lg
                  leading-none mt-0.5">⚠</span>
                <p className="text-red-700 text-sm">
                  {errors.general}
                </p>
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-5">
              {!requiresOtp ? (
                <>
                  <div>
                    <label className="label">Email Address</label>
                    <input
                      type="email"
                      placeholder="you@example.com"
                      className={`input-field ${errors.email ? 'input-error' : ''}`}
                      value={form.email}
                      onChange={e => {
                        setForm(p => ({...p, email: e.target.value}));
                        setErrors(p => ({...p, email: ''}));
                      }}
                    />
                    {errors.email && <p className="error-text">⚠ {errors.email}</p>}
                  </div>

                  <div>
                    <label className="label">Password</label>
                    <div className="relative">
                      <input
                        type={showPassword ? 'text' : 'password'}
                        placeholder="Enter your password"
                        className={`input-field pr-12 ${errors.password ? 'input-error' : ''}`}
                        value={form.password}
                        onChange={e => {
                          setForm(p => ({...p, password: e.target.value}));
                          setErrors(p => ({...p, password: ''}));
                        }}
                      />
                      <button type="button"
                        onClick={() => setShowPassword(p => !p)}
                        className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 text-lg transition-colors">
                        {showPassword ? '🙈' : '👁️'}
                      </button>
                    </div>
                    {errors.password && <p className="error-text">⚠ {errors.password}</p>}
                  </div>
                </>
              ) : (
                <>
                  <div className="mb-4 p-4 bg-blue-50 rounded-xl border border-blue-100">
                    <p className="text-sm text-blue-800">
                      We've sent a 6-digit OTP to <strong>{form.email}</strong>. Please enter it below to verify your identity.
                    </p>
                  </div>
                  <div>
                    <label className="label">OTP Code</label>
                    <input
                      type="text"
                      placeholder="123456"
                      maxLength={6}
                      className={`input-field text-center tracking-[0.5em] text-lg font-mono ${errors.otpCode ? 'input-error' : ''}`}
                      value={otpCode}
                      onChange={e => {
                        setOtpCode(e.target.value.replace(/\D/g, ''));
                        setErrors(p => ({...p, otpCode: ''}));
                      }}
                    />
                    {errors.otpCode && <p className="error-text">⚠ {errors.otpCode}</p>}
                  </div>
                </>
              )}

              <button type="submit" disabled={loading}
                className="btn-primary w-full py-3 text-base
                  flex items-center justify-center gap-2 mt-2">
                {loading ? (
                  <><div className="spinner" />Signing in...</>
                ) : 'Sign In →'}
              </button>
            </form>

            <div className="mt-6 pt-5 border-t border-gray-100">
              <p className="text-center text-xs text-gray-400">
                Admin access: admin@capfinloan.com
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

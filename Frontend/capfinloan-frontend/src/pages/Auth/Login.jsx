import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { Eye, EyeOff, AlertCircle, Zap, Shield, TrendingUp, Lock, Building2, ArrowLeft, KeyRound } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { authService } from '../../services/authService';

export default function Login() {
  const [form, setForm] = useState({ email: '', password: '' });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [requiresOtp, setRequiresOtp] = useState(false);
  const [otpCode, setOtpCode] = useState('');

  // Forgot password state
  const [forgotMode, setForgotMode] = useState(false);  // overlay mode
  const [forgotStep, setForgotStep] = useState(1);       // 1=email, 2=otp+newpw
  const [forgotEmail, setForgotEmail] = useState('');
  const [forgotOtp, setForgotOtp] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [forgotLoading, setForgotLoading] = useState(false);
  const [forgotErrors, setForgotErrors] = useState({});

  const { login } = useAuth();
  const navigate = useNavigate();

  // ── Login helpers ─────────────────────────────────────────────────────────
  const validate = () => {
    const errs = {};
    if (!form.email) errs.email = 'Email is required';
    else if (!/\S+@\S+\.\S+/.test(form.email)) errs.email = 'Enter a valid email address';
    if (!requiresOtp) {
      if (!form.password) errs.password = 'Password is required';
    } else {
      if (!otpCode || otpCode.length !== 6) errs.otpCode = 'Enter a valid 6-digit OTP';
    }
    return errs;
  };

  const processLoginSuccess = (d) => {
    login({ fullName: d.fullName, email: d.email, role: d.role, userId: d.userId }, d.token);
    toast.success(`Welcome back, ${d.fullName}!`);
    navigate(d.role === 'Admin' ? '/admin/dashboard' : '/applicant/dashboard');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const errs = validate();
    if (Object.keys(errs).length) { setErrors(errs); return; }
    setLoading(true);
    try {
      if (!requiresOtp) {
        const res = await authService.login(form);
        const d = res.data.data;
        if (d.requiresOtp) { setRequiresOtp(true); toast.success('OTP sent to your email!'); return; }
        processLoginSuccess(d);
      } else {
        const res = await authService.verifyOtp({ email: form.email, otpCode });
        processLoginSuccess(res.data.data);
      }
    } catch (err) {
      const msg = err.response?.data?.message || err.message || 'Login failed. Please try again.';
      setErrors({ general: msg });
      toast.error(msg);
    } finally { setLoading(false); }
  };

  // ── Forgot password helpers ───────────────────────────────────────────────
  const openForgot = () => {
    setForgotMode(true);
    setForgotStep(1);
    setForgotEmail(form.email); // pre-fill from login form if available
    setForgotOtp('');
    setNewPassword('');
    setConfirmPassword('');
    setForgotErrors({});
  };

  const closeForgot = () => {
    setForgotMode(false);
    setForgotStep(1);
    setForgotErrors({});
  };

  const handleForgotStep1 = async (e) => {
    e.preventDefault();
    if (!forgotEmail || !/\S+@\S+\.\S+/.test(forgotEmail)) {
      setForgotErrors({ email: 'Enter a valid email address' });
      return;
    }
    setForgotLoading(true);
    try {
      await authService.forgotPassword({ email: forgotEmail });
      toast.success('OTP sent! Check your email.');
      setForgotStep(2);
      setForgotErrors({});
    } catch (err) {
      const msg = err.response?.data?.message || 'Failed to send OTP. Try again.';
      setForgotErrors({ general: msg });
      toast.error(msg);
    } finally { setForgotLoading(false); }
  };

  const handleForgotStep2 = async (e) => {
    e.preventDefault();
    const errs = {};
    if (!forgotOtp || forgotOtp.length !== 6) errs.otp = 'Enter the 6-digit OTP';
    if (!newPassword || newPassword.length < 6) errs.newPassword = 'Password must be at least 6 characters';
    if (newPassword !== confirmPassword) errs.confirmPassword = 'Passwords do not match';
    if (Object.keys(errs).length) { setForgotErrors(errs); return; }

    setForgotLoading(true);
    try {
      await authService.resetPassword({ email: forgotEmail, otpCode: forgotOtp, newPassword });
      toast.success('Password reset successfully! Please sign in.');
      closeForgot();
      setForm(p => ({ ...p, email: forgotEmail, password: '' }));
    } catch (err) {
      const msg = err.response?.data?.message || 'Reset failed. Check your OTP and try again.';
      setForgotErrors({ general: msg });
      toast.error(msg);
    } finally { setForgotLoading(false); }
  };

  // ── Render ─────────────────────────────────────────────────────────────────
  return (
    <div className="min-h-screen flex">

      {/* ── Left: Sun-Filled Office Photography (50%) ── */}
      <div className="hidden lg:block lg:w-1/2 relative overflow-hidden bg-slate-100">
        <img
          src="https://images.unsplash.com/photo-1497366216548-37526070297c?q=80&w=2069&auto=format&fit=crop"
          alt="Modern financial office"
          className="absolute inset-0 w-full h-full object-cover"
        />
        <div className="absolute inset-0 bg-white/75" />

        <div className="relative z-10 h-full flex flex-col justify-between p-14">
          <Link to="/" className="flex items-center gap-3 group">
            <div className="w-12 h-12 bg-white/90 backdrop-blur-sm rounded-2xl flex items-center justify-center border border-slate-200/60 shadow-sm group-hover:shadow-md transition-shadow">
              <Building2 size={22} className="text-teal-600" />
            </div>
            <span className="text-slate-900 font-extrabold text-2xl tracking-tight drop-shadow-sm">CapFinLoan.</span>
          </Link>

          <div>
            <h2 className="text-[52px] font-extrabold text-slate-900 leading-[1.06] tracking-tight mb-5">
              Institutional<br />
              <span className="text-teal-600">Finance</span><br />
              Redefined.
            </h2>
            <p className="text-slate-600 text-lg mb-10 leading-relaxed max-w-sm font-light">
              Secure, rapid and transparent loan origination for modern financial institutions.
            </p>

            <div className="space-y-4">
              {[
                { icon: <Zap size={16} />, text: 'Automated decisions in 24–48 hours' },
                { icon: <Shield size={16} />, text: 'AES-256 bank-grade encryption' },
                { icon: <TrendingUp size={16} />, text: 'Real-time application telemetry' },
              ].map(item => (
                <div key={item.text} className="flex items-center gap-3 text-slate-700 text-sm">
                  <div className="w-8 h-8 rounded-lg bg-white/90 border border-slate-200/60 flex items-center justify-center text-teal-600 shrink-0 shadow-sm">
                    {item.icon}
                  </div>
                  {item.text}
                </div>
              ))}
            </div>
          </div>

          <p className="text-slate-400 text-xs">
            © {new Date().getFullYear()} CapFinLoan Systems · All rights reserved
          </p>
        </div>
      </div>

      {/* ── Right: Form Panel ── */}
      <div className="flex-1 lg:w-1/2 flex items-center justify-center bg-[#F8FAFC] p-6 sm:p-12">
        <div className="w-full max-w-[420px]">

          {/* Mobile logo */}
          <div className="lg:hidden flex items-center gap-2 mb-10">
            <div className="w-10 h-10 bg-teal-600 rounded-xl flex items-center justify-center shadow-sm">
              <Building2 size={18} className="text-white" />
            </div>
            <span className="font-extrabold text-slate-900 text-xl tracking-tight">CapFinLoan.</span>
          </div>

          {/* ── Forgot Password Card ── */}
          {forgotMode ? (
            <div className="bg-white rounded-[28px] shadow-[0_8px_30px_rgb(0,0,0,0.06)] ring-1 ring-slate-100 p-8 sm:p-10">

              {/* Header */}
              <div className="mb-7">
                <button onClick={closeForgot}
                  className="flex items-center gap-1.5 text-xs font-semibold text-slate-400 hover:text-slate-600 transition-colors mb-5">
                  <ArrowLeft size={13} /> Back to Sign In
                </button>
                <div className="w-12 h-12 bg-teal-50 rounded-2xl flex items-center justify-center mb-4">
                  <KeyRound size={22} className="text-teal-600" />
                </div>
                <h1 className="text-2xl font-extrabold text-slate-900 tracking-tight mb-1">
                  {forgotStep === 1 ? 'Reset password' : 'Set new password'}
                </h1>
                <p className="text-slate-500 text-sm">
                  {forgotStep === 1
                    ? 'Enter your email and we\'ll send you a reset code.'
                    : `Enter the OTP sent to ${forgotEmail} and choose a new password.`}
                </p>
              </div>

              {/* Step indicators */}
              <div className="flex items-center gap-2 mb-7">
                {[1, 2].map(s => (
                  <div key={s} className={`h-1.5 flex-1 rounded-full transition-all ${s <= forgotStep ? 'bg-teal-500' : 'bg-slate-100'}`} />
                ))}
              </div>

              {forgotErrors.general && (
                <div className="bg-red-50 border border-red-100 rounded-xl p-4 mb-5 flex items-start gap-3">
                  <AlertCircle size={16} className="text-red-400 shrink-0 mt-0.5" />
                  <p className="text-red-700 text-sm font-medium">{forgotErrors.general}</p>
                </div>
              )}

              {/* Step 1 — Email */}
              {forgotStep === 1 && (
                <form onSubmit={handleForgotStep1} className="space-y-5">
                  <div className="space-y-1.5">
                    <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest">
                      Email Address
                    </label>
                    <input
                      type="email"
                      placeholder="you@institution.com"
                      className={`w-full bg-white px-4 py-3.5 border ${forgotErrors.email ? 'border-red-300 focus:border-red-400 focus:ring-red-400/20' : 'border-slate-200 hover:border-slate-300 focus:border-teal-500 focus:ring-teal-500/20'} rounded-xl text-slate-800 text-sm placeholder:text-slate-300 focus:outline-none focus:ring-2 transition-all`}
                      value={forgotEmail}
                      onChange={e => { setForgotEmail(e.target.value); setForgotErrors(p => ({ ...p, email: '' })); }}
                    />
                    {forgotErrors.email && <p className="text-red-500 text-xs font-medium">{forgotErrors.email}</p>}
                  </div>
                  <button type="submit" disabled={forgotLoading}
                    className="w-full bg-teal-600 hover:bg-teal-700 text-white py-3.5 rounded-xl font-bold text-sm shadow-[0_4px_14px_rgba(20,184,166,0.25)] hover:-translate-y-0.5 transition-all duration-200 disabled:opacity-60 disabled:pointer-events-none flex justify-center items-center gap-2">
                    {forgotLoading
                      ? <><div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" /> Sending OTP...</>
                      : 'Send Reset Code'}
                  </button>
                </form>
              )}

              {/* Step 2 — OTP + New Password */}
              {forgotStep === 2 && (
                <form onSubmit={handleForgotStep2} className="space-y-4">
                  <div className="space-y-1.5">
                    <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest text-center">
                      Verification Code
                    </label>
                    <input
                      type="text"
                      placeholder="000000"
                      maxLength={6}
                      className={`w-full bg-white px-4 py-4 border ${forgotErrors.otp ? 'border-red-300' : 'border-slate-200 focus:border-teal-500 focus:ring-teal-500/20'} rounded-xl text-slate-800 text-2xl text-center font-mono tracking-[0.5em] focus:outline-none focus:ring-2 transition-all`}
                      value={forgotOtp}
                      onChange={e => { setForgotOtp(e.target.value.replace(/\D/g, '')); setForgotErrors(p => ({ ...p, otp: '' })); }}
                    />
                    {forgotErrors.otp && <p className="text-red-500 text-xs font-medium text-center">{forgotErrors.otp}</p>}
                  </div>

                  <div className="space-y-1.5">
                    <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest">
                      New Password
                    </label>
                    <div className="relative">
                      <input
                        type={showNewPassword ? 'text' : 'password'}
                        placeholder="Min. 6 characters"
                        className={`w-full bg-white px-4 py-3.5 pr-12 border ${forgotErrors.newPassword ? 'border-red-300' : 'border-slate-200 focus:border-teal-500 focus:ring-teal-500/20'} rounded-xl text-slate-800 text-sm placeholder:text-slate-300 focus:outline-none focus:ring-2 transition-all`}
                        value={newPassword}
                        onChange={e => { setNewPassword(e.target.value); setForgotErrors(p => ({ ...p, newPassword: '' })); }}
                      />
                      <button type="button" onClick={() => setShowNewPassword(p => !p)}
                        className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-300 hover:text-slate-500 transition-colors">
                        {showNewPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                      </button>
                    </div>
                    {forgotErrors.newPassword && <p className="text-red-500 text-xs font-medium">{forgotErrors.newPassword}</p>}
                  </div>

                  <div className="space-y-1.5">
                    <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest">
                      Confirm Password
                    </label>
                    <input
                      type="password"
                      placeholder="Repeat new password"
                      className={`w-full bg-white px-4 py-3.5 border ${forgotErrors.confirmPassword ? 'border-red-300' : 'border-slate-200 focus:border-teal-500 focus:ring-teal-500/20'} rounded-xl text-slate-800 text-sm placeholder:text-slate-300 focus:outline-none focus:ring-2 transition-all`}
                      value={confirmPassword}
                      onChange={e => { setConfirmPassword(e.target.value); setForgotErrors(p => ({ ...p, confirmPassword: '' })); }}
                    />
                    {forgotErrors.confirmPassword && <p className="text-red-500 text-xs font-medium">{forgotErrors.confirmPassword}</p>}
                  </div>

                  <div className="flex gap-3 pt-1">
                    <button type="button" onClick={() => { setForgotStep(1); setForgotErrors({}); }}
                      className="flex-1 px-4 py-3.5 border border-slate-200 hover:border-slate-300 text-slate-600 font-semibold text-sm rounded-xl transition-all hover:bg-slate-50">
                      Back
                    </button>
                    <button type="submit" disabled={forgotLoading}
                      className="flex-2 flex-grow bg-teal-600 hover:bg-teal-700 text-white py-3.5 rounded-xl font-bold text-sm shadow-[0_4px_14px_rgba(20,184,166,0.25)] hover:-translate-y-0.5 transition-all duration-200 disabled:opacity-60 disabled:pointer-events-none flex justify-center items-center gap-2">
                      {forgotLoading
                        ? <><div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" /> Resetting...</>
                        : 'Reset Password'}
                    </button>
                  </div>

                  <button type="button" onClick={handleForgotStep1}
                    className="w-full text-xs text-slate-400 hover:text-teal-600 font-medium transition-colors pt-1">
                    Didn't receive a code? Resend OTP
                  </button>
                </form>
              )}
            </div>

          ) : (

            /* ── Normal Login Card ── */
            <div className="bg-white rounded-[28px] shadow-[0_8px_30px_rgb(0,0,0,0.06)] ring-1 ring-slate-100 p-8 sm:p-10">
              <div className="mb-8">
                <h1 className="text-3xl font-extrabold text-slate-900 tracking-tight mb-2">
                  Welcome back
                </h1>
                <p className="text-slate-500 text-sm leading-relaxed">
                  Sign in to access your secure financial dashboard.
                </p>
              </div>

              {errors.general && (
                <div className="bg-red-50 border border-red-100 rounded-xl p-4 mb-6 flex items-start gap-3">
                  <AlertCircle size={18} className="text-red-400 shrink-0 mt-0.5" />
                  <p className="text-red-700 text-sm font-medium">{errors.general}</p>
                </div>
              )}

              <form onSubmit={handleSubmit} className="space-y-5">
                {!requiresOtp ? (
                  <>
                    <div className="space-y-1.5">
                      <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest" htmlFor="email">
                        Email Address
                      </label>
                      <input
                        id="email" type="email"
                        placeholder="you@institution.com"
                        className={`w-full bg-white px-4 py-3.5 border ${errors.email ? 'border-red-300 focus:border-red-400 focus:ring-red-400/20' : 'border-slate-200 hover:border-slate-300 focus:border-teal-500 focus:ring-teal-500/20'} rounded-xl text-slate-800 text-sm placeholder:text-slate-300 focus:outline-none focus:ring-2 transition-all`}
                        value={form.email}
                        onChange={e => { setForm(p => ({...p, email: e.target.value})); setErrors(p => ({...p, email: ''})); }}
                      />
                      {errors.email && <p className="text-red-500 text-xs font-medium">{errors.email}</p>}
                    </div>

                    <div className="space-y-1.5">
                      <div className="flex justify-between items-center">
                        <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest" htmlFor="password">
                          Password
                        </label>
                        <button type="button" onClick={openForgot}
                          className="text-xs font-semibold text-teal-600 hover:text-teal-800 transition-colors">
                          Forgot password?
                        </button>
                      </div>
                      <div className="relative">
                        <input
                          id="password"
                          type={showPassword ? 'text' : 'password'}
                          placeholder="••••••••••••"
                          className={`w-full bg-white px-4 py-3.5 pr-12 border ${errors.password ? 'border-red-300 focus:border-red-400 focus:ring-red-400/20' : 'border-slate-200 hover:border-slate-300 focus:border-teal-500 focus:ring-teal-500/20'} rounded-xl text-slate-800 text-sm placeholder:text-slate-300 focus:outline-none focus:ring-2 transition-all`}
                          value={form.password}
                          onChange={e => { setForm(p => ({...p, password: e.target.value})); setErrors(p => ({...p, password: ''})); }}
                        />
                        <button type="button" onClick={() => setShowPassword(p => !p)}
                          className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-300 hover:text-slate-500 transition-colors">
                          {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                        </button>
                      </div>
                      {errors.password && <p className="text-red-500 text-xs font-medium">{errors.password}</p>}
                    </div>
                  </>
                ) : (
                  <>
                    <div className="bg-teal-50 border border-teal-100 rounded-xl p-5 text-center">
                      <p className="text-sm text-teal-900 leading-relaxed">
                        A 6-digit token was dispatched to<br />
                        <strong className="text-teal-700">{form.email}</strong>
                      </p>
                    </div>
                    <div className="space-y-1.5">
                      <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest text-center" htmlFor="otpCode">
                        Verification Code
                      </label>
                      <input
                        id="otpCode" type="text"
                        placeholder="000000"
                        maxLength={6}
                        className={`w-full bg-white px-4 py-4 border ${errors.otpCode ? 'border-red-300' : 'border-slate-200 focus:border-teal-500 focus:ring-teal-500/20'} rounded-xl text-slate-800 text-2xl text-center font-mono tracking-[0.5em] focus:outline-none focus:ring-2 transition-all`}
                        value={otpCode}
                        onChange={e => { setOtpCode(e.target.value.replace(/\D/g, '')); setErrors(p => ({...p, otpCode: ''})); }}
                      />
                      {errors.otpCode && <p className="text-red-500 text-xs font-medium text-center">{errors.otpCode}</p>}
                    </div>
                  </>
                )}

                <button type="submit" disabled={loading}
                  className="w-full bg-teal-600 hover:bg-teal-700 active:bg-teal-800 text-white py-4 rounded-xl font-bold text-sm shadow-[0_4px_14px_rgba(20,184,166,0.25)] hover:shadow-[0_6px_20px_rgba(20,184,166,0.35)] hover:-translate-y-0.5 transition-all duration-200 disabled:opacity-60 disabled:pointer-events-none flex justify-center items-center gap-2 mt-2">
                  {loading ? (
                    <><div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" /> Authorizing...</>
                  ) : (
                    requiresOtp ? 'Verify & Continue' : 'Sign In Securely'
                  )}
                </button>
              </form>

              <div className="mt-8 pt-6 border-t border-slate-100 flex items-center justify-center gap-5">
                <div className="flex items-center gap-1.5">
                  <Lock size={12} className="text-slate-300" />
                  <span className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">256-bit SSL</span>
                </div>
                <div className="w-1 h-1 rounded-full bg-slate-200" />
                <div className="flex items-center gap-1.5">
                  <Shield size={12} className="text-slate-300" />
                  <span className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">FCA Compliant</span>
                </div>
              </div>
            </div>
          )}

          <p className="text-center text-sm text-slate-400 mt-6">
            New to CapFinLoan?{' '}
            <Link to="/auth/register" className="font-bold text-teal-600 hover:text-teal-800 transition-colors">
              Create account
            </Link>
          </p>
        </div>
      </div>

    </div>
  );
}

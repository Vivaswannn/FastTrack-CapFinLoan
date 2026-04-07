import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { Eye, EyeOff, AlertTriangle, Building2, ClipboardList, Paperclip, Clock, IndianRupee, ArrowRight } from 'lucide-react';
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
  if (score <= 3) return { label: 'Fair', color: 'bg-amber-400', width: '66%' };
  return { label: 'Strong', color: 'bg-teal-500', width: '100%' };
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
    if (Object.keys(errs).length) { setErrors(errs); toast.error('Please fix the errors below'); return; }
    setLoading(true);
    try {
      const res = await authService.register(form);
      const d = res.data.data;
      login({ fullName: d.fullName, email: d.email, role: d.role, userId: d.userId }, d.token);
      toast.success('Account created! Welcome to CapFinLoan.');
      navigate('/applicant/dashboard');
    } catch (err) {
      const msg = err.response?.data?.message || err.message || 'Registration failed';
      if (msg.toLowerCase().includes('email') || msg.toLowerCase().includes('exists')) {
        setErrors(p => ({ ...p, email: msg }));
      } else {
        setErrors(p => ({ ...p, general: msg }));
      }
      toast.error(msg);
    } finally { setLoading(false); }
  };

  const update = (field) => (e) => {
    setForm(p => ({ ...p, [field]: e.target.value }));
    setErrors(p => ({ ...p, [field]: '', general: '' }));
  };

  const inputCls = (err) =>
    `w-full bg-white px-4 py-3 border ${err
      ? 'border-red-300 focus:border-red-400 focus:ring-red-400/20'
      : 'border-slate-200 hover:border-slate-300 focus:border-teal-500 focus:ring-teal-500/20'
    } rounded-xl text-slate-800 text-sm placeholder:text-slate-300 focus:outline-none focus:ring-2 transition-all`;

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
            <h2 className="text-[50px] font-extrabold text-slate-900 leading-[1.08] tracking-tight mb-5">
              Your Financial<br />
              <span className="text-teal-600">Future</span><br />
              Starts Here.
            </h2>
            <p className="text-slate-600 text-lg mb-10 leading-relaxed max-w-sm font-light">
              Join thousands of applicants who secured their dreams through CapFinLoan.
            </p>

            <div className="bg-white/70 backdrop-blur-sm border border-slate-200/60 rounded-xl p-6 space-y-4 shadow-sm">
              {[
                { icon: <ClipboardList size={15} />, text: 'Apply for loans up to ₹1 Crore' },
                { icon: <Paperclip size={15} />, text: 'Upload KYC documents digitally' },
                { icon: <Clock size={15} />, text: 'Track status at every step' },
                { icon: <IndianRupee size={15} />, text: 'Instant EMI calculations' },
              ].map(item => (
                <div key={item.text} className="flex items-center gap-3 text-slate-700 text-sm">
                  <div className="w-7 h-7 rounded-lg bg-teal-50 border border-teal-100 flex items-center justify-center text-teal-600 shrink-0">
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

      {/* ── Right: Pristine Form Panel ── */}
      <div className="flex-1 lg:w-1/2 flex items-center justify-center bg-[#F8FAFC] p-6 overflow-y-auto">
        <div className="w-full max-w-lg py-8">

          {/* Mobile logo */}
          <div className="lg:hidden flex items-center gap-2 mb-8">
            <div className="w-10 h-10 bg-teal-600 rounded-xl flex items-center justify-center shadow-sm">
              <Building2 size={18} className="text-white" />
            </div>
            <span className="font-extrabold text-slate-900 text-xl tracking-tight">CapFinLoan.</span>
          </div>

          <div className="bg-white rounded-[28px] shadow-md border border-slate-100 p-8">
            <div className="mb-6">
              <h1 className="text-3xl font-extrabold text-slate-900 tracking-tight mb-1">Create Account</h1>
              <p className="text-slate-400 text-sm">
                Already registered?{' '}
                <Link to="/auth/login" className="font-bold text-teal-600 hover:text-teal-800 transition-colors">
                  Sign in
                </Link>
              </p>
            </div>

            {errors.general && (
              <div className="bg-red-50 border border-red-100 rounded-xl px-4 py-3 mb-5 flex items-start gap-2">
                <AlertTriangle size={16} className="text-red-400 shrink-0 mt-0.5" />
                <p className="text-red-700 text-sm">{errors.general}</p>
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1.5">Full Name</label>
                <input className={inputCls(errors.fullName)} placeholder="Rahul Sharma"
                  value={form.fullName} onChange={update('fullName')} />
                {errors.fullName && (
                  <p className="text-red-500 text-xs font-medium mt-1 flex items-center gap-1">
                    <AlertTriangle size={11} /> {errors.fullName}
                  </p>
                )}
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1.5">Email Address</label>
                  <input type="email" className={inputCls(errors.email)} placeholder="rahul@example.com"
                    value={form.email} onChange={update('email')} />
                  {errors.email && (
                    <p className="text-red-500 text-xs font-medium mt-1 flex items-center gap-1">
                      <AlertTriangle size={11} /> {errors.email}
                    </p>
                  )}
                </div>
                <div>
                  <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1.5">Mobile Number</label>
                  <input className={inputCls(errors.phone)} placeholder="9876543210" maxLength={10}
                    value={form.phone} onChange={update('phone')} />
                  {errors.phone && (
                    <p className="text-red-500 text-xs font-medium mt-1 flex items-center gap-1">
                      <AlertTriangle size={11} /> {errors.phone}
                    </p>
                  )}
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1.5">Password</label>
                  <div className="relative">
                    <input type={showPassword ? 'text' : 'password'}
                      className={`${inputCls(errors.password)} pr-12`}
                      placeholder="Min. 8 characters" value={form.password} onChange={update('password')} />
                    <button type="button" onClick={() => setShowPassword(p => !p)}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-300 hover:text-slate-500 transition-colors">
                      {showPassword ? <EyeOff size={17} /> : <Eye size={17} />}
                    </button>
                  </div>
                  {form.password && (
                    <div className="mt-1.5">
                      <div className="h-1.5 bg-slate-100 rounded-full overflow-hidden">
                        <div className={`h-full rounded-full transition-all duration-300 ${strength.color}`}
                          style={{ width: strength.width }} />
                      </div>
                      <p className="text-xs mt-0.5 text-slate-400">
                        Strength: <span className="font-semibold text-slate-600">{strength.label}</span>
                      </p>
                    </div>
                  )}
                  {errors.password && (
                    <p className="text-red-500 text-xs font-medium mt-1 flex items-center gap-1">
                      <AlertTriangle size={11} /> {errors.password}
                    </p>
                  )}
                </div>
                <div>
                  <label className="block text-xs font-bold text-slate-500 uppercase tracking-widest mb-1.5">Confirm Password</label>
                  <input type="password" className={inputCls(errors.confirmPassword)}
                    placeholder="Repeat password" value={form.confirmPassword} onChange={update('confirmPassword')} />
                  {errors.confirmPassword && (
                    <p className="text-red-500 text-xs font-medium mt-1 flex items-center gap-1">
                      <AlertTriangle size={11} /> {errors.confirmPassword}
                    </p>
                  )}
                </div>
              </div>

              <button type="submit" disabled={loading}
                className="w-full bg-teal-600 hover:bg-teal-700 active:bg-teal-800 text-white py-4 rounded-xl font-bold text-sm shadow-[0_4px_14px_rgba(20,184,166,0.25)] hover:shadow-[0_6px_20px_rgba(20,184,166,0.35)] hover:-translate-y-0.5 transition-all duration-200 disabled:opacity-60 disabled:pointer-events-none flex items-center justify-center gap-2 mt-2">
                {loading ? (
                  <><div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" /> Creating Account...</>
                ) : (
                  <>Create Account <ArrowRight size={16} /></>
                )}
              </button>
            </form>
          </div>
        </div>
      </div>

    </div>
  );
}

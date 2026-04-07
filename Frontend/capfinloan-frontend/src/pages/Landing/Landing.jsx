import { Link } from 'react-router-dom';
import {
  Zap, ShieldCheck, BarChart2, FileText,
  User, Home, Car, GraduationCap, Briefcase,
  Building2, ArrowRight, TrendingUp, CheckCircle2
} from 'lucide-react';

const features = [
  {
    icon: <Zap size={22} className="text-teal-600" />,
    title: 'Fast Processing',
    desc: 'Get loan decisions within 24–48 hours with our streamlined digital process.',
  },
  {
    icon: <ShieldCheck size={22} className="text-teal-600" />,
    title: 'Fully Secure',
    desc: 'Bank-grade security with JWT authentication and encrypted data storage.',
  },
  {
    icon: <BarChart2 size={22} className="text-teal-600" />,
    title: 'Real-time Tracking',
    desc: 'Track your application status at every step with a detailed timeline.',
  },
  {
    icon: <FileText size={22} className="text-teal-600" />,
    title: 'Paperless Process',
    desc: 'Upload all documents digitally. No physical paperwork required.',
  },
];

const steps = [
  { step: '01', title: 'Create Account', desc: 'Register in under 2 minutes' },
  { step: '02', title: 'Apply Online', desc: 'Fill our guided 4-step form' },
  { step: '03', title: 'Upload Documents', desc: 'Upload KYC and income proofs' },
  { step: '04', title: 'Get Decision', desc: 'Receive approval with terms' },
];

const loanTypes = [
  { name: 'Personal Loan', icon: <User size={26} className="text-teal-600" />, rate: '10.5%', max: '₹20L' },
  { name: 'Home Loan',     icon: <Home size={26} className="text-teal-600" />, rate: '8.5%',  max: '₹2Cr' },
  { name: 'Vehicle Loan',  icon: <Car size={26} className="text-teal-600" />, rate: '9.0%',  max: '₹50L' },
  { name: 'Education Loan',icon: <GraduationCap size={26} className="text-teal-600" />, rate: '7.5%', max: '₹40L' },
  { name: 'Business Loan', icon: <Briefcase size={26} className="text-teal-600" />, rate: '12.0%', max: '₹1Cr' },
];

const trustBadges = [
  { icon: <CheckCircle2 size={14} className="text-teal-500" />, text: 'RBI Registered NBFC' },
  { icon: <ShieldCheck size={14} className="text-teal-500" />, text: 'ISO 27001 Certified' },
  { icon: <TrendingUp size={14} className="text-teal-500" />, text: 'A+ Credit Rating' },
];

export default function Landing() {
  return (
    <div className="min-h-screen bg-white">

      {/* ── Navbar ── */}
      <nav className="fixed top-0 w-full bg-white/95 backdrop-blur-md border-b border-slate-200/60 z-50 shadow-[0_1px_3px_rgb(0,0,0,0.04)]">
        <div className="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 bg-teal-600 rounded-lg flex items-center justify-center shadow-sm">
              <Building2 size={16} className="text-white" />
            </div>
            <span className="font-extrabold text-slate-800 tracking-tight">CapFinLoan</span>
          </div>
          <div className="flex items-center gap-3">
            <Link to="/auth/login"
              className="text-slate-500 hover:text-teal-700 font-semibold text-sm px-4 py-2 rounded-xl hover:bg-teal-50 transition-colors">
              Login
            </Link>
            <Link to="/auth/register"
              className="inline-flex items-center gap-2 bg-teal-600 hover:bg-teal-700 text-white font-bold text-sm px-5 py-2.5 rounded-xl shadow-[0_4px_14px_rgba(20,184,166,0.25)] hover:shadow-[0_6px_20px_rgba(20,184,166,0.35)] hover:-translate-y-0.5 transition-all duration-200">
              Apply Now <ArrowRight size={14} />
            </Link>
          </div>
        </div>
      </nav>

      {/* ── Hero — Sun-Filled Office Photography ── */}
      <section className="relative pt-16 min-h-[90vh] flex items-center overflow-hidden">
        {/* Bright, airy office background */}
        <img
          src="https://images.unsplash.com/photo-1497366216548-37526070297c?q=80&w=2069&auto=format&fit=crop"
          alt="Modern financial office"
          className="absolute inset-0 w-full h-full object-cover"
        />
        {/* Soft white overlay — keeps the image bright, ensures dark text readability */}
        <div className="absolute inset-0 bg-gradient-to-r from-white/90 via-white/75 to-white/20" />

        <div className="relative z-10 max-w-7xl mx-auto px-6 py-28 w-full">
          <div className="max-w-xl">
            {/* Trust badge */}
            <div className="inline-flex items-center gap-2 bg-teal-50 text-teal-700 text-xs font-bold px-4 py-2 rounded-full mb-8 border border-teal-100 uppercase tracking-wider">
              <Building2 size={12} className="text-teal-500" />
              Trusted by 10,000+ applicants across India
            </div>

            <h1 className="text-6xl md:text-7xl font-extrabold text-slate-900 leading-[1.0] tracking-tight mb-6">
              Smart Loans<br />
              for Every<br />
              <span className="text-teal-600">Dream.</span>
            </h1>
            <p className="text-xl text-slate-500 mb-10 leading-relaxed font-light max-w-md">
              Apply for personal, home, vehicle, education and business loans online.
              Fast approval, transparent process, competitive rates.
            </p>

            <div className="flex flex-col sm:flex-row gap-4 mb-12">
              <Link to="/auth/register"
                className="inline-flex items-center justify-center gap-2 bg-teal-600 hover:bg-teal-700 text-white font-bold px-8 py-4 rounded-xl text-lg shadow-[0_8px_24px_rgba(20,184,166,0.30)] hover:shadow-[0_12px_32px_rgba(20,184,166,0.40)] hover:-translate-y-1 transition-all duration-200">
                Apply for Loan <ArrowRight size={20} />
              </Link>
              <Link to="/auth/login"
                className="bg-white hover:bg-slate-50 text-slate-700 font-semibold px-8 py-4 rounded-xl text-lg border border-slate-200 shadow-sm hover:-translate-y-0.5 transition-all">
                Track Application
              </Link>
            </div>

            {/* Trust badges */}
            <div className="flex flex-wrap gap-6">
              {trustBadges.map(badge => (
                <div key={badge.text} className="flex items-center gap-2 text-slate-400 text-sm">
                  {badge.icon}
                  <span>{badge.text}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* ── Stats Bar ── */}
      <section className="bg-slate-800 py-10">
        <div className="max-w-7xl mx-auto px-6">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8 text-center">
            {[
              ['₹500Cr+', 'Loans Disbursed'],
              ['10,000+', 'Happy Customers'],
              ['95%', 'Approval Rate'],
              ['24hrs', 'Avg. Processing'],
            ].map(([num, label]) => (
              <div key={label}>
                <div className="text-3xl font-extrabold text-teal-400">{num}</div>
                <div className="text-slate-400 text-sm mt-1 font-medium">{label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── Loan Types ── */}
      <section className="py-24 bg-[#F8FAFC]">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center mb-14">
            <h2 className="text-4xl font-extrabold text-slate-800 tracking-tight mb-3">Loan Products</h2>
            <p className="text-slate-400 text-lg">Choose the right loan for your needs</p>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
            {loanTypes.map((loan) => (
              <Link to="/auth/register" key={loan.name}
                className="bg-white rounded-2xl p-6 text-center hover:shadow-[0_8px_24px_rgb(0,0,0,0.08)] hover:-translate-y-1 transition-all border border-slate-100 group">
                <div className="flex items-center justify-center w-12 h-12 bg-teal-50 rounded-xl mx-auto mb-4 group-hover:bg-teal-100 transition-colors">
                  {loan.icon}
                </div>
                <div className="font-bold text-slate-800 text-sm mb-2">{loan.name}</div>
                <div className="text-teal-600 font-extrabold text-xl">{loan.rate}</div>
                <div className="text-slate-400 text-xs mt-0.5">up to {loan.max}</div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      {/* ── Features ── */}
      <section className="py-24 bg-white">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center mb-14">
            <h2 className="text-4xl font-extrabold text-slate-800 tracking-tight mb-3">Why Choose CapFinLoan?</h2>
            <p className="text-slate-400 text-lg">Built for speed, security, and transparency</p>
          </div>
          <div className="grid md:grid-cols-4 gap-6">
            {features.map((f) => (
              <div key={f.title}
                className="text-center p-8 rounded-xl border border-slate-100 hover:border-teal-100 hover:bg-teal-50/20 hover:shadow-md transition-all group">
                <div className="flex items-center justify-center w-14 h-14 bg-teal-50 rounded-2xl mx-auto mb-5 group-hover:bg-teal-100 transition-colors">
                  {f.icon}
                </div>
                <h3 className="font-bold text-slate-800 mb-2 text-base">{f.title}</h3>
                <p className="text-slate-400 text-sm leading-relaxed">{f.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── How It Works ── */}
      <section className="py-24 bg-[#F8FAFC]">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center mb-14">
            <h2 className="text-4xl font-extrabold text-slate-800 tracking-tight mb-3">How It Works</h2>
            <p className="text-slate-400 text-lg">Get your loan in 4 simple steps</p>
          </div>
          <div className="grid md:grid-cols-4 gap-8">
            {steps.map((s, i) => (
              <div key={s.step} className="text-center relative">
                {i < steps.length - 1 && (
                  <div className="hidden md:block absolute top-8 left-1/2 w-full h-0.5 bg-teal-100" />
                )}
                <div className="relative z-10 w-16 h-16 bg-teal-600 text-white rounded-2xl flex items-center justify-center text-lg font-extrabold mx-auto mb-5 shadow-[0_4px_16px_rgba(20,184,166,0.30)]">
                  {s.step}
                </div>
                <h3 className="font-bold text-slate-800 mb-1">{s.title}</h3>
                <p className="text-slate-400 text-sm">{s.desc}</p>
              </div>
            ))}
          </div>
          <div className="text-center mt-14">
            <Link to="/auth/register"
              className="inline-flex items-center gap-2 bg-teal-600 hover:bg-teal-700 text-white font-bold text-base px-10 py-4 rounded-xl shadow-[0_4px_16px_rgba(20,184,166,0.30)] hover:shadow-[0_8px_24px_rgba(20,184,166,0.40)] hover:-translate-y-0.5 transition-all duration-200">
              Start Your Application <ArrowRight size={18} />
            </Link>
          </div>
        </div>
      </section>

      {/* ── Footer ── */}
      <footer className="bg-slate-800 text-slate-400 py-10">
        <div className="max-w-7xl mx-auto px-6 flex flex-col md:flex-row justify-between items-center gap-4">
          <div className="flex items-center gap-2.5">
            <div className="w-7 h-7 bg-teal-600 rounded-lg flex items-center justify-center">
              <Building2 size={13} className="text-white" />
            </div>
            <span className="text-white font-extrabold tracking-tight">CapFinLoan</span>
          </div>
          <p className="text-sm">© {new Date().getFullYear()} CapFinLoan Financial Systems · Built for Capgemini Fast-Track.</p>
          <div className="flex gap-6 text-sm">
            <Link to="/auth/login" className="hover:text-white transition-colors font-medium">Login</Link>
            <Link to="/auth/register" className="hover:text-white transition-colors font-medium">Register</Link>
          </div>
        </div>
      </footer>

    </div>
  );
}

import { Link } from 'react-router-dom';

const features = [
  {
    icon: '⚡',
    title: 'Fast Processing',
    desc: 'Get loan decisions within 24-48 hours with our streamlined digital process.'
  },
  {
    icon: '🔒',
    title: 'Fully Secure',
    desc: 'Bank-grade security with JWT authentication and encrypted data storage.'
  },
  {
    icon: '📊',
    title: 'Real-time Tracking',
    desc: 'Track your application status at every step with detailed timeline.'
  },
  {
    icon: '📄',
    title: 'Paperless Process',
    desc: 'Upload all documents digitally. No physical paperwork required.'
  },
];

const steps = [
  { step: '01', title: 'Create Account', desc: 'Register in under 2 minutes' },
  { step: '02', title: 'Apply Online', desc: 'Fill our guided 4-step form' },
  { step: '03', title: 'Upload Documents', desc: 'Upload KYC and income proofs' },
  { step: '04', title: 'Get Decision', desc: 'Receive approval with terms' },
];

const loanTypes = [
  { name: 'Personal Loan', icon: '👤', rate: '10.5%', max: '₹20L' },
  { name: 'Home Loan',     icon: '🏠', rate: '8.5%',  max: '₹2Cr' },
  { name: 'Vehicle Loan',  icon: '🚗', rate: '9.0%',  max: '₹50L' },
  { name: 'Education Loan',icon: '🎓', rate: '7.5%',  max: '₹40L' },
  { name: 'Business Loan', icon: '💼', rate: '12.0%', max: '₹1Cr' },
];

export default function Landing() {
  return (
    <div className="min-h-screen bg-white">

      {/* Navbar */}
      <nav className="fixed top-0 w-full bg-white/95 backdrop-blur-sm border-b border-gray-100 z-50">
        <div className="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 bg-primary-600 rounded-lg flex items-center justify-center">
              <span className="text-white font-bold text-sm">CF</span>
            </div>
            <span className="font-bold text-gray-900 text-lg">CapFinLoan</span>
          </div>
          <div className="flex items-center gap-3">
            <Link to="/auth/login"
              className="text-gray-600 hover:text-primary-600 font-medium text-sm px-4 py-2 rounded-lg hover:bg-gray-50 transition-colors">
              Login
            </Link>
            <Link to="/auth/register" className="btn-primary text-sm">
              Apply Now
            </Link>
          </div>
        </div>
      </nav>

      {/* Hero */}
      <section className="pt-32 pb-20 bg-gradient-to-br from-primary-900 via-primary-700 to-primary-600">
        <div className="max-w-7xl mx-auto px-6 text-center">
          <span className="inline-block bg-white/20 text-white text-sm font-medium px-4 py-1.5 rounded-full mb-6">
            🏦 Trusted by 10,000+ applicants across India
          </span>
          <h1 className="text-5xl md:text-6xl font-bold text-white leading-tight mb-6">
            Smart Loans for<br />
            <span className="text-yellow-300">Every Dream</span>
          </h1>
          <p className="text-xl text-primary-100 max-w-2xl mx-auto mb-10">
            Apply for personal, home, vehicle, education and business loans online.
            Fast approval, transparent process, competitive rates.
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Link to="/auth/register"
              className="bg-yellow-400 hover:bg-yellow-300 text-gray-900 font-semibold px-8 py-4 rounded-xl text-lg transition-colors">
              Apply for Loan →
            </Link>
            <Link to="/auth/login"
              className="bg-white/10 hover:bg-white/20 text-white font-semibold px-8 py-4 rounded-xl text-lg border border-white/30 transition-colors">
              Track Application
            </Link>
          </div>
        </div>
      </section>

      {/* Stats */}
      <section className="bg-primary-800 py-8">
        <div className="max-w-7xl mx-auto px-6">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 text-center">
            {[
              ['₹500Cr+', 'Loans Disbursed'],
              ['10,000+', 'Happy Customers'],
              ['95%', 'Approval Rate'],
              ['24hrs', 'Avg. Processing'],
            ].map(([num, label]) => (
              <div key={label}>
                <div className="text-3xl font-bold text-yellow-300">{num}</div>
                <div className="text-primary-200 text-sm mt-1">{label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Loan Types */}
      <section className="py-20 bg-gray-50">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-3">Loan Products</h2>
            <p className="text-gray-500">Choose the right loan for your needs</p>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
            {loanTypes.map((loan) => (
              <Link to="/auth/register" key={loan.name}
                className="bg-white rounded-xl p-5 text-center hover:shadow-md hover:-translate-y-1 transition-all border border-gray-100 group">
                <div className="text-4xl mb-3">{loan.icon}</div>
                <div className="font-semibold text-gray-900 text-sm mb-2">{loan.name}</div>
                <div className="text-primary-600 font-bold text-lg">{loan.rate}</div>
                <div className="text-gray-400 text-xs">up to {loan.max}</div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="py-20 bg-white">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-3">Why Choose CapFinLoan?</h2>
          </div>
          <div className="grid md:grid-cols-4 gap-6">
            {features.map((f) => (
              <div key={f.title} className="text-center p-6 rounded-xl hover:bg-primary-50 transition-colors">
                <div className="text-4xl mb-4">{f.icon}</div>
                <h3 className="font-semibold text-gray-900 mb-2">{f.title}</h3>
                <p className="text-gray-500 text-sm leading-relaxed">{f.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* How it works */}
      <section className="py-20 bg-gray-50">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center mb-12">
            <h2 className="text-3xl font-bold text-gray-900 mb-3">How It Works</h2>
            <p className="text-gray-500">Get your loan in 4 simple steps</p>
          </div>
          <div className="grid md:grid-cols-4 gap-8">
            {steps.map((s, i) => (
              <div key={s.step} className="text-center relative">
                {i < steps.length - 1 && (
                  <div className="hidden md:block absolute top-8 left-1/2 w-full h-0.5 bg-primary-200" />
                )}
                <div className="relative z-10 w-16 h-16 bg-primary-600 text-white rounded-full flex items-center justify-center text-xl font-bold mx-auto mb-4">
                  {s.step}
                </div>
                <h3 className="font-semibold text-gray-900 mb-1">{s.title}</h3>
                <p className="text-gray-500 text-sm">{s.desc}</p>
              </div>
            ))}
          </div>
          <div className="text-center mt-12">
            <Link to="/auth/register" className="btn-primary text-base px-8 py-3">
              Start Your Application →
            </Link>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 text-gray-400 py-8">
        <div className="max-w-7xl mx-auto px-6 flex flex-col md:flex-row justify-between items-center gap-4">
          <div className="flex items-center gap-2">
            <div className="w-7 h-7 bg-primary-600 rounded-lg flex items-center justify-center">
              <span className="text-white font-bold text-xs">CF</span>
            </div>
            <span className="text-white font-semibold">CapFinLoan</span>
          </div>
          <p className="text-sm">© 2025 CapFinLoan. Built for Capgemini Fast-Track.</p>
          <div className="flex gap-6 text-sm">
            <Link to="/auth/login" className="hover:text-white transition-colors">Login</Link>
            <Link to="/auth/register" className="hover:text-white transition-colors">Register</Link>
          </div>
        </div>
      </footer>

    </div>
  );
}

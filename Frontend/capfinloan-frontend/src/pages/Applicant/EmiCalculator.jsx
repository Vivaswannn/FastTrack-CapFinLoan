import { useState, useMemo } from 'react';
import { Calculator, IndianRupee, TrendingDown, Info } from 'lucide-react';

const LOAN_PRESETS = [
  { label: 'Personal', rate: 10.5, color: 'bg-blue-100 text-blue-700 border-blue-200' },
  { label: 'Home',     rate: 8.5,  color: 'bg-green-100 text-green-700 border-green-200' },
  { label: 'Vehicle',  rate: 9.0,  color: 'bg-amber-100 text-amber-700 border-amber-200' },
  { label: 'Education',rate: 9.5,  color: 'bg-purple-100 text-purple-700 border-purple-200' },
  { label: 'Business', rate: 11.0, color: 'bg-rose-100 text-rose-700 border-rose-200' },
];

function formatINR(amount) {
  if (!amount || isNaN(amount)) return '₹0';
  return new Intl.NumberFormat('en-IN', {
    style: 'currency', currency: 'INR', maximumFractionDigits: 0,
  }).format(amount);
}

function calculateEmi(principal, annualRate, tenureMonths) {
  if (!principal || !annualRate || !tenureMonths) return 0;
  const r = annualRate / 12 / 100;
  const n = tenureMonths;
  if (r === 0) return principal / n;
  return (principal * r * Math.pow(1 + r, n)) / (Math.pow(1 + r, n) - 1);
}

function buildSchedule(principal, annualRate, tenureMonths, emi) {
  const r = annualRate / 12 / 100;
  let balance = principal;
  const rows = [];
  for (let month = 1; month <= tenureMonths; month++) {
    const interest = balance * r;
    const principalPaid = emi - interest;
    balance = Math.max(0, balance - principalPaid);
    rows.push({ month, emi, principal: principalPaid, interest, balance });
  }
  return rows;
}

export default function EmiCalculator() {
  const [principal, setPrincipal]   = useState(500000);
  const [rate, setRate]             = useState(10.5);
  const [tenure, setTenure]         = useState(60);
  const [showSchedule, setShowSchedule] = useState(false);
  const [scheduleRows, setScheduleRows] = useState(12);

  const emi          = useMemo(() => calculateEmi(principal, rate, tenure), [principal, rate, tenure]);
  const totalPayable = useMemo(() => emi * tenure, [emi, tenure]);
  const totalInterest = useMemo(() => totalPayable - principal, [totalPayable, principal]);
  const principalPct  = useMemo(() => principal > 0 ? ((principal / totalPayable) * 100).toFixed(1) : 0, [principal, totalPayable]);
  const interestPct   = useMemo(() => principal > 0 ? (100 - parseFloat(principalPct)).toFixed(1) : 0, [principalPct]);

  const schedule = useMemo(() => {
    if (!showSchedule || !emi) return [];
    return buildSchedule(principal, rate, tenure, emi);
  }, [showSchedule, principal, rate, tenure, emi]);

  const handlePreset = (r) => setRate(r);

  return (
    <div className="min-h-screen bg-slate-50 py-8 px-4">
      <div className="max-w-4xl mx-auto">

        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center gap-3 mb-1">
            <div className="w-9 h-9 bg-teal-600 rounded-xl flex items-center justify-center shadow-sm">
              <Calculator size={18} className="text-white" />
            </div>
            <h1 className="text-2xl font-bold text-slate-800">EMI Calculator</h1>
          </div>
          <p className="text-slate-500 text-sm ml-12">
            Estimate your monthly instalment before applying for a loan.
          </p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-5 gap-6">

          {/* ── LEFT: Inputs ── */}
          <div className="lg:col-span-3 space-y-6">

            {/* Loan Type Presets */}
            <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm">
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3">
                Quick Presets — Interest Rates
              </p>
              <div className="flex flex-wrap gap-2">
                {LOAN_PRESETS.map(({ label, rate: r, color }) => (
                  <button
                    key={label}
                    onClick={() => handlePreset(r)}
                    className={`text-xs font-semibold px-3 py-1.5 rounded-full border transition-all ${
                      parseFloat(rate) === r
                        ? color + ' ring-2 ring-offset-1 ring-teal-400'
                        : 'bg-slate-50 text-slate-600 border-slate-200 hover:bg-slate-100'
                    }`}
                  >
                    {label} · {r}%
                  </button>
                ))}
              </div>
            </div>

            {/* Sliders */}
            <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm space-y-6">

              {/* Principal */}
              <div>
                <div className="flex justify-between items-center mb-2">
                  <label className="text-sm font-semibold text-slate-700">Loan Amount</label>
                  <div className="flex items-center gap-1 text-teal-700 font-bold text-base">
                    <IndianRupee size={14} />
                    <input
                      type="number"
                      value={principal}
                      min={10000} max={10000000} step={10000}
                      onChange={e => setPrincipal(Number(e.target.value))}
                      className="w-28 text-right bg-teal-50 border border-teal-200 rounded-lg px-2 py-0.5 text-sm font-bold text-teal-700 outline-none focus:ring-2 focus:ring-teal-300"
                    />
                  </div>
                </div>
                <input type="range" min={10000} max={10000000} step={10000}
                  value={principal}
                  onChange={e => setPrincipal(Number(e.target.value))}
                  className="w-full accent-teal-600 h-2 rounded-full cursor-pointer" />
                <div className="flex justify-between text-xs text-slate-400 mt-1">
                  <span>₹10,000</span><span>₹1 Cr</span>
                </div>
              </div>

              {/* Interest Rate */}
              <div>
                <div className="flex justify-between items-center mb-2">
                  <label className="text-sm font-semibold text-slate-700">Interest Rate (per annum)</label>
                  <div className="flex items-center gap-1 font-bold text-teal-700">
                    <input
                      type="number"
                      value={rate}
                      min={1} max={30} step={0.1}
                      onChange={e => setRate(Number(e.target.value))}
                      className="w-16 text-right bg-teal-50 border border-teal-200 rounded-lg px-2 py-0.5 text-sm font-bold text-teal-700 outline-none focus:ring-2 focus:ring-teal-300"
                    />
                    <span className="text-sm">%</span>
                  </div>
                </div>
                <input type="range" min={1} max={30} step={0.1}
                  value={rate}
                  onChange={e => setRate(Number(e.target.value))}
                  className="w-full accent-teal-600 h-2 rounded-full cursor-pointer" />
                <div className="flex justify-between text-xs text-slate-400 mt-1">
                  <span>1%</span><span>30%</span>
                </div>
              </div>

              {/* Tenure */}
              <div>
                <div className="flex justify-between items-center mb-2">
                  <label className="text-sm font-semibold text-slate-700">Tenure</label>
                  <div className="flex items-center gap-1 font-bold text-teal-700">
                    <input
                      type="number"
                      value={tenure}
                      min={6} max={360} step={6}
                      onChange={e => setTenure(Number(e.target.value))}
                      className="w-16 text-right bg-teal-50 border border-teal-200 rounded-lg px-2 py-0.5 text-sm font-bold text-teal-700 outline-none focus:ring-2 focus:ring-teal-300"
                    />
                    <span className="text-sm">mo</span>
                  </div>
                </div>
                <input type="range" min={6} max={360} step={6}
                  value={tenure}
                  onChange={e => setTenure(Number(e.target.value))}
                  className="w-full accent-teal-600 h-2 rounded-full cursor-pointer" />
                <div className="flex justify-between text-xs text-slate-400 mt-1">
                  <span>6 months</span><span>30 years</span>
                </div>
              </div>
            </div>

            {/* Formula note */}
            <div className="flex items-start gap-2 text-xs text-slate-400 bg-slate-100 rounded-xl px-4 py-3">
              <Info size={13} className="mt-0.5 shrink-0" />
              <span>
                EMI = P × r × (1+r)^n ÷ ((1+r)^n − 1) &nbsp;|&nbsp;
                P = Principal, r = Monthly rate (annual ÷ 12 ÷ 100), n = Tenure months
              </span>
            </div>
          </div>

          {/* ── RIGHT: Results ── */}
          <div className="lg:col-span-2 space-y-4">

            {/* EMI Card */}
            <div className="bg-gradient-to-br from-teal-600 to-teal-700 rounded-2xl p-6 text-white shadow-lg">
              <p className="text-teal-200 text-xs font-semibold uppercase tracking-wider mb-1">
                Monthly EMI
              </p>
              <p className="text-4xl font-extrabold tracking-tight">
                {formatINR(Math.round(emi))}
              </p>
              <p className="text-teal-200 text-xs mt-1">per month for {tenure} months</p>
            </div>

            {/* Breakdown */}
            <div className="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm space-y-3">
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Breakdown</p>

              <div className="flex justify-between items-center py-2 border-b border-slate-100">
                <span className="text-sm text-slate-600">Principal Amount</span>
                <span className="text-sm font-bold text-slate-800">{formatINR(principal)}</span>
              </div>
              <div className="flex justify-between items-center py-2 border-b border-slate-100">
                <span className="text-sm text-slate-600">Total Interest</span>
                <span className="text-sm font-bold text-red-500">{formatINR(Math.round(totalInterest))}</span>
              </div>
              <div className="flex justify-between items-center py-2">
                <span className="text-sm font-semibold text-slate-700">Total Payable</span>
                <span className="text-sm font-extrabold text-teal-700">{formatINR(Math.round(totalPayable))}</span>
              </div>

              {/* Visual bar */}
              <div className="mt-2">
                <div className="flex rounded-full overflow-hidden h-3">
                  <div
                    className="bg-teal-500 transition-all duration-300"
                    style={{ width: `${principalPct}%` }}
                  />
                  <div
                    className="bg-red-400 transition-all duration-300"
                    style={{ width: `${interestPct}%` }}
                  />
                </div>
                <div className="flex justify-between text-xs text-slate-400 mt-1.5">
                  <span className="flex items-center gap-1">
                    <span className="w-2 h-2 rounded-full bg-teal-500 inline-block" />
                    Principal {principalPct}%
                  </span>
                  <span className="flex items-center gap-1">
                    <span className="w-2 h-2 rounded-full bg-red-400 inline-block" />
                    Interest {interestPct}%
                  </span>
                </div>
              </div>
            </div>

            {/* Apply CTA */}
            <a href="/applicant/apply"
              className="block w-full text-center bg-teal-600 hover:bg-teal-700 text-white font-semibold py-3 px-4 rounded-xl transition-colors shadow-sm text-sm">
              Apply for this Loan →
            </a>
          </div>
        </div>

        {/* Amortization Schedule */}
        <div className="mt-6">
          <button
            onClick={() => setShowSchedule(v => !v)}
            className="flex items-center gap-2 text-sm font-semibold text-teal-700 hover:text-teal-800 bg-white border border-slate-200 px-4 py-2.5 rounded-xl shadow-sm hover:shadow transition-all"
          >
            <TrendingDown size={15} />
            {showSchedule ? 'Hide' : 'View'} Amortization Schedule
          </button>

          {showSchedule && schedule.length > 0 && (
            <div className="mt-4 bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
              <div className="px-5 py-4 border-b border-slate-100 flex justify-between items-center">
                <div>
                  <h3 className="text-sm font-bold text-slate-800">Amortization Schedule</h3>
                  <p className="text-xs text-slate-400 mt-0.5">Month-by-month principal & interest split</p>
                </div>
                <select
                  value={scheduleRows}
                  onChange={e => setScheduleRows(Number(e.target.value))}
                  className="text-xs border border-slate-200 rounded-lg px-2 py-1.5 text-slate-600 outline-none focus:ring-2 focus:ring-teal-200"
                >
                  <option value={12}>Show 12</option>
                  <option value={24}>Show 24</option>
                  <option value={tenure}>Show All ({tenure})</option>
                </select>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="bg-slate-50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                      <th className="px-4 py-3 text-left">Month</th>
                      <th className="px-4 py-3 text-right">EMI</th>
                      <th className="px-4 py-3 text-right">Principal</th>
                      <th className="px-4 py-3 text-right">Interest</th>
                      <th className="px-4 py-3 text-right">Balance</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {schedule.slice(0, scheduleRows).map(row => (
                      <tr key={row.month} className="hover:bg-slate-50 transition-colors">
                        <td className="px-4 py-2.5 text-slate-600 font-medium">{row.month}</td>
                        <td className="px-4 py-2.5 text-right text-slate-800 font-medium">
                          {formatINR(Math.round(row.emi))}
                        </td>
                        <td className="px-4 py-2.5 text-right text-teal-600 font-medium">
                          {formatINR(Math.round(row.principal))}
                        </td>
                        <td className="px-4 py-2.5 text-right text-red-500">
                          {formatINR(Math.round(row.interest))}
                        </td>
                        <td className="px-4 py-2.5 text-right text-slate-500">
                          {formatINR(Math.round(row.balance))}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>

      </div>
    </div>
  );
}

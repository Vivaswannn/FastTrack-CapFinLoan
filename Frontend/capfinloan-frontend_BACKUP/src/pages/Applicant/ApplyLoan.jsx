import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import PageLayout from '../../components/layout/PageLayout';
import LoadingSpinner from '../../components/common/LoadingSpinner';
import Modal from '../../components/common/Modal';
import { loanService } from '../../services/loanService';
import { formatCurrency } from '../../utils/formatters';
import { LOAN_TYPES, EMPLOYMENT_TYPES } from '../../utils/constants';

const calculateEmi = (principal, rate = 10.5, months = 12) => {
  if (!principal || !months) return 0;
  const r = rate / 12 / 100;
  const factor = Math.pow(1 + r, months);
  return Math.round((principal * r * factor) / (factor - 1));
};

const Stepper = ({ currentStep, steps }) => (
  <div className="flex items-center justify-center mb-10">
    {steps.map((step, i) => (
      <div key={step} className="flex items-center">
        <div className="flex flex-col items-center">
          <div className={`w-10 h-10 rounded-full flex items-center justify-center text-sm font-semibold border-2 transition-all
            ${i < currentStep
              ? 'bg-primary-600 border-primary-600 text-white'
              : i === currentStep
              ? 'border-primary-600 text-primary-600 bg-white'
              : 'border-gray-300 text-gray-400 bg-white'}`}>
            {i < currentStep ? '✓' : i + 1}
          </div>
          <span className={`text-xs mt-2 font-medium hidden sm:block
            ${i === currentStep ? 'text-primary-600' : i < currentStep ? 'text-gray-600' : 'text-gray-400'}`}>
            {step}
          </span>
        </div>
        {i < steps.length - 1 && (
          <div className={`w-16 sm:w-24 h-0.5 mx-2 mb-5 ${i < currentStep ? 'bg-primary-600' : 'bg-gray-200'}`} />
        )}
      </div>
    ))}
  </div>
);

const STEPS = ['Personal', 'Employment', 'Loan Details', 'Review'];

export default function ApplyLoan() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [step, setStep] = useState(0);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [appId, setAppId] = useState(id || null);
  const [showSubmitModal, setShowSubmitModal] = useState(false);
  const [initialLoad, setInitialLoad] = useState(!!id);

  const [form, setForm] = useState({
    fullName: '', email: '', phone: '',
    dateOfBirth: '', address: '',
    employmentType: 'Salaried', employerName: '',
    jobTitle: '', monthlyIncome: '',
    yearsOfExperience: '', employerAddress: '',
    loanType: 'Personal', loanAmount: '',
    tenureMonths: 12, purpose: '',
  });
  const [errors, setErrors] = useState({});

  useEffect(() => {
    if (id) {
      loanService.getById(id).then(res => {
        const d = res.data.data;
        setForm({
          fullName: d.fullName || '',
          email: d.email || '',
          phone: d.phone || '',
          dateOfBirth: d.dateOfBirth ? d.dateOfBirth.split('T')[0] : '',
          address: d.address || '',
          employmentType: d.employmentType || 'Salaried',
          employerName: d.employerName || '',
          jobTitle: d.jobTitle || '',
          monthlyIncome: d.monthlyIncome || '',
          yearsOfExperience: d.yearsOfExperience || '',
          employerAddress: d.employerAddress || '',
          loanType: d.loanType || 'Personal',
          loanAmount: d.loanAmount || '',
          tenureMonths: d.tenureMonths || 12,
          purpose: d.purpose || '',
        });
      }).catch(() => toast.error('Failed to load draft'))
        .finally(() => setInitialLoad(false));
    }
  }, [id]);

  const update = (field) => (e) => {
    setForm(p => ({ ...p, [field]: e.target.value }));
    setErrors(p => ({ ...p, [field]: '' }));
  };

  const validateStep = () => {
    const errs = {};
    if (step === 0) {
      if (!form.fullName.trim()) errs.fullName = 'Full name is required';
      if (!form.email || !/\S+@\S+\.\S+/.test(form.email)) errs.email = 'Valid email is required';
      if (!form.phone || !/^[6-9]\d{9}$/.test(form.phone)) errs.phone = 'Valid 10-digit mobile number required';
    }
    if (step === 1) {
      if (!form.employerName?.trim()) errs.employerName = 'Employer name is required';
      if (!form.monthlyIncome || Number(form.monthlyIncome) <= 0) errs.monthlyIncome = 'Monthly income must be greater than 0';
    }
    if (step === 2) {
      if (!form.loanAmount || Number(form.loanAmount) < 10000) errs.loanAmount = 'Minimum loan amount is ₹10,000';
      if (Number(form.loanAmount) > 10000000) errs.loanAmount = 'Maximum loan amount is ₹1,00,00,000';
    }
    setErrors(errs);
    return errs;
  };

  const saveDraft = async () => {
    setSaving(true);
    try {
      const payload = {
        loanType: form.loanType || 'Personal',
        loanAmount: Number(form.loanAmount) || 0,
        tenureMonths: Number(form.tenureMonths) || 12,
        purpose: form.purpose || '',
        fullName: form.fullName || '',
        email: form.email || '',
        phone: form.phone || '',
        dateOfBirth: form.dateOfBirth || null,
        address: form.address || '',
        employerName: form.employerName || '',
        employmentType: form.employmentType || 'Salaried',
        jobTitle: form.jobTitle || '',
        monthlyIncome: Number(form.monthlyIncome) || 0,
        yearsOfExperience: Number(form.yearsOfExperience) || 0,
        employerAddress: form.employerAddress || '',
      };
      if (!appId) {
        const res = await loanService.createDraft(payload);
        const newId = res.data?.data?.applicationId;
        if (newId) {
          setAppId(newId);
          window.history.replaceState(null, '', `/applicant/apply/${newId}`);
        }
      } else {
        await loanService.updateDraft(appId, payload);
      }
      toast.success('Draft saved ✓');
      return true;
    } catch (err) {
      const msg = err.response?.data?.message
        || err.message
        || 'Failed to save draft';
      toast.error(msg);
      return false;
    } finally {
      setSaving(false);
    }
  };

  const handleNext = async () => {
    const errs = validateStep();
    if (Object.keys(errs).length > 0) {
      setErrors(errs);
      toast.error('Please fill all required fields correctly');
      window.scrollTo({ top: 0, behavior: 'smooth' });
      return;
    }
    const success = await saveDraft();
    if (success) {
      setStep(s => s + 1);
    }
  };

  const handleBack = () => setStep(s => s - 1);

  const handleSubmit = async () => {
    if (!appId) { toast.error('Save draft first'); return; }
    setLoading(true);
    try {
      await loanService.submit(appId);
      toast.success('Application submitted successfully! 🎉');
      setShowSubmitModal(false);
      navigate('/applicant/dashboard');
    } catch (err) {
      const msg = err.response?.data?.message || 'Submission failed';
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  const emi = calculateEmi(Number(form.loanAmount), 10.5, Number(form.tenureMonths));

  if (initialLoad) return <PageLayout title="Loading..."><LoadingSpinner /></PageLayout>;

  return (
    <PageLayout
      title={appId ? 'Continue Application' : 'Apply for Loan'}
      subtitle="Complete all steps to submit your application">

      <div className="max-w-2xl mx-auto">
        <Stepper currentStep={step} steps={STEPS} />

        <div className="card">

          {/* Step 1 — Personal */}
          {step === 0 && (
            <div className="space-y-5">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">Personal Information</h2>
              <div className="grid grid-cols-2 gap-4">
                <div className="col-span-2">
                  <label className="label">Full Name *</label>
                  <input className={`input-field ${errors.fullName ? 'border-red-400' : ''}`}
                    placeholder="As per Aadhaar card"
                    value={form.fullName} onChange={update('fullName')} />
                  {errors.fullName && <p className="error-text">{errors.fullName}</p>}
                </div>
                <div>
                  <label className="label">Email Address *</label>
                  <input type="email"
                    className={`input-field ${errors.email ? 'border-red-400' : ''}`}
                    placeholder="you@example.com"
                    value={form.email} onChange={update('email')} />
                  {errors.email && <p className="error-text">{errors.email}</p>}
                </div>
                <div>
                  <label className="label">Mobile Number *</label>
                  <input className={`input-field ${errors.phone ? 'border-red-400' : ''}`}
                    placeholder="10-digit mobile"
                    maxLength={10}
                    value={form.phone} onChange={update('phone')} />
                  {errors.phone && <p className="error-text">{errors.phone}</p>}
                </div>
                <div>
                  <label className="label">Date of Birth</label>
                  <input type="date" className="input-field"
                    max={new Date().toISOString().split('T')[0]}
                    value={form.dateOfBirth} onChange={update('dateOfBirth')} />
                </div>
                <div className="col-span-2">
                  <label className="label">Residential Address</label>
                  <textarea className="input-field" rows={3}
                    placeholder="Full address with city and pincode"
                    value={form.address} onChange={update('address')} />
                </div>
              </div>
            </div>
          )}

          {/* Step 2 — Employment */}
          {step === 1 && (
            <div className="space-y-5">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">Employment Details</h2>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="label">Employment Type *</label>
                  <select className="input-field"
                    value={form.employmentType} onChange={update('employmentType')}>
                    {EMPLOYMENT_TYPES.map(t => (
                      <option key={t.value} value={t.value}>{t.label}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="label">Employer Name *</label>
                  <input className={`input-field ${errors.employerName ? 'border-red-400' : ''}`}
                    placeholder="Company name"
                    value={form.employerName} onChange={update('employerName')} />
                  {errors.employerName && <p className="error-text">{errors.employerName}</p>}
                </div>
                <div>
                  <label className="label">Job Title</label>
                  <input className="input-field" placeholder="Software Engineer"
                    value={form.jobTitle} onChange={update('jobTitle')} />
                </div>
                <div>
                  <label className="label">Monthly Income (₹) *</label>
                  <input type="number"
                    className={`input-field ${errors.monthlyIncome ? 'border-red-400' : ''}`}
                    placeholder="50000"
                    value={form.monthlyIncome} onChange={update('monthlyIncome')} />
                  {errors.monthlyIncome && <p className="error-text">{errors.monthlyIncome}</p>}
                </div>
                <div>
                  <label className="label">Years of Experience</label>
                  <input type="number" className="input-field"
                    placeholder="3" min={0} max={50}
                    value={form.yearsOfExperience} onChange={update('yearsOfExperience')} />
                </div>
                <div className="col-span-2">
                  <label className="label">Employer Address</label>
                  <textarea className="input-field" rows={2}
                    placeholder="Office address"
                    value={form.employerAddress} onChange={update('employerAddress')} />
                </div>
              </div>
            </div>
          )}

          {/* Step 3 — Loan Details */}
          {step === 2 && (
            <div className="space-y-5">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">Loan Details</h2>
              <div>
                <label className="label">Loan Type *</label>
                <div className="grid grid-cols-3 sm:grid-cols-5 gap-3">
                  {LOAN_TYPES.map(t => (
                    <button key={t.value} type="button"
                      onClick={() => setForm(p => ({ ...p, loanType: t.value }))}
                      className={`p-3 rounded-xl border-2 text-center transition-all text-sm font-medium
                        ${form.loanType === t.value
                          ? 'border-primary-600 bg-primary-50 text-primary-700'
                          : 'border-gray-200 hover:border-gray-300 text-gray-700'}`}>
                      {t.label.split(' ')[0]}
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="label">
                  Loan Amount (₹) *
                  <span className="text-gray-400 font-normal ml-1">(₹10,000 — ₹1,00,00,000)</span>
                </label>
                <div className="relative">
                  <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 font-medium">₹</span>
                  <input type="number"
                    className={`input-field pl-7 ${errors.loanAmount ? 'border-red-400' : ''}`}
                    placeholder="500000"
                    value={form.loanAmount} onChange={update('loanAmount')} />
                </div>
                {form.loanAmount && (
                  <p className="text-xs text-gray-500 mt-1">{formatCurrency(Number(form.loanAmount))}</p>
                )}
                {errors.loanAmount && <p className="error-text">{errors.loanAmount}</p>}
              </div>

              <div>
                <label className="label">
                  Tenure: {form.tenureMonths} months
                  ({Math.floor(form.tenureMonths / 12)} years
                  {form.tenureMonths % 12 > 0 ? ` ${form.tenureMonths % 12} months` : ''})
                </label>
                <input type="range" min={6} max={360} step={6}
                  value={form.tenureMonths}
                  onChange={update('tenureMonths')}
                  className="w-full accent-primary-600" />
                <div className="flex justify-between text-xs text-gray-400 mt-1">
                  <span>6 months</span>
                  <span>30 years</span>
                </div>
              </div>

              {form.loanAmount && Number(form.loanAmount) > 0 && (
                <div className="bg-primary-50 border border-primary-200 rounded-xl p-4">
                  <div className="text-sm font-medium text-primary-700 mb-3">
                    📊 EMI Preview (estimated at 10.5% p.a.)
                  </div>
                  <div className="grid grid-cols-3 gap-4 text-center">
                    <div>
                      <div className="text-2xl font-bold text-primary-700">{formatCurrency(emi)}</div>
                      <div className="text-xs text-primary-500 mt-1">Monthly EMI</div>
                    </div>
                    <div>
                      <div className="text-2xl font-bold text-gray-700">{formatCurrency(Number(form.loanAmount))}</div>
                      <div className="text-xs text-gray-500 mt-1">Principal</div>
                    </div>
                    <div>
                      <div className="text-2xl font-bold text-gray-700">{form.tenureMonths}</div>
                      <div className="text-xs text-gray-500 mt-1">Months</div>
                    </div>
                  </div>
                  <p className="text-xs text-primary-400 mt-3">
                    * This is an estimate. Actual EMI depends on approved interest rate.
                  </p>
                </div>
              )}

              <div>
                <label className="label">Loan Purpose</label>
                <textarea className="input-field" rows={3}
                  placeholder="Brief description of loan purpose"
                  value={form.purpose} onChange={update('purpose')} />
              </div>
            </div>
          )}

          {/* Step 4 — Review */}
          {step === 3 && (
            <div>
              <h2 className="text-xl font-semibold text-gray-900 mb-6">Review Your Application</h2>
              <div className="space-y-4">
                {[
                  {
                    title: 'Personal Information',
                    edit: () => setStep(0),
                    items: [
                      ['Full Name', form.fullName],
                      ['Email', form.email],
                      ['Phone', form.phone],
                      ['Date of Birth', form.dateOfBirth || '—'],
                      ['Address', form.address || '—'],
                    ]
                  },
                  {
                    title: 'Employment Details',
                    edit: () => setStep(1),
                    items: [
                      ['Employment Type', form.employmentType],
                      ['Employer', form.employerName],
                      ['Job Title', form.jobTitle || '—'],
                      ['Monthly Income', formatCurrency(Number(form.monthlyIncome))],
                      ['Experience', `${form.yearsOfExperience || 0} years`],
                    ]
                  },
                  {
                    title: 'Loan Details',
                    edit: () => setStep(2),
                    items: [
                      ['Loan Type', form.loanType],
                      ['Amount', formatCurrency(Number(form.loanAmount))],
                      ['Tenure', `${form.tenureMonths} months`],
                      ['Est. EMI', formatCurrency(emi) + '/month'],
                      ['Purpose', form.purpose || '—'],
                    ]
                  },
                ].map(section => (
                  <div key={section.title} className="border border-gray-100 rounded-xl overflow-hidden">
                    <div className="flex justify-between items-center bg-gray-50 px-4 py-3">
                      <span className="font-medium text-gray-700 text-sm">{section.title}</span>
                      <button onClick={section.edit} className="text-xs text-primary-600 hover:underline">
                        Edit
                      </button>
                    </div>
                    <div className="divide-y divide-gray-50">
                      {section.items.map(([key, val]) => (
                        <div key={key} className="flex justify-between px-4 py-2.5 text-sm">
                          <span className="text-gray-500">{key}</span>
                          <span className="font-medium text-gray-900 text-right max-w-xs">{val}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Navigation */}
          <div className="flex justify-between items-center mt-8 pt-6 border-t border-gray-100">
            <div className="flex gap-3">
              {step > 0 && (
                <button onClick={handleBack} className="btn-secondary">← Back</button>
              )}
              <button onClick={saveDraft} disabled={saving}
                className="btn-secondary text-sm flex items-center gap-2">
                {saving ? (
                  <div className="w-3 h-3 border-2 border-gray-400 border-t-gray-700 rounded-full animate-spin" />
                ) : '💾'}
                Save Draft
              </button>
            </div>
            {step < 3 ? (
              <button onClick={handleNext} disabled={saving} className="btn-primary flex items-center gap-2">
                Next Step →
              </button>
            ) : (
              <button onClick={() => setShowSubmitModal(true)} className="btn-success flex items-center gap-2">
                Submit Application 🚀
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Submit Confirmation Modal */}
      <Modal isOpen={showSubmitModal} onClose={() => setShowSubmitModal(false)} title="Submit Application">
        <p className="text-gray-600 mb-6">
          Are you sure you want to submit this application? Once submitted you cannot edit it.
        </p>
        <div className="flex gap-3 justify-end">
          <button onClick={() => setShowSubmitModal(false)} className="btn-secondary">Cancel</button>
          <button onClick={handleSubmit} disabled={loading}
            className="btn-success flex items-center gap-2">
            {loading ? (
              <div className="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />
            ) : '🚀'}
            Yes, Submit
          </button>
        </div>
      </Modal>

    </PageLayout>
  );
}

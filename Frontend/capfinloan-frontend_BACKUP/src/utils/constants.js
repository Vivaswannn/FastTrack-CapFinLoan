export const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/gateway';

export const LOAN_TYPES = [
  { value: 'Personal',  label: 'Personal Loan' },
  { value: 'Home',      label: 'Home Loan' },
  { value: 'Vehicle',   label: 'Vehicle Loan' },
  { value: 'Education', label: 'Education Loan' },
  { value: 'Business',  label: 'Business Loan' },
];

export const EMPLOYMENT_TYPES = [
  { value: 'Salaried',      label: 'Salaried' },
  { value: 'Self-Employed', label: 'Self-Employed' },
];

export const DOCUMENT_TYPES = [
  { value: 'AadhaarCard',   label: 'Aadhaar Card' },
  { value: 'PAN',           label: 'PAN Card' },
  { value: 'Passport',      label: 'Passport' },
  { value: 'SalarySlip',    label: 'Salary Slip' },
  { value: 'BankStatement', label: 'Bank Statement' },
  { value: 'ITReturn',      label: 'IT Return' },
  { value: 'UtilityBill',   label: 'Utility Bill' },
  { value: 'Other',         label: 'Other' },
];

export const STATUS_COLORS = {
  Draft:        'badge-draft',
  Submitted:    'badge-submitted',
  DocsPending:  'badge-docspending',
  DocsVerified: 'badge-docsverified',
  UnderReview:  'badge-underreview',
  Approved:     'badge-approved',
  Rejected:     'badge-rejected',
  Closed:       'badge-closed',
};

export default function Modal({ isOpen, onClose, title, children, size = 'md' }) {
  if (!isOpen) return null;
  const sizes = { sm: 'max-w-md', md: 'max-w-lg', lg: 'max-w-2xl', xl: 'max-w-4xl' };
  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex min-h-screen items-center justify-center p-4">
        <div className="fixed inset-0 bg-black bg-opacity-40" onClick={onClose} />
        <div className={`relative bg-white rounded-2xl shadow-xl w-full ${sizes[size]} p-6`}>
          <div className="flex justify-between items-center mb-4">
            <h3 className="text-lg font-semibold text-gray-900">{title}</h3>
            <button onClick={onClose}
              className="text-gray-400 hover:text-gray-600 text-2xl leading-none">
              &times;
            </button>
          </div>
          {children}
        </div>
      </div>
    </div>
  );
}

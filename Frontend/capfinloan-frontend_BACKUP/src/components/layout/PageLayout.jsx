import Navbar from './Navbar';

export default function PageLayout({ children, title, subtitle, action }) {
  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {(title || action) && (
          <div className="flex justify-between items-start mb-8">
            <div>
              {title && <h1 className="text-2xl font-bold text-gray-900">{title}</h1>}
              {subtitle && <p className="text-gray-500 mt-1">{subtitle}</p>}
            </div>
            {action && <div>{action}</div>}
          </div>
        )}
        {children}
      </main>
    </div>
  );
}

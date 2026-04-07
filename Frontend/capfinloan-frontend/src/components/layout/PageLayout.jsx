import Navbar from './Navbar';

export default function PageLayout({ children, title, subtitle, action }) {
  return (
    <div className="min-h-screen bg-[#F8FAFC]">
      <Navbar />

      {/* Clean Light Page Header */}
      {(title || action) && (
        <div className="bg-white border-b border-slate-200/60 shadow-[0_1px_3px_rgb(0,0,0,0.03)]">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
            <div className="flex justify-between items-center gap-4">
              <div>
                {title && (
                  <h1 className="text-2xl font-extrabold text-slate-800 tracking-tight leading-tight">
                    {title}
                  </h1>
                )}
                {subtitle && (
                  <p className="text-slate-400 text-sm mt-1">{subtitle}</p>
                )}
              </div>
              {action && <div className="shrink-0">{action}</div>}
            </div>
          </div>
        </div>
      )}

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {children}
      </main>
    </div>
  );
}

import React from 'react';

export default React.memo(function LoadingSpinner({ size = 'md' }) {
  const sizes = { sm: 'h-6 w-6', md: 'h-10 w-10', lg: 'h-16 w-16' };
  return (
    <div className="flex justify-center items-center py-12">
      <div className={`animate-spin rounded-full border-b-2 border-primary-600 ${sizes[size]}`} />
    </div>
  );
});

import React from 'react';

export default React.memo(function EmptyState({ title, description, action }) {
  return (
    <div className="text-center py-16">
      <div className="text-6xl mb-4">📋</div>
      <h3 className="text-lg font-medium text-gray-900 mb-2">{title}</h3>
      <p className="text-gray-500 mb-6">{description}</p>
      {action}
    </div>
  );
});

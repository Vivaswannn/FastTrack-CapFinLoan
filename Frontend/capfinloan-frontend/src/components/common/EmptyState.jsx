import React from 'react';
import { ClipboardList } from 'lucide-react';

export default React.memo(function EmptyState({ title, description, action }) {
  return (
    <div className="text-center py-16">
      <div className="flex items-center justify-center w-16 h-16 bg-gray-100 rounded-2xl mx-auto mb-4">
        <ClipboardList size={28} className="text-gray-400" />
      </div>
      <h3 className="text-lg font-medium text-gray-900 mb-2">{title}</h3>
      <p className="text-gray-500 mb-6">{description}</p>
      {action}
    </div>
  );
});

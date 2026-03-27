import React from 'react';
import { getStatusBadgeClass } from '../../utils/formatters';

export default React.memo(function StatusBadge({ status }) {
  return (
    <span className={getStatusBadgeClass(status)}>
      {status?.replace(/([A-Z])/g, ' $1').trim()}
    </span>
  );
});

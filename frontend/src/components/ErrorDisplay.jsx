import React from 'react';
import { XCircle } from 'lucide-react';

const ErrorDisplay = ({ error }) => {
  if (!error) return null;

  return (
    <div className="bg-red-50 border-l-4 border-red-400 rounded-lg p-4 mb-6 shadow-sm">
      <div className="flex items-center">
        <XCircle className="w-5 h-5 text-red-600 mr-2" />
        <span className="text-red-800 font-medium">Scan Error</span>
      </div>
      <p className="text-red-700 mt-1">{error}</p>
    </div>
  );
};

export default ErrorDisplay;
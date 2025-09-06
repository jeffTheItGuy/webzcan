import React from 'react';
import { XCircle, AlertTriangle, CheckCircle, Globe } from 'lucide-react';

const TargetSelector = ({ targets, selectedTarget, setSelectedTarget }) => {
  const getTargetIcon = (key) => {
    if (['testfire', 'vulnweb', 'gruyere'].includes(key)) {
      return <XCircle className="w-4 h-4 ml-2" />;
    }
    if (key === 'juice_shop') {
      return <AlertTriangle className="w-4 h-4 ml-2" />;
    }
    if (key === 'httpbin') {
      return <CheckCircle className="w-4 h-4 ml-2" />;
    }
    return null;
  };

  return (
    <div className="mb-6">
      <label className="block text-sm font-medium text-gray-700 mb-3">
        Select Target Site
      </label>
      
      {/* Legend */}
      <div className="mb-4 p-3 bg-gray-100 rounded-lg">
        <div className="text-base text-gray-900 mb-2 font-semibold">Site Types:</div>
        <div className="flex flex-wrap gap-4 text-sm">
          <div className="flex items-center">
            <XCircle className="w-3 h-3 text-red-600 mr-1" />
            <span className="text-gray-900">Intentionally Vulnerable</span>
          </div>
          <div className="flex items-center">
            <AlertTriangle className="w-3 h-3 text-orange-600 mr-1" />
            <span className="text-gray-900">Testing Service</span>
          </div>
          <div className="flex items-center">
            <CheckCircle className="w-3 h-3 text-green-600 mr-1" />
            <span className="text-gray-900">Well-Secured Site</span>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {Object.entries(targets).map(([key, target]) => (
          <label
            key={key}
            className={`relative flex items-center p-4 border-2 rounded-lg cursor-pointer transition-all hover:shadow-md ${
              selectedTarget === key
                ? 'border-blue-500 bg-blue-50'
                : 'border-gray-200 hover:border-gray-300 bg-white'
            }`}
          >
            <input
              type="radio"
              name="target"
              value={key}
              checked={selectedTarget === key}
              onChange={(e) => setSelectedTarget(e.target.value)}
              className="mr-3 text-blue-600"
            />
            <div className="flex-1">
              <div className={`font-medium text-gray-900 flex items-center ${target.color}`}>
                {target.name}
                {getTargetIcon(key)}
              </div>
              <div className="text-sm text-gray-600 mt-1">{target.description}</div>
              <div className="text-xs text-gray-500 font-mono mt-1 flex items-center">
                <Globe className="w-3 h-3 mr-1" />
                {target.url}
              </div>
            </div>
          </label>
        ))}
      </div>
    </div>
  );
};

export default TargetSelector;
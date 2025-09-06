import React from 'react';
import { CheckCircle } from 'lucide-react';

const AlertsList = ({ alerts, getRiskColor, getRiskIcon }) => {
  if (!alerts || !Array.isArray(alerts) || alerts.length === 0) {
    return (
      <div className="text-center py-8">
        <CheckCircle className="w-12 h-12 text-green-500 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">No Security Issues Found</h3>
        <p className="text-gray-600">
          The scan completed successfully with no alerts detected.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-medium text-gray-800">Security Alerts</h3>
      {alerts.map((alert, index) => (
        <div key={index} className="border rounded-lg p-4 hover:bg-gray-50">
          <div className="flex items-start justify-between mb-2">
            <h4 className="font-medium text-gray-900 flex-1">{alert.name}</h4>
            <span
              className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getRiskColor(
                alert.risk
              )}`}
            >
              {getRiskIcon(alert.risk)}
              <span className="ml-1">{alert.risk}</span>
            </span>
          </div>

          {alert.description && (
            <p className="text-gray-700 text-sm mb-2">{alert.description}</p>
          )}

          {alert.url && (
            <div className="text-xs text-gray-500 font-mono mb-1">URL: {alert.url}</div>
          )}

          {alert.param && (
            <div className="text-xs text-gray-500">Parameter: {alert.param}</div>
          )}
        </div>
      ))}
    </div>
  );
};

export default AlertsList;
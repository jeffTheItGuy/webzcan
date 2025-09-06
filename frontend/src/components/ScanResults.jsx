import React from 'react';
import { Shield, Clock } from 'lucide-react';
import ScanStats from './ScanStats';
import SummaryStats from './SummaryStats';
import AlertsList from './AlertsList';

const ScanResults = ({ 
  scanResults, 
  formatDuration, 
  getRiskColor, 
  getRiskIcon 
}) => {
  if (!scanResults) return null;

  return (
    <div className="bg-white rounded-lg shadow-lg border border-gray-100 overflow-hidden">
      {/* Results Header */}
      <div className="bg-gradient-to-r from-gray-50 to-gray-100 p-6 border-b border-gray-200">
        <div className="flex items-center justify-between">
          <h2 className="text-xl font-semibold text-gray-800 flex items-center">
            <Shield className="w-5 h-5 mr-2 text-blue-600" />
            Scan Results
          </h2>
          <div className="text-sm text-gray-600">
            <Clock className="w-4 h-4 inline mr-1" />
            {new Date(scanResults.timestamp).toLocaleString()}
          </div>
        </div>
        <div className="mt-2 text-sm text-gray-600">
          Target:{' '}
          <span className="font-mono bg-gray-200 px-2 py-1 rounded">{scanResults.target}</span>
        </div>
      </div>

      <div className="p-6">
        <ScanStats scanResults={scanResults} formatDuration={formatDuration} />
        <SummaryStats summary={scanResults.summary} />
        <AlertsList 
          alerts={scanResults.alerts} 
          getRiskColor={getRiskColor} 
          getRiskIcon={getRiskIcon} 
        />

        {/* Raw JSON Toggle */}
        <details className="mt-6">
          <summary className="cursor-pointer text-blue-600 hover:text-blue-800 text-sm font-medium">
            View Raw JSON Results
          </summary>
          <pre className="mt-3 bg-gray-900 text-green-400 p-4 rounded-lg overflow-auto text-xs">
            {JSON.stringify(scanResults, null, 2)}
          </pre>
        </details>
      </div>
    </div>
  );
};

export default ScanResults;
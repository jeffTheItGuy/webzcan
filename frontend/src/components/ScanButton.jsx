import React from 'react';
import { Play, Loader, Timer } from 'lucide-react';

const ScanButton = ({ 
  isScanning, 
  rateLimitInfo, 
  formatResetTime, 
  startScan 
}) => {
  const isRateLimited = rateLimitInfo && rateLimitInfo.remaining === 0;
  const isDisabled = isScanning || isRateLimited;

  const getButtonContent = () => {
    if (isScanning) {
      return (
        <>
          <Loader className="w-5 h-5 mr-2 animate-spin" />
          Scanning in Progress...
        </>
      );
    }
    
    if (isRateLimited) {
      return (
        <>
          <Timer className="w-5 h-5 mr-2" />
          Rate Limit Reached - Wait {formatResetTime(rateLimitInfo.resetInSeconds)}
        </>
      );
    }

    return (
      <>
        <Play className="w-5 h-5 mr-2" />
        Start Security Scan
        {rateLimitInfo && (
          <span className="ml-2 text-sm opacity-75">
            ({rateLimitInfo.remaining} left)
          </span>
        )}
      </>
    );
  };

  return (
    <button
      onClick={startScan}
      disabled={isDisabled}
      className="flex items-center justify-center w-full bg-gradient-to-r from-blue-600 to-blue-700 text-white px-6 py-4 rounded-lg hover:from-blue-700 hover:to-blue-800 disabled:from-gray-400 disabled:to-gray-500 disabled:cursor-not-allowed transition-all shadow-lg hover:shadow-xl transform hover:-translate-y-0.5 disabled:transform-none"
    >
      {getButtonContent()}
    </button>
  );
};

export default ScanButton;
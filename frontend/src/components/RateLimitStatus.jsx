import React from 'react';
import { Timer } from 'lucide-react';

const RateLimitStatus = ({ rateLimitInfo, formatResetTime }) => {
  if (!rateLimitInfo) return null;

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center">
          <Timer className="w-5 h-5 text-blue-600 mr-2" />
          <span className="text-sm font-medium text-gray-700">Rate Limit Status</span>
        </div>
        <div className="text-sm text-gray-600">
          {rateLimitInfo.remaining} / {rateLimitInfo.limit} scans remaining
        </div>
      </div>
      
      <div className="mt-3">
        <div className="flex justify-between text-xs text-gray-500 mb-1">
          <span>Used: {rateLimitInfo.limit - rateLimitInfo.remaining}</span>
          <span>Resets in: {formatResetTime(rateLimitInfo.resetInSeconds)}</span>
        </div>
        <div className="w-full bg-gray-200 rounded-full h-2">
          <div 
            className={`h-2 rounded-full ${
              rateLimitInfo.remaining === 0 ? 'bg-red-500' : 
              rateLimitInfo.remaining <= 1 ? 'bg-yellow-500' : 'bg-green-500'
            }`}
            style={{ width: `${(rateLimitInfo.remaining / rateLimitInfo.limit) * 100}%` }}
          ></div>
        </div>
        <div className="text-xs text-gray-500 mt-1">
          Window: {rateLimitInfo.windowMinutes} minutes
        </div>
      </div>
    </div>
  );
};

export default RateLimitStatus;
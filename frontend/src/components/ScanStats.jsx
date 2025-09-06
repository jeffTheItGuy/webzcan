import React from 'react';
import { Clock, Globe, Activity, Shield } from 'lucide-react';

const ScanStats = ({ scanResults, formatDuration }) => {
  if (!scanResults.scanInfo) return null;

  const stats = [
    {
      icon: Clock,
      value: formatDuration(scanResults.duration),
      label: 'Total Time',
      color: 'blue'
    },
    {
      icon: Globe,
      value: scanResults.scanInfo.urlsFound ?? 0,
      label: 'URLs Found',
      color: 'purple'
    },
    {
      icon: Activity,
      value: formatDuration(scanResults.scanInfo.spiderDuration),
      label: 'Spider Time',
      color: 'green'
    },
    {
      icon: Shield,
      value: formatDuration(scanResults.scanInfo.activeScanDuration),
      label: 'Active Scan',
      color: 'orange'
    }
  ];

  return (
    <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-6">
      {stats.map((stat, index) => (
        <div key={index} className={`bg-${stat.color}-50 p-4 rounded-lg text-center border border-${stat.color}-200`}>
          <stat.icon className={`w-6 h-6 text-${stat.color}-600 mx-auto mb-2`} />
          <div className={`text-lg font-bold text-${stat.color}-800`}>
            {stat.value}
          </div>
          <div className={`text-sm text-${stat.color}-600`}>{stat.label}</div>
        </div>
      ))}
    </div>
  );
};

export default ScanStats;
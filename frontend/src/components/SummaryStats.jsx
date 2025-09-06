import React from 'react';

const SummaryStats = ({ summary }) => {
  const stats = [
    { label: 'Total Alerts', value: summary?.total ?? 0, color: 'gray' },
    { label: 'High Risk', value: summary?.high ?? 0, color: 'red' },
    { label: 'Medium Risk', value: summary?.medium ?? 0, color: 'orange' },
    { label: 'Low Risk', value: summary?.low ?? 0, color: 'yellow' },
    { label: 'Informational', value: summary?.info ?? 0, color: 'blue' }
  ];

  return (
    <div className="grid grid-cols-1 md:grid-cols-5 gap-4 mb-6">
      {stats.map((stat, index) => (
        <div key={index} className={`bg-${stat.color}-50 p-4 rounded-lg text-center border border-${stat.color}-200`}>
          <div className={`text-2xl font-bold text-${stat.color}-600`}>
            {stat.value}
          </div>
          <div className="text-sm text-gray-600">{stat.label}</div>
        </div>
      ))}
    </div>
  );
};

export default SummaryStats;
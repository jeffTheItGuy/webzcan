import React from 'react';
import { AlertTriangle, Shield, Timer } from 'lucide-react';

const NotificationBanner = ({ type, title, message, icon: CustomIcon }) => {
  const getStyles = (type) => {
    switch (type) {
      case 'warning':
        return 'bg-yellow-50 border-yellow-400 text-yellow-800 text-yellow-700';
      case 'info':
        return 'bg-blue-50 border-blue-400 text-blue-800 text-blue-700';
      case 'error':
        return 'bg-red-50 border-red-400 text-red-800 text-red-700';
      default:
        return 'bg-gray-50 border-gray-400 text-gray-800 text-gray-700';
    }
  };

  const getIcon = (type) => {
    if (CustomIcon) return CustomIcon;
    switch (type) {
      case 'warning':
        return AlertTriangle;
      case 'info':
        return Shield;
      case 'error':
        return Timer;
      default:
        return AlertTriangle;
    }
  };

  const styles = getStyles(type);
  const [bgColor, borderColor, titleColor, textColor] = styles.split(' ');
  const Icon = getIcon(type);

  return (
    <div className={`mb-6 ${bgColor} border-l-4 ${borderColor} rounded-lg p-4`}>
      <div className="flex items-center">
        <Icon className={`w-5 h-5 ${titleColor.replace('text-', 'text-').replace('-800', '-600')} mr-2`} />
        <span className={`${titleColor} font-medium`}>{title}</span>
      </div>
      <p className={`${textColor} mt-1 text-sm`}>{message}</p>
    </div>
  );
};

export default NotificationBanner;
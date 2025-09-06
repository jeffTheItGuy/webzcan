import React from 'react';

const Header = ({ displayApiUrl }) => {
  return (
    <div className="text-center mb-8">
      <div className="py-10 pt-20">
        <div className="w-44 sm:w-94 h-20 sm:h-36 mx-auto bg-blue-600 rounded-lg flex items-center justify-center">
          <span className="text-white text-2xl font-bold">WebZcan</span>
        </div>
      </div>
      <p className="text-gray-600 text-lg max-w-2xl mx-auto">
        Web Application Security Scanner powered by OWASP ZAP
      </p>
      <div className="mt-2 text-sm text-gray-500">
        Scan legitimate, legal testing targets to learn about web application security
      </div>
      <div className="mt-1 text-xs text-gray-400">
        API: {displayApiUrl}/api
      </div>
    </div>
  );
};

export default Header;
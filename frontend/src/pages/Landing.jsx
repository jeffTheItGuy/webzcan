import React, { useState, useEffect } from 'react';
import { Settings, XCircle, AlertTriangle, CheckCircle } from 'lucide-react';
import Header from '../components/Header.jsx';
import RateLimitStatus from '../components/RateLimitStatus.jsx';
import TargetSelector from '../components/TargetSelector';
import NotificationBanner from '../components/NotificationBanner';
import ScanButton from '../components/ScanButton';
import ErrorDisplay from '../components/ErrorDisplay';
import ScanResults from '../components/ScanResults';

const Landing = () => {
  const [selectedTarget, setSelectedTarget] = useState('testfire');
  const [isScanning, setIsScanning] = useState(false);
  const [scanResults, setScanResults] = useState(null);
  const [error, setError] = useState(null);
  const [rateLimitInfo, setRateLimitInfo] = useState(null);

  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '';

  const targets = {
    testfire: {
      name: 'IBM Security AppScan Demo',
      url: 'https://demo.testfire.net',
      description: 'Banking application with intentional vulnerabilities for testing',
      container: 'testfire',
      color: 'text-red-600',
    },
    vulnweb: {
      name: 'Acunetix Test Site',
      url: 'http://testphp.vulnweb.com',
      description: 'PHP application designed for security scanner testing',
      container: 'vulnweb',
      color: 'text-red-600',
    },
    httpbin: {
      name: 'HTTPBin Test Service',
      url: 'https://httpbin.org',
      description: 'HTTP request/response service for testing web scanners',
      container: 'httpbin',
      color: 'text-blue-600',
    },
    juice_shop: {
      name: 'OWASP Juice Shop Demo',
      url: 'https://juice-shop.herokuapp.com',
      description: 'Modern web application with OWASP Top 10 vulnerabilities',
      container: 'juice-shop',
      color: 'text-orange-600',
    }
  };

  // Utility functions
  const getRiskColor = (risk) => {
    switch (risk?.toLowerCase()) {
      case 'high':
        return 'text-red-600 bg-red-50 border-red-200';
      case 'medium':
        return 'text-orange-600 bg-orange-50 border-orange-200';
      case 'low':
        return 'text-yellow-600 bg-yellow-50 border-yellow-200';
      case 'informational':
        return 'text-blue-600 bg-blue-50 border-blue-200';
      default:
        return 'text-gray-600 bg-gray-50 border-gray-200';
    }
  };

  const getRiskIcon = (risk) => {
    switch (risk?.toLowerCase()) {
      case 'high':
        return <XCircle className="w-4 h-4" />;
      case 'medium':
      case 'low':
        return <AlertTriangle className="w-4 h-4" />;
      default:
        return <CheckCircle className="w-4 h-4" />;
    }
  };

  const formatDuration = (duration) => {
    if (!duration) return 'N/A';
    const match = duration.match(/(?:(\d+)m)?(\d+(?:\.\d+)?)s/);
    if (match) {
      const minutes = parseInt(match[1] || 0);
      const seconds = parseFloat(match[2]);
      if (minutes > 0) {
        return `${minutes}m ${seconds.toFixed(1)}s`;
      }
      return `${seconds.toFixed(1)}s`;
    }
    return duration;
  };

  const formatResetTime = (resetInSeconds) => {
    if (!resetInSeconds || resetInSeconds <= 0) return 'Now';
    
    const minutes = Math.floor(resetInSeconds / 60);
    const seconds = resetInSeconds % 60;
    
    if (minutes > 0) {
      return `${minutes}m ${seconds}s`;
    }
    return `${seconds}s`;
  };

  // API functions
  useEffect(() => {
    fetchRateLimitInfo();
  }, []);

  const fetchRateLimitInfo = async () => {
    try {
      const apiUrl = API_BASE_URL ? `${API_BASE_URL}/api/ratelimit` : '/api/ratelimit';
      const response = await fetch(apiUrl);
      if (response.ok) {
        const data = await response.json();
        setRateLimitInfo(data);
      }
    } catch (err) {
      console.log('Could not fetch rate limit info:', err);
    }
  };

  const startScan = async () => {
    setIsScanning(true);
    setError(null);
    setScanResults(null);

    const targetUrl = targets[selectedTarget].url;
    const targetName = targets[selectedTarget].name;

    try {
      const apiUrl = API_BASE_URL ? `${API_BASE_URL}/api/scan` : '/api/scan';
      
      const response = await fetch(apiUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          target: targetUrl,
          targetName: targetName,
        }),
      });

      // Update rate limit info from response headers
      const rateLimitHeaders = {
        limit: response.headers.get('X-RateLimit-Limit'),
        remaining: response.headers.get('X-RateLimit-Remaining'),
        reset: response.headers.get('X-RateLimit-Reset'),
        window: response.headers.get('X-RateLimit-Window')
      };

      if (rateLimitHeaders.limit) {
        setRateLimitInfo({
          limit: parseInt(rateLimitHeaders.limit),
          remaining: parseInt(rateLimitHeaders.remaining),
          windowMinutes: parseInt(rateLimitHeaders.window?.replace('m', '') || '60'),
          resetInSeconds: parseInt(rateLimitHeaders.reset) - Math.floor(Date.now() / 1000)
        });
      }

      if (response.status === 429) {
        const errorData = await response.json();
        setError(`Rate limit exceeded: ${errorData.message}. Please wait before trying again.`);
        setTimeout(fetchRateLimitInfo, 1000);
        return;
      }

      if (!response.ok) {
        throw new Error(`Scan failed: ${response.statusText}`);
      }

      const results = await response.json();
      setScanResults(results);
      
      fetchRateLimitInfo();
    } catch (err) {
      console.error('Scan error:', err);
      setError(`${err.message}. If using an ad blocker, please disable it for this site.`);
    } finally {
      setIsScanning(false);
    }
  };

  const displayApiUrl = API_BASE_URL || window.location.origin;

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 p-4">
      <div className="max-w-7xl mx-auto">
        <Header displayApiUrl={displayApiUrl} />
        
        <RateLimitStatus 
          rateLimitInfo={rateLimitInfo} 
          formatResetTime={formatResetTime} 
        />

        {/* Control Panel */}
        <div className="bg-white rounded-lg shadow-lg p-6 mb-6 border border-gray-100">
          <h2 className="text-xl font-semibold mb-4 text-gray-800 flex items-center">
            <Settings className="w-5 h-5 mr-2 text-blue-600" />
            Scan Configuration
          </h2>

          <TargetSelector 
            targets={targets}
            selectedTarget={selectedTarget}
            setSelectedTarget={setSelectedTarget}
          />

          <NotificationBanner
            type="warning"
            title="Ad Blocker Notice"
            message="If you're using an ad blocker and experiencing connection issues, please whitelist this site or try in incognito mode."
          />

          <NotificationBanner
            type="info"
            title="Legal Notice"
            message="These are legitimate testing targets provided by security organizations for educational purposes."
          />

          {rateLimitInfo && rateLimitInfo.remaining === 0 && (
            <NotificationBanner
              type="error"
              title="Rate Limit Reached"
              message={`You've reached your scan limit. Please wait ${formatResetTime(rateLimitInfo.resetInSeconds)} before scanning again.`}
            />
          )}

          <ScanButton
            isScanning={isScanning}
            rateLimitInfo={rateLimitInfo}
            formatResetTime={formatResetTime}
            startScan={startScan}
          />
        </div>

        <ErrorDisplay error={error} />

        <ScanResults
          scanResults={scanResults}
          formatDuration={formatDuration}
          getRiskColor={getRiskColor}
          getRiskIcon={getRiskIcon}
        />
      </div>
    </div>
  );
};

export default Landing;
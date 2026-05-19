export const API_CONFIG = {
  BASE_URL: 'http://localhost:5000/api',
  ENDPOINTS: {
    SEARCH: {
      EXECUTE: '/search/execute',
      MULTI_FILE: '/search/multi-file',
      COMPARE_ALL: '/search/compare-all',
      ALGORITHMS: '/search/algorithms',
    }
  },
  PROMETHEUS_URL: 'http://localhost:9090',
  JAEGER_URL: 'http://localhost:16686',
} as const;

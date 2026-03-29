export const API_CONFIG = {
  BASE_URL: 'https://localhost:64872/api', // aqui você muda a url para que gerar ao rodar o backend localmente
  ENDPOINTS: {
    SEARCH: {
      EXECUTE: '/search/execute',
      MULTI_FILE: '/search/multi-file',
      COMPARE_ALL: '/search/compare-all',
      ALGORITHMS: '/search/algorithms',
    },
  },
} as const;

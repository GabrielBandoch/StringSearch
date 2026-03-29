// ─── API Configuration ───────────────────────────────────────────────────────
// Altere BASE_URL para apontar para seu backend em produção.

export const API_CONFIG = {
  BASE_URL: 'https://localhost:64872/api', // aqui você muda a url para que gerar ao rodar o backend localmente
  ENDPOINTS: {
    SEARCH: {
      EXECUTE: '/search/execute',
      STEP_BY_STEP: '/search/step-by-step',
      MULTI_FILE: '/search/multi-file',
      COMPARE_ALL: '/search/compare-all',
      ALGORITHMS: '/search/algorithms',
    },
  },
} as const;

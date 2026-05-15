import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = __ENV.TAILBOOK_BASE_URL || 'https://localhost:5001';
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || 'admin@tailbook.local';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || 'MyV3ryC00lAdminP@ss';

export const options = {
  scenarios: {
    saturate: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '10s', target: 50 },
        { duration: '10s', target: 100 },
        { duration: '10s', target: 200 },
        { duration: '10s', target: 400 },
        { duration: '10s', target: 600 },
        { duration: '10s', target: 800 },
        { duration: '10s', target: 1000 },
        { duration: '10s', target: 1200 },
        { duration: '10s', target: 1500 },
      ],
      gracefulRampDown: '5s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
  },
};

export function setup() {
  const loginRes = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  if (loginRes.status !== 200) {
    console.error('Admin login failed');
    return { token: '' };
  }

  return { token: loginRes.json('accessToken') };
}

export default function (data) {
  if (!data.token) return;

  const res = http.get(`${BASE_URL}/api/identity/me`, {
    headers: { Authorization: `Bearer ${data.token}` },
  });

  check(res, {
    'me 200': (r) => r.status === 200,
  });
}

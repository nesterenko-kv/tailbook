import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = __ENV.TAILBOOK_BASE_URL || 'https://localhost:5001';
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || 'admin@tailbook.local';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || 'MyV3ryC00lAdminP@ss';

const USER_POOL_SIZE = parseInt(__ENV.USER_POOL_SIZE || '100', 10);

export const options = {
  scenarios: {
    max_rps: {
      executor: 'ramping-arrival-rate',
      startRate: 100,
      timeUnit: '1s',
      preAllocatedVUs: 100,
      maxVUs: 500,
      stages: [
        { target: 200, duration: '15s' },
        { target: 500, duration: '15s' },
        { target: 1000, duration: '15s' },
        { target: 2000, duration: '15s' },
        { target: 4000, duration: '15s' },
        { target: 6000, duration: '15s' },
      ],
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<2000'],
  },
};

export function setup() {
  const loginRes = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  if (loginRes.status !== 200) {
    console.error('Admin login failed');
    return { tokens: [] };
  }

  const adminToken = loginRes.json('accessToken');
  const tokens = [];

  for (let i = 0; i < USER_POOL_SIZE; i++) {
    const email = `me-${i}-${Date.now()}@test.local`;
    const createRes = http.post(`${BASE_URL}/api/admin/iam/users`,
      JSON.stringify({ email, password: 'MeP@ss123', displayName: `ME User ${i}`, roleCodes: ['client'] }),
      {
        headers: { Authorization: `Bearer ${adminToken}`, 'Content-Type': 'application/json' },
      },
    );
    if (createRes.status !== 201 && createRes.status !== 200) continue;

    const userLogin = http.post(`${BASE_URL}/api/identity/auth/login`,
      JSON.stringify({ email, password: 'MeP@ss123' }),
      { headers: { 'Content-Type': 'application/json' } },
    );
    if (userLogin.status === 200) {
      tokens.push(userLogin.json('accessToken'));
    }
  }

  console.log(`Pre-fetched ${tokens.length} tokens`);
  return { tokens };
}

export default function (data) {
  if (!data.tokens || data.tokens.length === 0) return;

  const token = data.tokens[__ITER % data.tokens.length];

  const res = http.get(`${BASE_URL}/api/identity/me`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  check(res, {
    'me 200': (r) => r.status === 200,
    'has email': (r) => r.json('email') !== undefined,
  });
}

import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = __ENV.TAILBOOK_BASE_URL || 'https://localhost:5001';
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || 'admin@tailbook.local';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || 'MyV3ryC00lAdminP@ss';

const USER_POOL_SIZE = parseInt(__ENV.USER_POOL_SIZE || '50', 10);

export const options = {
  scenarios: {
    max_rps: {
      executor: 'ramping-arrival-rate',
      startRate: 50,
      timeUnit: '1s',
      preAllocatedVUs: 100,
      maxVUs: 500,
      stages: [
        { target: 100, duration: '15s' },
        { target: 200, duration: '15s' },
        { target: 400, duration: '15s' },
        { target: 600, duration: '15s' },
        { target: 800, duration: '15s' },
        { target: 1000, duration: '15s' },
        { target: 1200, duration: '15s' },
      ],
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<5000'],
  },
};

let users = [];

export function setup() {
  const loginRes = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  if (loginRes.status !== 200) {
    console.error('Admin login failed');
    return [];
  }

  const adminToken = loginRes.json('accessToken');
  const created = [];

  for (let i = 0; i < USER_POOL_SIZE; i++) {
    const email = `rps-${i}-${Date.now()}@test.local`;
    const res = http.post(`${BASE_URL}/api/admin/iam/users`,
      JSON.stringify({ email, password: 'RpsP@ss123', displayName: `RPS User ${i}`, roleCodes: ['client'] }),
      {
        headers: { Authorization: `Bearer ${adminToken}`, 'Content-Type': 'application/json' },
      },
    );
    if (res.status === 201 || res.status === 200) {
      created.push({ email, password: 'RpsP@ss123' });
    }
  }

  console.log(`Created ${created.length} users for RPS test`);
  return created;
}

export default function (data) {
  if (!data || data.length === 0) return;

  const user = data[__ITER % data.length];

  const res = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({ email: user.email, password: user.password }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  check(res, {
    'login 200': (r) => r.status === 200,
    'has token': (r) => r.json('accessToken') !== undefined,
  });
}

import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.TAILBOOK_BASE_URL || 'https://localhost:5001';
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || 'admin@tailbook.local';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || 'MyV3ryC00lAdminP@ss';

const USERS_PER_VU = parseInt(__ENV.USERS_PER_VU || '2', 10);

export const options = {
  stages: [
    { duration: '5s', target: 5 },
    { duration: '20s', target: 30 },
    { duration: '5s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    http_req_failed: ['rate<0.01'],
  },
  noConnectionReuse: true,
};

export function setup() {
  const loginRes = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  check(loginRes, { 'admin login': (r) => r.status === 200 });
  if (loginRes.status !== 200) return { adminToken: null, clientUsers: [] };

  const adminToken = loginRes.json('accessToken');

  // Create client users for this run
  const clientUsers = [];
  for (let i = 0; i < USERS_PER_VU; i++) {
    const uid = `${__VU}-${i}-${Date.now()}`;
    const email = `auth-${uid}@test.local`;
    const password = 'AuthStr3ss!';

    const createRes = http.post(`${BASE_URL}/api/admin/iam/users`,
      JSON.stringify({
        email,
        password,
        displayName: `Auth Stress VU${__VU}`,
        roleCodes: ['client'],
      }),
      {
        headers: {
          Authorization: `Bearer ${adminToken}`,
          'Content-Type': 'application/json',
        },
      },
    );
    if (createRes.status === 201 || createRes.status === 200) {
      clientUsers.push({ email, password });
    }
  }

  return { adminToken, clientUsers };
}

export default function (data) {
  const { adminToken, clientUsers } = data;

  // Every 3rd iteration: admin endpoints
  if (__ITER % 3 === 0 && adminToken) {
    const adminHeaders = { Authorization: `Bearer ${adminToken}` };
    const endpoints = [
      '/api/admin/iam/users',
      '/api/admin/iam/roles',
      '/api/admin/iam/permissions',
    ];
    for (const ep of endpoints) {
      const res = http.get(`${BASE_URL}${ep}`, { headers: adminHeaders });
      check(res, { [`admin ${ep} 200`]: (r) => r.status === 200 });
    }
  } else if (clientUsers.length > 0) {
    const user = clientUsers[__ITER % clientUsers.length];

    const loginRes = http.post(`${BASE_URL}/api/identity/auth/login`,
      JSON.stringify({ email: user.email, password: user.password }),
      { headers: { 'Content-Type': 'application/json' } },
    );

    if (check(loginRes, { 'identity login 200': (r) => r.status === 200 })) {
      const token = loginRes.json('accessToken');
      const meRes = http.get(`${BASE_URL}/api/identity/me`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      check(meRes, { 'identity me 200': (r) => r.status === 200 });
    }
  }

  sleep(0.5);
}

import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.TAILBOOK_BASE_URL || 'https://localhost:5001';
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || 'admin@tailbook.local';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || 'MyV3ryC00lAdminP@ss';

const USERS_PER_VU = parseInt(__ENV.USERS_PER_VU || '2', 10);

export const options = {
  stages: [
    { duration: '10s', target: 20 },
    { duration: '40s', target: 80 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<5000'],
    http_req_failed: ['rate<0.05'],
  },
  noConnectionReuse: true,
};

export function setup() {
  const loginRes = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  check(loginRes, { 'admin login': (r) => r.status === 200 });
  if (loginRes.status !== 200) return { adminToken: null, users: [] };

  const adminToken = loginRes.json('accessToken');

  const users = [];
  for (let i = 0; i < USERS_PER_VU; i++) {
    const uid = `${__VU}-${i}-${Date.now()}`;
    const email = `wl-${uid}@test.local`;
    const password = 'WkLdStr3ss!';

    const createRes = http.post(`${BASE_URL}/api/admin/iam/users`,
      JSON.stringify({
        email,
        password,
        displayName: `WL Stress VU${__VU}`,
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
      users.push({ email, password });
    }
  }
  return { adminToken, users };
}

function fullSessionFlow(email, password) {
  const loginRes = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({ email, password }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  const loginOk = check(loginRes, {
    'flow login 200': (r) => r.status === 200,
    'has refresh token': (r) => r.json('refreshToken') !== undefined,
  });

  if (!loginOk) return;

  const accessToken = loginRes.json('accessToken');
  const refreshToken = loginRes.json('refreshToken');

  const meRes = http.get(`${BASE_URL}/api/identity/me`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  check(meRes, { 'flow me 200': (r) => r.status === 200 });

  const refreshRes = http.post(`${BASE_URL}/api/identity/auth/refresh`,
    JSON.stringify({ refreshToken }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  check(refreshRes, {
    'flow refresh 200': (r) => r.status === 200,
    'flow new token': (r) => r.json('accessToken') !== undefined,
  });
}

function adminWorkflow(adminToken) {
  if (!adminToken) return;
  const headers = { Authorization: `Bearer ${adminToken}` };
  http.get(`${BASE_URL}/api/admin/iam/users`, { headers });
  http.get(`${BASE_URL}/api/admin/iam/roles`, { headers });
  http.get(`${BASE_URL}/api/admin/iam/permissions`, { headers });
}

export default function (data) {
  const { adminToken, users } = data;

  if (users.length === 0) {
    sleep(1);
    return;
  }

  // 70% user flow, 30% admin workflow
  if (__ITER % 3 === 0) {
    adminWorkflow(adminToken);
  } else {
    const user = users[__ITER % users.length];
    fullSessionFlow(user.email, user.password);
  }

  sleep(1);
}

import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.TAILBOOK_BASE_URL || 'https://localhost:5001';
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || 'admin@tailbook.local';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || 'MyV3ryC00lAdminP@ss';

const USERS_PER_VU = parseInt(__ENV.USERS_PER_VU || '3', 10);

export const options = {
  stages: [
    { duration: '10s', target: 10 },
    { duration: '30s', target: 50 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed: ['rate<0.02'],
  },
  noConnectionReuse: true,
};

function adminLogin() {
  const res = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  check(res, { 'admin login': (r) => r.status === 200 });
  return res.status === 200 ? res.json('accessToken') : null;
}

function createTestUser(token, idx) {
  const uid = `${__VU}-${idx}-${Date.now()}`;
  const email = `stress-${uid}@test.local`;
  const password = 'Str3ssUs3rP@ss';

  const createRes = http.post(`${BASE_URL}/api/admin/iam/users`,
    JSON.stringify({
      email,
      password,
      displayName: `Stress VU${__VU} U${idx}`,
      roleCodes: ['client'],
    }),
    {
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    },
  );

  if (createRes.status === 201 || createRes.status === 200) {
    return { email, password };
  }
  return null;
}

export function setup() {
  const token = adminLogin();
  if (!token) return { users: [] };

  const users = [];
  for (let i = 0; i < USERS_PER_VU; i++) {
    const u = createTestUser(token, i);
    if (u) users.push(u);
  }
  return { users, token };
}

export default function (data) {
  const { users } = data;
  if (users.length === 0) {
    sleep(1);
    return;
  }

  const user = users[__ITER % users.length];

  // Login with valid credentials
  const loginRes = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({ email: user.email, password: user.password }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  const loginOk = check(loginRes, {
    'login 200': (r) => r.status === 200,
    'has access token': (r) => r.json('accessToken') !== undefined,
  });

  if (loginOk) {
    const token = loginRes.json('accessToken');

    // Verify authenticated access
    const meRes = http.get(`${BASE_URL}/api/identity/me`, {
      headers: { Authorization: `Bearer ${token}` },
    });

    check(meRes, {
      'me 200': (r) => r.status === 200,
      'me returns email': (r) => r.json('email') !== undefined,
    });
  }

  sleep(1);
}

import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = __ENV.TAILBOOK_BASE_URL || 'https://localhost:5001';
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || 'admin@tailbook.local';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || 'MyV3ryC00lAdminP@ss';

export const options = {
  scenarios: {
    constant: {
      executor: 'constant-vus',
      vus: 500,
      duration: '30s',
    },
  },
};

export function setup() {
  const loginRes = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  return { token: loginRes.status === 200 ? loginRes.json('accessToken') : '' };
}

export default function (data) {
  if (!data.token) return;
  const res = http.get(`${BASE_URL}/api/identity/me`, {
    headers: { Authorization: `Bearer ${data.token}` },
  });
  check(res, { 'me 200': (r) => r.status === 200 });
}

// k6 setup script: seeds test users for identity stress tests.
// Run once before stress tests:  k6 run ops/scripts/stress/seed-identity-stress-data.js
import http from 'k6/http';

const BASE_URL = __ENV.TAILBOOK_BASE_URL || 'http://localhost:5000';

const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || 'admin@test.local';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || 'MyV3ryC00lAdminP@ss';

const USERS_TO_SEED = parseInt(__ENV.USERS_TO_SEED || '100', 10);

export const options = {
  vus: 1,
  iterations: 1,
};

function adminLogin() {
  const res = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  if (res.status !== 200) {
    throw new Error(`Admin login failed: ${res.status} ${res.body}`);
  }
  return res.json('accessToken');
}

function getAdminTokenOrBootstrap() {
  // Try admin login first
  const loginRes = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  if (loginRes.status === 200) {
    return loginRes.json('accessToken');
  }

  // If that fails, try bootstrapping via the admin create endpoint
  // with a setup-only admin registration
  console.warn('Could not log in as admin — ensure the app is running and seeded.');
  return null;
}

export default function () {
  const token = getAdminTokenOrBootstrap();
  if (!token) {
    console.error('Cannot seed data without admin access. Aborting.');
    return;
  }

  const authHeaders = {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  };

  // Fetch existing roles to know role IDs
  const rolesRes = http.get(`${BASE_URL}/api/admin/iam/roles`, { headers: authHeaders });
  if (rolesRes.status !== 200) {
    console.error(`Failed to fetch roles: ${rolesRes.status}`);
    return;
  }

  const rolesBody = rolesRes.json();
  const roles = rolesBody.value || rolesBody;
  const clientRole = roles.find((r) => r.code === 'client');
  const groomerRole = roles.find((r) => r.code === 'groomer');

  if (!clientRole || !groomerRole) {
    console.error('Required roles (client, groomer) not found');
    return;
  }

  let created = 0;
  for (let i = 0; i < USERS_TO_SEED; i++) {
    const email = `stress-${i}-${Math.random().toString(36).substring(2, 10)}@test.local`;
    const roleCode = i % 2 === 0 ? 'client' : 'groomer';

    const createRes = http.post(`${BASE_URL}/api/admin/iam/users`,
      JSON.stringify({
        email,
        password: 'Str3ssUs3rP@ss',
        displayName: `Stress User ${i}`,
        roles: [roleCode],
      }),
      { headers: authHeaders },
    );

    if (createRes.status === 201 || createRes.status === 200) {
      created++;
    } else {
      console.warn(`Failed to create user ${email}: ${createRes.status}`);
    }
  }

  console.log(`Seeded ${created} users.`);
}

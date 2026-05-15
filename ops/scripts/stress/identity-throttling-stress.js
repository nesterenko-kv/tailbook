import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend } from 'k6/metrics';

const throttlingRate = new Trend('throttling_rate');

const BASE_URL = __ENV.TAILBOOK_BASE_URL || 'http://localhost:5000';

// Single non-existent user — all VUs try the same credentials to trigger throttling
const TARGET_EMAIL = `throttle-target-${__ENV.TEST_RUN_ID || 'default'}@test.local`;
const WRONG_PASSWORD = 'WrongP@ss123!';

export const options = {
  stages: [
    { duration: '5s', target: 5 },   // warm-up
    { duration: '20s', target: 30 },  // ramp-up concurrency
    { duration: '10s', target: 0 },   // cool-down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],
    // Expect SOME 429s as throttling kicks in
    // Also expect 401s for invalid credentials
  },
};

export default function () {
  const res = http.post(`${BASE_URL}/api/identity/auth/login`,
    JSON.stringify({
      email: TARGET_EMAIL,
      password: WRONG_PASSWORD,
    }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  // Acceptable responses:
  // - 401: invalid credentials (normal)
  // - 429: throttled (expected behavior)
  // - 200: should NOT happen for wrong password
  check(res, {
    'not 200': (r) => r.status !== 200,
    'has retry-after on 429': (r) => r.status !== 429 || r.headers['Retry-After'] !== undefined,
    'response time < 3s': (r) => r.timings.duration < 3000,
  });

  throttlingRate.add(res.status === 429 ? 1 : 0);

  sleep(0.5);
}

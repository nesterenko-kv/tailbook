import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e/tests",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [
    ["html", { outputFolder: "playwright-report" }],
    ["list"],
  ],
  use: {
    trace: "on-first-retry",
    screenshot: "only-on-failure",
  },
  projects: [
    {
      name: "admin-web",
      use: {
        baseURL: process.env.ADMIN_WEB_URL || "http://localhost:3001",
        ...devices["Desktop Chrome"],
      },
      testMatch: "**/admin/**/*.spec.ts",
    },
    {
      name: "client-web",
      use: {
        baseURL: process.env.CLIENT_WEB_URL || "http://localhost:3002",
        ...devices["Desktop Chrome"],
      },
      testMatch: "**/client/**/*.spec.ts",
    },
    {
      name: "groomer-web",
      use: {
        baseURL: process.env.GROOMER_WEB_URL || "http://localhost:3003",
        ...devices["Desktop Chrome"],
      },
      testMatch: "**/groomer/**/*.spec.ts",
    },
  ],
});

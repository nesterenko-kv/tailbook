import { test, expect } from "@playwright/test";

test.describe("Admin login page", () => {
  test("renders login form with pre-filled credentials", async ({ page }) => {
    await page.goto("/login");

    await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
    await expect(page.getByText("Tailbook")).toBeVisible();
    await expect(page.getByText("Admin Web MVP")).toBeVisible();

    const emailInput = page.getByLabel("Email");
    const passwordInput = page.getByLabel("Password");
    await expect(emailInput).toHaveValue("admin@tailbook.local");
    await expect(passwordInput).toHaveValue("MyV3ryC00lAdminP@ss");

    await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
    await expect(page.getByText(/Default seed:/)).toContainText("admin@tailbook.local");
  });

  test("has correct page title", async ({ page }) => {
    await page.goto("/login");
    await expect(page).toHaveTitle(/Admin/);
  });

  test("redirects unauthenticated root to login", async ({ page }) => {
    await page.goto("/");
    await expect(page).toHaveURL(/\/login/);
  });
});

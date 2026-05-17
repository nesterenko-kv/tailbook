import { test, expect } from "@playwright/test";

test.describe("Groomer login page", () => {
  test("renders login form with pre-filled credentials", async ({ page }) => {
    await page.goto("/login");

    await expect(page.getByText(/Groomer login/)).toBeVisible();
    await expect(page.getByRole("heading", { name: "Sign in" })).toBeVisible();
    await expect(page.getByText(/groomer-linked IAM user/)).toBeVisible();

    const emailInput = page.getByLabel("Email");
    const passwordInput = page.getByLabel("Password");
    await expect(emailInput).toHaveValue("groomer@tailbook.local");
    await expect(passwordInput).toHaveValue("Groomer123!");

    await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
  });

  test("has correct page title", async ({ page }) => {
    await page.goto("/login");
    await expect(page).toHaveTitle(/Groomer/);
  });
});

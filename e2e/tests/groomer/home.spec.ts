import { test, expect } from "@playwright/test";

test.describe("Groomer landing page", () => {
  test("renders stage badge and title", async ({ page }) => {
    await page.goto("/");

    await expect(page.getByText(/Stage 8 groomer-safe web/)).toBeVisible();
    await expect(page.getByRole("heading", { name: "Tailbook Groomer" })).toBeVisible();
    await expect(page.getByText(/Groomer-safe appointment/)).toBeVisible();
  });

  test("renders login card link", async ({ page }) => {
    await page.goto("/");

    const loginLink = page.getByRole("link", { name: "Login" });
    await expect(loginLink).toBeVisible();
    await expect(loginLink).toHaveAttribute("href", "/login");
    await expect(page.getByText("Authenticate with a groomer account")).toBeVisible();
  });

  test("renders appointments card link", async ({ page }) => {
    await page.goto("/");

    const appointmentsLink = page.getByRole("link", { name: "My appointments" });
    await expect(appointmentsLink).toBeVisible();
    await expect(appointmentsLink).toHaveAttribute("href", "/appointments");
    await expect(page.getByText("Review assigned appointments")).toBeVisible();
  });

  test("has correct page title", async ({ page }) => {
    await page.goto("/");
    await expect(page).toHaveTitle(/Groomer/);
  });
});

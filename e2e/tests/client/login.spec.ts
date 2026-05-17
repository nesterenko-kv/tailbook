import { test, expect } from "@playwright/test";

test.describe("Client login page", () => {
  test("renders login form", async ({ page }) => {
    await page.goto("/login");

    await expect(page.getByRole("heading", { name: "Вхід у портал" })).toBeVisible();
    await expect(page.getByText("Керуйте записами, вихованцями та історією відвідувань.")).toBeVisible();

    await expect(page.getByLabel("Email")).toBeVisible();
    await expect(page.getByLabel("Пароль")).toBeVisible();
    await expect(page.getByRole("button", { name: "Увійти" })).toBeVisible();
  });

  test("has link to forgot password", async ({ page }) => {
    await page.goto("/login");

    const forgotLink = page.getByRole("link", { name: "Забули?" });
    await expect(forgotLink).toBeVisible();
    await expect(forgotLink).toHaveAttribute("href", "/forgot-password");
  });

  test("has link to register", async ({ page }) => {
    await page.goto("/login");

    const registerLink = page.getByRole("link", { name: "Створити" });
    await expect(registerLink).toBeVisible();
    await expect(registerLink).toHaveAttribute("href", "/register");
  });

  test("has correct page title", async ({ page }) => {
    await page.goto("/login");
    const title = await page.title();
    expect(title.length).toBeGreaterThan(0);
  });
});

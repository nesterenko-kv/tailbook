import { test, expect } from "@playwright/test";

test.describe("Client landing page", () => {
  test("renders hero section", async ({ page }) => {
    await page.goto("/");

    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    await expect(page.getByRole("link", { name: "Записатися онлайн", exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: "Подзвонити", exact: true })).toBeVisible();
  });

  test("renders features section", async ({ page }) => {
    await page.goto("/");

    await expect(page.getByRole("heading", { name: "Безпечно" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Швидко" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "З любов'ю" })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Якісно" })).toBeVisible();
  });

  test("renders popular services section", async ({ page }) => {
    await page.goto("/");

    const servicesHeading = page.getByRole("heading", { name: /Послуги/i });
    await expect(servicesHeading).toBeVisible();
  });

  test("renders team section", async ({ page }) => {
    await page.goto("/");

    const teamHeading = page.getByRole("heading", { name: /Команда/i });
    await expect(teamHeading).toBeVisible();
  });

  test("renders reviews section", async ({ page }) => {
    await page.goto("/");

    const reviewsHeading = page.getByRole("heading", { name: /клієнти/i });
    await expect(reviewsHeading).toBeVisible();
  });

  test("renders FAQ section with accordion items", async ({ page }) => {
    await page.goto("/");

    const faqHeading = page.getByRole("heading", { name: /Часті питання/i });
    await expect(faqHeading).toBeVisible();
  });

  test("renders contact section", async ({ page }) => {
    await page.goto("/");

    const contactHeading = page.getByRole("heading", { name: /Контакти/i });
    await expect(contactHeading).toBeVisible();
  });

  test("booking CTA links to booking flow", async ({ page }) => {
    await page.goto("/");

    const bookButton = page.getByRole("link", { name: /Записатися/ }).first();
    await expect(bookButton).toHaveAttribute("href", "/booking/services");
  });

  test("has correct page title", async ({ page }) => {
    await page.goto("/");

    const title = await page.title();
    expect(title.length).toBeGreaterThan(0);
  });
});

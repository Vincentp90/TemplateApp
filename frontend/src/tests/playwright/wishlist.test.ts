import { test, expect } from "@playwright/test";

//const apiurl = "http://localhost:5186" //import.meta.env.VITE_API_URL doesn't work
const frontendurl = "http://localhost:5173"

test("login", async ({ page }) => {
    await page.goto(frontendurl);

    await page.getByLabel("Email").fill("playwrightuser@example.com");
    await page.getByLabel("Password").fill("playwrightpassword");
    await page.getByRole("button", { name: /register/i }).click();
    await page.getByRole("button", { name: "Submitting", exact: true }).first().waitFor({ state: "hidden" });
    await page.getByRole("button", { name: "Login", exact: true }).click();

    await expect(page.getByRole("heading", { name: "Search steam games to add to wishlist", level: 2 })).toBeVisible();
});
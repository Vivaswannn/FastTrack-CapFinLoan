const { chromium } = require('playwright');
const path = require('path');

(async () => {
    console.log('Starting Playwright test...');
    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({ viewport: { width: 1280, height: 720 } });
    const page = await context.newPage();

    try {
        // Step 1: Admin Login
        console.log('Navigating to login...');
        await page.goto('http://localhost:5173/auth/login');
        await page.fill('input[type="email"]', 'admin@capfinloan.com');
        await page.fill('input[type="password"]', 'Admin@1234');
        await page.click('button:has-text("Sign In")');

        // Wait for dashboard to load
        console.log('Waiting for admin dashboard...');
        await page.waitForSelector('text=Admin Dashboard', { timeout: 10000 });

        // Step 2: Navigate to Users page
        console.log('Navigating to Users page...');
        await page.click('a:has-text("Users")');
        await page.waitForSelector('text=User Management');

        // Note: Wait for the API to load the users list
        await page.waitForTimeout(2000); 

        // Step 3: Deactivate Test User Two
        console.log('Deactivating Test User...');
        // Find the row containing "testuser2@example.com" and click its "Deactivate" button.
        // Or if they are all generic "Deactivate" buttons, we find the row first.
        const userRow = page.locator('tr').filter({ hasText: 'testuser2@example.com' });
        
        // Ensure "Test User Two" is currently Active and can be deactivated
        const deactivateBtn = userRow.locator('button:has-text("Deactivate")');
        if (await deactivateBtn.count() > 0) {
            await deactivateBtn.click();
            console.log('Clicked Deactivate.');
            await page.waitForTimeout(2000); // Wait for API response and toast
        } else {
             // If already inactive, let's reactivate then deactivate for testing
            const activateBtn = userRow.locator('button:has-text("Activate")');
            if (await activateBtn.count() > 0) {
                 await activateBtn.click();
                 await page.waitForTimeout(2000);
                 await userRow.locator('button:has-text("Deactivate")').click();
                 await page.waitForTimeout(2000);
            }
        }

        // Take screenshot of deactivated status
        await page.screenshot({ path: path.join('C:', 'Users', 'vivas', '.gemini', 'antigravity', 'brain', '12b19156-5ec5-4be4-ac14-8b779816dc21', 'after_deactivation.png') });
        console.log('Screenshot saved: after_deactivation.png');

        // Step 4: Logout
        console.log('Logging out...');
        await page.click('button:has-text("Logout")');
        await page.waitForSelector('text=New to CapFinLoan?', { timeout: 10000 });

        // Step 5: Login as deactivated user
        console.log('Attempting login as deactivated user...');
        await page.fill('input[type="email"]', 'testuser2@example.com');
        await page.fill('input[type="password"]', 'Test@1234');
        await page.click('button:has-text("Sign In")');

        // Wait for Toast error
        console.log('Waiting for error message...');
        await page.waitForTimeout(2000);
        
        // Take screenshot of blocked login attempt (showing error message)
        await page.screenshot({ path: path.join('C:', 'Users', 'vivas', '.gemini', 'antigravity', 'brain', '12b19156-5ec5-4be4-ac14-8b779816dc21', 'blocked_login.png') });
        console.log('Screenshot saved: blocked_login.png');

        // Final Reactivate Cleanup
        console.log('Reactivating user for cleanup...');
        await page.fill('input[type="email"]', '');
        await page.fill('input[type="password"]', '');
        await page.fill('input[type="email"]', 'admin@capfinloan.com');
        await page.fill('input[type="password"]', 'Admin@1234');
        await page.click('button:has-text("Sign In")');
        await page.waitForSelector('text=Admin Dashboard', { timeout: 10000 });
        await page.click('a:has-text("Users")');
        await page.waitForTimeout(2000);
        const uRow = page.locator('tr').filter({ hasText: 'testuser2@example.com' });
        await uRow.locator('button:has-text("Activate")').click();
        await page.waitForTimeout(1000);

        console.log('Test completed successfully.');

    } catch (e) {
        console.error('Test failed:', e);
        await page.screenshot({ path: path.join('C:', 'Users', 'vivas', '.gemini', 'antigravity', 'brain', '12b19156-5ec5-4be4-ac14-8b779816dc21', 'playwright_error_fallback.png') });
    } finally {
        await browser.close();
    }
})();

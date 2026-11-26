const {chromium} = require("playwright-core");
const assert = require("node:assert/strict");
const {readFileSync} = require("node:fs");

async function main() {
    const {baseUrl, password, candidate, employer, admin} = JSON.parse(readFileSync(0, "utf8"));
    const browser = await chromium.launch({channel:process.env.SWAGGER_BROWSER_CHANNEL || "chrome"});
    try {
        for (const email of [candidate, employer, admin]) {
            const page = await browser.newPage({viewport:{width:390, height:844}});
            page.setDefaultTimeout(10000);
            await page.goto(new URL("/#/account/settings", baseUrl).href);
            const login = page.locator("#account-form");
            await login.getByLabel("Email", {exact:true}).fill(email);
            await login.getByLabel("Mật khẩu", {exact:true}).fill(password);
            await login.getByRole("button", {name:"Đăng nhập"}).click();
            await page.waitForURL("**/#/profile");
            await page.getByRole("heading", {level:1, name:"Hồ sơ và tài khoản"}).waitFor();
            assert.equal(await page.locator("#candidate-profile").count(), email === candidate ? 1 : 0);
            assert.equal(await page.locator("#company-logo-form").count(), email === employer ? 1 : 0);
            assert.equal(await page.locator("#account a[href='#/profile']").count(), 1);
            assert.equal(await page.locator("a[href='#/account/settings']").count(), 0);

            await page.getByRole("button", {name:"Sửa thông tin", exact:true}).click();
            const details = page.getByRole("dialog", {name:"Sửa thông tin tài khoản"});
            assert.equal(await details.getByLabel("Tên công ty").count(), email === employer ? 1 : 0);
            await details.getByLabel("Họ và tên").fill("Tên thử trên trang hồ sơ");
            await details.getByRole("button", {name:"Lưu thay đổi"}).click();
            await page.locator(".profile-heading").getByRole("heading", {name:"Tên thử trên trang hồ sơ"}).waitFor();

            await page.getByRole("button", {name:"Đổi mật khẩu", exact:true}).click();
            const security = page.getByRole("dialog", {name:"Đổi mật khẩu"});
            for (const field of ["currentPassword","newPassword","confirmNewPassword"]) {
                const input = security.locator("#"+field);
                const toggle = security.locator(`button[aria-controls="${field}"]`);
                await toggle.click();
                assert.equal(await input.getAttribute("type"),"text");
                await toggle.click();
                assert.equal(await input.getAttribute("type"),"password");
            }
            if (email === candidate) {
                const replacement = "Updated-profile-2026!";
                await security.locator("#currentPassword").fill(password);
                await security.locator("#newPassword").fill(replacement);
                await security.locator("#confirmNewPassword").fill("different-password");
                await security.getByRole("button", {name:"Đổi mật khẩu", exact:true}).click();
                await security.getByText("Xác nhận mật khẩu mới không khớp.").waitFor();
                await security.locator("#confirmNewPassword").fill(replacement);
                await security.getByRole("button", {name:"Đổi mật khẩu", exact:true}).click();
                await security.waitFor({state:"hidden"});
                const session = await page.request.post(new URL("/api/auth/login",baseUrl).href, {data:{email,password:replacement}});
                assert.equal(session.status(),200);
                await page.locator("#candidate-profile [data-setup]").first().click();
                await page.getByRole("dialog", {name:"Thiết lập hồ sơ"}).getByRole("button", {name:"Để sau"}).click();
            } else {
                await security.getByRole("button", {name:"Hủy"}).click();
            }
            assert.equal(new URL(page.url()).hash,"#/profile");
            await page.close();
        }
    } finally { await browser.close(); }
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });

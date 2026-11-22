// Chạy từ SwaggerTests để dùng đúng Kestrel và database riêng của lần test.
const { chromium } = require("playwright-core");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");

async function main() {
    const { baseUrl, email, password, jobId } = JSON.parse(fs.readFileSync(0, "utf8"));
    const browser = await chromium.launch({channel: process.env.SWAGGER_BROWSER_CHANNEL || "chrome", headless: true});
    try {
        const page = await browser.newPage();
        page.setDefaultTimeout(15000);
        await page.goto(new URL("/swagger/index.html", baseUrl).href);
        const errors = [];
        page.on("pageerror", error => errors.push(error.message));

        async function tryIt(method, template) {
            const selector = '[data-path="' + template + '"]';
            const op = page.locator(".opblock-" + method.toLowerCase()).filter({has: page.locator(selector)});
            await op.locator(".opblock-summary-control").click();
            await op.getByRole("button", {name:"Try it out"}).click();
            return op;
        }
        async function execute(op, method, apiPath, expected) {
            const [response] = await Promise.all([
                page.waitForResponse(r => r.request().method() === method && new URL(r.url()).pathname === apiPath),
                op.getByRole("button", {name:"Execute", exact:true}).click()
            ]);
            assert.equal(response.status(), expected, method + " " + apiPath);
            const statusCell = op.locator(".live-responses-table tbody .response-col_status").filter({hasText:String(expected)});
            await statusCell.waitFor();
            assert.doesNotMatch(await statusCell.innerText(), /undocumented/i);
            return response;
        }

        const jobs = await tryIt("GET", "/api/jobs");
        const listings = await execute(jobs, "GET", "/api/jobs", 200);
        assert.ok((await listings.json()).totalCount > 0);

        const me = await tryIt("GET", "/api/auth/me");
        await execute(me, "GET", "/api/auth/me", 401);
        const login = await tryIt("POST", "/api/auth/login");
        await login.locator("textarea").fill(JSON.stringify({email,password}));
        const session = await (await execute(login,"POST","/api/auth/login",200)).json();
        await page.locator(".scheme-container button.authorize").click();
        const dialog = page.locator(".dialog-ux");
        await dialog.getByLabel("auth-bearer-value").fill(session.accessToken);
        await dialog.getByRole("button",{name:"Apply credentials"}).click();
        await dialog.getByRole("button",{name:"Close",exact:true}).click();
        assert.equal((await (await execute(me,"GET","/api/auth/me",200)).json()).email,email);

        const file = path.join(__dirname,"cv-sample.pdf");
        const upload = await tryIt("POST","/api/jobs/{jobId}/applications");
        await upload.getByPlaceholder("jobId",{exact:true}).fill(String(jobId));
        await upload.locator('input[type="file"]').setInputFiles(file);
        await upload.getByPlaceholder("Message",{exact:true}).fill("Nộp CV qua Swagger UI");
        const route = "/api/jobs/" + jobId + "/applications";
        const {id} = await (await execute(upload,"POST",route,201)).json();
        await execute(upload,"POST",route,409);

        const download = await tryIt("GET","/api/applications/{id}/cv");
        await download.getByPlaceholder("id",{exact:true}).fill(String(id));
        const cv = await execute(download,"GET","/api/applications/"+id+"/cv",200);
        assert.deepEqual(await cv.body(), fs.readFileSync(file));
        assert.deepEqual(errors, []);
    } finally {
        await browser.close();
    }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });

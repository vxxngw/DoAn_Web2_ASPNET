// Chạy từ SwaggerTests để dùng đúng Kestrel và database riêng của lần test.
const { chromium } = require("playwright-core");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");

async function main() {
    const { baseUrl, email, employerEmail, password, jobId } = JSON.parse(fs.readFileSync(0, "utf8"));
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
        async function authorize(accountEmail) {
            const login = await tryIt("POST", "/api/auth/login");
            await login.locator("textarea").fill(JSON.stringify({email:accountEmail,password}));
            const session = await (await execute(login,"POST","/api/auth/login",200)).json();
            await page.locator(".scheme-container button.authorize").click();
            const dialog = page.locator(".dialog-ux");
            await dialog.getByLabel("auth-bearer-value").fill(session.accessToken);
            await dialog.getByRole("button",{name:"Apply credentials"}).click();
            await dialog.getByRole("button",{name:"Close",exact:true}).click();
            return session.user;
        }
        await authorize(email);
        assert.equal((await (await execute(me,"GET","/api/auth/me",200)).json()).email,email);
        const publish = await tryIt("PUT","/api/auth/me/candidate-profile");
        await publish.locator("textarea").fill(JSON.stringify({isProfilePublic:true,professionalTitle:"Backend"}));
        await execute(publish,"PUT","/api/auth/me/candidate-profile",200);

        const industries = await tryIt("GET","/api/jobs/industries");
        assert.ok(Array.isArray(await (await execute(industries,"GET","/api/jobs/industries",200)).json()));
        const bookmarkPath = "/api/saved-jobs/" + jobId;
        const save = await tryIt("PUT","/api/saved-jobs/{jobId}");
        await save.getByPlaceholder("jobId",{exact:true}).fill(String(jobId));
        await execute(save,"PUT",bookmarkPath,204);
        const saved = await tryIt("GET","/api/saved-jobs");
        assert.deepEqual((await (await execute(saved,"GET","/api/saved-jobs",200)).json()).items.map(j=>j.id),[jobId]);
        const remove = await tryIt("DELETE","/api/saved-jobs/{jobId}");
        await remove.getByPlaceholder("jobId",{exact:true}).fill(String(jobId));
        await execute(remove,"DELETE",bookmarkPath,204);
        assert.equal((await (await execute(saved,"GET","/api/saved-jobs",200)).json()).totalCount,0);

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

        await page.reload();
        const employer = await authorize(employerEmail);
        const report = await tryIt("GET","/api/employer/report");
        const counts = await (await execute(report,"GET","/api/employer/report",200)).json();
        assert.equal(counts.totalApplications,1);
        const received = await tryIt("GET","/api/employer/applications");
        assert.equal((await (await execute(received,"GET","/api/employer/applications",200)).json()).items[0].id,id);
        const directory = await tryIt("GET","/api/employer/candidates");
        assert.ok((await (await execute(directory,"GET","/api/employer/candidates",200)).json()).items.some(p=>p.email===email));
        const jd = await tryIt("POST","/api/jobs/with-jd");
        await jd.locator('input[type="file"]').setInputFiles(file);
        await jd.getByPlaceholder("Title",{exact:true}).fill("Tin có JD qua Swagger");
        await jd.getByPlaceholder("Deadline",{exact:true}).fill(new Date(Date.now()+86400000).toISOString());
        await jd.locator("tr").filter({has:page.locator(".parameter__name",{hasText:"IsSalaryNegotiable"})}).locator("select").selectOption("false");
        const pdfJob = await (await execute(jd,"POST","/api/jobs/with-jd",201)).json();
        const readJd = await tryIt("GET","/api/jobs/{id}/jd");
        await readJd.getByPlaceholder("id",{exact:true}).fill(String(pdfJob.id));
        const pdf = await execute(readJd,"GET","/api/jobs/"+pdfJob.id+"/jd",200);
        assert.deepEqual(await pdf.body(),fs.readFileSync(file));
        const create = await tryIt("POST","/api/jobs");
        const draft = {title:"Tin thử lương",description:"Kiểm tra mức lương",location:"Hà Nội",jdUrl:"https://example.test/jd",
            deadline:new Date(Date.now()+86400000).toISOString(),isOpen:true,isSalaryNegotiable:false,salaryMin:12000000,salaryMax:18000000};
        await create.locator("textarea").fill(JSON.stringify({...draft,salaryMax:1}));
        await execute(create,"POST","/api/jobs",400);
        await create.locator("textarea").fill(JSON.stringify(draft));
        const created = await (await execute(create,"POST","/api/jobs",201)).json();
        const read = await tryIt("GET","/api/jobs/{id}");
        await read.getByPlaceholder("id",{exact:true}).fill(String(created.id));
        const detailPath = "/api/jobs/"+created.id;
        const priced = await (await execute(read,"GET",detailPath,200)).json();
        assert.equal(priced.jdUrl, draft.jdUrl);
        assert.deepEqual([priced.isSalaryNegotiable,priced.salaryMin,priced.salaryMax],[false,12000000,18000000]);
        const edit = await tryIt("PUT","/api/jobs/{id}");
        await edit.getByPlaceholder("id",{exact:true}).fill(String(created.id));
        await edit.locator("textarea").fill(JSON.stringify({...draft,isSalaryNegotiable:true,salaryMin:null,salaryMax:null,rowVersion:priced.rowVersion}));
        await execute(edit,"PUT",detailPath,204);
        const agreed = await (await execute(read,"GET",detailPath,200)).json();
        assert.deepEqual([agreed.isSalaryNegotiable,agreed.salaryMin,agreed.salaryMax],[true,null,null]);

        const png = Buffer.from("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aScsAAAAASUVORK5CYII=","base64");
        const logo = await tryIt("PUT","/api/companies/me/logo");
        await logo.locator('input[type="file"]').setInputFiles({name:"logo.png",mimeType:"image/png",buffer:png});
        await execute(logo,"PUT","/api/companies/me/logo",204);
        const logoRead = await tryIt("GET","/api/companies/{id}/logo");
        await logoRead.getByPlaceholder("id",{exact:true}).fill(String(employer.id));
        const logoFile = await execute(logoRead,"GET","/api/companies/"+employer.id+"/logo",200);
        assert.equal(logoFile.headers()["content-type"],"image/png");
        assert.deepEqual(await logoFile.body(),png);
        assert.deepEqual(errors, []);
    } finally {
        await browser.close();
    }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });

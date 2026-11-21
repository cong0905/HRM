import { test, expect, type Page, type Browser, type BrowserContext } from "@playwright/test";
import fs from "node:fs";
import crypto from "node:crypto";
import type {PayrollBatchResponse, PayrollSnapshotResponse, PayslipDetailResponse} from "../src/features/payroll/api";
const file=process.env.HRM_T19_FIXTURE_FILE;
if(!file) throw new Error("T19 NOT_RUN: an isolated real SQL fixture is required.");
const fixture=JSON.parse(fs.readFileSync(file,"utf8")) as {actors:Record<string,string>;password:string;employeeIds:Record<string,string>;names:Record<string,string>};
const root="../docs/verification/t19";
const sessions:Record<string,{context:BrowserContext;page:Page}>={};
let periodId="";let firstBatch:PayrollBatchResponse;let publishedDetail:PayslipDetailResponse;
test.describe.configure({mode:"serial"});
async function actor(browser:Browser,role:string){
 if(sessions[role])return sessions[role]!;
 const context=await browser.newContext({baseURL:process.env.E2E_BASE_URL,ignoreHTTPSErrors:true});
 const page=await context.newPage();page.setDefaultTimeout(30000);page.setDefaultNavigationTimeout(60000);await page.goto("/login");
 await page.getByLabel("Tên đăng nhập",{exact:true}).fill(fixture.actors[role]!);
 await page.getByLabel("Mật khẩu",{exact:true}).fill(fixture.password);
 const loginResponse=page.waitForResponse(r=>r.request().method()==="POST" && r.url().includes("/auth/web/login"),{timeout:60000});
 await page.getByRole("button",{name:/Đăng nhập$/}).click();expect((await loginResponse).status()).toBe(204);await expect(page).toHaveURL("/");
 return sessions[role]={context,page};
}
async function get(page:Page,path:string){const r=await page.request.get(path);expect(r.status(),await r.text()).toBe(200);return r.json();}
async function post(page:Page,path:string,data:unknown){
 const csrf=await get(page,"/api/v1/auth/csrf");
 return page.request.post(path,{data,headers:{"X-XSRF-TOKEN":csrf.requestToken,"Idempotency-Key":crypto.randomUUID()}});
}
async function command(page:Page,button:string,expected=204){
 await page.getByRole("button",{name:new RegExp(button+"$")}).click();
 const dialog=page.getByRole("dialog");await dialog.getByLabel("Lý do thực hiện thao tác").fill("T19 independent SQL browser verification");
 const response=page.waitForResponse(r=>r.request().method()==="POST" && r.url().includes("/workflow/"));
 await dialog.getByRole("button",{name:/Xác nhận thực hiện$/}).click();
 const r=await response;expect(r.status(),await r.text()).toBe(expected);
 if(expected===204)await expect(dialog).not.toBeVisible();
 return r;
}
async function createPeriod(page:Page,month:number){
 await page.getByRole("tab",{name:"Quản lý kỳ lương",exact:true}).click();
 await page.getByRole("button",{name:/Tạo kỳ lương mới$/}).click();
 const d=page.getByRole("dialog");await d.getByLabel("Năm",{exact:true}).fill("2026");await d.getByLabel("Tháng",{exact:true}).fill(String(month));
 await d.getByLabel("Lý do khởi tạo kỳ lương").fill("T19 create hand-calculated fixture month");
 const response=page.waitForResponse(r=>r.request().method()==="POST" && new URL(r.url()).pathname==="/api/v1/payroll-periods");
 await d.getByRole("button",{name:/Tạo kỳ lương$/}).click();const r=await response;expect(r.status(),await r.text()).toBe(201);const state=await r.json();await expect(d).not.toBeVisible();return state.id as string;
}
async function pickEmployee(page:Page,name:string){await page.getByRole("combobox").last().press("ArrowDown");await page.locator(".ant-select-item-option-content:visible").filter({hasText:name}).click();}
async function salary(page:Page,employee:string,amount:string,from:string){
 await page.getByRole("tab",{name:"Điều chỉnh & Lương cơ bản",exact:true}).click();
 await page.getByRole("tab",{name:"Lịch sử mức lương (Salary History)",exact:true}).click();await pickEmployee(page,fixture.names[employee]!);
 await page.getByRole("button",{name:/Thiết lập mức lương mới$/}).click();const d=page.getByRole("dialog");
 await d.getByLabel("Ngày bắt đầu hiệu lực").fill(from);await d.getByLabel("Mức lương cơ bản (VND)").fill(amount);await d.getByLabel("Lý do điều chỉnh lương").fill("T19 explicit salary fixture decision");
 const response=page.waitForResponse(r=>r.request().method()==="POST" && r.url().includes("/salary-history"));await d.getByRole("button",{name:"Lưu mức lương mới"}).click();const r=await response;expect(r.status(),await r.text()).toBe(201);await expect(d).not.toBeVisible();
}
async function adjustment(page:Page,employee:string,amount:string,kind="Bonus"){
 await page.getByRole("tab",{name:"Điều chỉnh & Lương cơ bản",exact:true}).click();await page.getByRole("tab",{name:"Khoản cộng / trừ thủ công (Manual Adjustments)",exact:true}).click();
 await page.getByRole("button",{name:/Thêm khoản điều chỉnh$/}).click();const d=page.getByRole("dialog");
 await d.getByLabel("Nhân viên",{exact:true}).click();await page.locator(".ant-select-item-option-content:visible").filter({hasText:fixture.names[employee]!}).click();
 await d.getByLabel("Loại điều chỉnh",{exact:true}).click();await page.locator(".ant-select-item-option-content:visible").filter({hasText:kind==="Bonus"?"Thưởng demo (Bonus)":"Khấu trừ khác demo (OtherDeduction)"}).click();
 await d.getByLabel("Số tiền (VND)").fill(amount);await d.getByLabel("Lý do điều chỉnh",{exact:true}).fill("T19 explicit manual adjustment fixture");
 const response=page.waitForResponse(r=>r.request().method()==="POST" && r.url().endsWith("/adjustments"));await d.getByRole("button",{name:"Lưu khoản điều chỉnh"}).click();const r=await response;expect(r.status(),await r.text()).toBe(201);await expect(d).not.toBeVisible();return r.json();
}
test.afterAll(async()=>{for(const s of Object.values(sessions))await s.context.close();});

test("role accounts: HR/Admin never query payroll dashboard; employees use own endpoints",async({browser})=>{
 for(const role of ["HR","SystemAdmin"]){const {page}=await actor(browser,role);const reads:string[]=[];page.on("request",r=>{if(/\/api\/v1\/.*(?:payroll|payslip)/.test(r.url()))reads.push(r.url());});await page.reload();await expect(page.getByRole("heading",{name:"Không gian làm việc"})).toBeVisible();expect(reads).toEqual([]);expect((await page.request.get("/api/v1/payroll-periods")).status()).toBe(403);}
 const {page}=await actor(browser,"Employee");await page.goto("/payroll");await expect(page.getByText("Phiếu lương của tôi",{exact:true})).toBeVisible();await expect(page.getByRole("tab",{name:"Quản lý kỳ lương"})).toHaveCount(0);await expect(page.getByRole("button",{name:"Xuất CSV"})).toHaveCount(0);expect((await page.request.get("/api/v1/payroll-periods")).status()).toBe(403);
});

test("create real period, fail closed on missing salary, then seed salary and adjustments through UI",async({browser})=>{
 const {page}=await actor(browser,"PayrollCalculator");await page.goto("/payroll");periodId=await createPeriod(page,4);await command(page,"Tính toán nháp",422);await expect(page.getByRole("dialog")).toContainText(/lương|salary/i);await page.getByRole("button",{name:"Close",exact:true}).click();
 expect((await get(page,`/api/v1/payroll-periods/${periodId}`)).runVersion).toBe(0);
 await salary(page,"Employee","12000000","2026-01-01");await salary(page,"EmployeeOther","9000000","2026-01-01");
 await adjustment(page,"Employee","0.50");await adjustment(page,"EmployeeOther","1000.50");
});

test("hand-calculated totals equal API snapshot; GET/reload does not recalculate; calculator cannot self-approve",async({browser})=>{
 const {page}=await actor(browser,"PayrollCalculator");await page.getByRole("tab",{name:"Quản lý kỳ lương",exact:true}).click();await command(page,"Tính toán nháp");
 firstBatch=await get(page,`/api/v1/payroll-periods/${periodId}/snapshots`);expect(firstBatch.version).toBe(1);expect(firstBatch.snapshots.find(s=>s.employeeId===fixture.employeeIds.Employee)!.input.days.some(d=>d.paidLeaveApproved)).toBe(true);const nets=firstBatch.snapshots.map((s:PayrollSnapshotResponse)=>s.output.net).sort();expect(nets).toEqual(["1001","12000001"]);
 const table=page.getByRole("region",{name:"Danh sách tính toán nhân viên"});await expect(table.getByRole("row").filter({hasText:fixture.names.Employee!})).toContainText("12000001");await expect(page.getByRole("button",{name:/Phê duyệt$/})).toBeDisabled();
 const state=await get(page,`/api/v1/payroll-periods/${periodId}`);await page.reload();expect(await get(page,`/api/v1/payroll-periods/${periodId}`)).toEqual(state);expect(await get(page,`/api/v1/payroll-periods/${periodId}/snapshots`)).toEqual(firstBatch);
 const forbidden=await post(page,`/api/v1/payroll-periods/${periodId}/workflow/approve`,{rowVersion:state.rowVersion,runVersion:state.runVersion,reason:"T19 calculator must not self approve"});expect(forbidden.status()).toBe(403);
});

test("changed inputs block approval; recalculation exposes previous run; stale client cannot approve new version",async({browser})=>{
 const calc=(await actor(browser,"PayrollCalculator")).page;const reviewer=(await actor(browser,"PayrollApprover")).page;await reviewer.goto("/payroll");const old=await get(reviewer,`/api/v1/payroll-periods/${periodId}`);
 await adjustment(calc,"Employee","100");await command(reviewer,"Phê duyệt",409);await expect(reviewer.getByRole("dialog")).toContainText("Cần tính toán nháp lại");await reviewer.getByRole("button",{name:"Close",exact:true}).click();
 await reviewer.getByRole("button",{name:/Phê duyệt$/}).click();await reviewer.getByRole("dialog").getByLabel("Lý do thực hiện thao tác").fill("T19 stale confirmation must require comparison");
 await calc.getByRole("tab",{name:"Quản lý kỳ lương",exact:true}).click();await command(calc,"Tính toán nháp");const stale=await post(reviewer,`/api/v1/payroll-periods/${periodId}/workflow/approve`,{rowVersion:old.rowVersion,runVersion:old.runVersion,reason:"T19 old browser run must fail closed"});expect(stale.status()).toBe(409);expect((await stale.json()).errorCode).toBe("payroll.version_conflict");
 await calc.getByRole("combobox",{name:"Đối chiếu bản chạy",exact:true}).press("ArrowDown");await calc.getByText("Bản chạy 1",{exact:true}).click();await expect(calc.getByRole("region",{name:"Danh sách tính toán nhân viên"}).getByRole("row").filter({hasText:fixture.names.Employee!})).toContainText("12000101");await expect(calc.getByRole("region",{name:"Danh sách tính toán nhân viên"})).toContainText("(12000001)");
 const staleResponse=reviewer.waitForResponse(r=>r.request().method()==="POST" && r.url().includes("/workflow/approve"));
 await reviewer.getByRole("dialog").getByRole("button",{name:"Xác nhận thực hiện",exact:true}).click();expect((await staleResponse).status()).toBe(409);
 await expect(reviewer.getByText("Kỳ lương đã thay đổi; cần đối chiếu bản mới",{exact:true})).toBeVisible();
 await reviewer.getByRole("button",{name:"Tải bản mới để đối chiếu",exact:true}).click();
 const approved=reviewer.waitForResponse(r=>r.request().method()==="POST" && r.url().includes("/workflow/approve"));await reviewer.getByRole("dialog").getByRole("button",{name:"Xác nhận thực hiện",exact:true}).click();expect((await approved).status()).toBe(204);await expect(reviewer.getByRole("dialog")).not.toBeVisible();await command(reviewer,"Công bố");
 const summary=await get(reviewer,`/api/v1/payroll-periods/${periodId}/reconciliation`);expect(summary.payslipNet).toMatch(/^12001102(?:\.0+)?$/);expect(summary.difference).toMatch(/^0(?:\.0+)?$/);await expect(reviewer.getByText("12001102 VND",{exact:true})).toHaveCount(2);
});

test("published own payslip stays immutable after future salary; IDOR and export denied; CSV matches snapshot; lock",async({browser})=>{
 const employee=(await actor(browser,"Employee")).page;const other=(await actor(browser,"EmployeeOther")).page;const reviewer=(await actor(browser,"PayrollApprover")).page;const calc=(await actor(browser,"PayrollCalculator")).page;
 await employee.goto("/payroll");await employee.getByRole("button",{name:/Xem chi tiết$/}).click();await expect(employee.getByRole("dialog")).toContainText("12000101");const own=await get(employee,"/api/v1/me/payslips");publishedDetail=await get(employee,`/api/v1/me/payslips/${own.items[0].id}`);expect(publishedDetail.calculation.net).toBe("12000101");expect((await other.request.get(`/api/v1/me/payslips/${own.items[0].id}`)).status()).toBe(404);
 for(const page of [employee,other,(await actor(browser,"HR")).page])expect((await page.request.get(`/api/v1/payroll-periods/${periodId}/export`)).status()).toBe(403);
 await salary(calc,"Employee","16000000","2026-05-01");await employee.reload();await employee.getByRole("button",{name:/Xem chi tiết$/}).click();expect(await get(employee,`/api/v1/me/payslips/${own.items[0].id}`)).toEqual(publishedDetail);await expect(employee.getByRole("dialog")).toContainText("12000101");
 const denied=await post(calc,`/api/v1/employees/${fixture.employeeIds.Employee}/salary-history`,{effectiveFrom:"2026-04-15",monthlyBase:"18000000",reason:"T19 cannot rewrite closed period salary"});expect(denied.status()).toBe(409);
 const downloadEvent=reviewer.waitForEvent("download");await reviewer.getByRole("button",{name:/Xuất CSV$/}).click();const download=await downloadEvent;const path=await download.path();const csv=fs.readFileSync(path!,"utf8");expect(csv).toContain("12000101");expect(csv).toContain("1001");await download.saveAs(`${root}/payroll-export.csv`);
 await command(reviewer,"Khóa kỳ lương");await expect(reviewer.getByRole("button",{name:/Tính toán nháp$/})).toHaveCount(0);expect(await get(employee,`/api/v1/me/payslips/${own.items[0].id}`)).toEqual({...publishedDetail,periodStatus:"Locked"});
 fs.writeFileSync(`${root}/browser-financial-evidence.json`,JSON.stringify({firstBatch,publishedDetail,finalState:await get(reviewer,`/api/v1/payroll-periods/${periodId}`)},null,2));
});

test("incomplete attendance and negative net cannot be approved/published; responsive mobile and dashboard counts",async({browser})=>{
 const calc=(await actor(browser,"PayrollCalculator")).page;const reviewer=(await actor(browser,"PayrollApprover")).page;
 await calc.getByRole("tab",{name:"Quản lý kỳ lương",exact:true}).click();const june=await createPeriod(calc,6);await command(calc,"Tính toán nháp");await expect(calc.getByText("Bảng lương có điều kiện chặn (Không thể công bố)")).toBeVisible();
 await reviewer.reload();const juneState=await get(reviewer,`/api/v1/payroll-periods/${june}`);const reject=await post(reviewer,`/api/v1/payroll-periods/${june}/workflow/approve`,{rowVersion:juneState.rowVersion,runVersion:juneState.runVersion,reason:"T19 incomplete attendance blocks approval"});expect(reject.status()).toBe(409);await expect(reviewer.getByRole("button",{name:/Phê duyệt$/})).toBeDisabled();
 await adjustment(calc,"EmployeeOther","1000000000","OtherDeduction");await calc.getByRole("tab",{name:"Quản lý kỳ lương",exact:true}).click();await command(calc,"Tính toán nháp");const blocked=await get(calc,`/api/v1/payroll-periods/${june}/snapshots`);expect(blocked.snapshots.some((s:PayrollSnapshotResponse)=>s.output.net.startsWith("-"))).toBe(true);expect(blocked.snapshots.some((s:PayrollSnapshotResponse)=>s.output.blockingReasons.length>0)).toBe(true);
 await calc.setViewportSize({width:375,height:812});await expect(calc.getByRole("heading",{name:"Quản lý Bảng lương"})).toBeVisible();expect(await calc.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth+1)).toBe(true);await calc.screenshot({path:`${root}/payroll-mobile.png`,fullPage:true,animations:"disabled"});await calc.setViewportSize({width:1440,height:1000});await expect(calc.locator(".desktop-sider")).toHaveCSS("width","216px");await calc.screenshot({path:`${root}/payroll-desktop.png`,fullPage:true,animations:"disabled"});
 const emp=(await actor(browser,"EmployeeOther")).page;await emp.goto("/");await expect(emp.getByText("Phiếu lương đã công bố",{exact:true})).toBeVisible();const ownCount=(await get(emp,"/api/v1/me/payslips")).totalCount;await expect(emp.locator(".metric-card").filter({hasText:"Phiếu lương đã công bố"}).locator(".ant-statistic-content-value")).toHaveText(String(ownCount));await emp.goto("/attendance");await expect(emp.getByRole("heading",{name:"Chấm công",exact:true})).toBeVisible();await emp.goto("/leave");await expect(emp.getByRole("heading",{name:"Nghỉ phép",exact:true})).toBeVisible();
});

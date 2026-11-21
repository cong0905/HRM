import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { DashboardMetrics } from "../src/features/dashboard/DashboardMetrics";
import { ApiClientError } from "../src/api/types";
const mocks=vi.hoisted(()=>({permissions:[] as string[],periods:vi.fn(),slips:vi.fn(),today:vi.fn(),leaves:vi.fn()}));
vi.mock("../src/context/AuthContext",()=>({useAuth:()=>({hasPermission:(p:string)=>mocks.permissions.includes(p),hasAnyPermission:(ps:string[])=>ps.some(p=>mocks.permissions.includes(p))})}));
vi.mock("../src/features/payroll/api",()=>({payrollApi:{listPeriods:mocks.periods,myPayslips:mocks.slips}}));
vi.mock("../src/features/attendance/api",()=>({attendanceApi:{today:mocks.today}}));
vi.mock("../src/features/leave/api",()=>({leaveApi:{team:mocks.leaves}}));
let client:QueryClient;
beforeEach(()=>{vi.clearAllMocks();mocks.permissions=[];client=new QueryClient({defaultOptions:{queries:{retry:false,gcTime:0}}});});
afterEach(()=>{cleanup();client.clear();});
function show(){return render(<MemoryRouter><QueryClientProvider client={client}><DashboardMetrics /></QueryClientProvider></MemoryRouter>);}
describe("T19 dashboard data boundaries",()=>{
 it("does not request payroll data for an HR-only account",async()=>{mocks.permissions=["leaves.read_all"];mocks.leaves.mockResolvedValue({totalCount:2});show();await screen.findByText("Đơn nghỉ phép chờ duyệt");expect(mocks.periods).not.toHaveBeenCalled();expect(mocks.slips).not.toHaveBeenCalled();expect(mocks.today).not.toHaveBeenCalled();});
 it("shows an attendance service failure instead of claiming the employee has not checked in",async()=>{mocks.permissions=["attendance.read_own"];mocks.today.mockRejectedValue(new ApiClientError({status:503,message:"T19 attendance unavailable"}));show();await screen.findByText("Không tải được dữ liệu");expect(screen.queryByText("Chưa vào ca")).not.toBeInTheDocument();expect(mocks.periods).not.toHaveBeenCalled();expect(mocks.slips).not.toHaveBeenCalled();});
});

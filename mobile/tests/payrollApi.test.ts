import { getMyPayslips, getMyPayslipDetail } from "../src/api/payroll";
import { SessionManager } from "../src/api/session";
import { SecureStoreAdapter } from "../src/storage/secureStore";
import { DataCacheManager } from "../src/storage/cache";

jest.mock("expo-secure-store", () => ({
  getItemAsync: jest.fn(),
  setItemAsync: jest.fn(),
  deleteItemAsync: jest.fn(),
  AFTER_FIRST_UNLOCK: "AFTER_FIRST_UNLOCK",
}));

describe("Payroll API - Phiếu lương Published và giải thích thành phần tiền", () => {
  const originalFetch = globalThis.fetch;

  beforeEach(async () => {
    jest.restoreAllMocks();
    SecureStoreAdapter.setMockMode(true);
    SecureStoreAdapter.resetMockStore();
    await SessionManager.resetForTesting();
    DataCacheManager.clearAll();
    process.env.EXPO_PUBLIC_API_URL = "https://api.example.invalid";

    await SessionManager.setSession({
      tokenType: "Bearer",
      accessToken: "employee_token_payroll",
      refreshToken: "refresh_token_payroll",
      expiresIn: 3600,
    });
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  test("Lấy danh sách phiếu lương của mình chỉ trả về phiếu Published", async () => {
    globalThis.fetch = jest.fn(async (input: RequestInfo | URL) => {
      expect(input.toString()).toContain("/api/v1/me/payslips?page=1&pageSize=20");
      return new Response(
        JSON.stringify({
          items: [
            {
              id: "slip-1",
              periodId: "period-1",
              employeeId: "emp-1",
              amount: "15000000",
              status: "Published",
              publishedAtUtc: "2026-11-05T08:00:00Z",
            },
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1,
        }),
        {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }
      );
    });

    const res = await getMyPayslips(1, 20, true);
    expect(res.items.length).toBe(1);
    expect(res.items[0]?.status).toBe("Published");
    expect(res.items[0]?.amount).toBe("15000000");
  });

  test("Lấy chi tiết phiếu lương giải thích đầy đủ các thành phần lương và làm tròn", async () => {
    globalThis.fetch = jest.fn(async (input: RequestInfo | URL) => {
      expect(input.toString()).toContain("/api/v1/me/payslips/slip-1");
      return new Response(
        JSON.stringify({
          id: "slip-1",
          employeeId: "emp-1",
          periodId: "period-1",
          batchId: "batch-1",
          snapshotId: "snap-1",
          periodCode: "2026-10",
          periodStatus: "Published",
          currency: "VND",
          status: "Published",
          publishedAtUtc: "2026-11-05T08:00:00Z",
          notice: "Đã công bố",
          runVersion: 1,
          calculation: {
            standardPay: "14000000",
            overtimePay: "1500000",
            gross: "15500000",
            net: "15000000",
            standardMonthMinutes: "10560",
            creditedMinutes: "10560",
            approvedOvertimeMinutes: "360",
            unroundedStandardPay: "14000000",
            unroundedOvertimePay: "1500000",
            unroundedGross: "15500000",
            unroundedNet: "15000000",
            roundingAdjustment: "0",
            policyVersion: "2026.1",
            canPublish: true,
            blockingReasons: [],
            lines: [
              { code: "ALLOWANCE_MEAL", amount: "500000" },
              { code: "DEDUCTION_INSURANCE", amount: "-1000000" },
            ],
          },
          policy: {
            version: "2026.1",
            automaticPerformanceLinkEnabled: false,
            weekdayOvertimeMultiplier: "1.5",
            weekendOvertimeMultiplier: "2.0",
            holidayOvertimeMultiplier: "3.0",
          },
          calendar: [],
          days: [],
        }),
        {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }
      );
    });

    const detail = await getMyPayslipDetail("slip-1", true);

    expect(detail.id).toBe("slip-1");
    expect(detail.calculation.standardPay).toBe("14000000");
    expect(detail.calculation.overtimePay).toBe("1500000");
    expect(detail.calculation.net).toBe("15000000");
    expect(detail.calculation.lines.length).toBe(2);
    expect(detail.calculation.lines[0]?.code).toBe("ALLOWANCE_MEAL");
  });
});

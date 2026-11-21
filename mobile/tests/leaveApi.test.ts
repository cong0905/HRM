import {
  getMyLeaveBalances,
  getMyLeaveRequests,
  createLeaveRequest,
  cancelLeaveRequest,
} from "../src/api/leave";
import { SessionManager } from "../src/api/session";
import { SecureStoreAdapter } from "../src/storage/secureStore";
import { DataCacheManager } from "../src/storage/cache";
import { ApiClientError } from "../src/api/errors";

jest.mock("expo-secure-store", () => ({
  getItemAsync: jest.fn(),
  setItemAsync: jest.fn(),
  deleteItemAsync: jest.fn(),
  AFTER_FIRST_UNLOCK: "AFTER_FIRST_UNLOCK",
}));

describe("Leave API - Quỹ phép, tạo đơn và hủy đơn Pending", () => {
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
      accessToken: "employee_token_leave",
      refreshToken: "refresh_token_leave",
      expiresIn: 3600,
    });
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  test("Lấy quỹ phép năm trả về allocated, used, remainingDays", async () => {
    globalThis.fetch = jest.fn(async () => {
      return new Response(
        JSON.stringify([
          {
            id: "bal-1",
            employeeId: "emp-1",
            year: 2026,
            allocatedDays: "12",
            usedDays: "2",
            remainingDays: "10",
            updatedAtUtc: "2026-11-01T00:00:00Z",
            rowVersion: "AQID",
          },
        ]),
        {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }
      );
    });

    const balances = await getMyLeaveBalances(2026, true);
    expect(balances.length).toBe(1);
    expect(balances[0]?.remainingDays).toBe("10");
    expect(balances[0]?.usedDays).toBe("2");
  });

  test("Tạo đơn nghỉ phép gửi kèm Idempotency-Key và nhận lại bản ghi LeaveRecord", async () => {
    let capturedKey: string | null = null;
    let capturedBody: any = null;

    globalThis.fetch = jest.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const headers = new Headers(init?.headers);
      capturedKey = headers.get("Idempotency-Key");
      if (init?.body) capturedBody = JSON.parse(init.body as string);

      return new Response(
        JSON.stringify({
          id: "req-1",
          employeeId: "emp-1",
          startDate: "2026-11-25",
          endDate: "2026-11-26",
          reason: "Nghỉ việc gia đình",
          status: "Pending",
          workingDays: "2",
          createdAtUtc: "2026-11-20T08:00:00Z",
        }),
        {
          status: 201,
          headers: { "Content-Type": "application/json" },
        }
      );
    });

    const res = await createLeaveRequest(
      {
        startDate: "2026-11-25",
        endDate: "2026-11-26",
        reason: "Nghỉ việc gia đình",
      },
      "custom-leave-key-123"
    );

    expect(capturedKey).toBe("custom-leave-key-123");
    expect(capturedBody.startDate).toBe("2026-11-25");
    expect(res.status).toBe("Pending");
    expect(res.workingDays).toBe("2");
  });

  test("Lỗi nghiệp vụ khi tạo đơn (422 vượt quỹ phép hoặc 409 giao ngày) được chuyển hóa rõ ràng", async () => {
    globalThis.fetch = jest.fn(async () => {
      return new Response(
        JSON.stringify({
          errorCode: "leave.insufficient_balance",
          detail: "Số ngày nghỉ vượt quá quỹ phép còn lại trong năm.",
          status: 422,
        }),
        {
          status: 422,
          headers: { "Content-Type": "application/problem+json" },
        }
      );
    });

    await expect(
      createLeaveRequest({
        startDate: "2026-12-01",
        endDate: "2026-12-25",
        reason: "Nghỉ dài ngày",
      })
    ).rejects.toMatchObject({
      status: 422,
      code: "leave.insufficient_balance",
      message: "Số ngày nghỉ vượt quá quỹ phép còn lại trong năm.",
    });
  });

  test("Hủy đơn nghỉ phép Pending thành công với mã 204 No Content", async () => {
    let capturedUrl = "";
    let capturedKey: string | null = null;

    globalThis.fetch = jest.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      capturedUrl = input.toString();
      const headers = new Headers(init?.headers);
      capturedKey = headers.get("Idempotency-Key");

      return new Response(null, { status: 204 });
    });

    await cancelLeaveRequest("req-guid-999", "Nhân viên hủy đơn", "AAAA", "cancel-key-001");

    expect(capturedUrl).toContain("/api/v1/leave-requests/req-guid-999/cancel");
    expect(capturedKey).toBe("cancel-key-001");
  });
});

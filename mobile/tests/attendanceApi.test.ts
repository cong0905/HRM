import { getOwnTodayAttendance, getOwnAttendanceHistory, checkIn, checkOut } from "../src/api/attendance";
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

describe("Attendance API - Chấm công trực tuyến và bảo vệ Idempotency", () => {
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
      accessToken: "employee_token_123",
      refreshToken: "refresh_token_123",
      expiresIn: 3600,
    });
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
    jest.restoreAllMocks();
  });

  test("Check-in gửi kèm Idempotency-Key và chỉ thành công khi server phản hồi 200", async () => {
    let capturedHeaderKey: string | null = null;
    let capturedBody: any = null;

    globalThis.fetch = jest.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const headers = new Headers(init?.headers);
      capturedHeaderKey = headers.get("Idempotency-Key");
      if (init?.body) {
        capturedBody = JSON.parse(init.body as string);
      }

      return new Response(
        JSON.stringify({
          id: "att-day-1",
          employeeId: "emp-1",
          businessDate: "2026-11-20",
          checkInUtc: "2026-11-20T01:30:00Z",
          checkOutUtc: null,
          status: "Present",
          eligibleMinutes: "0",
          lateMinutes: "0",
          earlyLeaveMinutes: "0",
          confirmedOvertimeMinutes: "0",
          updatedAtUtc: "2026-11-20T01:30:00Z",
          calendarVersionId: "cal-1",
          rowVersion: "AAAA",
        }),
        {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }
      );
    });

    const res = await checkIn("Vào ca sáng đúng giờ", "custom-uuid-key-001");

    expect(capturedHeaderKey).toBe("custom-uuid-key-001");
    expect(capturedBody?.note).toBe("Vào ca sáng đúng giờ");
    expect(res.checkInUtc).toBe("2026-11-20T01:30:00Z");
    expect(res.status).toBe("Present");
  });

  test("Check-out gửi kèm Idempotency-Key và trả về dữ liệu công cập nhật", async () => {
    let capturedHeaderKey: string | null = null;

    globalThis.fetch = jest.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const headers = new Headers(init?.headers);
      capturedHeaderKey = headers.get("Idempotency-Key");

      return new Response(
        JSON.stringify({
          id: "att-day-1",
          employeeId: "emp-1",
          businessDate: "2026-11-20",
          checkInUtc: "2026-11-20T01:30:00Z",
          checkOutUtc: "2026-11-20T10:30:00Z",
          status: "DuCong",
          eligibleMinutes: "480",
          lateMinutes: "0",
          earlyLeaveMinutes: "0",
          confirmedOvertimeMinutes: "0",
          updatedAtUtc: "2026-11-20T10:30:00Z",
          calendarVersionId: "cal-1",
          rowVersion: "BBBB",
        }),
        {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }
      );
    });

    const res = await checkOut(undefined, "checkout-uuid-002");

    expect(capturedHeaderKey).toBe("checkout-uuid-002");
    expect(res.checkOutUtc).toBe("2026-11-20T10:30:00Z");
    expect(res.eligibleMinutes).toBe("480");
  });

  test("Mất mạng khi chấm công -> Báo lỗi mạng rõ ràng, không hiển thị thành công giả", async () => {
    globalThis.fetch = jest.fn(async () => {
      throw new TypeError("Failed to fetch");
    });

    await expect(checkIn("Thử chấm công khi mất mạng")).rejects.toMatchObject({
      isNetworkError: true,
      code: "network.offline",
    });

    // Dữ liệu cache không được ghi nhận chấm công thành công
    const today = await getOwnTodayAttendance().catch(() => null);
    expect(today).toBeNull();
  });

  test("Chưa check-in hôm nay -> Server trả 404, getOwnTodayAttendance trả về null thay vì ném lỗi", async () => {
    globalThis.fetch = jest.fn(async () => {
      return new Response(
        JSON.stringify({
          errorCode: "attendance.not_found",
          detail: "Không tìm thấy dữ liệu công hôm nay.",
        }),
        {
          status: 404,
          headers: { "Content-Type": "application/problem+json" },
        }
      );
    });

    const today = await getOwnTodayAttendance(true);
    expect(today).toBeNull();
  });

  test("Lấy lịch sử công phân trang thành công", async () => {
    globalThis.fetch = jest.fn(async (input: RequestInfo | URL) => {
      expect(input.toString()).toContain("/api/v1/attendance/me/history?page=1&pageSize=10");
      return new Response(
        JSON.stringify({
          items: [
            {
              id: "hist-1",
              businessDate: "2026-11-19",
              checkInUtc: "2026-11-19T01:30:00Z",
              checkOutUtc: "2026-11-19T10:30:00Z",
              eligibleMinutes: "480",
              status: "DuCong",
            },
          ],
          page: 1,
          pageSize: 10,
          totalCount: 1,
        }),
        {
          status: 200,
          headers: { "Content-Type": "application/json" },
        }
      );
    });

    const res = await getOwnAttendanceHistory(1, 10, true);
    expect(res.items.length).toBe(1);
    expect(res.items[0]?.businessDate).toBe("2026-11-19");
  });
});

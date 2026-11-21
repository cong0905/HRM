import { mobileFetch } from "./client";
import { generateIdempotencyKey } from "../utils/uuid";
import { DataCacheManager } from "../storage/cache";
import type {
  LeaveBalanceResponse,
  LeaveRecordResponse,
  CreateLeaveRequest,
  CancelLeaveRequest,
  PagedResultOfLeaveRecordResponse,
} from "./types";

const CACHE_KEY_BALANCES = "leave_me_balances";
const CACHE_KEY_REQUESTS = "leave_me_requests";

/**
 * Lấy quỹ phép (Leave Balances) của chính mình.
 * Route: GET /api/v1/me/leave-balances?year=...
 */
export async function getMyLeaveBalances(
  year?: number,
  bypassCache = false
): Promise<LeaveBalanceResponse[]> {
  const currentYear = year || new Date().getFullYear();
  const cacheKey = `${CACHE_KEY_BALANCES}_${currentYear}`;

  if (!bypassCache) {
    const cached = DataCacheManager.get<LeaveBalanceResponse[]>(cacheKey);
    if (cached) return cached;
  }

  const query = year ? `?year=${year}` : "";
  const result = await mobileFetch<LeaveBalanceResponse[]>(`/api/v1/me/leave-balances${query}`, {
    method: "GET",
  });

  DataCacheManager.set(cacheKey, result, 2 * 60 * 1000);
  return result;
}

/**
 * Lấy danh sách đơn nghỉ phép của chính mình.
 * Route: GET /api/v1/me/leave-requests?page=...&pageSize=...
 */
export async function getMyLeaveRequests(
  page = 1,
  pageSize = 20,
  bypassCache = false
): Promise<PagedResultOfLeaveRecordResponse> {
  const cacheKey = `${CACHE_KEY_REQUESTS}_p${page}_s${pageSize}`;

  if (!bypassCache) {
    const cached = DataCacheManager.get<PagedResultOfLeaveRecordResponse>(cacheKey);
    if (cached) return cached;
  }

  const result = await mobileFetch<PagedResultOfLeaveRecordResponse>(
    `/api/v1/me/leave-requests?page=${page}&pageSize=${pageSize}`,
    {
      method: "GET",
    }
  );

  DataCacheManager.set(cacheKey, result, 2 * 60 * 1000);
  return result;
}

/**
 * Tạo đơn nghỉ phép mới.
 * Route: POST /api/v1/leave-requests
 * Gửi kèm Idempotency-Key và kiểm tra tính hợp lệ nghiệp vụ.
 */
export async function createLeaveRequest(
  request: CreateLeaveRequest,
  idempotencyKey?: string
): Promise<LeaveRecordResponse> {
  const key = idempotencyKey || generateIdempotencyKey();

  const result = await mobileFetch<LeaveRecordResponse>("/api/v1/leave-requests", {
    method: "POST",
    headers: {
      "Idempotency-Key": key,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  // Xóa cache cũ để người dùng thấy đơn mới ngay
  DataCacheManager.remove(`${CACHE_KEY_REQUESTS}_p1_s20`);
  DataCacheManager.clearAll();

  return result;
}

/**
 * Hủy đơn nghỉ phép đang ở trạng thái Pending.
 * Route: POST /api/v1/leave-requests/{id}/cancel
 * Chỉ chủ đơn mới được phép hủy đơn Pending.
 */
export async function cancelLeaveRequest(
  id: string,
  reason?: string,
  rowVersion?: string,
  idempotencyKey?: string
): Promise<void> {
  const key = idempotencyKey || generateIdempotencyKey();

  const body: CancelLeaveRequest = {
    reason: reason || "Nhân viên hủy đơn",
    rowVersion: rowVersion || undefined,
  };

  await mobileFetch<void>(`/api/v1/leave-requests/${id}/cancel`, {
    method: "POST",
    headers: {
      "Idempotency-Key": key,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  });

  // Xóa cache sau khi hủy đơn
  DataCacheManager.clearAll();
}

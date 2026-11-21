import { mobileFetch } from "./client";
import { generateIdempotencyKey } from "../utils/uuid";
import { DataCacheManager } from "../storage/cache";
import { ApiClientError } from "./errors";
import type {
  AttendanceDayResponse,
  AttendanceCommandRequest,
  PagedResultOfAttendanceDayResponse,
} from "./types";

const CACHE_KEY_TODAY = "attendance_me_today";
const CACHE_KEY_HISTORY = "attendance_me_history";

/**
 * Lấy thông tin công hôm nay của chính người dùng hiện hành.
 * Route: GET /api/v1/attendance/me/today.
 * Nếu chưa check-in hôm nay (máy chủ trả 404), trả về null.
 */
export async function getOwnTodayAttendance(bypassCache = false): Promise<AttendanceDayResponse | null> {
  if (!bypassCache) {
    const cached = DataCacheManager.get<AttendanceDayResponse>(CACHE_KEY_TODAY);
    if (cached) return cached;
  }

  try {
    const result = await mobileFetch<AttendanceDayResponse>("/api/v1/attendance/me/today", {
      method: "GET",
    });
    DataCacheManager.set(CACHE_KEY_TODAY, result, 60 * 1000);
    return result;
  } catch (err) {
    if (err instanceof ApiClientError && err.status === 404) {
      // Chưa có dữ liệu công hôm nay
      return null;
    }
    throw err;
  }
}

/**
 * Lấy lịch sử công của chính người dùng hiện hành.
 * Route: GET /api/v1/attendance/me/history?page=...&pageSize=...
 */
export async function getOwnAttendanceHistory(
  page = 1,
  pageSize = 20,
  bypassCache = false
): Promise<PagedResultOfAttendanceDayResponse> {
  const cacheKey = `${CACHE_KEY_HISTORY}_p${page}_s${pageSize}`;
  if (!bypassCache) {
    const cached = DataCacheManager.get<PagedResultOfAttendanceDayResponse>(cacheKey);
    if (cached) return cached;
  }

  const result = await mobileFetch<PagedResultOfAttendanceDayResponse>(
    `/api/v1/attendance/me/history?page=${page}&pageSize=${pageSize}`,
    {
      method: "GET",
    }
  );

  DataCacheManager.set(cacheKey, result, 2 * 60 * 1000);
  return result;
}

/**
 * Thực hiện chấm công vào (Check-in) trực tuyến.
 * Bắt buộc gửi kèm Idempotency-Key.
 * Chỉ xác nhận thành công sau khi nhận được response 200 từ máy chủ.
 */
export async function checkIn(
  note?: string,
  idempotencyKey?: string
): Promise<AttendanceDayResponse> {
  const key = idempotencyKey || generateIdempotencyKey();

  const body: AttendanceCommandRequest = {
    clientTimeUtc: new Date().toISOString(),
    note: note || undefined,
  };

  const result = await mobileFetch<AttendanceDayResponse>("/api/v1/attendance/me/check-in", {
    method: "POST",
    headers: {
      "Idempotency-Key": key,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  });

  // Cập nhật bộ nhớ đệm
  DataCacheManager.set(CACHE_KEY_TODAY, result, 60 * 1000);
  DataCacheManager.remove(`${CACHE_KEY_HISTORY}_p1_s20`);

  return result;
}

/**
 * Thực hiện chấm công ra (Check-out) trực tuyến.
 * Bắt buộc gửi kèm Idempotency-Key.
 * Chỉ xác nhận thành công sau khi nhận được response 200 từ máy chủ.
 */
export async function checkOut(
  note?: string,
  idempotencyKey?: string
): Promise<AttendanceDayResponse> {
  const key = idempotencyKey || generateIdempotencyKey();

  const body: AttendanceCommandRequest = {
    clientTimeUtc: new Date().toISOString(),
    note: note || undefined,
  };

  const result = await mobileFetch<AttendanceDayResponse>("/api/v1/attendance/me/check-out", {
    method: "POST",
    headers: {
      "Idempotency-Key": key,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  });

  // Cập nhật bộ nhớ đệm
  DataCacheManager.set(CACHE_KEY_TODAY, result, 60 * 1000);
  DataCacheManager.remove(`${CACHE_KEY_HISTORY}_p1_s20`);

  return result;
}

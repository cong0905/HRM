import { mobileFetch } from "./client";
import { DataCacheManager } from "../storage/cache";
import type {
  PayslipResponse,
  PayslipDetailResponse,
  PagedResultOfPayslipResponse,
} from "./types";

const CACHE_KEY_PAYSLIPS = "payroll_me_payslips";
const CACHE_KEY_PAYSLIP_DETAIL = "payroll_me_detail";

/**
 * Lấy danh sách phiếu lương đã công bố (Published) của chính mình.
 * Route: GET /api/v1/me/payslips?page=...&pageSize=...
 */
export async function getMyPayslips(
  page = 1,
  pageSize = 20,
  bypassCache = false
): Promise<PagedResultOfPayslipResponse> {
  const cacheKey = `${CACHE_KEY_PAYSLIPS}_p${page}_s${pageSize}`;

  if (!bypassCache) {
    const cached = DataCacheManager.get<PagedResultOfPayslipResponse>(cacheKey);
    if (cached) return cached;
  }

  const result = await mobileFetch<PagedResultOfPayslipResponse>(
    `/api/v1/me/payslips?page=${page}&pageSize=${pageSize}`,
    {
      method: "GET",
    }
  );

  DataCacheManager.set(cacheKey, result, 5 * 60 * 1000);
  return result;
}

/**
 * Lấy chi tiết phiếu lương đã công bố cùng giải thích chi tiết các thành phần lương.
 * Route: GET /api/v1/me/payslips/{id}
 */
export async function getMyPayslipDetail(
  id: string,
  bypassCache = false
): Promise<PayslipDetailResponse> {
  const cacheKey = `${CACHE_KEY_PAYSLIP_DETAIL}_${id}`;

  if (!bypassCache) {
    const cached = DataCacheManager.get<PayslipDetailResponse>(cacheKey);
    if (cached) return cached;
  }

  const result = await mobileFetch<PayslipDetailResponse>(`/api/v1/me/payslips/${id}`, {
    method: "GET",
  });

  DataCacheManager.set(cacheKey, result, 5 * 60 * 1000);
  return result;
}

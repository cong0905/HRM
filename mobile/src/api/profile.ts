import { mobileFetch } from "./client";
import { DataCacheManager } from "../storage/cache";
import type { EmployeeProfileResponse } from "./types";

const CACHE_KEY_PROFILE = "personal_me_profile";

/**
 * Lấy hồ sơ nhân viên chi tiết của chính người dùng hiện hành.
 * Route: GET /api/v1/me/profile
 */
export async function getMyProfile(bypassCache = false): Promise<EmployeeProfileResponse> {
  if (!bypassCache) {
    const cached = DataCacheManager.get<EmployeeProfileResponse>(CACHE_KEY_PROFILE);
    if (cached) return cached;
  }

  const result = await mobileFetch<EmployeeProfileResponse>("/api/v1/me/profile", {
    method: "GET",
  });

  DataCacheManager.set(CACHE_KEY_PROFILE, result, 5 * 60 * 1000);
  return result;
}

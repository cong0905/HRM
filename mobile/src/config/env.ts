/**
 * HRM Next - Cấu hình môi trường mạng (Environment Configuration)
 * 
 * RÀNG BUỘC BẢO MẬT BẮT BUỘC (T20 Review):
 * - Chỉ cho phép giao thức bảo mật HTTPS.
 * - Tuyệt đối không fallback HTTP dạng cleartext.
 * - Xác thực URL nghiêm ngặt: không userinfo, không query, không fragment.
 * - Không sử dụng default hostname staging giả lập.
 * - Không in raw URL có thể chứa thông tin bí mật vào thông báo lỗi.
 */

export interface AppConfig {
  apiBaseUrl: string;
  apiOrigin: string;
  timeoutMs: number;
  tokenRefreshLeadTimeSeconds: number;
}

/**
 * Xác thực và chuẩn hóa URL máy chủ API từ biến môi trường.
 */
export function validateAndNormalizeApiUrl(rawUrl?: string): { apiBaseUrl: string; apiOrigin: string } {
  const trimmed = (rawUrl || "").trim();

  if (!trimmed) {
    throw new Error(
      "Biến môi trường EXPO_PUBLIC_API_URL chưa được cấu hình. Vui lòng cung cấp URL HTTPS hợp lệ của máy chủ API."
    );
  }

  let parsed: URL;
  try {
    parsed = new URL(trimmed);
  } catch {
    throw new Error("URL API cấu hình không hợp lệ theo chuẩn URI.");
  }

  if (parsed.protocol !== "https:") {
    throw new Error("Chỉ chấp nhận giao thức bảo mật HTTPS cho máy chủ API.");
  }

  if (parsed.username || parsed.password) {
    throw new Error("URL máy chủ API không được chứa thông tin xác thực người dùng (userinfo).");
  }

  if (parsed.search && parsed.search.length > 0) {
    throw new Error("URL máy chủ API không được chứa chuỗi truy vấn (query string).");
  }

  if (parsed.hash && parsed.hash.length > 0) {
    throw new Error("URL máy chủ API không được chứa định danh đoạn (fragment).");
  }

  const apiOrigin = parsed.origin;
  const normalizedPath = parsed.pathname === "/" ? "" : parsed.pathname.replace(/\/+$/, "");
  const apiBaseUrl = `${apiOrigin}${normalizedPath}`;

  return { apiBaseUrl, apiOrigin };
}

/**
 * Trả về cấu hình môi trường ứng dụng hiện hành.
 */
export function getAppConfig(): AppConfig {
  const configuredUrl = process.env.EXPO_PUBLIC_API_URL;
  const { apiBaseUrl, apiOrigin } = validateAndNormalizeApiUrl(configuredUrl);

  return {
    apiBaseUrl,
    apiOrigin,
    timeoutMs: 15000, // Timeout 15 giây cho toàn bộ chu kỳ yêu cầu mạng
    tokenRefreshLeadTimeSeconds: 60, // Chủ động refresh trước khi token hết hạn 60 giây
  };
}

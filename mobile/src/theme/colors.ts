/**
 * HRM Next - Theme Colors
 * Quy chuẩn mã màu theo web/DESIGN.md:
 * - Nền sáng, White (#FFFFFF) & Cool mist (#F7F8FA)
 * - Màu chữ chính Graphite (#20252B) - độ tương phản >= 12:1 vượt chuẩn WCAG 4.5:1
 * - Chữ phụ Slate (#626B77)
 * - Đường kẻ Divider grey (#E3E6EB)
 * - Điểm nhấn Work blue (#3157D5)
 */
export const colors = {
  // Bề mặt & nền
  surface: "#FFFFFF",
  background: "#F7F8FA",
  
  // Văn bản
  textPrimary: "#20252B",
  textSecondary: "#626B77",
  textMuted: "#8C95A0",
  textInverse: "#FFFFFF",
  
  // Biên & đường phân cách
  border: "#E3E6EB",
  borderFocused: "#3157D5",
  borderError: "#D32F2F",
  
  // Màu thương hiệu / hành động chính (Work Blue)
  primary: "#3157D5",
  primaryHover: "#2748B8",
  primaryPressed: "#1E3996",
  primaryLight: "#EEF2FD",
  
  // Trạng thái hệ thống
  success: "#2E7D32",
  successLight: "#EDF7ED",
  warning: "#ED6C02",
  warningLight: "#FFF4E5",
  error: "#D32F2F",
  errorLight: "#FDEDED",
  
  // Phân vùng & lớp phủ
  disabledBackground: "#E9ECF0",
  disabledText: "#9CA3AF",
  overlay: "rgba(32, 37, 43, 0.4)",
} as const;

export type ColorName = keyof typeof colors;

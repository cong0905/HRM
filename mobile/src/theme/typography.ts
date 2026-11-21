import { Platform, TextStyle } from "react-native";
import { colors } from "./colors";

/**
 * HRM Next - Theme Typography
 * Phông chữ hệ thống hỗ trợ tiếng Việt đầy đủ (Be Vietnam Pro / Roboto / Segoe UI / San Francisco).
 * Bố cục font scaling đảm bảo văn bản co giãn theo cài đặt trợ năng của người dùng.
 */
const fontFamily = Platform.select({
  ios: "System",
  android: "Roboto",
  default: "sans-serif",
});

export const typography: Record<string, TextStyle> = {
  h1: {
    fontFamily,
    fontSize: 22,
    fontWeight: "600",
    lineHeight: 28,
    color: colors.textPrimary,
  },
  h2: {
    fontFamily,
    fontSize: 18,
    fontWeight: "600",
    lineHeight: 24,
    color: colors.textPrimary,
  },
  h3: {
    fontFamily,
    fontSize: 16,
    fontWeight: "600",
    lineHeight: 22,
    color: colors.textPrimary,
  },
  body: {
    fontFamily,
    fontSize: 15,
    fontWeight: "400",
    lineHeight: 22,
    color: colors.textPrimary,
  },
  bodyMedium: {
    fontFamily,
    fontSize: 15,
    fontWeight: "500",
    lineHeight: 22,
    color: colors.textPrimary,
  },
  caption: {
    fontFamily,
    fontSize: 13,
    fontWeight: "400",
    lineHeight: 18,
    color: colors.textSecondary,
  },
  captionMedium: {
    fontFamily,
    fontSize: 13,
    fontWeight: "500",
    lineHeight: 18,
    color: colors.textSecondary,
  },
  label: {
    fontFamily,
    fontSize: 14,
    fontWeight: "500",
    lineHeight: 20,
    color: colors.textPrimary,
  },
  button: {
    fontFamily,
    fontSize: 15,
    fontWeight: "600",
    lineHeight: 20,
    color: colors.textInverse,
    textAlign: "center",
  },
  helperError: {
    fontFamily,
    fontSize: 13,
    fontWeight: "400",
    lineHeight: 18,
    color: colors.error,
  },
};

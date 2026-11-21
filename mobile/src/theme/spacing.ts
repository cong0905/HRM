/**
 * HRM Next - Theme Spacing & Layout
 * Ràng buộc kích thước theo web/DESIGN.md và hướng dẫn tiếp cận:
 * - Touch target tối thiểu 44dp cho mọi nút tương tác
 * - Padding chuẩn 16dp
 * - Functional radius: 4dp cho input/button, 6dp cho container/drawer
 */
export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 24,
  xxl: 32,
  
  // Ràng buộc công thái học & tiếp cận (Accessibility)
  minTouchTarget: 44,
  inputHeight: 48,
  buttonHeight: 48,
  
  // Bán kính bo góc công năng (Functional Radius)
  radiusSm: 4,
  radiusMd: 6,
  radiusLg: 8,
} as const;

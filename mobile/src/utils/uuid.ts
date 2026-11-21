/**
 * Sinh khóa Idempotency (Idempotency Key) định dạng UUID v4 an toàn.
 * Tương thích với cả môi trường Web, React Native và Node/Jest.
 */
export function generateIdempotencyKey(): string {
  if (
    typeof globalThis !== "undefined" &&
    globalThis.crypto &&
    typeof globalThis.crypto.randomUUID === "function"
  ) {
    return globalThis.crypto.randomUUID();
  }

  // RFC4122 version 4 fallback
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

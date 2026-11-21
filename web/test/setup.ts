import "@testing-library/jest-dom/vitest";
import { afterEach } from "vitest";
import { cleanup } from "@testing-library/react";
import { transferableAbortController } from "node:util";

// jsdom has no pseudo-element layout. AntD asks for scrollbar pseudo styles;
// use element styles here and verify actual scroll containment in Playwright.
const elementStyle = window.getComputedStyle.bind(window);
window.getComputedStyle = element => elementStyle(element);

// Cleanup DOM after each test
afterEach(() => {
  cleanup();
});

// Realm pairing adapter: Pair Node native AbortController and AbortSignal
// so that jsdom signal instances match the realm expected by Node/undici native Request constructor.
const nativeController = transferableAbortController();
const NativeAbortController = nativeController.constructor;
const NativeAbortSignal = nativeController.signal.constructor;

Object.defineProperty(globalThis, "AbortController", {
  value: NativeAbortController,
  writable: true,
  configurable: true,
});

Object.defineProperty(globalThis, "AbortSignal", {
  value: NativeAbortSignal,
  writable: true,
  configurable: true,
});

if (typeof window !== "undefined") {
  Object.defineProperty(window, "AbortController", {
    value: NativeAbortController,
    writable: true,
    configurable: true,
  });

  Object.defineProperty(window, "AbortSignal", {
    value: NativeAbortSignal,
    writable: true,
    configurable: true,
  });
}

// Mock matchMedia for Ant Design responsive components in jsdom
Object.defineProperty(window, "matchMedia", {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});

// Mock ResizeObserver using globalThis to be standard across environments
globalThis.ResizeObserver = class ResizeObserver {
  observe(): void {}
  unobserve(): void {}
  disconnect(): void {}
};

// Mock scrollTo
window.scrollTo = () => {};

import { SessionManager } from '../src/api/session';
import { mobileFetch } from '../src/api/client';
import { SecureStoreAdapter } from '../src/storage/secureStore';
import { mobileLogin, forgotPassword, resetPassword, changePassword, logout } from '../src/api/auth';

jest.mock('expo-secure-store', () => ({
  getItemAsync: jest.fn(), setItemAsync: jest.fn(), deleteItemAsync: jest.fn(),
  AFTER_FIRST_UNLOCK: 'AFTER_FIRST_UNLOCK',
}));

const pair = { tokenType: 'Bearer', accessToken: 'unit-access', refreshToken: 'unit-refresh', expiresIn: 900 };
const json = (data: unknown, status = 200) => new Response(JSON.stringify(data), {
  status, headers: { 'content-type': 'application/json' },
});
function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>(done => { resolve = done; });
  return { promise, resolve };
}

beforeEach(async () => {
  jest.restoreAllMocks();
  SecureStoreAdapter.setMockMode(true);
  SecureStoreAdapter.resetMockStore();
  await SessionManager.resetForTesting();
  process.env.EXPO_PUBLIC_API_URL = 'https://api.example.invalid';
  await SessionManager.setSession(pair);
});
afterEach(() => { jest.restoreAllMocks(); });

test('never attaches a session bearer to a different HTTPS origin', async () => {
  const fetcher = jest.spyOn(globalThis, 'fetch').mockResolvedValue(json({}));
  await expect(mobileFetch('https://other.example.invalid/capture')).rejects.toThrow();
  expect(fetcher).not.toHaveBeenCalled();
});

test('a request from the previous account cannot return private data after an account switch', async () => {
  const response = deferred<Response>();
  jest.spyOn(globalThis, 'fetch').mockImplementation(() => response.promise);
  const pending = mobileFetch('/api/v1/access/me');
  await SessionManager.invalidateSession();
  await SessionManager.setSession({ ...pair, accessToken: 'other-account' });
  response.resolve(json({ id: 'previous-account', permissions: ['private'] }));
  await expect(pending).rejects.toThrow();
});

test('a second 401 after the single retry invalidates credentials', async () => {
  jest.spyOn(globalThis, 'fetch')
    .mockResolvedValueOnce(json({}, 401))
    .mockResolvedValueOnce(json(pair))
    .mockResolvedValueOnce(json({}, 401));
  await expect(mobileFetch('/api/v1/access/me')).rejects.toThrow();
  expect(SessionManager.getSession()).toBeNull();
  expect(await SecureStoreAdapter.getSession()).toBeNull();
});

test('logout during a delayed SecureStore refresh write cannot resurrect credentials', async () => {
  const saving = deferred<void>();
  const release = deferred<void>();
  const originalSave = SecureStoreAdapter.saveSession.bind(SecureStoreAdapter);
  jest.spyOn(SecureStoreAdapter, 'saveSession').mockImplementation(async params => {
    saving.resolve();
    await release.promise;
    return originalSave(params);
  });
  jest.spyOn(globalThis, 'fetch').mockResolvedValue(json({ ...pair, accessToken: 'refreshed' }));
  const refresh = SessionManager.refreshSingleFlight();
  await saving.promise;
  const logout = SessionManager.invalidateSession();
  expect(SessionManager.getSession()).toBeNull();
  release.resolve();
  await Promise.all([refresh, logout]);
  expect(SessionManager.getSession()).toBeNull();
  expect(await SecureStoreAdapter.getSession()).toBeNull();
});

test('a delayed old write cannot erase the new account credentials', async () => {
  const saving = deferred<void>();
  const release = deferred<void>();
  const originalSave = SecureStoreAdapter.saveSession.bind(SecureStoreAdapter);
  jest.spyOn(SecureStoreAdapter, 'saveSession').mockImplementationOnce(async params => {
    saving.resolve();
    await release.promise;
    return originalSave(params);
  });
  const oldSave = SessionManager.setSession({ ...pair, accessToken: 'old-write' });
  await saving.promise;
  const logout = SessionManager.invalidateSession();
  const newSave = SessionManager.setSession({ ...pair, accessToken: 'new-account' });
  release.resolve();
  await Promise.all([oldSave, logout, newSave]);
  expect(SessionManager.getSession()?.accessToken).toBe('new-account');
  expect((await SecureStoreAdapter.getSession())?.accessToken).toBe('new-account');
});

test('refresh callers share one request even when the session is only in storage', async () => {
  await SessionManager.resetForTesting();
  await SecureStoreAdapter.saveSession(pair);
  const fetcher = jest.spyOn(globalThis, 'fetch').mockImplementation(async () => json(pair));
  await Promise.all([SessionManager.refreshSingleFlight(), SessionManager.refreshSingleFlight()]);
  expect(fetcher).toHaveBeenCalledTimes(1);
});

test('persisted counters cannot change the current process generation during restore', async () => {
  const epoch = SessionManager.getEpoch();
  await SecureStoreAdapter.incrementSessionEpoch();
  const restored = await SessionManager.initialize();
  expect(restored?.accessToken).toBe(pair.accessToken);
  expect(SessionManager.getEpoch()).toBe(epoch);
});

test('logout while a login response is pending cannot restore that login', async () => {
  const response = deferred<Response>();
  const started = deferred<void>();
  jest.spyOn(globalThis, 'fetch').mockImplementation(() => {
    started.resolve();
    return response.promise;
  });
  const login = mobileLogin({ username: 'unit-user', password: 'unit-only-password' });
  await started.promise;
  await SessionManager.invalidateSession();
  response.resolve(json(pair));
  await expect(login).rejects.toThrow();
  expect(SessionManager.getSession()).toBeNull();
  expect(await SecureStoreAdapter.getSession()).toBeNull();
});

test('the request deadline also bounds a stalled response body', async () => {
  const response = json({});
  jest.spyOn(response, 'json').mockImplementation(() => new Promise(() => {}));
  jest.spyOn(globalThis, 'fetch').mockResolvedValue(response);
  await expect(mobileFetch('/api/v1/access/me', { timeoutMs: 10 }))
    .rejects.toMatchObject({ code: 'network.timeout' });
  expect(SessionManager.getSession()).not.toBeNull();
});

test('caller cancellation bounds a native fetch that does not honor AbortSignal', async () => {
  jest.spyOn(globalThis, 'fetch').mockImplementation(() => new Promise(() => {}));
  const controller = new AbortController();
  const pending = mobileFetch('/api/v1/access/me', { signal: controller.signal });
  controller.abort();
  await expect(pending).rejects.toMatchObject({ isNetworkError: true });
  expect(SessionManager.getSession()).not.toBeNull();
});

test('forgot password uses the public generated request shape and delivery response', async () => {
  const fetcher = jest.spyOn(globalThis, 'fetch').mockResolvedValue(json({ deliveryMode: 'test-only', message: 'Đã tiếp nhận' }, 202));
  const result = await forgotPassword({ email: 'unit@example.invalid' });
  expect(result.deliveryMode).toBe('test-only');
  const [url, request] = fetcher.mock.calls[0]!;
  expect(String(url)).toBe('https://api.example.invalid/api/v1/auth/forgot-password');
  expect(JSON.parse(request!.body as string)).toEqual({ email: 'unit@example.invalid' });
  expect(new Headers(request!.headers).has('Authorization')).toBe(false);
});

test('reset submits the test-channel code without attaching existing account credentials', async () => {
  const fetcher = jest.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 204 }));
  const request = { userId: '11111111-1111-4111-8111-111111111111', code: 'unit-reset-code', newPassword: 'unit-only-password' };
  await resetPassword(request);
  const [url, options] = fetcher.mock.calls[0]!;
  expect(String(url)).toBe('https://api.example.invalid/api/v1/auth/reset-password');
  expect(JSON.parse(options!.body as string)).toEqual(request);
  expect(new Headers(options!.headers).has('Authorization')).toBe(false);
});

test('successful password change clears the now-revoked local session', async () => {
  jest.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 204 }));
  await changePassword({ currentPassword: 'unit-old-password', newPassword: 'unit-new-password' });
  expect(SessionManager.getSession()).toBeNull();
  expect(await SecureStoreAdapter.getSession()).toBeNull();
});

test('offline logout clears locally before contacting the server and reports unconfirmed revocation', async () => {
  jest.spyOn(globalThis, 'fetch').mockImplementation(async () => {
    expect(SessionManager.getSession()).toBeNull();
    expect(await SecureStoreAdapter.getSession()).toBeNull();
    throw new TypeError('unit network unavailable');
  });
  const result = await logout();
  expect(result.serverConfirmed).toBe(false);
  expect(SessionManager.getSession()).toBeNull();
});

test('logout 401 cannot claim that a still-valid refresh session was revoked', async () => {
  jest.spyOn(globalThis, 'fetch').mockResolvedValue(json({}, 401));
  expect((await logout()).serverConfirmed).toBe(false);
  expect(await SecureStoreAdapter.getSession()).toBeNull();
});

test('storage deletion failure still attempts server revocation with the captured token', async () => {
  jest.spyOn(SecureStoreAdapter, 'clearSession').mockRejectedValueOnce(new Error('unit storage unavailable'));
  const fetcher = jest.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 204 }));
  const result = await logout();
  expect(result.serverConfirmed).toBe(true);
  expect(result.localCleared).toBe(false);
  expect(new Headers(fetcher.mock.calls[0]![1]!.headers).get('Authorization')).toBe('Bearer ' + pair.accessToken);
  expect(SessionManager.getSession()).toBeNull();
});

test('a fresh refresh caller cannot reload credentials while logout is clearing an old write', async () => {
  const writing = deferred<void>();
  const release = deferred<void>();
  const original = SecureStoreAdapter.saveSession.bind(SecureStoreAdapter);
  jest.spyOn(SecureStoreAdapter, 'saveSession').mockImplementationOnce(async params => {
    writing.resolve(); await release.promise; return original(params);
  });
  const oldWrite = SessionManager.setSession(pair);
  await writing.promise;
  const clearing = SessionManager.invalidateSession();
  const fetcher = jest.spyOn(globalThis, 'fetch').mockResolvedValue(json(pair));
  const refresh = SessionManager.refreshSingleFlight();
  release.resolve();
  await Promise.all([oldWrite, clearing, refresh]);
  expect(fetcher).not.toHaveBeenCalled();
  expect(SessionManager.getSession()).toBeNull();
});

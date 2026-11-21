import { ApiClientError } from './errors';

/** Bound all async stages, including a response body or native fetch ignoring abort. */
export function withAbort<T>(operation: Promise<T>, signal: AbortSignal): Promise<T> {
  return new Promise<T>((resolve, reject) => {
    const aborted = () => reject(new ApiClientError({
      message: 'Yêu cầu đã bị hủy hoặc hết thời gian chờ. Vui lòng thử lại.',
      status: 0, code: 'network.timeout', isNetworkError: true,
    }));
    if (signal.aborted) { operation.catch(() => undefined); aborted(); return; }
    signal.addEventListener('abort', aborted, { once: true });
    operation.then(resolve, reject).finally(() => signal.removeEventListener('abort', aborted));
  });
}

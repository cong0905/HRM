import { useRef } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ApiClientError } from "../../api/types";

/** Keep the same receipt for a manual retry with identical input after an uncertain response. */
export class CommandReceipt {
  private previous?: { payload: string; key: string };
  keyFor(value: unknown): string {
    const payload = JSON.stringify(value);
    if (!this.previous || this.previous.payload !== payload) this.previous = { payload, key: crypto.randomUUID() };
    return this.previous.key;
  }
  clear() { this.previous = undefined; }
}

export function useCommand<V, R>(execute: (values: V, key: string) => Promise<R>, keys: string[], onSuccess: (result: R) => void, scope = "") {
  const receipt = useRef(new CommandReceipt());
  const client = useQueryClient();
  const refresh = () => Promise.all(keys.map(key => client.invalidateQueries({ queryKey: [key] })));
  return useMutation({
    mutationFn: (values: V) => execute(values, receipt.current.keyFor({ scope, values })), retry: false,
    onSuccess: async result => { receipt.current.clear(); await refresh(); onSuccess(result); },
    onError: async error => { if (error instanceof ApiClientError && error.isConflict) await refresh(); },
  });
}

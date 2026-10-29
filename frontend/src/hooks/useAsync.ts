// hook genérico de carregamento de dados com estados de loading/erro, cancelamento e recarga.
import { useCallback, useEffect, useMemo, useRef, useState, type DependencyList } from "react";
import { ApiError, isAbortError } from "../services/api";

export interface AsyncState<T> {
  data: T | undefined;
  loading: boolean;
  error: ApiError | null;
  reload: () => void;
  setData: (updater: T | ((current: T | undefined) => T)) => void;
}

export function toApiError(error: unknown): ApiError {
  return error instanceof ApiError ? error : new ApiError(0, "UNKNOWN", "Ocorreu um erro inesperado.");
}

export function useAsync<T>(loader: (signal: AbortSignal) => Promise<T>, deps: DependencyList, enabled = true): AsyncState<T> {
  const [data, setDataState] = useState<T>();
  const [version, setVersion] = useState(0);
  // identifica a requisição atual: muda sempre que as dependências mudam ou um reload é pedido.
  // as dependências usadas no projeto são valores simples (ids, filtros, páginas), por isso podem ser serializadas.
  const depsKey = JSON.stringify(deps);
  // eslint-disable-next-line react-hooks/exhaustive-deps
  const request = useMemo(() => ({}), [depsKey, version, enabled]);
  const [settled, setSettled] = useState<{ request: object; error: ApiError | null } | null>(null);

  useEffect(() => {
    if (!enabled) return;
    // cada execução cancela a anterior: evita resultados fora de ordem e requisições duplicadas.
    const controller = new AbortController();
    loader(controller.signal)
      .then((result) => {
        if (controller.signal.aborted) return;
        setDataState(result);
        setSettled({ request, error: null });
      })
      .catch((err) => {
        if (controller.signal.aborted || isAbortError(err)) return;
        setSettled({ request, error: toApiError(err) });
      });
    return () => controller.abort();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [request]);

  const reload = useCallback(() => setVersion((v) => v + 1), []);
  const setData = useCallback((updater: T | ((current: T | undefined) => T)) => {
    setDataState((current) => (typeof updater === "function" ? (updater as (c: T | undefined) => T)(current) : updater));
  }, []);

  const isCurrent = settled?.request === request;
  return {
    data,
    loading: enabled && !isCurrent,
    error: isCurrent ? settled!.error : null,
    reload,
    setData,
  };
}

// executa uma ação (salvar, excluir...) controlando o estado de envio e o erro.
export function useAction<TArgs extends unknown[], TResult>(action: (...args: TArgs) => Promise<TResult>) {
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const actionRef = useRef(action);
  useEffect(() => {
    actionRef.current = action;
  });

  const run = useCallback(async (...args: TArgs): Promise<TResult | undefined> => {
    setPending(true);
    setError(null);
    try {
      return await actionRef.current(...args);
    } catch (err) {
      setError(toApiError(err));
      return undefined;
    } finally {
      setPending(false);
    }
  }, []);

  const clearError = useCallback(() => setError(null), []);
  return { run, pending, error, clearError };
}

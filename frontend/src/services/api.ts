// cliente HTTP central da aplicação.
// - define a URL base da API (VITE_API_URL);
// - envia o access token do Auth0 em todas as requisições protegidas;
// - converte respostas de erro (ProblemDetails do backend) em ApiError;
// - aplica timeout e permite cancelamento (AbortSignal).
import { env } from "../config/env";

export type FieldErrors = Record<string, string[]>;

// erro padronizado: o backend sempre devolve { status, title, detail, code, errors? }.
export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly fieldErrors: FieldErrors;

  constructor(status: number, code: string, message: string, fieldErrors: FieldErrors = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.fieldErrors = fieldErrors;
  }
}

type TokenProvider = () => Promise<string | undefined>;
type SessionEndedHandler = (code: string) => void;

let tokenProvider: TokenProvider | null = null;
let sessionEndedHandler: SessionEndedHandler | null = null;

// registrado pelo ApiAuthBridge (dentro do Auth0Provider) para obter o token sem acoplar o cliente ao React.
export function setTokenProvider(provider: TokenProvider | null) {
  tokenProvider = provider;
}

// chamado quando o backend informa sessão expirada/revogada (RNF03).
export function setSessionEndedHandler(handler: SessionEndedHandler | null) {
  sessionEndedHandler = handler;
}

type Query = Record<string, string | number | boolean | null | undefined | Array<string | number>>;

export interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  query?: Query;
  signal?: AbortSignal;
  timeoutMs?: number;
  // quando true, devolve o Blob da resposta (ex.: arquivos).
  asBlob?: boolean;
}

const DEFAULT_TIMEOUT_MS = 20_000;

function buildUrl(path: string, query?: Query) {
  const url = new URL(`${env.apiUrl}${path}`);
  Object.entries(query ?? {}).forEach(([key, value]) => {
    if (value === undefined || value === null || value === "") return;
    if (Array.isArray(value)) value.forEach((v) => url.searchParams.append(key, String(v)));
    else url.searchParams.set(key, String(value));
  });
  return url.toString();
}

async function toApiError(response: Response): Promise<ApiError> {
  let code = "HTTP_ERROR";
  let message = `Erro ${response.status} ao comunicar com o servidor.`;
  let fieldErrors: FieldErrors = {};
  try {
    const problem = await response.json();
    code = problem.code ?? code;
    message = problem.detail ?? problem.title ?? message;
    fieldErrors = problem.errors ?? {};
  } catch {
    // resposta sem corpo JSON: mantém a mensagem genérica.
  }
  if (response.status === 401 && code === "HTTP_ERROR") {
    code = "UNAUTHENTICATED";
    message = "Sua sessão não é válida. Entre novamente.";
  }
  if (response.status === 429) message = "Muitas requisições em pouco tempo. Aguarde alguns instantes.";
  return new ApiError(response.status, code, message, fieldErrors);
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { method = "GET", body, query, signal, timeoutMs = DEFAULT_TIMEOUT_MS, asBlob } = options;

  // combina o cancelamento externo com o timeout.
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(new DOMException("timeout", "TimeoutError")), timeoutMs);
  const abortFromCaller = () => controller.abort(signal?.reason);
  signal?.addEventListener("abort", abortFromCaller);

  try {
    const headers: Record<string, string> = { Accept: "application/json" };
    const token = tokenProvider ? await tokenProvider() : undefined;
    if (token) headers.Authorization = `Bearer ${token}`;

    let payload: BodyInit | undefined;
    if (body instanceof FormData) payload = body;
    else if (body !== undefined) {
      headers["Content-Type"] = "application/json";
      payload = JSON.stringify(body);
    }

    const response = await fetch(buildUrl(path, query), { method, headers, body: payload, signal: controller.signal });

    if (!response.ok) {
      const error = await toApiError(response);
      if (response.status === 401 && (error.code === "SESSION_EXPIRED" || error.code === "SESSION_REVOKED"))
        sessionEndedHandler?.(error.code);
      throw error;
    }
    if (asBlob) return (await response.blob()) as T;
    if (response.status === 204) return undefined as T;
    const text = await response.text();
    return (text ? JSON.parse(text) : undefined) as T;
  } catch (error) {
    if (error instanceof ApiError) throw error;
    if (signal?.aborted) throw error; // cancelado pelo chamador: não é erro para o usuário.
    if (controller.signal.aborted)
      throw new ApiError(0, "TIMEOUT", "O servidor demorou para responder. Tente novamente.");
    if (error instanceof Error && error.message.toLowerCase().includes("login required"))
      throw new ApiError(401, "LOGIN_REQUIRED", "Sua sessão expirou. Entre novamente.");
    throw new ApiError(0, "NETWORK_ERROR", "Não foi possível conectar ao servidor. Verifique sua conexão e se a API está em execução.");
  } finally {
    window.clearTimeout(timeout);
    signal?.removeEventListener("abort", abortFromCaller);
  }
}

// atalhos tipados.
export const api = {
  get: <T>(path: string, query?: Query, signal?: AbortSignal) => apiRequest<T>(path, { query, signal }),
  post: <T>(path: string, body?: unknown) => apiRequest<T>(path, { method: "POST", body }),
  put: <T>(path: string, body?: unknown) => apiRequest<T>(path, { method: "PUT", body }),
  patch: <T>(path: string, body?: unknown) => apiRequest<T>(path, { method: "PATCH", body }),
  delete: <T = void>(path: string, body?: unknown) => apiRequest<T>(path, { method: "DELETE", body }),
};

// verdadeiro quando o erro veio de um cancelamento intencional (ex.: nova busca iniciada).
export function isAbortError(error: unknown) {
  return error instanceof DOMException && error.name === "AbortError";
}

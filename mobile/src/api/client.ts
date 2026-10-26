/**
 * Cliente da API do UniCar. Toda resposta de erro do backend vem como { code, message } e a
 * message já está pronta para mostrar ao usuário (ApiError do C#).
 */

/** Sem barra no fim. No celular use o túnel (ngrok) ou o IP do notebook, nunca localhost. */
export const API_URL = (process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5080').replace(/\/+$/, '');

export class ApiError extends Error {
  constructor(
    public readonly code: string,
    message: string,
    public readonly status: number,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

let token: string | null = null;
let onUnauthorized: (() => void) | null = null;

export function setAuthToken(value: string | null) {
  token = value;
}

export function getAuthToken() {
  return token;
}

/** Chamado quando a API responde 401 com o token de acesso (sessão vencida): o app volta para a entrada. */
export function setUnauthorizedHandler(handler: (() => void) | null) {
  onUnauthorized = handler;
}

type Options = {
  body?: unknown;
  form?: FormData;
  /** Usa outro token só nesta chamada (o token de cadastro no register). */
  bearer?: string;
  signal?: AbortSignal;
};

async function request<T>(method: string, path: string, opts: Options = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  const auth = opts.bearer ?? token;
  if (auth) headers.Authorization = `Bearer ${auth}`;
  let body: BodyInit | undefined;
  if (opts.form) body = opts.form;
  else if (opts.body !== undefined) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(opts.body);
  }

  let res: Response;
  try {
    res = await fetch(API_URL + path, { method, headers, body, signal: opts.signal });
  } catch (e) {
    if ((e as Error)?.name === 'AbortError') throw e;
    throw new ApiError('network', 'Sem conexão com o UniCar. Confira a internet e tente de novo.', 0);
  }

  if (res.status === 204) return undefined as T;
  const text = await res.text();
  const data = text ? safeJson(text) : undefined;

  if (!res.ok) {
    if (res.status === 401 && !opts.bearer && token) onUnauthorized?.();
    const err = data as { code?: string; message?: string } | undefined;
    if (err?.message) throw new ApiError(err.code ?? 'error', err.message, res.status);
    throw new ApiError(`http_${res.status}`, fallbackMessage(res.status), res.status);
  }
  return data as T;
}

function safeJson(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return undefined;
  }
}

function fallbackMessage(status: number) {
  if (status === 401) return 'Sua sessão terminou. Entre de novo.';
  if (status === 403) return 'Você não tem acesso a isso.';
  if (status === 404) return 'Não encontramos o que você procurou.';
  if (status === 503) return 'Esse serviço está indisponível no momento.';
  return 'Algo deu errado. Tente de novo em instantes.';
}

export const api = {
  get: <T>(path: string, opts?: Options) => request<T>('GET', path, opts),
  post: <T>(path: string, body?: unknown, opts?: Options) => request<T>('POST', path, { ...opts, body }),
  put: <T>(path: string, body?: unknown, opts?: Options) => request<T>('PUT', path, { ...opts, body }),
  del: <T>(path: string, opts?: Options) => request<T>('DELETE', path, opts),
  upload: <T>(path: string, form: FormData, opts?: Options) => request<T>('POST', path, { ...opts, form }),
};

/** Mensagem para mostrar de qualquer erro (ApiError ou inesperado). */
export function messageOf(e: unknown) {
  if (e instanceof ApiError) return e.message;
  return 'Algo deu errado. Tente de novo em instantes.';
}

import type { DeckAnalysisRequest, DeckAnalysisResult, PreconSearchResult, PreconDeck } from '../types/deck';

const API_BASE = '/api';

/** Typed API error that carries the HTTP status alongside the message. */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

/** Throws an ApiError for non-OK responses, extracting the server error message when available. */
async function assertOk(response: Response): Promise<void> {
  if (response.ok) return;
  const body = await response.json().catch(() => null) as { error?: string } | null;
  throw new ApiError(response.status, body?.error ?? `HTTP ${response.status}`);
}

export async function analyzeDeck(
  request: DeckAnalysisRequest,
  signal?: AbortSignal,
): Promise<DeckAnalysisResult> {
  const response = await fetch(`${API_BASE}/deck/analyze`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
    signal,
  });
  await assertOk(response);
  return response.json() as Promise<DeckAnalysisResult>;
}

/** Parameters for the precon search endpoint — groups all filter options. */
export interface PreconSearchParams {
  query?: string;
  year?: string;
  colors?: string[];
  page?: number;
  pageSize?: number;
}

export async function searchPrecons(
  params: PreconSearchParams = {},
  signal?: AbortSignal,
): Promise<PreconSearchResult> {
  const { query, year, colors, page = 1, pageSize = 20 } = params;

  const qs = new URLSearchParams();
  if (query) qs.append('query', query);
  if (year) qs.append('year', year);
  colors?.forEach((c) => qs.append('colors', c));
  qs.append('page', String(page));
  qs.append('pageSize', String(pageSize));

  const response = await fetch(`${API_BASE}/precon/search?${qs}`, { signal });
  await assertOk(response);
  return response.json() as Promise<PreconSearchResult>;
}

export async function getPrecon(name: string, signal?: AbortSignal): Promise<PreconDeck> {
  const response = await fetch(`${API_BASE}/precon/${encodeURIComponent(name)}`, { signal });
  await assertOk(response);
  return response.json() as Promise<PreconDeck>;
}
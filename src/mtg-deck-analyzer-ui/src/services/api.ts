import { DeckAnalysisRequest, DeckAnalysisResult, PreconSearchResult, PreconDeck } from '../types/deck';

const API_BASE = '/api';

export async function analyzeDeck(request: DeckAnalysisRequest): Promise<DeckAnalysisResult> {
  const response = await fetch(`${API_BASE}/deck/analyze`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Unknown error' }));
    throw new Error(error.error || `HTTP ${response.status}`);
  }

  return response.json();
}

export const searchPrecons = async (
  query?: string,
  year?: string,
  colors?: string[],
  page: number = 1,
  pageSize: number = 20
): Promise<PreconSearchResult> => {
  const params = new URLSearchParams();
  if (query) params.append('query', query);
  if (year) params.append('year', year);
  if (colors && colors.length > 0) {
    colors.forEach(color => params.append('colors', color));
  }
  params.append('page', page.toString());
  params.append('pageSize', pageSize.toString());

  const response = await fetch(`${API_BASE}/precon/search?${params}`);
  if (!response.ok) {
    throw new Error('Failed to search precons');
  }
  return response.json();
};

export const getPrecon = async (name: string): Promise<PreconDeck> => {
  const response = await fetch(`${API_BASE}/precon/${encodeURIComponent(name)}`);
  if (!response.ok) {
    throw new Error('Failed to get precon');
  }
  return response.json();
};

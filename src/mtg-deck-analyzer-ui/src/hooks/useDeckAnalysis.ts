import { useState, useCallback, useRef } from 'react';
import type { DeckAnalysisResult, PreconDeck } from '../types/deck';
import { analyzeDeck } from '../services/api';

interface UseDeckAnalysisReturn {
  result: DeckAnalysisResult | null;
  loading: boolean;
  error: string | null;
  deckList: string;
  setDeckList: (value: string) => void;
  handleAnalyze: (list: string) => void;
  handleSelectPrecon: (precon: PreconDeck) => void;
}

/**
 * Encapsulates all deck-analysis state and side effects.
 * Keeps App.tsx focused on layout/composition (SRP).
 */
export function useDeckAnalysis(): UseDeckAnalysisReturn {
  const [result, setResult] = useState<DeckAnalysisResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [deckList, setDeckList] = useState('');

  // Cancel any in-flight request when a new analysis is triggered.
  const abortRef = useRef<AbortController | null>(null);

  const handleAnalyze = useCallback((list: string) => {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setLoading(true);
    setError(null);
    setResult(null);

    analyzeDeck({ deckList: list }, controller.signal)
      .then(setResult)
      .catch((err: unknown) => {
        if (err instanceof Error && err.name === 'AbortError') return;
        setError(err instanceof Error ? err.message : 'Failed to analyze deck');
      })
      .finally(() => setLoading(false));
  }, []);

  const handleSelectPrecon = useCallback((precon: PreconDeck) => {
    const lines: string[] = [];

    precon.commanders?.forEach((commander) => lines.push(`1 ${commander}`));
    if (lines.length > 0) lines.push('');

    if (typeof precon.deckList === 'string') {
      precon.deckList.split('\n').filter((l) => l.trim()).forEach((l) => lines.push(l));
    } else if (Array.isArray(precon.deckList)) {
      (precon.deckList as string[]).forEach((card) => lines.push(`1 ${card}`));
    }

    setDeckList(lines.join('\n'));
  }, []);

  return { result, loading, error, deckList, setDeckList, handleAnalyze, handleSelectPrecon };
}
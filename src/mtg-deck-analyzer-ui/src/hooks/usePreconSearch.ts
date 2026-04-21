import { useState, useEffect, useCallback, useRef } from 'react';
import type { PreconSearchResult } from '../types/deck';
import { searchPrecons } from '../services/api';

interface UsePreconSearchReturn {
  results: PreconSearchResult;
  loading: boolean;
  searchQuery: string;
  selectedYear: string;
  selectedColors: string[];
  currentPage: number;
  totalPages: number;
  setSearchQuery: (v: string) => void;
  setSelectedYear: (v: string) => void;
  toggleColor: (color: string) => void;
  clearFilters: () => void;
  setCurrentPage: (page: number) => void;
}

const PAGE_SIZE = 12;
const EMPTY_RESULTS: PreconSearchResult = { precons: [], totalCount: 0 };

/**
 * Encapsulates precon-search state and API calls.
 * PreconSearch component only handles presentation (SRP).
 */
export function usePreconSearch(): UsePreconSearchReturn {
  const [results, setResults] = useState<PreconSearchResult>(EMPTY_RESULTS);
  const [loading, setLoading] = useState(false);
  const [searchQuery, setSearchQueryRaw] = useState('');
  const [selectedYear, setSelectedYearRaw] = useState('');
  const [selectedColors, setSelectedColors] = useState<string[]>([]);
  const [currentPage, setCurrentPageRaw] = useState(1);

  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setLoading(true);
    searchPrecons(
      { query: searchQuery, year: selectedYear, colors: selectedColors, page: currentPage, pageSize: PAGE_SIZE },
      controller.signal,
    )
      .then(setResults)
      .catch((err: unknown) => {
        if (err instanceof Error && err.name === 'AbortError') return;
        console.error('Error searching precons:', err);
      })
      .finally(() => setLoading(false));

    return () => controller.abort();
  }, [searchQuery, selectedYear, selectedColors, currentPage]);

  const setSearchQuery = useCallback((v: string) => {
    setSearchQueryRaw(v);
    setCurrentPageRaw(1);
  }, []);

  const setSelectedYear = useCallback((v: string) => {
    setSelectedYearRaw(v);
    setCurrentPageRaw(1);
  }, []);

  const toggleColor = useCallback((color: string) => {
    setSelectedColors((prev) =>
      prev.includes(color) ? prev.filter((c) => c !== color) : [...prev, color],
    );
    setCurrentPageRaw(1);
  }, []);

  const clearFilters = useCallback(() => {
    setSearchQueryRaw('');
    setSelectedYearRaw('');
    setSelectedColors([]);
    setCurrentPageRaw(1);
  }, []);

  const totalPages = Math.ceil(results.totalCount / PAGE_SIZE);

  return {
    results,
    loading,
    searchQuery,
    selectedYear,
    selectedColors,
    currentPage,
    totalPages,
    setSearchQuery,
    setSelectedYear,
    toggleColor,
    clearFilters,
    setCurrentPage: setCurrentPageRaw,
  };
}
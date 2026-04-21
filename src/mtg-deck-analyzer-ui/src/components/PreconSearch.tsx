import { usePreconSearch } from '../hooks/usePreconSearch';
import { COLOR_ORDER, COLOR_NAMES, getColorSymbol, getColorCircle } from '../constants/colors';
import type { PreconDeck } from '../types/deck';
import './PreconSearch.css';

const FALLBACK_IMAGE = 'https://cards.scryfall.io/art_crop/front/0/0/0000579f-7b35-4ed3-b44c-db2a538066fe.jpg';

/** Year options from 2026 down to 2011. */
const YEARS = Array.from({ length: 2026 - 2011 + 1 }, (_, i) => 2026 - i);

interface PreconSearchProps {
  onSelectPrecon: (precon: PreconDeck) => void;
  onClose: () => void;
}

export default function PreconSearch({ onSelectPrecon, onClose }: PreconSearchProps) {
  const {
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
    setCurrentPage,
  } = usePreconSearch();

  return (
    <div className="precon-search-overlay" role="dialog" aria-modal="true" aria-label="Choose a preconstructed deck">
      <div className="precon-search-modal">
        <div className="precon-search-header">
          <h2>🏛️ Choose a Preconstructed Deck</h2>
          <button className="close-button" onClick={onClose} aria-label="Close">✕</button>
        </div>

        <div className="precon-filters">
          <div className="filter-row">
            <input
              type="search"
              placeholder="Search by name, theme, or commander..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="search-input"
              aria-label="Search precon decks"
            />

            <select
              value={selectedYear}
              onChange={(e) => setSelectedYear(e.target.value)}
              className="year-select"
              aria-label="Filter by year"
            >
              <option value="">All Years</option>
              {YEARS.map((year) => (
                <option key={year} value={year}>{year}</option>
              ))}
            </select>

            <button onClick={clearFilters} className="clear-button">
              Clear Filters
            </button>
          </div>

          <div className="color-filters" role="group" aria-label="Filter by color">
            <span>Colors:</span>
            {COLOR_ORDER.map((color) => (
              <button
                key={color}
                onClick={() => toggleColor(color)}
                className={`color-button ${selectedColors.includes(color) ? 'selected' : ''}`}
                aria-label={COLOR_NAMES[color]}
                aria-pressed={selectedColors.includes(color)}
              >
                {getColorCircle(color)}
              </button>
            ))}
          </div>
        </div>

        <div className="precon-results">
          {loading ? (
            <div className="loading" role="status" aria-live="polite">Loading precons...</div>
          ) : (
            <>
              <div className="results-info" aria-live="polite">
                Found {results.totalCount} preconstructed deck{results.totalCount !== 1 ? 's' : ''}
              </div>

              <div className="precon-grid">
                {results.precons.map((precon) => (
                  <button
                    key={precon.name}
                    className="precon-card"
                    onClick={() => onSelectPrecon(precon)}
                    aria-label={`Select ${precon.name}`}
                  >
                    <div className="precon-image">
                      <img
                        src={precon.imageUrl}
                        alt=""
                        loading="lazy"
                        onError={(e) => { e.currentTarget.src = FALLBACK_IMAGE; }}
                      />
                    </div>
                    <div className="precon-info">
                      <h3>{precon.name}</h3>
                      <div className="precon-year">{precon.year}</div>
                      <div className="precon-theme">{precon.theme}</div>
                      <div className="precon-commanders">{precon.commanders.join(', ')}</div>
                      <div className="precon-colors" aria-label="Color identity">
                        {precon.colorIdentity.map((color) => (
                          <span key={color} className="color-pip">{getColorSymbol(color)}</span>
                        ))}
                      </div>
                    </div>
                  </button>
                ))}
              </div>

              {totalPages > 1 && (
                <nav className="pagination" aria-label="Precon search pages">
                  <button
                    onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
                    disabled={currentPage === 1}
                    aria-label="Previous page"
                  >
                    ← Previous
                  </button>
                  <span className="page-info">Page {currentPage} of {totalPages}</span>
                  <button
                    onClick={() => setCurrentPage(Math.min(totalPages, currentPage + 1))}
                    disabled={currentPage === totalPages}
                    aria-label="Next page"
                  >
                    Next →
                  </button>
                </nav>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
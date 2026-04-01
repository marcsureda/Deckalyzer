import React, { useState, useEffect } from 'react';
import { PreconDeck, PreconSearchResult } from '../types/deck';
import { searchPrecons } from '../services/api';
import './PreconSearch.css';

interface PreconSearchProps {
  onSelectPrecon: (precon: PreconDeck) => void;
  onClose: () => void;
}

const PreconSearch: React.FC<PreconSearchProps> = ({ onSelectPrecon, onClose }) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedYear, setSelectedYear] = useState('');
  const [selectedColors, setSelectedColors] = useState<string[]>([]);
  const [results, setResults] = useState<PreconSearchResult>({ precons: [], totalCount: 0 });
  const [loading, setLoading] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);

  const colors = ['W', 'U', 'B', 'R', 'G'];
  const colorNames = { W: 'White', U: 'Blue', B: 'Black', R: 'Red', G: 'Green' };
  const years = Array.from({ length: 2026 - 2011 + 1 }, (_, i) => 2026 - i);

  useEffect(() => {
    searchPreconDecks();
  }, [searchQuery, selectedYear, selectedColors, currentPage]);

  const searchPreconDecks = async () => {
    setLoading(true);
    try {
      const result = await searchPrecons(searchQuery, selectedYear, selectedColors, currentPage, 12);
      setResults(result);
    } catch (error) {
      console.error('Error searching precons:', error);
    } finally {
      setLoading(false);
    }
  };

  const toggleColor = (color: string) => {
    setSelectedColors(prev => 
      prev.includes(color) 
        ? prev.filter(c => c !== color)
        : [...prev, color]
    );
    setCurrentPage(1);
  };

  const clearFilters = () => {
    setSearchQuery('');
    setSelectedYear('');
    setSelectedColors([]);
    setCurrentPage(1);
  };

  const getColorSymbol = (color: string) => {
    const symbols = {
      W: '⚪', U: '🔵', B: '⚫', R: '🔴', G: '🟢'
    };
    return symbols[color as keyof typeof symbols] || color;
  };

  const totalPages = Math.ceil(results.totalCount / 12);

  return (
    <div className="precon-search-overlay">
      <div className="precon-search-modal">
        <div className="precon-search-header">
          <h2>🏛️ Choose a Preconstructed Deck</h2>
          <button className="close-button" onClick={onClose}>✕</button>
        </div>

        <div className="precon-filters">
          <div className="filter-row">
            <input
              type="text"
              placeholder="Search by name, theme, or commander..."
              value={searchQuery}
              onChange={(e) => {
                setSearchQuery(e.target.value);
                setCurrentPage(1);
              }}
              className="search-input"
            />
            
            <select
              value={selectedYear}
              onChange={(e) => {
                setSelectedYear(e.target.value);
                setCurrentPage(1);
              }}
              className="year-select"
            >
              <option value="">All Years</option>
              {years.map(year => (
                <option key={year} value={year}>{year}</option>
              ))}
            </select>

            <button onClick={clearFilters} className="clear-button">
              Clear Filters
            </button>
          </div>

          <div className="color-filters">
            <span>Colors:</span>
            {colors.map(color => (
              <button
                key={color}
                onClick={() => toggleColor(color)}
                className={`color-button ${selectedColors.includes(color) ? 'selected' : ''}`}
                title={colorNames[color as keyof typeof colorNames]}
              >
                {getColorSymbol(color)}
              </button>
            ))}
          </div>
        </div>

        <div className="precon-results">
          {loading ? (
            <div className="loading">Loading precons...</div>
          ) : (
            <>
              <div className="results-info">
                Found {results.totalCount} preconstructed deck{results.totalCount !== 1 ? 's' : ''}
              </div>
              
              <div className="precon-grid">
                {results.precons.map((precon) => (
                  <div
                    key={precon.name}
                    className="precon-card"
                    onClick={() => onSelectPrecon(precon)}
                  >
                    <div className="precon-image">
                      <img 
                        src={precon.imageUrl} 
                        alt={precon.name}
                        loading="lazy"
                        onError={(e) => {
                          console.log('Image failed to load:', precon.imageUrl);
                          e.currentTarget.src = 'https://cards.scryfall.io/art_crop/front/0/0/0000579f-7b35-4ed3-b44c-db2a538066fe.jpg';
                        }}
                      />
                    </div>
                    <div className="precon-info">
                      <h3>{precon.name}</h3>
                      <div className="precon-year">{precon.year}</div>
                      <div className="precon-theme">{precon.theme}</div>
                      <div className="precon-commanders">
                        {precon.commanders.join(', ')}
                      </div>
                      <div className="precon-colors">
                        {precon.colorIdentity.map(color => (
                          <span key={color} className="color-pip">
                            {getColorSymbol(color)}
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>
                ))}
              </div>

              {totalPages > 1 && (
                <div className="pagination">
                  <button
                    onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                    disabled={currentPage === 1}
                  >
                    ← Previous
                  </button>
                  
                  <span className="page-info">
                    Page {currentPage} of {totalPages}
                  </span>
                  
                  <button
                    onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                    disabled={currentPage === totalPages}
                  >
                    Next →
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default PreconSearch;
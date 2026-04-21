import { useState } from 'react';
import { useDeckAnalysis } from './hooks/useDeckAnalysis';
import DeckInput from './components/DeckInput';
import PowerOverview from './components/PowerOverview';
import BracketDisplay from './components/BracketDisplay';
import DeckCompositionChart from './components/DeckCompositionChart';
import ManaCurveChart from './components/ManaCurveChart';
import DeckGallery from './components/DeckGallery';
import Recommendations from './components/Recommendations';
import StrengthsWeaknesses from './components/StrengthsWeaknesses';
import DeckStrategy from './components/DeckStrategy';
import TokenDisplay from './components/TokenDisplay';
import PreconSearch from './components/PreconSearch';
import './App.css';

export default function App() {
  const { result, loading, error, deckList, setDeckList, handleAnalyze, handleSelectPrecon } =
    useDeckAnalysis();
  const [showPreconSearch, setShowPreconSearch] = useState(false);

  return (
    <div className="app">
      <header className="app-header">
        <div className="container">
          <div className="header-content">
            <div className="logo">
              <svg className="logo-mtg" viewBox="0 0 100 140" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
                <path d="M50 0C35 0 25 15 25 30C25 45 35 55 50 55C65 55 75 45 75 30C75 15 65 0 50 0ZM50 8C60 8 67 18 67 30C67 42 60 47 50 47C40 47 33 42 33 30C33 18 40 8 50 8Z" fill="url(#grad1)" />
                <path d="M30 50L15 90L35 75L50 100L65 75L85 90L70 50" fill="url(#grad1)" />
                <path d="M50 95L40 115L50 140L60 115Z" fill="url(#grad1)" />
                <circle cx="50" cy="30" r="10" fill="url(#grad1)" />
                <defs>
                  <linearGradient id="grad1" x1="15" y1="0" x2="85" y2="140" gradientUnits="userSpaceOnUse">
                    <stop stopColor="#c9a44a" />
                    <stop offset="0.5" stopColor="#f0d060" />
                    <stop offset="1" stopColor="#c9a44a" />
                  </linearGradient>
                </defs>
              </svg>
              <h1>MTG Deck<span className="logo-accent">alyzer</span></h1>
            </div>
            <p className="subtitle">
              Analyze power levels, brackets, and get recommendations for your EDH deck
            </p>
          </div>
        </div>
      </header>

      <main className="container">
        <DeckInput
          onAnalyze={handleAnalyze}
          loading={loading}
          onSearchPrecons={() => setShowPreconSearch(true)}
          value={deckList}
          onChange={setDeckList}
        />

        {error && (
          <div className="error-banner" role="alert">
            <span aria-hidden="true">⚠️</span> {error}
          </div>
        )}

        {loading && (
          <div className="loading-container" aria-live="polite" aria-busy="true">
            <div className="loading-spinner" role="status" aria-label="Analyzing deck" />
            <p>Analyzing your deck via Scryfall...</p>
            <p className="loading-sub">This may take a moment for large decklists</p>
          </div>
        )}

        {result && (
          <div className="results">
            {result.warnings && result.warnings.length > 0 && (
              <div className="warnings-banner" role="alert">
                {result.warnings.map((w, idx) => (
                  <div key={idx} className="warning-item warning-info">
                    <span aria-hidden="true">⚠️</span> {w}
                  </div>
                ))}
              </div>
            )}

            <PowerOverview result={result} />

            <div className="results-grid">
              <BracketDisplay result={result} />
              <StrengthsWeaknesses result={result} />
            </div>

            {result.strategy && <DeckStrategy strategy={result.strategy} />}

            {result.tokens && result.tokens.length > 0 && (
              <TokenDisplay tokens={result.tokens} />
            )}

            <div className="results-grid">
              <DeckCompositionChart composition={result.composition} />
              <ManaCurveChart manaAnalysis={result.manaAnalysis} />
            </div>

            <Recommendations result={result} />
            <DeckGallery cards={result.cards} />
          </div>
        )}
      </main>

      <footer className="app-footer">
        <div className="container">
          <p>
            Card data provided by{' '}
            <a href="https://scryfall.com/" target="_blank" rel="noreferrer">Scryfall</a>.
            Inspired by{' '}
            <a href="https://edhpowerlevel.com/" target="_blank" rel="noreferrer">EDHPowerLevel</a>{' '}
            and <a href="https://edhrec.com/" target="_blank" rel="noreferrer">EDHREC</a>.
          </p>
          <p className="disclaimer">
            Wizards of the Coast, Magic: The Gathering, and their logos are trademarks of Wizards of the Coast LLC.
            This tool is not affiliated with or endorsed by Wizards of the Coast.
          </p>
        </div>
      </footer>

      {showPreconSearch && (
        <PreconSearch
          onSelectPrecon={(precon) => {
            handleSelectPrecon(precon);
            setShowPreconSearch(false);
          }}
          onClose={() => setShowPreconSearch(false)}
        />
      )}
    </div>
  );
}
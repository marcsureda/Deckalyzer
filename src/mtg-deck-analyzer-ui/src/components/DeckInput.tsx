import { SAMPLE_DECK } from '../constants/sampleDeck';
import './DeckInput.css';

interface DeckInputProps {
  onAnalyze: (deckList: string) => void;
  loading: boolean;
  onSearchPrecons: () => void;
  value: string;
  onChange: (deckList: string) => void;
}

/** Counts non-empty, non-comment, non-header lines in a deck list string. */
function countDeckLines(raw: string): number {
  return raw
    .trim()
    .split('\n')
    .filter((l) => l.trim() && !l.startsWith('Commander') && !l.startsWith('//'))
    .length;
}

export default function DeckInput({ onAnalyze, loading, onSearchPrecons, value, onChange }: DeckInputProps) {
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = value.trim();
    if (trimmed) onAnalyze(trimmed);
  };

  return (
    <div className="card deck-input">
      <h2>📋 Paste Your Deck List</h2>
      <p className="deck-input-help">
        Paste your Commander deck list below. Supports most common formats including MTGO, Arena,
        Moxfield, and Archidekt exports. The commander is auto-detected from the first card, last
        card (if separated by a blank line), or a "Commander:" header. Sideboard cards are skipped.
        The deck should have exactly 100 cards.
      </p>

      <form onSubmit={handleSubmit}>
        <textarea
          aria-label="Deck list"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={`1 Edgar Markov\n\n1 Sol Ring\n1 Arcane Signet\n1 Command Tower\n...`}
          rows={14}
          disabled={loading}
        />

        <div className="deck-input-actions">
          <button
            type="button"
            className="btn-secondary"
            onClick={() => onChange(SAMPLE_DECK)}
            disabled={loading}
          >
            Load Sample Deck
          </button>

          <button
            type="button"
            className="btn-secondary precon-button"
            onClick={onSearchPrecons}
            disabled={loading}
          >
            🏛️ Browse Precons
          </button>

          <button
            type="button"
            className="btn-secondary"
            onClick={() => onChange('')}
            disabled={loading || !value}
          >
            Clear
          </button>

          <div className="spacer" />

          <span className="card-count" aria-live="polite">
            {value.trim() ? countDeckLines(value) : 0} lines
          </span>

          <button
            type="submit"
            className="btn-primary"
            disabled={loading || !value.trim()}
          >
            {loading ? 'Analyzing...' : '⚡ Analyze Deck'}
          </button>
        </div>
      </form>
    </div>
  );
}
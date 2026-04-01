import { DeckStrategy as DeckStrategyType } from '../types/deck';
import './DeckStrategy.css';

interface DeckStrategyProps {
  strategy: DeckStrategyType;
}

function getArchetypeIcon(name: string): string {
  const lower = name.toLowerCase();
  if (lower.includes('token')) return '🪙';
  if (lower.includes('tribal')) return '🐺';
  if (lower.includes('voltron')) return '⚔️';
  if (lower.includes('aristocrat') || lower.includes('sacrifice')) return '💀';
  if (lower.includes('reanimate')) return '⚰️';
  if (lower.includes('spell')) return '🔮';
  if (lower.includes('counter')) return '⬆️';
  if (lower.includes('stax') || lower.includes('control')) return '🔒';
  if (lower.includes('mill')) return '📚';
  if (lower.includes('lifegain')) return '❤️';
  if (lower.includes('graveyard')) return '🪦';
  if (lower.includes('combo')) return '♾️';
  if (lower.includes('aggro')) return '⚡';
  return '🎯';
}

export default function DeckStrategy({ strategy }: DeckStrategyProps) {
  if (!strategy || !strategy.tags || strategy.tags.length === 0) {
    return (
      <div className="card deck-strategy">
        <h3>🎯 Deck Strategy</h3>
        <p className="strategy-summary">
          <strong>{strategy?.primaryArchetype || 'Midrange / Goodstuff'}</strong> — {strategy?.summary || 'General value strategy.'}
        </p>
      </div>
    );
  }

  return (
    <div className="card deck-strategy">
      <h3>🎯 Deck Strategy</h3>
      <p className="strategy-summary">{strategy.summary}</p>

      <div className="strategy-tags">
        {strategy.tags.map((tag) => (
          <div key={tag.name} className="strategy-tag">
            <div className="tag-header">
              <span className="tag-icon">{getArchetypeIcon(tag.name)}</span>
              <span className="tag-name">{tag.name}</span>
              <span className="tag-count">{tag.cardCount} cards ({tag.percentage}%)</span>
            </div>
            <div className="tag-bar">
              <div
                className="tag-bar-fill"
                style={{ width: `${Math.min(tag.percentage, 100)}%` }}
              />
            </div>
            <div className="tag-examples">
              {tag.exampleCards.map((card) => (
                <span key={card} className="tag-example-card">{card}</span>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

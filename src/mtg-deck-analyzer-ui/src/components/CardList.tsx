import { useState, useCallback } from 'react';
import type { CardInfo } from '../types/deck';
import './CardList.css';

interface CardListProps {
  cards: CardInfo[];
}

type SortKey = 'impact' | 'playability' | 'cmc' | 'name' | 'powerScore';

interface CardTag {
  label: string;
  className: string;
}

/** Derives display tags for a card — defined outside the component to avoid re-creation on every render. */
function getCardTags(card: CardInfo): CardTag[] {
  const tags: CardTag[] = [];
  if (card.isCommander) tags.push({ label: 'Commander', className: 'tag-commander' });
  if (card.isGameChanger) tags.push({ label: 'Game Changer', className: 'tag-gc' });
  if (card.isFastMana) tags.push({ label: 'Fast Mana', className: 'tag-fast' });
  if (card.isTutor) tags.push({ label: 'Tutor', className: 'tag-tutor' });
  if (card.isInfiniteComboPiece) tags.push({ label: 'Combo', className: 'tag-combo' });
  if (card.isExtraTurn) tags.push({ label: 'Extra Turn', className: 'tag-extra' });
  if (card.isMassLandDenial) tags.push({ label: 'MLD', className: 'tag-mld' });
  return tags;
}

/** Returns the sort comparator value for two cards. */
function compareCards(a: CardInfo, b: CardInfo, key: SortKey): number {
  switch (key) {
    case 'impact': return a.impact - b.impact;
    case 'playability': return a.playability - b.playability;
    case 'cmc': return a.cmc - b.cmc;
    case 'name': return a.name.localeCompare(b.name);
    case 'powerScore': return a.powerScore - b.powerScore;
  }
}

/** Column header button with sort indicator. */
function SortHeader({
  label,
  sortKey,
  currentSort,
  ascending,
  onSort,
  numeric,
}: {
  label: string;
  sortKey: SortKey;
  currentSort: SortKey;
  ascending: boolean;
  onSort: (key: SortKey) => void;
  numeric?: boolean;
}) {
  const active = currentSort === sortKey;
  return (
    <th
      onClick={() => onSort(sortKey)}
      className={`sortable${numeric ? ' num' : ''}`}
      aria-sort={active ? (ascending ? 'ascending' : 'descending') : 'none'}
    >
      {label} {active ? (ascending ? '↑' : '↓') : ''}
    </th>
  );
}

export default function CardList({ cards }: CardListProps) {
  const [sortBy, setSortBy] = useState<SortKey>('impact');
  const [sortAsc, setSortAsc] = useState(false);
  const [filter, setFilter] = useState('');
  const [focusedCard, setFocusedCard] = useState<CardInfo | null>(null);

  const handleSort = useCallback((key: SortKey) => {
    setSortBy((prev) => {
      if (prev === key) setSortAsc((asc) => !asc);
      else setSortAsc(false);
      return key;
    });
  }, []);

  const filtered = cards.filter(
    (c) =>
      c.name.toLowerCase().includes(filter.toLowerCase()) ||
      c.typeLine.toLowerCase().includes(filter.toLowerCase()),
  );

  const sorted = [...filtered].sort((a, b) => {
    const diff = compareCards(a, b, sortBy);
    return sortAsc ? diff : -diff;
  });

  return (
    <div className="card card-list-section">
      <div className="card-list-header">
        <h3>🃏 Card Analysis ({cards.length} cards)</h3>
        <input
          type="search"
          placeholder="Filter cards..."
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          className="card-filter"
          aria-label="Filter card list"
        />
      </div>

      <div className="card-list-table-wrap">
        <table className="card-list-table" aria-label="Deck card analysis">
          <thead>
            <tr>
              <SortHeader label="Card" sortKey="name" currentSort={sortBy} ascending={sortAsc} onSort={handleSort} />
              <th>Type</th>
              <SortHeader label="CMC" sortKey="cmc" currentSort={sortBy} ascending={sortAsc} onSort={handleSort} numeric />
              <SortHeader label="Playability" sortKey="playability" currentSort={sortBy} ascending={sortAsc} onSort={handleSort} numeric />
              <SortHeader label="Impact" sortKey="impact" currentSort={sortBy} ascending={sortAsc} onSort={handleSort} numeric />
              <SortHeader label="Power" sortKey="powerScore" currentSort={sortBy} ascending={sortAsc} onSort={handleSort} numeric />
              <th>Tags</th>
            </tr>
          </thead>
          <tbody>
            {sorted.map((card, idx) => {
              const tags = getCardTags(card);
              return (
                <tr
                  key={`${card.name}-${idx}`}
                  className={card.isCommander ? 'commander-row' : ''}
                  // Support both mouse hover and keyboard focus for the card image preview
                  onMouseEnter={() => setFocusedCard(card)}
                  onMouseLeave={() => setFocusedCard(null)}
                  onFocus={() => setFocusedCard(card)}
                  onBlur={() => setFocusedCard(null)}
                  tabIndex={0}
                >
                  <td className="card-name-cell">
                    {card.scryfallUri ? (
                      <a href={card.scryfallUri} target="_blank" rel="noreferrer">
                        {card.name}
                      </a>
                    ) : (
                      card.name
                    )}
                  </td>
                  <td className="type-cell">{card.typeLine}</td>
                  <td className="num">{card.cmc}</td>
                  <td className="num">{card.playability.toFixed(1)}%</td>
                  <td className="num">{card.impact.toFixed(1)}</td>
                  <td className="num">{card.powerScore.toFixed(1)}</td>
                  <td>
                    <div className="card-tags">
                      {tags.map((tag) => (
                        <span key={tag.label} className={`card-tag ${tag.className}`}>
                          {tag.label}
                        </span>
                      ))}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {focusedCard?.imageUri && (
        <div className="card-preview" aria-hidden="true">
          <img src={focusedCard.imageUri} alt={focusedCard.name} />
        </div>
      )}
    </div>
  );
}
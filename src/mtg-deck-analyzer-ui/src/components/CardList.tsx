import { useState } from 'react';
import { CardInfo } from '../types/deck';
import './CardList.css';

interface CardListProps {
  cards: CardInfo[];
}

type SortKey = 'impact' | 'playability' | 'cmc' | 'name' | 'powerScore';

export default function CardList({ cards }: CardListProps) {
  const [sortBy, setSortBy] = useState<SortKey>('impact');
  const [sortAsc, setSortAsc] = useState(false);
  const [filter, setFilter] = useState('');
  const [hoveredCard, setHoveredCard] = useState<CardInfo | null>(null);

  const handleSort = (key: SortKey) => {
    if (sortBy === key) {
      setSortAsc(!sortAsc);
    } else {
      setSortBy(key);
      setSortAsc(false);
    }
  };

  const filtered = cards.filter((c) =>
    c.name.toLowerCase().includes(filter.toLowerCase()) ||
    c.typeLine.toLowerCase().includes(filter.toLowerCase())
  );

  const sorted = [...filtered].sort((a, b) => {
    let diff = 0;
    switch (sortBy) {
      case 'impact': diff = a.impact - b.impact; break;
      case 'playability': diff = a.playability - b.playability; break;
      case 'cmc': diff = a.cmc - b.cmc; break;
      case 'name': diff = a.name.localeCompare(b.name); break;
      case 'powerScore': diff = a.powerScore - b.powerScore; break;
    }
    return sortAsc ? diff : -diff;
  });

  const getTags = (card: CardInfo) => {
    const tags: { label: string; className: string }[] = [];
    if (card.isCommander) tags.push({ label: 'Commander', className: 'tag-commander' });
    if (card.isGameChanger) tags.push({ label: 'Game Changer', className: 'tag-gc' });
    if (card.isFastMana) tags.push({ label: 'Fast Mana', className: 'tag-fast' });
    if (card.isTutor) tags.push({ label: 'Tutor', className: 'tag-tutor' });
    if (card.isInfiniteComboPiece) tags.push({ label: 'Combo', className: 'tag-combo' });
    if (card.isExtraTurn) tags.push({ label: 'Extra Turn', className: 'tag-extra' });
    if (card.isMassLandDenial) tags.push({ label: 'MLD', className: 'tag-mld' });
    return tags;
  };

  return (
    <div className="card card-list-section">
      <div className="card-list-header">
        <h3>🃏 Card Analysis ({cards.length} cards)</h3>
        <input
          type="text"
          placeholder="Filter cards..."
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          className="card-filter"
        />
      </div>

      <div className="card-list-table-wrap">
        <table className="card-list-table">
          <thead>
            <tr>
              <th onClick={() => handleSort('name')} className="sortable">
                Card {sortBy === 'name' ? (sortAsc ? '↑' : '↓') : ''}
              </th>
              <th>Type</th>
              <th onClick={() => handleSort('cmc')} className="sortable num">
                CMC {sortBy === 'cmc' ? (sortAsc ? '↑' : '↓') : ''}
              </th>
              <th onClick={() => handleSort('playability')} className="sortable num">
                Playability {sortBy === 'playability' ? (sortAsc ? '↑' : '↓') : ''}
              </th>
              <th onClick={() => handleSort('impact')} className="sortable num">
                Impact {sortBy === 'impact' ? (sortAsc ? '↑' : '↓') : ''}
              </th>
              <th onClick={() => handleSort('powerScore')} className="sortable num">
                Power {sortBy === 'powerScore' ? (sortAsc ? '↑' : '↓') : ''}
              </th>
              <th>Tags</th>
            </tr>
          </thead>
          <tbody>
            {sorted.map((card, idx) => (
              <tr
                key={`${card.name}-${idx}`}
                className={card.isCommander ? 'commander-row' : ''}
                onMouseEnter={() => setHoveredCard(card)}
                onMouseLeave={() => setHoveredCard(null)}
              >
                <td className="card-name-cell">
                  {card.scryfallUri ? (
                    <a href={card.scryfallUri} target="_blank" rel="noreferrer">{card.name}</a>
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
                    {getTags(card).map((tag) => (
                      <span key={tag.label} className={`card-tag ${tag.className}`}>{tag.label}</span>
                    ))}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {hoveredCard?.imageUri && (
        <div className="card-preview">
          <img src={hoveredCard.imageUri} alt={hoveredCard.name} />
        </div>
      )}
    </div>
  );
}

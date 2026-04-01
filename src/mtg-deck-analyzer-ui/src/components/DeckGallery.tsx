import { useState, useMemo } from 'react';
import { CardInfo } from '../types/deck';
import CardDetailModal from './CardDetailModal';
import './DeckGallery.css';
import './CardList.css';

interface DeckGalleryProps {
  cards: CardInfo[];
}

type ViewMode = 'gallery' | 'table';
type GroupBy = 'type' | 'cmc' | 'category' | 'none';
type SortKey = 'impact' | 'playability' | 'cmc' | 'name' | 'powerScore' | 'synergy' | 'priceEur';

const TYPE_ORDER = [
  'Commander',
  'Creature',
  'Instant',
  'Sorcery',
  'Artifact',
  'Enchantment',
  'Planeswalker',
  'Land',
  'Other',
];

function getCardGroup(card: CardInfo, groupBy: GroupBy): string {
  if (card.isCommander) return 'Commander';
  switch (groupBy) {
    case 'type':
      if (card.isCreature) return 'Creature';
      if (card.isInstant) return 'Instant';
      if (card.isSorcery) return 'Sorcery';
      if (card.isArtifact) return 'Artifact';
      if (card.isEnchantment) return 'Enchantment';
      if (card.isPlaneswalker) return 'Planeswalker';
      if (card.isLand) return 'Land';
      return 'Other';
    case 'cmc':
      return card.isLand ? 'Land' : `${Math.min(Math.floor(card.cmc), 7)}${card.cmc >= 7 ? '+' : ''} CMC`;
    case 'category': {
      if (card.isTutor) return 'Tutor';
      if (card.isFastMana) return 'Fast Mana';
      if (card.isCounterspell) return 'Counterspell';
      if (card.isBoardWipe) return 'Board Wipe';
      if (card.isRemoval) return 'Removal';
      if (card.isCardDraw) return 'Card Draw';
      if (card.isRamp) return 'Ramp';
      if (card.isLand) return 'Land';
      if (card.isCreature) return 'Creature';
      return 'Other';
    }
    default:
      return 'All Cards';
  }
}

function getGroupIcon(group: string): string {
  const icons: Record<string, string> = {
    Commander: '👑',
    Creature: '🐉',
    Instant: '⚡',
    Sorcery: '🔮',
    Artifact: '⚙️',
    Enchantment: '✨',
    Planeswalker: '🌟',
    Land: '🏔️',
    Tutor: '🔍',
    'Fast Mana': '💎',
    Counterspell: '🛡️',
    'Board Wipe': '💥',
    Removal: '🎯',
    'Card Draw': '📚',
    Ramp: '🌱',
    Other: '📦',
  };
  return icons[group] || '📦';
}

function getTags(card: CardInfo): { label: string; className: string }[] {
  const tags: { label: string; className: string }[] = [];
  if (card.isCommander) tags.push({ label: 'Commander', className: 'tag-commander' });
  if (card.isGameChanger) tags.push({ label: 'Game Changer', className: 'tag-gc' });
  if (card.isFastMana) tags.push({ label: 'Fast Mana', className: 'tag-fast' });
  if (card.isTutor) tags.push({ label: 'Tutor', className: 'tag-tutor' });
  if (card.isInfiniteComboPiece) tags.push({ label: 'Combo', className: 'tag-combo' });
  if (card.isExtraTurn) tags.push({ label: 'Extra Turn', className: 'tag-extra' });
  if (card.isMassLandDenial) tags.push({ label: 'MLD', className: 'tag-mld' });
  return tags;
}

export default function DeckGallery({ cards }: DeckGalleryProps) {
  const [viewMode, setViewMode] = useState<ViewMode>('gallery');
  const [groupBy, setGroupBy] = useState<GroupBy>('type');
  const [sortBy, setSortBy] = useState<SortKey>('impact');
  const [sortAsc, setSortAsc] = useState(false);
  const [filter, setFilter] = useState('');
  const [hoveredCard, setHoveredCard] = useState<CardInfo | null>(null);
  const [mousePos, setMousePos] = useState({ x: 0, y: 0 });
  const [selectedCard, setSelectedCard] = useState<CardInfo | null>(null);

  const filtered = useMemo(() =>
    cards.filter((c) =>
      c.name.toLowerCase().includes(filter.toLowerCase()) ||
      c.typeLine.toLowerCase().includes(filter.toLowerCase())
    ), [cards, filter]);

  const sorted = useMemo(() =>
    [...filtered].sort((a, b) => {
      // Commander always first
      if (a.isCommander && !b.isCommander) return -1;
      if (!a.isCommander && b.isCommander) return 1;
      let diff = 0;
      switch (sortBy) {
        case 'impact': diff = a.impact - b.impact; break;
        case 'playability': diff = a.playability - b.playability; break;
        case 'cmc': diff = a.cmc - b.cmc; break;
        case 'name': diff = a.name.localeCompare(b.name); break;
        case 'powerScore': diff = a.powerScore - b.powerScore; break;
        case 'synergy': diff = a.synergy - b.synergy; break;
      }
      return sortAsc ? diff : -diff;
    }), [filtered, sortBy, sortAsc]);

  const groups = useMemo(() => {
    const map = new Map<string, CardInfo[]>();
    for (const card of sorted) {
      const group = getCardGroup(card, groupBy);
      if (!map.has(group)) map.set(group, []);
      map.get(group)!.push(card);
    }
    // Sort groups by predefined order for type, otherwise alphabetically
    const entries = [...map.entries()];
    if (groupBy === 'type') {
      entries.sort((a, b) => TYPE_ORDER.indexOf(a[0]) - TYPE_ORDER.indexOf(b[0]));
    }
    return entries;
  }, [sorted, groupBy]);

  const handleSort = (key: SortKey) => {
    if (sortBy === key) setSortAsc(!sortAsc);
    else { setSortBy(key); setSortAsc(false); }
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    setMousePos({ x: e.clientX, y: e.clientY });
  };

  return (
    <div className="card deck-gallery-section">
      <div className="deck-gallery-header">
        <h3>🃏 Deck View ({cards.length} cards)</h3>
        <div className="deck-gallery-controls">
          <input
            type="text"
            placeholder="Filter cards..."
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            className="card-filter"
          />
          <div className="view-toggle">
            <button
              className={`toggle-btn ${viewMode === 'gallery' ? 'active' : ''}`}
              onClick={() => setViewMode('gallery')}
              title="Gallery View"
            >🖼️</button>
            <button
              className={`toggle-btn ${viewMode === 'table' ? 'active' : ''}`}
              onClick={() => setViewMode('table')}
              title="Table View"
            >📋</button>
          </div>
          <select
            value={groupBy}
            onChange={(e) => setGroupBy(e.target.value as GroupBy)}
            className="gallery-select"
          >
            <option value="type">Group by Type</option>
            <option value="cmc">Group by CMC</option>
            <option value="category">Group by Category</option>
            <option value="none">No Grouping</option>
          </select>
          <select
            value={`${sortBy}-${sortAsc ? 'asc' : 'desc'}`}
            onChange={(e) => {
              const [key, dir] = e.target.value.split('-');
              setSortBy(key as SortKey);
              setSortAsc(dir === 'asc');
            }}
            className="gallery-select"
          >
            <option value="impact-desc">Impact ↓</option>
            <option value="impact-asc">Impact ↑</option>
            <option value="cmc-asc">CMC ↑</option>
            <option value="cmc-desc">CMC ↓</option>
            <option value="name-asc">Name A-Z</option>
            <option value="name-desc">Name Z-A</option>
            <option value="playability-desc">Playability ↓</option>
            <option value="powerScore-desc">Power ↓</option>
            <option value="synergy-desc">Synergy ↓</option>
            <option value="synergy-asc">Synergy ↑</option>
          </select>
        </div>
      </div>

      {viewMode === 'gallery' ? (
        <div className="deck-gallery-groups">
          {groups.map(([group, groupCards]) => (
            <div key={group} className="gallery-group">
              <div className="gallery-group-header">
                <span className="gallery-group-icon">{getGroupIcon(group)}</span>
                <span className="gallery-group-name">{group}</span>
                <span className="gallery-group-count">({groupCards.length})</span>
              </div>
              <div className="gallery-grid">
                {groupCards.map((card, idx) => (
                  <div
                    key={`${card.name}-${idx}`}
                    className={`gallery-card ${card.isCommander ? 'gallery-commander' : ''}`}
                    onMouseEnter={() => setHoveredCard(card)}
                    onMouseMove={handleMouseMove}
                    onMouseLeave={() => setHoveredCard(null)}
                    onClick={() => setSelectedCard(card)}
                  >
                    {card.imageUri ? (
                      <img
                        src={card.imageUri}
                        alt={card.name}
                        className="gallery-card-img"
                        loading="lazy"
                      />
                    ) : (
                      <div className="gallery-card-placeholder">
                        <span className="placeholder-name">{card.name}</span>
                      </div>
                    )}
                    {card.isGameChanger && (
                      <div className="gallery-badge gallery-badge-gc" title="Game Changer">GC</div>
                    )}
                    {card.isCommander && (
                      <div className="gallery-badge gallery-badge-cmdr" title="Commander">👑</div>
                    )}
                    {card.isInfiniteComboPiece && !card.isCommander && (
                      <div className="gallery-badge gallery-badge-combo" title="Combo Piece">♾️</div>
                    )}
                    {card.isTutor && !card.isCommander && (
                      <div className="gallery-badge gallery-badge-tutor" title="Tutor">🔍</div>
                    )}
                    <div className="gallery-card-stats">
                      <div className="stats-row">
                        <span title="Impact"><span className="stat-icon">💥</span><span className="stat-val">{card.impact.toFixed(1)}</span></span>
                        <span title="Synergy"><span className="stat-icon">🔗</span><span className="stat-val">{card.synergy.toFixed(0)}%</span></span>
                        <span title="Playability"><span className="stat-icon">🕹️</span><span className="stat-val">{card.playability.toFixed(0)}%</span></span>
                        {card.cmc > 0 && <span title="CMC"><span className="stat-icon">💧</span><span className="stat-val">{card.cmc}</span></span>}
                      </div>
                      {card.priceEur > 0 && (
                        <div className="stats-row stats-row-price">
                          <span title="Cardmarket EUR" className="gallery-price-eur">
                            <span className="stat-icon">€</span>
                            <span className="stat-val">{card.priceEur.toFixed(2)}</span>
                          </span>
                        </div>
                      )}
                    </div>
                    <div className="gallery-card-overlay">
                      <div className="overlay-name">{card.name}</div>
                      {getTags(card).length > 0 && (
                        <div className="overlay-tags">
                          {getTags(card).map((tag) => (
                            <span key={tag.label} className={`card-tag ${tag.className}`}>{tag.label}</span>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      ) : (
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
                <th onClick={() => handleSort('synergy')} className="sortable num">
                  Synergy {sortBy === 'synergy' ? (sortAsc ? '↑' : '↓') : ''}
                </th>
                <th onClick={() => handleSort('powerScore')} className="sortable num">
                  Power {sortBy === 'powerScore' ? (sortAsc ? '↑' : '↓') : ''}
                </th>
                <th onClick={() => handleSort('priceEur')} className="sortable num">
                  € Price {sortBy === 'priceEur' ? (sortAsc ? '↑' : '↓') : ''}
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
                  onClick={() => setSelectedCard(card)}
                  style={{ cursor: 'pointer' }}
                >
                  <td className="card-name-cell">
                    {card.scryfallUri ? (
                      <a href={card.scryfallUri} target="_blank" rel="noreferrer">{card.name}</a>
                    ) : card.name}
                  </td>
                  <td className="type-cell">{card.typeLine}</td>
                  <td className="num">{card.cmc}</td>
                  <td className="num">{card.playability.toFixed(1)}%</td>
                  <td className="num">{card.impact.toFixed(1)}</td>
                  <td className="num">{card.synergy.toFixed(0)}%</td>
                  <td className="num">{card.powerScore.toFixed(1)}</td>
                  <td className="num price-eur-cell">{card.priceEur > 0 ? `€${card.priceEur.toFixed(2)}` : '—'}</td>
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
            <tfoot>
              <tr className="deck-total-row">
                <td colSpan={6} style={{ textAlign: 'right', fontWeight: 'bold' }}>Deck Total:</td>
                <td className="num" style={{ fontWeight: 'bold' }}></td>
                <td className="num price-eur-cell" style={{ fontWeight: 'bold', color: '#5ba3d9' }}>
                  €{sorted.reduce((sum, c) => sum + c.priceEur * c.quantity, 0).toFixed(2)}
                </td>
                <td></td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}

      {hoveredCard?.imageUri && viewMode === 'table' && (
        <div
          className="card-preview-floating"
          style={{
            top: Math.min(mousePos.y - 150, window.innerHeight - 420),
            left: mousePos.x + 20,
          }}
        >
          <img src={hoveredCard.imageUri} alt={hoveredCard.name} />
        </div>
      )}

      {selectedCard && (
        <CardDetailModal card={selectedCard} onClose={() => setSelectedCard(null)} />
      )}
    </div>
  );
}

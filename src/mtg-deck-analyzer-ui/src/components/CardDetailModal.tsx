import type { ReactElement } from 'react';
import { CardInfo } from '../types/deck';
import './CardDetailModal.css';

interface CardDetailModalProps {
  card: CardInfo;
  onClose: () => void;
}

const MANA_SYMBOLS: Record<string, string> = {
  W: '🌞', U: '💧', B: '💀', R: '🔥', G: '🌲',
};

function formatManaCost(manaCost: string): ReactElement[] {
  if (!manaCost) return [];
  const symbols = manaCost.match(/\{[^}]+\}/g) || [];
  return symbols.map((sym, i) => {
    const inner = sym.replace(/[{}]/g, '');
    const imgUrl = `https://svgs.scryfall.io/card-symbols/${inner.replace('/', '')}.svg`;
    return (
      <img
        key={i}
        src={imgUrl}
        alt={inner}
        className="modal-mana-symbol"
        title={inner}
      />
    );
  });
}

function formatOracleText(text: string): ReactElement[] {
  if (!text) return [];
  return text.split('\n').map((line, i) => {
    // Replace mana symbols {X} with inline images
    const parts = line.split(/(\{[^}]+\})/g);
    return (
      <p key={i} className="oracle-line">
        {parts.map((part, j) => {
          const match = part.match(/^\{([^}]+)\}$/);
          if (match) {
            const sym = match[1].replace('/', '');
            return (
              <img
                key={j}
                src={`https://svgs.scryfall.io/card-symbols/${sym}.svg`}
                alt={match[1]}
                className="oracle-mana-symbol"
                title={match[1]}
              />
            );
          }
          return <span key={j}>{part}</span>;
        })}
      </p>
    );
  });
}

function getRarityClass(rarity: string): string {
  switch (rarity?.toLowerCase()) {
    case 'mythic': return 'rarity-mythic';
    case 'rare': return 'rarity-rare';
    case 'uncommon': return 'rarity-uncommon';
    default: return 'rarity-common';
  }
}

export default function CardDetailModal({ card, onClose }: CardDetailModalProps) {
  const tags = [];
  if (card.isCommander) tags.push({ label: 'Commander', cls: 'tag-commander' });
  if (card.isGameChanger) tags.push({ label: 'Game Changer', cls: 'tag-gc' });
  if (card.isFastMana) tags.push({ label: 'Fast Mana', cls: 'tag-fast' });
  if (card.isTutor) tags.push({ label: 'Tutor', cls: 'tag-tutor' });
  if (card.isInfiniteComboPiece) tags.push({ label: 'Combo Piece', cls: 'tag-combo' });
  if (card.isExtraTurn) tags.push({ label: 'Extra Turn', cls: 'tag-extra' });
  if (card.isMassLandDenial) tags.push({ label: 'Mass Land Denial', cls: 'tag-mld' });
  if (card.isCounterspell) tags.push({ label: 'Counterspell', cls: 'tag-counter' });
  if (card.isBoardWipe) tags.push({ label: 'Board Wipe', cls: 'tag-wipe' });
  if (card.isRemoval) tags.push({ label: 'Removal', cls: 'tag-removal' });
  if (card.isCardDraw) tags.push({ label: 'Card Draw', cls: 'tag-draw' });
  if (card.isRamp) tags.push({ label: 'Ramp', cls: 'tag-ramp' });

  const colorNames: Record<string, string> = {
    W: 'White', U: 'Blue', B: 'Black', R: 'Red', G: 'Green',
  };

  return (
    <div className="card-modal-backdrop" onClick={onClose}>
      <div className="card-modal" onClick={(e) => e.stopPropagation()}>
        <button className="modal-close" onClick={onClose} title="Close">✕</button>

        <div className="modal-content">
          {/* Left: Card Image */}
          <div className="modal-image-section">
            {card.imageUri ? (
              <img src={card.imageUri} alt={card.name} className="modal-card-img" />
            ) : (
              <div className="modal-card-placeholder">{card.name}</div>
            )}
          </div>

          {/* Right: Card Details */}
          <div className="modal-details-section">
            <div className="modal-header">
              <h2 className="modal-card-name">{card.name}</h2>
              <div className="modal-mana-cost">{formatManaCost(card.manaCost)}</div>
            </div>

            <div className="modal-type-line">
              <span className={`modal-rarity ${getRarityClass(card.rarity)}`}>
                {card.rarity?.charAt(0).toUpperCase()}{card.rarity?.slice(1)}
              </span>
              <span className="modal-type">{card.typeLine}</span>
            </div>

            {/* Tags */}
            {tags.length > 0 && (
              <div className="modal-tags">
                {tags.map((t) => (
                  <span key={t.label} className={`card-tag ${t.cls}`}>{t.label}</span>
                ))}
              </div>
            )}

            {/* Oracle Text */}
            {card.oracleText && (
              <div className="modal-oracle-box">
                {formatOracleText(card.oracleText)}
              </div>
            )}

            {/* Keywords */}
            {card.keywords && card.keywords.length > 0 && (
              <div className="modal-keywords">
                {card.keywords.map((kw) => (
                  <span key={kw} className="modal-keyword">{kw}</span>
                ))}
              </div>
            )}

            {/* Statistics Grid */}
            <div className="modal-stats-grid">
              <div className="modal-stat">
                <span className="stat-label">Impact</span>
                <span className="stat-value stat-impact">{card.impact.toFixed(1)}</span>
              </div>
              <div className="modal-stat">
                <span className="stat-label">Playability</span>
                <span className="stat-value stat-playability">{card.playability.toFixed(1)}%</span>
              </div>
              <div className="modal-stat">
                <span className="stat-label">Synergy</span>
                <span className="stat-value stat-synergy">{card.synergy.toFixed(0)}%</span>
              </div>
              <div className="modal-stat">
                <span className="stat-label">Power</span>
                <span className="stat-value stat-power">{card.powerScore.toFixed(1)}</span>
              </div>
              <div className="modal-stat">
                <span className="stat-label">CMC</span>
                <span className="stat-value">{card.cmc}</span>
              </div>
              <div className="modal-stat">
                <span className="stat-label">EDHREC Rank</span>
                <span className="stat-value">
                  {card.edhrecRank > 0 ? `#${card.edhrecRank.toLocaleString()}` : '—'}
                </span>
              </div>
            </div>

            {/* Color Identity */}
            {card.colorIdentity && card.colorIdentity.length > 0 && (
              <div className="modal-colors">
                <span className="modal-section-label">Color Identity:</span>
                <div className="modal-color-pips">
                  {card.colorIdentity.map((c) => (
                    <span key={c} className={`color-pip pip-${c.toLowerCase()}`} title={colorNames[c] || c}>
                      {MANA_SYMBOLS[c] || c}
                    </span>
                  ))}
                </div>
              </div>
            )}

            {/* Price */}
            {(card.priceEur > 0 || card.price > 0) && (
              <div className="modal-price">
                <span className="modal-section-label">Price:</span>
                {card.priceEur > 0 && <span className="price-value price-eur">€{card.priceEur.toFixed(2)}</span>}
                {card.price > 0 && <span className="price-value price-usd">${card.price.toFixed(2)}</span>}
              </div>
            )}

            {/* Links */}
            <div className="modal-links">
              {card.scryfallUri && (
                <a
                  href={card.scryfallUri}
                  target="_blank"
                  rel="noreferrer"
                  className="modal-link scryfall-link"
                >
                  View on Scryfall ↗
                </a>
              )}
              <a
                href={`https://edhrec.com/cards/${card.name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '')}`}
                target="_blank"
                rel="noreferrer"
                className="modal-link edhrec-link"
              >
                View on EDHREC ↗
              </a>
              <a
                href={card.cardmarketUri || `https://www.cardmarket.com/en/Magic/Cards/${card.name.replace(/ /g, '-').replace(/[^a-zA-Z0-9-]/g, '')}`}
                target="_blank"
                rel="noreferrer"
                className="modal-link cardmarket-link"
              >
                View on Cardmarket ↗
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

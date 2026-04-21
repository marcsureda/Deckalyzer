import { useState } from 'react';
import type { DeckAnalysisResult } from '../types/deck';
import { getColorSymbol } from '../constants/colors';
import InfoButton from './InfoButton';
import './PowerOverview.css';

interface PowerOverviewProps {
  result: DeckAnalysisResult;
}

/** Maps power level to a CSS custom-property color token. */
function getPowerColor(level: number): string {
  if (level <= 3) return 'var(--accent-green)';
  if (level <= 5) return 'var(--accent-blue)';
  if (level <= 7) return 'var(--accent-yellow)';
  if (level <= 8.5) return 'var(--accent-orange)';
  return 'var(--accent-red)';
}

interface MetricExplanation {
  title: string;
  description: string;
}

const METRIC_EXPLANATIONS: Record<string, MetricExplanation> = {
  powerLevel: {
    title: 'Power Level',
    description:
      "Rates your deck from 1 (very casual) to 10 (competitive cEDH). Calculated using a sigmoid curve on the average card impact, adjusted by efficiency and playability. " +
      "Impact is derived from each card's EDHREC rank — how widely it is played across all decks. " +
      "A precon typically scores 3–4, a tuned deck 5–6, an optimized one 7–8, and cEDH decks 9+.",
  },
  tippingPoint: {
    title: 'Tipping Point',
    description:
      'The mana value (CMC) at which the most impactful cards in your deck concentrate. ' +
      'A low tipping point (2–3) means your powerful plays come early. A high tipping point (5+) means you need time to set up. ' +
      'Aggressive decks want 2–3, midrange 3–4, and control 4–5.',
  },
  efficiency: {
    title: 'Efficiency',
    description:
      'Measures how much impact your deck delivers per mana spent. Uses an exponential decay formula on the impact-weighted average CMC: ' +
      'the lower your average CMC while maintaining high-impact cards, the higher the efficiency. ' +
      'Score 8+ = very lean and efficient, 5–7 = average, below 5 = high curve and potentially slow.',
  },
  totalImpact: {
    title: 'Total Impact',
    description:
      "The sum of all individual card impact scores across the deck. Each card's impact is based on its EDHREC rank " +
      '(popularity across all Commander decks). Sol Ring (rank ~1) ≈ 20 impact, an average card (rank ~5000) ≈ 3.4. ' +
      'Higher total impact means more universally powerful cards. Typical range: 300–600.',
  },
  score: {
    title: 'Score',
    description:
      "A composite score from 0 to 1000 that blends power level with deck quality factors (efficiency and playability). " +
      "Think of it as your deck's \"overall grade\". 800+ is exceptional, 500–700 is solid, below 400 is casual.",
  },
  playability: {
    title: 'Average Playability',
    description:
      'Based on EDHREC rank — the percentage of Commander decks that include each card. ' +
      'A card at rank 1000 has ~62% playability, rank 3000 ≈ 54%, rank 10000 ≈ 46%. ' +
      'This metric averages all cards in your deck. 55%+ is strong, 45–55% is average, below 45% contains many niche picks.',
  },
  deckValue: {
    title: 'Deck Value (Cardmarket)',
    description:
      'Total cost of the deck using the cheapest available English print of each card on Cardmarket (EUR). ' +
      "Fetched from Scryfall's pricing data across all printings. Useful for budget planning.",
  },
};

export default function PowerOverview({ result }: PowerOverviewProps) {
  const powerPct = (result.powerLevel / 10) * 100;
  const effPct = (result.efficiency / 10) * 100;
  const [infoModal, setInfoModal] = useState<string | null>(null);

  const commanderImages =
    result.commanderImageUris?.length > 0
      ? result.commanderImageUris
      : result.commanderImageUri
        ? [result.commanderImageUri]
        : [];

  const commanderLabel =
    result.commanderNames?.length > 1
      ? result.commanderNames.join(' + ')
      : result.commanderName || 'Unknown Commander';

  return (
    <div className="card power-overview">
      <div className="power-overview-header">
        <div className="commander-images">
          {commanderImages.map((uri, idx) => (
            <img
              key={idx}
              src={uri}
              alt={result.commanderNames?.[idx] ?? result.commanderName}
              className={`commander-image ${commanderImages.length > 1 ? 'partner' : ''}`}
            />
          ))}
        </div>
        <div className="commander-info">
          <h2>{commanderLabel}</h2>
          <div className="color-identity" aria-label="Color identity">
            {result.colorIdentity.map((c) => (
              <span key={c} className="color-pip" title={c}>
                {getColorSymbol(c)}
              </span>
            ))}
          </div>
          <span className="total-cards">{result.totalCards} cards</span>
        </div>
      </div>

      <div className="metrics-grid">
        <div className="metric">
          <div className="metric-label">
            ⚡ Power Level{' '}
            <InfoButton label="Power Level" onClick={() => setInfoModal('powerLevel')} />
          </div>
          <div className="metric-value" style={{ color: getPowerColor(result.powerLevel) }}>
            {result.powerLevel.toFixed(2)}
            <span className="metric-max"> / 10</span>
          </div>
          <div className="metric-bar" role="meter" aria-valuenow={result.powerLevel} aria-valuemin={0} aria-valuemax={10}>
            <div className="metric-bar-fill" style={{ width: `${powerPct}%`, background: getPowerColor(result.powerLevel) }} />
          </div>
        </div>

        <div className="metric">
          <div className="metric-label">
            ⚖️ Tipping Point{' '}
            <InfoButton label="Tipping Point" onClick={() => setInfoModal('tippingPoint')} />
          </div>
          <div className="metric-value">{result.tippingPoint} CMC</div>
          <div className="metric-sub">Where your deck's impact concentrates</div>
        </div>

        <div className="metric">
          <div className="metric-label">
            ⏱️ Efficiency{' '}
            <InfoButton label="Efficiency" onClick={() => setInfoModal('efficiency')} />
          </div>
          <div className="metric-value">
            {result.efficiency.toFixed(1)}
            <span className="metric-max"> / 10</span>
          </div>
          <div className="metric-bar" role="meter" aria-valuenow={result.efficiency} aria-valuemin={0} aria-valuemax={10}>
            <div className="metric-bar-fill" style={{ width: `${effPct}%`, background: 'var(--accent-blue)' }} />
          </div>
        </div>

        <div className="metric">
          <div className="metric-label">
            💥 Total Impact{' '}
            <InfoButton label="Total Impact" onClick={() => setInfoModal('totalImpact')} />
          </div>
          <div className="metric-value">{result.totalImpact.toFixed(0)}</div>
          <div className="metric-sub">Cumulative card impact score</div>
        </div>

        <div className="metric">
          <div className="metric-label">
            🎯 Score{' '}
            <InfoButton label="Score" onClick={() => setInfoModal('score')} />
          </div>
          <div className="metric-value">
            {result.score.toFixed(0)}
            <span className="metric-max"> / 1000</span>
          </div>
          <div className="metric-bar" role="meter" aria-valuenow={result.score} aria-valuemin={0} aria-valuemax={1000}>
            <div className="metric-bar-fill" style={{ width: `${result.score / 10}%`, background: 'var(--accent-purple)' }} />
          </div>
        </div>

        <div className="metric">
          <div className="metric-label">
            🕹️ Avg Playability{' '}
            <InfoButton label="Average Playability" onClick={() => setInfoModal('playability')} />
          </div>
          <div className="metric-value">{result.averagePlayability.toFixed(1)}%</div>
          <div className="metric-sub">Avg CMC: {result.averageCmc.toFixed(2)}</div>
        </div>

        <div className="metric">
          <div className="metric-label">
            💰 Deck Value (CM){' '}
            <InfoButton label="Deck Value" onClick={() => setInfoModal('deckValue')} />
          </div>
          <div className="metric-value deck-value-eur">
            €{result.deckTotalValueEur.toFixed(2)}
          </div>
          <div className="metric-sub">Cheapest English prints on Cardmarket</div>
        </div>
      </div>

      {infoModal && METRIC_EXPLANATIONS[infoModal] && (
        <div
          className="metric-modal-backdrop"
          onClick={() => setInfoModal(null)}
          role="dialog"
          aria-modal="true"
          aria-labelledby="metric-modal-title"
        >
          <div className="metric-modal" onClick={(e) => e.stopPropagation()}>
            <button
              className="metric-modal-close"
              onClick={() => setInfoModal(null)}
              aria-label="Close explanation"
            >
              ✕
            </button>
            <h3 id="metric-modal-title">{METRIC_EXPLANATIONS[infoModal].title}</h3>
            <p>{METRIC_EXPLANATIONS[infoModal].description}</p>
          </div>
        </div>
      )}
    </div>
  );
}
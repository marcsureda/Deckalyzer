import { DeckAnalysisResult } from '../types/deck';
import './BracketDisplay.css';

interface BracketDisplayProps {
  result: DeckAnalysisResult;
}

const bracketDescriptions: Record<number, string> = {
  1: 'Precons and very casual builds. No combos, no extra turns, no stax.',
  2: 'Core experience. Upgraded precons with focused strategies.',
  3: 'Strong builds with some powerful cards and possible late-game combos.',
  4: 'Optimized lists with strong synergies and efficient win conditions.',
  5: 'Competitive EDH. Maximum power, no holds barred.',
};

const bracketFullExplanations: Record<number, string> = {
  1: 'Your deck fits Bracket 1 — the most casual tier. This means it has no extra turn spells, no mass land destruction, no two-card infinite combos, and zero game-changer cards from the official WotC list. This is the precon / beginner-friendly zone.',
  2: 'Your deck fits Bracket 2 — the core Commander experience. It may contain game changers and powerful lands, but avoids extra turns, mass land denial, and two-card infinite combos. Think upgraded precons and focused casual builds.',
  3: 'Your deck fits Bracket 3 — strong but bounded. It may contain extra turn spells, a small number of game changers, or two-card combos that require significant setup (high CMC, no immediate tutoring). Powerful but not fully optimized.',
  4: 'Your deck is Bracket 4 — optimized with efficient win cons. It likely has easily-assembled two-card combos, tutors to find them, multiple game changers, and/or mass land denial. This is the "bring your A-game" tier.',
  5: 'Your deck is Bracket 5 — competitive EDH (cEDH). All restrictions are off. Fast mana, efficient tutors, early-game infinite combos, stax, and maximum power. Intended only for cEDH tables.',
};

function getBracketReasons(result: DeckAnalysisResult): string[] {
  const { bracket, bracketDetails } = result;
  const reasons: string[] = [];

  // Explain what pushed the bracket UP
  if (bracketDetails.gameChangerCount > 0) {
    reasons.push(`🏆 Contains ${bracketDetails.gameChangerCount} game changer card${bracketDetails.gameChangerCount > 1 ? 's' : ''}: ${bracketDetails.gameChangerCards.slice(0, 5).join(', ')}${bracketDetails.gameChangerCards.length > 5 ? '…' : ''}`);
    if (bracketDetails.gameChangerCount > 3) {
      reasons.push('↑ More than 3 game changers pushes the deck to Bracket 3+');
    }
  }

  if (bracketDetails.hasExtraTurns) {
    reasons.push(`⏳ Contains extra turn cards: ${bracketDetails.extraTurnCards.join(', ')}`);
    if (bracketDetails.hasChainingExtraTurns) {
      reasons.push('↑ Chaining extra turns raises the bracket further');
    }
  }

  if (bracketDetails.hasMassLandDenial) {
    reasons.push(`🚫 Contains mass land denial: ${bracketDetails.massLandDenialCards.join(', ')}`);
  }

  if (bracketDetails.hasTwoCardCombos) {
    const twoCardCount = result.combos?.filter(c => c.cards.length <= 2).length ?? 0;
    const threeCardCount = result.combos?.filter(c => c.cards.length >= 3).length ?? 0;
    const comboParts: string[] = [];
    if (twoCardCount > 0) comboParts.push(`${twoCardCount} two-card combo${twoCardCount > 1 ? 's' : ''}`);
    if (threeCardCount > 0) comboParts.push(`${threeCardCount} three-card combo${threeCardCount > 1 ? 's' : ''}`);
    reasons.push(`♾️ Contains ${comboParts.join(' and ')}: ${bracketDetails.comboCards.slice(0, 6).join(', ')}${bracketDetails.comboCards.length > 6 ? '…' : ''}`);
    if (!bracketDetails.hasOnlyLateGameCombos) {
      reasons.push('↑ Combos are efficiently accessible (low CMC or tutorable), raising the bracket');
    } else {
      reasons.push('↓ Combos are late-game only (high CMC, no tutors), keeping bracket lower');
    }
  }

  // Power-level floor adjustment
  if (result.powerLevel >= 7 && bracket >= 4) {
    reasons.push(`⚡ Power level ${result.powerLevel.toFixed(2)} enforces a minimum bracket floor of 4`);
  } else if (result.powerLevel >= 5.5 && bracket >= 3) {
    reasons.push(`⚡ Power level ${result.powerLevel.toFixed(2)} enforces a minimum bracket floor of 3`);
  }

  // If nothing pushed it up, explain why it's low
  if (reasons.length === 0) {
    reasons.push('✅ No game changers, no extra turns, no mass land denial, no two-card combos — clean for Bracket 1');
  }

  return reasons;
}

export default function BracketDisplay({ result }: BracketDisplayProps) {
  const { bracket, bracketName, bracketDetails } = result;
  const reasons = getBracketReasons(result);

  return (
    <div className="card bracket-display">
      <h3>🏆 Commander Bracket</h3>

      <div className="bracket-badges">
        {bracketDetails.requirements.map((req) => (
          <div
            key={req.bracket}
            className={`bracket-badge ${req.bracket === bracket ? 'active' : ''} ${req.passes ? 'passes' : 'fails'
              }`}
          >
            <span className="bracket-number">{req.bracket}</span>
            <span className="bracket-name">{req.name}</span>
            {req.bracket === bracket && <span className="bracket-indicator">◀</span>}
          </div>
        ))}
      </div>

      <div className="bracket-result">
        <div className="bracket-result-number">{bracket}</div>
        <div>
          <div className="bracket-result-name">{bracketName}</div>
          <div className="bracket-result-desc">
            {bracketDescriptions[bracket]}
          </div>
        </div>
      </div>

      <div className="bracket-explanation">
        <h4>📋 Why Bracket {bracket}?</h4>
        <p className="bracket-full-desc">{bracketFullExplanations[bracket]}</p>
        <div className="bracket-reasons">
          {reasons.map((reason, idx) => (
            <div key={idx} className={`bracket-reason ${reason.startsWith('↑') ? 'reason-up' : reason.startsWith('↓') ? 'reason-down' : reason.startsWith('✅') ? 'reason-pass' : ''}`}>
              {reason}
            </div>
          ))}
        </div>
      </div>

      <div className="bracket-rules">
        <h4>Requirement Tracker</h4>
        {bracketDetails.requirements
          .filter((r) => r.bracket <= 3)
          .map((req) => (
            <div key={req.bracket} className="bracket-req">
              <div className="bracket-req-header">
                Bracket {req.bracket}: {req.name}
              </div>
              {req.rules.map((rule, idx) => (
                <div key={idx} className={`bracket-rule ${rule.passes ? 'pass' : 'fail'}`}>
                  <span>{rule.passes ? '✓' : '✗'}</span>
                  {rule.description}
                </div>
              ))}
            </div>
          ))}
      </div>

      {bracketDetails.gameChangerCards.length > 0 && (
        <div className="bracket-flagged">
          <h4>🏆 Game Changers ({bracketDetails.gameChangerCount})</h4>
          <div className="flagged-list">
            {bracketDetails.gameChangerCards.map((name) => (
              <span key={name} className="flagged-tag gc">{name}</span>
            ))}
          </div>
        </div>
      )}

      {bracketDetails.comboCards.length > 0 && (
        <div className="bracket-flagged">
          <h4>♾️ Combo Pieces</h4>
          {result.combos && result.combos.length > 0 ? (
            <div className="combo-list">
              {result.combos.map((combo, idx) => (
                <a
                  key={idx}
                  href={combo.url}
                  target="_blank"
                  rel="noreferrer"
                  className="combo-link"
                  title={combo.description}
                >
                  <div className="combo-cards">
                    <span className={`combo-size-badge ${combo.cards.length >= 3 ? 'three-card' : 'two-card'}`}>
                      {combo.cards.length}c
                    </span>
                    {combo.cards.map((name) => (
                      <span key={name} className="flagged-tag combo">{name}</span>
                    ))}
                    {combo.isInfinite && combo.infiniteEffect && (
                      <span className="combo-infinite">{combo.infiniteEffect}</span>
                    )}
                    {combo.isInfinite && !combo.infiniteEffect && (
                      <span className="combo-infinite">♾️ Infinite</span>
                    )}
                    <span className="combo-arrow">↗</span>
                  </div>
                  <div className="combo-description">{combo.description}</div>
                </a>
              ))}
            </div>
          ) : (
            <div className="flagged-list">
              {bracketDetails.comboCards.map((name) => (
                <span key={name} className="flagged-tag combo">{name}</span>
              ))}
            </div>
          )}
        </div>
      )}

      {bracketDetails.extraTurnCards.length > 0 && (
        <div className="bracket-flagged">
          <h4>⏳ Extra Turn Cards</h4>
          <div className="flagged-list">
            {bracketDetails.extraTurnCards.map((name) => (
              <span key={name} className="flagged-tag extra">{name}</span>
            ))}
          </div>
        </div>
      )}

      {bracketDetails.massLandDenialCards.length > 0 && (
        <div className="bracket-flagged">
          <h4>🚫 Mass Land Denial</h4>
          <div className="flagged-list">
            {bracketDetails.massLandDenialCards.map((name) => (
              <span key={name} className="flagged-tag mld">{name}</span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

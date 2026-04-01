import { DeckAnalysisResult } from '../types/deck';
import './StrengthsWeaknesses.css';

interface StrengthsWeaknessesProps {
  result: DeckAnalysisResult;
}

export default function StrengthsWeaknesses({ result }: StrengthsWeaknessesProps) {
  return (
    <div className="card strengths-weaknesses">
      <h3>📋 Deck Assessment</h3>

      <div className="sw-section">
        <h4 className="sw-strengths-title">💪 Strengths</h4>
        <ul className="sw-list">
          {result.strengths.map((s, idx) => (
            <li key={idx} className="sw-strength">{s}</li>
          ))}
        </ul>
      </div>

      <div className="sw-section">
        <h4 className="sw-weaknesses-title">⚠️ Weaknesses</h4>
        <ul className="sw-list">
          {result.weaknesses.map((w, idx) => (
            <li key={idx} className="sw-weakness">{w}</li>
          ))}
        </ul>
      </div>
    </div>
  );
}

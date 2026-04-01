import { DeckAnalysisResult } from '../types/deck';
import './Recommendations.css';

interface RecommendationsProps {
  result: DeckAnalysisResult;
}

export default function Recommendations({ result }: RecommendationsProps) {
  return (
    <div className="recommendations">
      <div className="card rec-section">
        <h3>✂️ Cards to Consider Cutting</h3>
        <p className="rec-desc">
          These cards have the lowest impact in your deck and could be replaced with stronger options.
        </p>
        {result.cutRecommendations.length === 0 ? (
          <p className="rec-empty">No cut recommendations — your deck looks solid!</p>
        ) : (
          <div className="rec-grid">
            {result.cutRecommendations.map((rec, idx) => (
              <div key={idx} className="rec-card cut">
                {rec.imageUri && (
                  <img src={rec.imageUri} alt={rec.cardName} className="rec-card-img" />
                )}
                <div className="rec-card-info">
                  <div className="rec-card-name">{rec.cardName}</div>
                  <div className="rec-card-category">{rec.category}</div>
                  <div className="rec-card-reason">{rec.reason}</div>
                  <div className="rec-card-impact">
                    Impact: <span>{rec.estimatedImpact.toFixed(1)}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="card rec-section">
        <h3>✨ Cards to Consider Adding</h3>
        <p className="rec-desc">
          Based on your deck's weaknesses, these popular Commander staples could strengthen your build.
        </p>
        {result.addRecommendations.length === 0 ? (
          <p className="rec-empty">No add recommendations — your deck covers all bases!</p>
        ) : (
          <div className="rec-grid">
            {result.addRecommendations.map((rec, idx) => (
              <div key={idx} className="rec-card add">
                {rec.imageUri && (
                  <img src={rec.imageUri} alt={rec.cardName} className="rec-card-img" />
                )}
                <div className="rec-card-info">
                  <div className="rec-card-name">{rec.cardName}</div>
                  <div className="rec-card-category">{rec.category}</div>
                  <div className="rec-card-reason">{rec.reason}</div>
                  <div className="rec-card-impact">
                    Est. Impact: <span>+{rec.estimatedImpact.toFixed(1)}</span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

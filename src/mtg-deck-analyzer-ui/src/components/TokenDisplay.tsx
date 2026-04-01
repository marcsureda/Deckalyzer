import { TokenInfo } from '../types/deck';
import './TokenDisplay.css';

interface TokenDisplayProps {
  tokens: TokenInfo[];
}

export default function TokenDisplay({ tokens }: TokenDisplayProps) {
  if (!tokens || tokens.length === 0) return null;

  return (
    <div className="card token-display">
      <h3>🪙 Tokens Used</h3>
      <div className="token-list">
        {tokens.map((token, idx) => (
          <div key={idx} className="token-item">
            {token.imageUri ? (
              <div className="token-image-wrap">
                <img
                  src={token.imageUri}
                  alt={token.description}
                  className="token-image"
                  loading="lazy"
                />
              </div>
            ) : (
              <div className="token-image-placeholder">🪙</div>
            )}
            <div className="token-info">
              <div className="token-header">
                <span className="token-desc">{token.description}</span>
                <span className="token-count">
                  {token.producedBy.length} source{token.producedBy.length > 1 ? 's' : ''}
                </span>
              </div>
              <div className="token-sources">
                {token.producedBy.map((card) => (
                  <span key={card} className="token-source">{card}</span>
                ))}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

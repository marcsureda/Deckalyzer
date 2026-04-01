import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { ManaAnalysis } from '../types/deck';
import './ManaCurveChart.css';

interface ManaCurveChartProps {
  manaAnalysis: ManaAnalysis;
}

export default function ManaCurveChart({ manaAnalysis }: ManaCurveChartProps) {
  const curveData = Object.entries(manaAnalysis.manaCurve)
    .map(([cmc, count]) => ({
      cmc: Number(cmc) >= 8 ? '8+' : String(cmc),
      count,
    }))
    .sort((a, b) => {
      const aVal = a.cmc === '8+' ? 8 : Number(a.cmc);
      const bVal = b.cmc === '8+' ? 8 : Number(b.cmc);
      return aVal - bVal;
    });

  const colorNames: Record<string, string> = {
    W: 'White', U: 'Blue', B: 'Black', R: 'Red', G: 'Green', C: 'Colorless',
  };
  const colorHex: Record<string, string> = {
    W: '#f9faf4', U: '#0e68ab', B: '#666', R: '#d3202a', G: '#00733e', C: '#a0a0a0',
  };

  return (
    <div className="card mana-curve-chart">
      <h3>📈 Mana Curve & Analysis</h3>

      <div className="chart-container">
        <ResponsiveContainer width="100%" height={220}>
          <BarChart data={curveData}>
            <CartesianGrid strokeDasharray="3 3" stroke="#2a2a3e" />
            <XAxis dataKey="cmc" tick={{ fill: '#9090a8', fontSize: 12 }} label={{ value: 'CMC', position: 'bottom', fill: '#606078', fontSize: 11 }} />
            <YAxis tick={{ fill: '#9090a8', fontSize: 12 }} />
            <Tooltip contentStyle={{ background: '#1a1a2e', border: '1px solid #2a2a3e', borderRadius: 8 }} />
            <Bar dataKey="count" fill="#8b5cf6" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>

      <div className="mana-stats">
        <div className="mana-stat">
          <span className="mana-stat-icon">🔩</span>
          <div>
            <div className="mana-stat-label">Mana Screw</div>
            <div className="mana-stat-value">{manaAnalysis.manaScrew.toFixed(1)}%</div>
          </div>
        </div>
        <div className="mana-stat">
          <span className="mana-stat-icon">🌊</span>
          <div>
            <div className="mana-stat-label">Mana Flood</div>
            <div className="mana-stat-value">{manaAnalysis.manaFlood.toFixed(1)}%</div>
          </div>
        </div>
        <div className="mana-stat">
          <span className="mana-stat-icon">🍬</span>
          <div>
            <div className="mana-stat-label">Sweet Spot</div>
            <div className="mana-stat-value">{manaAnalysis.sweetSpot.toFixed(1)}%</div>
          </div>
        </div>
      </div>

      <h4>Color Distribution</h4>
      <div className="color-dist">
        {Object.entries(manaAnalysis.colorSymbols)
          .filter(([, count]) => count > 0)
          .map(([color, count]) => (
            <div key={color} className="color-dist-row">
              <div className="color-dist-label" style={{ color: colorHex[color] }}>
                {colorNames[color] || color}
              </div>
              <div className="color-dist-bars">
                <div className="color-dist-bar">
                  <span>Symbols</span>
                  <div className="mini-bar">
                    <div
                      className="mini-bar-fill"
                      style={{
                        width: `${Math.min(100, (count / 20) * 100)}%`,
                        background: colorHex[color],
                      }}
                    />
                  </div>
                  <span className="mini-bar-val">{count}</span>
                </div>
                <div className="color-dist-bar">
                  <span>Sources</span>
                  <div className="mini-bar">
                    <div
                      className="mini-bar-fill"
                      style={{
                        width: `${Math.min(100, ((manaAnalysis.colorProducers[color] || 0) / 20) * 100)}%`,
                        background: colorHex[color],
                        opacity: 0.6,
                      }}
                    />
                  </div>
                  <span className="mini-bar-val">{manaAnalysis.colorProducers[color] || 0}</span>
                </div>
              </div>
            </div>
          ))}
      </div>

      <h4>Mana Coverage (Pips Needed vs Sources Available)</h4>
      <div className="mana-coverage-table">
        <div className="mana-coverage-header">
          <span className="mc-col mc-color">Color</span>
          <span className="mc-col mc-pips">Pips</span>
          <span className="mc-col mc-pct">% Needed</span>
          <span className="mc-col mc-sources">Sources</span>
          <span className="mc-col mc-pct">% Have</span>
          <span className="mc-col mc-cov">Coverage</span>
          <span className="mc-col mc-status">Status</span>
        </div>
        {(() => {
          const totalPips = Object.values(manaAnalysis.colorSymbols).reduce((s, v) => s + v, 0) || 1;
          const totalSources = Object.values(manaAnalysis.colorProducers).reduce((s, v) => s + v, 0) || 1;
          return Object.entries(manaAnalysis.colorSymbols)
            .filter(([, count]) => count > 0)
            .map(([color, pips]) => {
              const sources = manaAnalysis.colorProducers[color] || 0;
              const pctNeeded = (pips / totalPips) * 100;
              const pctHave = (sources / totalSources) * 100;
              const coverage = pctNeeded > 0 ? (pctHave / pctNeeded) * 100 : 100;
              const status = coverage >= 90 ? 'good' : coverage >= 60 ? 'warning' : 'danger';
              const statusLabel = coverage >= 90 ? '✅ Good' : coverage >= 60 ? '⚠️ Low' : '❌ Short';
              return (
                <div key={color} className="mana-coverage-row">
                  <span className="mc-col mc-color" style={{ color: colorHex[color], fontWeight: 700 }}>
                    {colorNames[color] || color}
                  </span>
                  <span className="mc-col mc-pips">{pips}</span>
                  <span className="mc-col mc-pct">{pctNeeded.toFixed(1)}%</span>
                  <span className="mc-col mc-sources">{sources}</span>
                  <span className="mc-col mc-pct">{pctHave.toFixed(1)}%</span>
                  <span className="mc-col mc-cov">
                    <div className="coverage-bar">
                      <div
                        className={`coverage-bar-fill coverage-${status}`}
                        style={{ width: `${Math.min(100, coverage)}%` }}
                      />
                    </div>
                    <span className="coverage-val">{coverage.toFixed(0)}%</span>
                  </span>
                  <span className={`mc-col mc-status coverage-label-${status}`}>{statusLabel}</span>
                </div>
              );
            });
        })()}
      </div>
    </div>
  );
}

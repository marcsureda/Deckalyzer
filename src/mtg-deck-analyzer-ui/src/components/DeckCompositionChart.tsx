import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts';
import { DeckComposition } from '../types/deck';
import './DeckCompositionChart.css';

interface DeckCompositionChartProps {
  composition: DeckComposition;
}

const COLORS = ['#4a9eff', '#8b5cf6', '#ef4444', '#f59e0b', '#10b981', '#06b6d4', '#f97316', '#6b7280'];

export default function DeckCompositionChart({ composition }: DeckCompositionChartProps) {
  const data = [
    { name: 'Creatures', value: composition.creatures },
    { name: 'Instants', value: composition.instants },
    { name: 'Sorceries', value: composition.sorceries },
    { name: 'Artifacts', value: composition.artifacts },
    { name: 'Enchantments', value: composition.enchantments },
    { name: 'PWs', value: composition.planeswalkers },
    { name: 'Lands', value: composition.lands },
    { name: 'Other', value: composition.other },
  ].filter(d => d.value > 0);

  const funcData = [
    { name: 'Ramp', value: composition.ramp, color: '#10b981' },
    { name: 'Draw', value: composition.cardDraw, color: '#4a9eff' },
    { name: 'Removal', value: composition.removal, color: '#ef4444' },
    { name: 'Wipes', value: composition.boardWipes, color: '#f97316' },
    { name: 'Counters', value: composition.counterspells, color: '#8b5cf6' },
    { name: 'Tutors', value: composition.tutors, color: '#f59e0b' },
  ];

  return (
    <div className="card composition-chart">
      <h3>📊 Deck Composition</h3>

      <div className="chart-container">
        <ResponsiveContainer width="100%" height={220}>
          <BarChart data={data}>
            <CartesianGrid strokeDasharray="3 3" stroke="#2a2a3e" />
            <XAxis dataKey="name" tick={{ fill: '#9090a8', fontSize: 12 }} />
            <YAxis tick={{ fill: '#9090a8', fontSize: 12 }} />
            <Tooltip
              contentStyle={{ background: '#1a1a2e', border: '1px solid #2a2a3e', borderRadius: 8 }}
              labelStyle={{ color: '#e8e8f0' }}
            />
            <Bar dataKey="value" radius={[4, 4, 0, 0]}>
              {data.map((_, idx) => (
                <Cell key={idx} fill={COLORS[idx % COLORS.length]} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>

      <h4>Functional Breakdown</h4>
      <div className="func-bars">
        {funcData.map((item) => (
          <div key={item.name} className="func-bar-row">
            <span className="func-bar-label">{item.name}</span>
            <div className="func-bar-track">
              <div
                className="func-bar-fill"
                style={{
                  width: `${Math.min(100, (item.value / 15) * 100)}%`,
                  background: item.color,
                }}
              />
            </div>
            <span className="func-bar-value">{item.value}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

import { useState } from 'react';
import './DeckInput.css';

interface DeckInputProps {
  onAnalyze: (deckList: string) => void;
  loading: boolean;
  onSearchPrecons: () => void;
  value: string;
  onChange: (deckList: string) => void;
}

const SAMPLE_DECK = `1 Edgar Markov

1 Sol Ring
1 Arcane Signet
1 Command Tower
1 Champion of Dusk
1 Vampire Nocturnus
1 Captivating Vampire
1 Legion Lieutenant
1 Stromkirk Captain
1 Cordial Vampire
1 Drana, Liberator of Malakir
1 Sanctum Seeker
1 Bloodline Keeper
1 Twilight Prophet
1 Malakir Bloodwitch
1 Butcher of Malakir
1 Olivia Voldaren
1 Kalitas, Traitor of Ghet
1 Knight of the Ebon Legion
1 Viscera Seer
1 Blood Artist
1 Cruel Celebrant
1 Indulgent Aristocrat
1 Falkenrath Gorger
1 Gifted Aetherborn
1 Stromkirk Noble
1 Vito, Thorn of the Dusk Rose
1 Vampire of the Dire Moon
1 Asylum Visitor
1 Olivia's Attendants
1 Bloodlord of Vaasgoth
1 Patron of the Vein
1 Shared Animosity
1 Coat of Arms
1 Door of Destinies
1 Vanquisher's Banner
1 Herald's Horn
1 Skullclamp
1 Lightning Greaves
1 Swiftfoot Boots
1 Bolas's Citadel
1 Phyrexian Arena
1 Necropotence
1 Teferi's Protection
1 Swords to Plowshares
1 Path to Exile
1 Generous Gift
1 Anguished Unmaking
1 Chaos Warp
1 Despark
1 Vindicate
1 Wrath of God
1 Blasphemous Act
1 Toxic Deluge
1 Demonic Tutor
1 Vampiric Tutor
1 Diabolic Intent
1 Living Death
1 Kindred Dominance
1 Exquisite Blood
1 Sanguine Bond
1 Reconnaissance
1 Cathars' Crusade
1 Smothering Tithe
1 Black Market
1 Read the Bones
1 Night's Whisper
1 Sign in Blood
1 Talisman of Indulgence
1 Talisman of Hierarchy
1 Talisman of Conviction
1 Rakdos Signet
1 Orzhov Signet
1 Fellwar Stone
1 Blood Crypt
1 Godless Shrine
1 Sacred Foundry
1 Dragonskull Summit
1 Isolated Chapel
1 Clifftop Retreat
1 Luxury Suite
1 Vault of Champions
1 Spectator Seating
1 Unclaimed Territory
1 Cavern of Souls
1 Reflecting Pool
1 Exotic Orchard
1 Bloodstained Mire
1 Marsh Flats
1 Arid Mesa
1 Smoldering Marsh
1 Shambling Vent
1 Nomad Outpost
1 Path of Ancestry
1 Bojuka Bog
1 Castle Locthwain
1 Swamp
1 Swamp
1 Plains
1 Mountain`;

export default function DeckInput({ onAnalyze, loading, onSearchPrecons, value, onChange }: DeckInputProps) {

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (value.trim()) {
      onAnalyze(value.trim());
    }
  };

  const loadSampleDeck = () => {
    onChange(SAMPLE_DECK);
  };

  return (
    <div className="card deck-input">
      <h2>📋 Paste Your Deck List</h2>
      <p className="deck-input-help">
        Paste your Commander deck list below. Supports most common formats including
        MTGO, Arena, Moxfield, and Archidekt exports.
        The commander is auto-detected from the first card, last card (if separated by a blank line),
        or a "Commander:" header. Sideboard cards are skipped. The deck should have exactly 100 cards.
      </p>
      <form onSubmit={handleSubmit}>
        <textarea
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={`1 Edgar Markov\n\n1 Sol Ring\n1 Arcane Signet\n1 Command Tower\n...`}
          rows={14}
          disabled={loading}
        />
        <div className="deck-input-actions">
          <button
            type="button"
            className="btn-secondary"
            onClick={loadSampleDeck}
            disabled={loading}
          >
            Load Sample Deck
          </button>
          <button
            type="button"
            className="btn-secondary precon-button"
            onClick={onSearchPrecons}
            disabled={loading}
          >
            🏛️ Browse Precons
          </button>
          <button
            type="button"
            className="btn-secondary"
            onClick={() => onChange('')}
            disabled={loading || !value}
          >
            Clear
          </button>
          <div className="spacer" />
          <span className="card-count">
            {value.trim() ? value.trim().split('\n').filter((l: string) => l.trim() && !l.startsWith('Commander') && !l.startsWith('//')).length : 0} lines
          </span>
          <button
            type="submit"
            className="btn-primary"
            disabled={loading || !value.trim()}
          >
            {loading ? 'Analyzing...' : '⚡ Analyze Deck'}
          </button>
        </div>
      </form>
    </div>
  );
}

export interface DeckAnalysisRequest {
  deckList: string;
}

export interface CardInfo {
  name: string;
  quantity: number;
  cmc: number;
  manaCost: string;
  typeLine: string;
  oracleText: string;
  colorIdentity: string[];
  colors: string[];
  keywords: string[];
  rarity: string;
  edhrecRank: number;
  price: number;
  priceEur: number;
  imageUri: string;
  scryfallUri: string;
  isCommander: boolean;
  isLand: boolean;
  isCreature: boolean;
  isArtifact: boolean;
  isEnchantment: boolean;
  isInstant: boolean;
  isSorcery: boolean;
  isPlaneswalker: boolean;
  powerScore: number;
  playability: number;
  impact: number;
  isGameChanger: boolean;
  isTutor: boolean;
  isExtraTurn: boolean;
  isMassLandDenial: boolean;
  isInfiniteComboPiece: boolean;
  isFastMana: boolean;
  isCounterspell: boolean;
  isBoardWipe: boolean;
  isRemoval: boolean;
  isCardDraw: boolean;
  isRamp: boolean;
  synergy: number;
  cardmarketUri: string;
  imageUriCheapest: string;
}

export interface DeckAnalysisResult {
  commanderName: string;
  commanderImageUri: string;
  commanderNames: string[];
  commanderImageUris: string[];
  colorIdentity: string[];
  totalCards: number;
  powerLevel: number;
  bracket: number;
  bracketName: string;
  efficiency: number;
  totalImpact: number;
  score: number;
  averagePlayability: number;
  averageCmc: number;
  tippingPoint: number;
  bracketDetails: BracketDetails;
  composition: DeckComposition;
  manaAnalysis: ManaAnalysis;
  cards: CardInfo[];
  cutRecommendations: CardRecommendation[];
  addRecommendations: CardRecommendation[];
  strengths: string[];
  weaknesses: string[];
  warnings: string[];
  combos: ComboInfo[];
  strategy: DeckStrategy;
  tokens: TokenInfo[];
  deckTotalValueEur: number;
}

export interface BracketDetails {
  hasExtraTurns: boolean;
  hasChainingExtraTurns: boolean;
  hasMassLandDenial: boolean;
  hasTwoCardCombos: boolean;
  hasOnlyLateGameCombos: boolean;
  gameChangerCount: number;
  extraTurnCards: string[];
  massLandDenialCards: string[];
  comboCards: string[];
  gameChangerCards: string[];
  requirements: BracketRequirement[];
}

export interface BracketRequirement {
  bracket: number;
  name: string;
  rules: BracketRule[];
  passes: boolean;
}

export interface BracketRule {
  description: string;
  passes: boolean;
}

export interface DeckComposition {
  creatures: number;
  instants: number;
  sorceries: number;
  artifacts: number;
  enchantments: number;
  planeswalkers: number;
  lands: number;
  other: number;
  ramp: number;
  cardDraw: number;
  removal: number;
  boardWipes: number;
  counterspells: number;
  tutors: number;
}

export interface ManaAnalysis {
  colorSymbols: Record<string, number>;
  colorProducers: Record<string, number>;
  manaCurve: Record<string, number>;
  manaScrew: number;
  manaFlood: number;
  sweetSpot: number;
}

export interface CardRecommendation {
  cardName: string;
  reason: string;
  category: string;
  estimatedImpact: number;
  imageUri: string;
}

export interface ComboInfo {
  cards: string[];
  description: string;
  url: string;
  isInfinite: boolean;
  infiniteEffect: string;
}

export interface DeckStrategy {
  primaryArchetype: string;
  tags: StrategyTag[];
  summary: string;
}

export interface StrategyTag {
  name: string;
  cardCount: number;
  percentage: number;
  exampleCards: string[];
}

export interface TokenInfo {
  description: string;
  imageUri: string;
  producedBy: string[];
}

export interface PreconDeck {
  name: string;
  year: string;
  commanders: string[];
  colorIdentity: string[];
  deckList: string;
  theme: string;
  imageUrl: string;
  price?: number;
}

export interface PreconSearchResult {
  precons: PreconDeck[];
  totalCount: number;
}

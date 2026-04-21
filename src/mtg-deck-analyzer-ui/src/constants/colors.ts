/** MTG color single-letter identifiers in WUBRG order. */
export const COLOR_ORDER = ['W', 'U', 'B', 'R', 'G'] as const;

export type MtgColor = (typeof COLOR_ORDER)[number];

/** Emoji symbol for each MTG color — used in color identity display. */
export const COLOR_SYMBOLS: Record<MtgColor, string> = {
  W: '☀️',
  U: '💧',
  B: '💀',
  R: '🔥',
  G: '🌿',
};

/** Full name for each MTG color. */
export const COLOR_NAMES: Record<MtgColor, string> = {
  W: 'White',
  U: 'Blue',
  B: 'Black',
  R: 'Red',
  G: 'Green',
};

/** Filled circle emoji for each MTG color — used in filter toggle buttons. */
export const COLOR_CIRCLE: Record<MtgColor, string> = {
  W: '⚪',
  U: '🔵',
  B: '⚫',
  R: '🔴',
  G: '🟢',
};

/** Returns the emoji symbol for a given color identifier, falling back to the raw string. */
export function getColorSymbol(color: string): string {
  return COLOR_SYMBOLS[color as MtgColor] ?? color;
}

/** Returns the circle emoji for a given color identifier — used in filter buttons. */
export function getColorCircle(color: string): string {
  return COLOR_CIRCLE[color as MtgColor] ?? color;
}

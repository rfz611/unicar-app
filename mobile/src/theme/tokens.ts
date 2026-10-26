/**
 * Cores, tipografia e medidas copiadas do protótipo aprovado
 * (docs/prototipo/UniCar-prototipo.dc.html → renderVals, classes .btn/.field/.chip/.list...).
 */

export const ACCENT = '#2347E0';

export type ThemeName = 'dark' | 'light';

export type Palette = {
  name: ThemeName;
  accent: string;
  /** Texto em cor de destaque sobre o fundo (rótulos "Carona aceita", "Alterar"). */
  accentText: string;
  /** Fundo escuro do ícone de boas-vindas. */
  accentDeep: string;
  accentGlow: string;
  bg: string;
  s1: string;
  s2: string;
  s3: string;
  line: string;
  fg: string;
  mut: string;
  mut2: string;
  glass: string;
  shadow: string;
  err: string;
  mapBg: string;
  mapRoad: string;
  mapRoad2: string;
  mapPark: string;
  mapWater: string;
  mapLabel: string;
};

const rgb = (hex: string) => {
  const n = parseInt(hex.replace('#', ''), 16);
  return [(n >> 16) & 255, (n >> 8) & 255, n & 255] as const;
};

/** Mesmas contas do protótipo: mistura o destaque com preto (claro) ou branco (escuro). */
function accentVariants(accent: string, light: boolean) {
  const [r, g, b] = rgb(accent);
  const mix = (t: number, to: number) =>
    `rgb(${Math.round(r + (to - r) * t)},${Math.round(g + (to - g) * t)},${Math.round(b + (to - b) * t)})`;
  return {
    accentText: light ? mix(0.25, 0) : mix(0.45, 255),
    accentDeep: `rgb(${Math.round(r * 0.45)},${Math.round(g * 0.45)},${Math.round(b * 0.45)})`,
    accentGlow: `rgba(${r},${g},${b},${light ? 0.28 : 0.5})`,
  };
}

export const palettes: Record<ThemeName, Palette> = {
  dark: {
    name: 'dark',
    accent: ACCENT,
    ...accentVariants(ACCENT, false),
    bg: '#141416',
    s1: '#1C1C1F',
    s2: '#222226',
    s3: '#2A2A2F',
    line: '#2F2F35',
    fg: '#E8E8E3',
    mut: '#A6A6AB',
    mut2: '#8F8F96',
    glass: 'rgba(28,28,31,0.92)',
    shadow: 'rgba(0,0,0,0.55)',
    err: '#FF9F8A',
    mapBg: '#18181B',
    mapRoad: '#2C2C32',
    mapRoad2: '#202025',
    mapPark: '#1A231D',
    mapWater: '#1D2B40',
    mapLabel: '#7D7D85',
  },
  light: {
    name: 'light',
    accent: ACCENT,
    ...accentVariants(ACCENT, true),
    bg: '#F4F4F2',
    s1: '#FFFFFF',
    s2: '#EDEDEA',
    s3: '#E2E2DE',
    line: '#DADAD5',
    fg: '#0A0A0B',
    mut: '#4E4E53',
    mut2: '#66666B',
    glass: 'rgba(255,255,255,0.94)',
    shadow: 'rgba(0,0,0,0.18)',
    err: '#B3261E',
    mapBg: '#E8E8E4',
    mapRoad: '#FFFFFF',
    mapRoad2: '#F6F6F3',
    mapPark: '#D6E3D2',
    mapWater: '#BCD0EA',
    mapLabel: '#77777D',
  },
};

/** Geist só tem os 4 pesos que o protótipo usa. Em RN cada peso é uma família. */
export const fonts = {
  400: 'Geist_400Regular',
  500: 'Geist_500Medium',
  600: 'Geist_600SemiBold',
  700: 'Geist_700Bold',
} as const;

export type Weight = keyof typeof fonts;

export const fontFiles = {
  [fonts[400]]: require('../../assets/fonts/Geist_400Regular.ttf'),
  [fonts[500]]: require('../../assets/fonts/Geist_500Medium.ttf'),
  [fonts[600]]: require('../../assets/fonts/Geist_600SemiBold.ttf'),
  [fonts[700]]: require('../../assets/fonts/Geist_700Bold.ttf'),
};

/** letter-spacing do CSS vem em em; no RN é em px. */
export const em = (value: number, fontSize: number) => value * fontSize;

/** Medidas que se repetem no protótipo. */
export const radius = { field: 16, btn: 18, list: 20, card: 22, panel: 28, pill: 99 } as const;
export const TAB_BAR_HEIGHT = 84;
/** Toque mínimo (acessibilidade). */
export const HIT = 44;

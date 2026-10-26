import Svg, { Circle, Path, Rect } from 'react-native-svg';

/** Caminhos SVG dos ícones do protótipo (viewBox 0 0 24 24, traço arredondado). */
export const icons = {
  back: 'M19 12H5M11 18l-6-6 6-6',
  close: 'M6 6l12 12M18 6L6 18',
  arrow: 'M5 12h14M13 6l6 6-6 6',
  chevron: 'M9 6l6 6-6 6',
  check: 'M5 12l5 5 9-10',
  pin: 'M12 21s-7-6.2-7-11.5A7 7 0 0 1 19 9.5C19 14.8 12 21 12 21zM12 12a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5z',
  clock: 'M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18zM12 7v5l3 2',
  plus: 'M12 5v14M5 12h14',
  user: 'M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2M12 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8z',
  campus: 'M3 9l9-5 9 5-9 5-9-5zM7 11.5V16c0 1.5 2.2 3 5 3s5-1.5 5-3v-4.5',
  home: 'M3 11l9-7 9 7v9a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z',
  shop: 'M5 8h14l-1 12H6zM9 8V6a3 3 0 0 1 6 0v2',
  gps: 'M12 2v3M12 19v3M2 12h3M19 12h3M12 18a6 6 0 1 0 0-12 6 6 0 0 0 0 12z',
  bolt: 'M13 3L5 14h6l-1 7 8-11h-6z',
  chat: 'M4 5h16v11H8l-4 4z',
  car: 'M5 17h14M6 17l1.5-6h9L18 17M8 17v2M16 17v2',
  star: 'M12 2.5l2.9 6 6.6.9-4.8 4.6 1.2 6.5L12 17.4l-5.9 3.1 1.2-6.5L2.5 9.4l6.6-.9z',
  camera: 'M4 8h3l2-3h6l2 3h3v11H4z',
  gear:
    'M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z',
} as const;

export type IconName = keyof typeof icons;

type Props = {
  name?: IconName;
  /** Caminho próprio, quando não está na lista. */
  d?: string;
  size?: number;
  color: string;
  strokeWidth?: number;
  fill?: string;
};

export function Icon({ name, d, size = 20, color, strokeWidth = 2, fill = 'none' }: Props) {
  const path = d ?? (name ? icons[name] : '');
  return (
    <Svg width={size} height={size} viewBox="0 0 24 24">
      <Path d={path} fill={fill} stroke={color} strokeWidth={strokeWidth} strokeLinecap="round" strokeLinejoin="round" />
      {name === 'gear' && <Circle cx={12} cy={12} r={3} fill="none" stroke={color} strokeWidth={strokeWidth} />}
      {name === 'camera' && <Circle cx={12} cy={13} r={3.5} fill="none" stroke={color} strokeWidth={strokeWidth} />}
    </Svg>
  );
}

/** Calendário do botão "Caronas marcadas" (tem retângulo, não cabe num path só). */
export function CalendarIcon({ size = 20, color }: { size?: number; color: string }) {
  return (
    <Svg width={size} height={size} viewBox="0 0 24 24">
      <Rect x={3} y={5} width={18} height={16} rx={3} fill="none" stroke={color} strokeWidth={2} />
      <Path d="M3 10h18M8 3v4M16 3v4" stroke={color} strokeWidth={2} strokeLinecap="round" />
    </Svg>
  );
}

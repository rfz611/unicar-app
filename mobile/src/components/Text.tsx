import { Text as RNText, type TextProps, type TextStyle } from 'react-native';
import { useTheme } from '@/theme/ThemeProvider';
import { em, fonts, type Weight } from '@/theme/tokens';

type Props = TextProps & {
  size?: number;
  weight?: Weight;
  color?: string;
  /** letter-spacing em "em", como no CSS do protótipo (ex.: -0.04). */
  tracking?: number;
  /** line-height relativo (1.45), como no CSS. */
  leading?: number;
  /** font-variant-numeric: tabular-nums (horários e preços). */
  tabular?: boolean;
  align?: TextStyle['textAlign'];
};

/** Texto em Geist. Use sempre este em vez do Text do React Native. */
export function Text({ size = 16, weight = 400, color, tracking, leading, tabular, align, style, ...rest }: Props) {
  const { t } = useTheme();
  return (
    <RNText
      {...rest}
      style={[
        {
          fontFamily: fonts[weight],
          fontSize: size,
          color: color ?? t.fg,
          letterSpacing: tracking ? em(tracking, size) : undefined,
          lineHeight: leading ? Math.round(leading * size) : undefined,
          fontVariant: tabular ? ['tabular-nums'] : undefined,
          textAlign: align,
        },
        style,
      ]}
    />
  );
}

/** .h1 do protótipo: 32px, 700, -0.04em, line-height 1.08. */
export function H1(props: Omit<Props, 'size' | 'weight' | 'tracking' | 'leading'>) {
  return <Text accessibilityRole="header" size={32} weight={700} tracking={-0.04} leading={1.08} {...props} />;
}

/** .lbl do protótipo: rótulo em frase normal, 14px, 500, cor secundária. Nunca caixa alta. */
export function Label(props: Props) {
  const { t } = useTheme();
  return <Text size={14} weight={500} color={t.mut} {...props} />;
}

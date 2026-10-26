import { View } from 'react-native';
import Svg, { Circle, Path } from 'react-native-svg';
import { useTheme } from '@/theme/ThemeProvider';
import { Text } from './Text';

/** Marca "unicar": o "u" com o ponto azul (viewBox 0 0 80 80 do protótipo). */
export function LogoMark({ size = 28, color, dot }: { size?: number; color?: string; dot?: string }) {
  const { t } = useTheme();
  return (
    <Svg width={size} height={size} viewBox="0 0 80 80">
      <Path d="M22 18 V44 A18 18 0 0 0 58 44 V35" fill="none" stroke={color ?? t.fg} strokeWidth={11} strokeLinecap="round" />
      <Circle cx={58} cy={18} r={7} fill={dot ?? t.accent} />
    </Svg>
  );
}

/** Logo com o nome, como no topo das telas (28px/24px na entrada, 24px/21px na aba Caronas). */
export function Logo({ mark = 28, text = 24 }: { mark?: number; text?: number }) {
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: mark > 24 ? 10 : 8 }}>
      <LogoMark size={mark} />
      <Text size={text} weight={700} tracking={-0.05}>
        unicar
      </Text>
    </View>
  );
}

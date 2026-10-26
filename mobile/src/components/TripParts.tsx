import { useEffect, useState, type ReactNode } from 'react';
import { Platform, Pressable, View, type LayoutChangeEvent } from 'react-native';
import Animated, { Easing, useAnimatedStyle, useSharedValue, withRepeat, withTiming } from 'react-native-reanimated';
import type { Ride } from '@/api/types';
import { useTheme } from '@/theme/ThemeProvider';
import { Icon } from './Icon';
import { Avatar } from './Page';
import { Text } from './Text';

/** Placa no estilo do protótipo: Geist Mono, fundo do texto, cantos de 6. (Mono do sistema no lugar da Geist Mono.) */
export function PlateBadge({ plate, size = 12 }: { plate: string; size?: number }) {
  const { t } = useTheme();
  return (
    <View style={{ backgroundColor: t.fg, borderRadius: 6, paddingVertical: 3, paddingHorizontal: 7 }}>
      <Text size={size} weight={500} color={t.bg} tracking={0.06} style={{ fontFamily: Platform.select({ ios: 'Menlo', android: 'monospace', default: 'ui-monospace, Menlo, monospace' }) }}>
        {plate}
      </Text>
    </View>
  );
}

/** Nota com estrela azul: "4,8" (ou "Nova" sem avaliações). */
export function RatingInline({ rating, size = 13 }: { rating: number | null; size?: number }) {
  const { t } = useTheme();
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
      <Icon name="star" size={12} color={t.accent} fill={t.accent} strokeWidth={0} />
      <Text size={size} weight={500} color={t.mut}>
        {rating !== null ? rating.toFixed(1).replace('.', ',') : 'Nova'}
      </Text>
    </View>
  );
}

/** Caixa do motorista nos painéis da corrida: foto/iniciais, nome, nota, curso, carro e placa. */
export function DriverCard({ driver, vehicle }: { driver: Ride['driver']; vehicle: Ride['vehicle'] }) {
  const { t } = useTheme();
  const meta = driver.course ? `${driver.course} na ${driver.university}` : driver.university;
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12, backgroundColor: t.s2, borderRadius: 18, padding: 12 }}>
      <Avatar name={driver.firstName} photoUrl={driver.photoUrl} size={44} ring={0} />
      <View style={{ flex: 1, gap: 2 }}>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
          <Text size={16} weight={600}>
            {driver.firstName}
          </Text>
          <RatingInline rating={driver.rating} />
        </View>
        <Text size={13} color={t.mut2} numberOfLines={1}>
          {meta}
        </Text>
      </View>
      <View style={{ alignItems: 'flex-end', gap: 4 }}>
        <Text size={13} color={t.mut}>
          {carLabel(vehicle)}
        </Text>
        <PlateBadge plate={vehicle.plate.replace('-', '')} />
      </View>
    </View>
  );
}

/** "Onix prata" (última palavra do modelo + cor), como no protótipo. */
export const carLabel = (v: Ride['vehicle']) => `${v.model.split(' ').slice(-1)[0]} ${v.color.toLowerCase()}`;

/** Barra azul com o brilho que corre (.scan do protótipo): "procurando" e "em análise". */
export function ScanBar() {
  const { t } = useTheme();
  const [w, setW] = useState(0);
  const x = useSharedValue(0);
  useEffect(() => {
    x.set(withRepeat(withTiming(1, { duration: 1300, easing: Easing.inOut(Easing.ease) }), -1, false));
  }, [x]);
  const glow = useAnimatedStyle(() => ({ transform: [{ translateX: -0.4 * w + x.get() * 1.4 * w }] }));
  return (
    <View onLayout={(e: LayoutChangeEvent) => setW(e.nativeEvent.layout.width)} style={{ height: 4, borderRadius: 99, backgroundColor: t.accent, overflow: 'hidden' }}>
      <Animated.View style={[{ position: 'absolute', top: 0, bottom: 0, width: w * 0.4, backgroundColor: 'rgba(255,255,255,0.55)', borderRadius: 99 }, glow]} />
    </View>
  );
}

/** Nota de 1 a 5 (radiogroup do protótipo). */
export function StarPicker({ value, onChange }: { value: number; onChange: (v: number) => void }) {
  const { t } = useTheme();
  return (
    <View accessibilityRole="radiogroup" accessibilityLabel="Nota da carona" style={{ flexDirection: 'row', gap: 8 }}>
      {[1, 2, 3, 4, 5].map((v) => (
        <Pressable
          key={v}
          accessibilityRole="radio"
          accessibilityLabel={v === 1 ? '1 estrela' : `${v} estrelas`}
          accessibilityState={{ checked: value === v }}
          onPress={() => onChange(v)}
          style={{ flex: 1, height: 52, borderRadius: 14, borderWidth: 1, borderColor: t.line, backgroundColor: t.s2, alignItems: 'center', justifyContent: 'center' }}
        >
          <Icon name="star" size={22} color={v <= value ? t.accent : t.mut2} fill={v <= value ? t.accent : 'none'} strokeWidth={1.6} />
        </Pressable>
      ))}
    </View>
  );
}

/** Painel flutuante (.panel do protótipo). */
export function Panel({ children, bottom }: { children: ReactNode; bottom: number }) {
  const { t } = useTheme();
  return (
    <View
      style={{
        position: 'absolute',
        left: 12,
        right: 12,
        bottom,
        backgroundColor: t.s1,
        borderWidth: 1,
        borderColor: t.line,
        borderRadius: 28,
        padding: 20,
        gap: 16,
        boxShadow: `0 30px 60px -10px ${t.shadow}`,
      }}
    >
      {children}
    </View>
  );
}

import { useEffect, useRef, useState } from 'react';
import { Pressable, View, type LayoutChangeEvent } from 'react-native';
import Animated, {
  Easing,
  useAnimatedStyle,
  useSharedValue,
  withSequence,
  withTiming,
} from 'react-native-reanimated';
import Svg, { Path } from 'react-native-svg';
import type { BottomTabBarProps } from 'expo-router/tabs';
import { useTheme } from '@/theme/ThemeProvider';
import { TAB_BAR_HEIGHT } from '@/theme/tokens';
import { Icon, icons, type IconName } from './Icon';
import { Text } from './Text';

/**
 * Barra de abas do protótipo (.tabbar/.bubble/.blob0/.blob1):
 * - a aba ativa vira um círculo azul de 56px, 22px acima da barra, com o ícone branco;
 * - a barra tem um recorte curvo em volta do círculo (círculo de raio 34,5 com centro 6px abaixo do topo);
 * - ao trocar, círculo e recorte deslizam juntos em 600ms com cubic-bezier(.34,1.35,.5,1) (leve quique)
 *   e o círculo estica na horizontal no meio do caminho.
 */

const TABS: Record<string, { label: string; icon: IconName }> = {
  index: { label: 'Caronas', icon: 'pin' },
  trips: { label: 'Viagens', icon: 'clock' },
  offer: { label: 'Oferecer', icon: 'plus' },
  profile: { label: 'Perfil', icon: 'user' },
};

const BUBBLE = 56;
const LIFT = 22;
const NOTCH_R = 34.5;
const NOTCH_CY = 6;
const CORNER = 24;
const SLIDE = { duration: 600, easing: Easing.bezier(0.34, 1.35, 0.5, 1) };
const BLOB = Easing.bezier(0.3, 0.7, 0.4, 1);

/**
 * Faixa com o dobro da largura e o recorte fixo no meio (x = w). Como no protótipo (mask-position),
 * o recorte anda deslizando a faixa: translateX = cx − w. Só transform anima igual em web, iOS e Android.
 */
function stripPath(w: number, h: number) {
  const dx = Math.sqrt(NOTCH_R * NOTCH_R - NOTCH_CY * NOTCH_CY);
  return `M0 0 L${w - dx} 0 A${NOTCH_R} ${NOTCH_R} 0 1 0 ${w + dx} 0 L${2 * w} 0 L${2 * w} ${h} L0 ${h} Z`;
}

export function TabBar({ state, navigateToTab, insets, dots = {} }: BottomTabBarProps & { dots?: Record<string, boolean> }) {
  const { t } = useTheme();
  const [width, setWidth] = useState(0);
  const height = TAB_BAR_HEIGHT + insets.bottom;
  const count = state.routes.length;

  const cx = useSharedValue(0);
  const sx = useSharedValue(1);
  const sy = useSharedValue(1);
  const iconIn = useSharedValue(1);
  /** Na primeira medida o círculo já nasce no lugar, sem deslizar. */
  const placed = useRef(false);

  useEffect(() => {
    if (!width) return;
    const target = (width / count) * (state.index + 0.5);
    if (!placed.current) {
      placed.current = true;
      cx.set(target);
      return;
    }
    cx.set(withTiming(target, SLIDE));
    // @keyframes blob: 35% → scale(1.28,.86); 70% → (.94,1.06); 100% → (1,1)
    sx.set(withSequence(withTiming(1.28, { duration: 210, easing: BLOB }), withTiming(0.94, { duration: 210, easing: BLOB }), withTiming(1, { duration: 180, easing: BLOB })));
    sy.set(withSequence(withTiming(0.86, { duration: 210, easing: BLOB }), withTiming(1.06, { duration: 210, easing: BLOB }), withTiming(1, { duration: 180, easing: BLOB })));
    // .bicon: o ícone novo entra de scale(.6) rotate(-12deg) em .45s
    iconIn.set(0);
    iconIn.set(withTiming(1, { duration: 450 }));
  }, [state.index, width, count, cx, sx, sy, iconIn]);

  const stripStyle = useAnimatedStyle(() => ({ transform: [{ translateX: cx.get() - width }] }));
  const bubbleStyle = useAnimatedStyle(() => ({
    transform: [{ translateX: cx.get() - BUBBLE / 2 }, { scaleX: sx.get() }, { scaleY: sy.get() }],
  }));
  const iconStyle = useAnimatedStyle(() => ({
    opacity: iconIn.get(),
    transform: [{ scale: 0.6 + 0.4 * iconIn.get() }, { rotate: `${-12 * (1 - iconIn.get())}deg` }],
  }));

  const active = state.routes[state.index];
  const activeIcon = TABS[active?.name ?? 'index']?.icon ?? 'pin';

  return (
    <View
      onLayout={(e: LayoutChangeEvent) => setWidth(e.nativeEvent.layout.width)}
      accessibilityRole="tablist"
      style={{ position: 'absolute', left: 0, right: 0, bottom: 0, height }}
    >
      {width > 0 && (
        <View style={{ position: 'absolute', left: 0, top: 0, width, height, borderTopLeftRadius: CORNER, borderTopRightRadius: CORNER, overflow: 'hidden' }}>
          <Animated.View style={[{ width: width * 2, height }, stripStyle]}>
            <Svg width={width * 2} height={height}>
              <Path d={stripPath(width, height)} fill={t.s3} />
            </Svg>
          </Animated.View>
        </View>
      )}

      {width > 0 && (
        <Animated.View
          pointerEvents="none"
          style={[
            {
              position: 'absolute',
              top: -LIFT,
              left: 0,
              width: BUBBLE,
              height: BUBBLE,
              borderRadius: BUBBLE / 2,
              backgroundColor: t.accent,
              alignItems: 'center',
              justifyContent: 'center',
              shadowColor: '#000',
              shadowOpacity: 0.45,
              shadowRadius: 8,
              shadowOffset: { width: 0, height: 6 },
              elevation: 6,
            },
            bubbleStyle,
          ]}
        >
          <Animated.View key={active?.key} style={iconStyle}>
            <Icon d={icons[activeIcon]} size={24} color="#FFFFFF" strokeWidth={2.1} />
          </Animated.View>
        </Animated.View>
      )}

      <View style={{ position: 'absolute', left: 0, right: 0, top: 0, height: TAB_BAR_HEIGHT, flexDirection: 'row' }}>
        {state.routes.map((route, i) => {
          const def = TABS[route.name] ?? { label: route.name, icon: 'pin' as IconName };
          const focused = i === state.index;
          return (
            <Pressable
              key={route.key}
              accessibilityRole="tab"
              accessibilityLabel={def.label}
              accessibilityState={{ selected: focused }}
              onPress={() => navigateToTab(route.key)}
              style={{ flex: 1, alignItems: 'center', justifyContent: 'flex-start', gap: 4, paddingTop: 16 }}
            >
              <View style={{ opacity: focused ? 0 : 1, transform: [{ translateY: focused ? -16 : 0 }] }}>
                <Icon name={def.icon} size={22} color={focused ? t.fg : t.mut2} />
              </View>
              <Text size={11} weight={focused ? 700 : 600} color={focused ? t.fg : t.mut2} style={{ transform: [{ translateY: focused ? 16 : 0 }] }}>
                {def.label}
              </Text>
              {dots[route.name] && !focused ? (
                <View style={{ position: 'absolute', top: 14, marginLeft: 22, width: 8, height: 8, borderRadius: 4, backgroundColor: t.accent }} />
              ) : null}
            </Pressable>
          );
        })}
      </View>
    </View>
  );
}

/** Espaço que as telas deixam embaixo para a barra (ela fica por cima, como no protótipo). */
export function tabBarSpace(bottomInset: number) {
  return TAB_BAR_HEIGHT + bottomInset;
}

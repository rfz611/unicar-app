import type { ReactNode } from 'react';
import { ScrollView, View, type StyleProp, type ViewStyle } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Image } from 'react-native';
import { API_URL } from '@/api/client';
import { useTheme } from '@/theme/ThemeProvider';
import { tabBarSpace } from './TabBar';
import { Text } from './Text';
import { initialsOf } from './ui';

/** .page do protótipo: rolagem com padding 56/20/44 e gap 20, acima da barra de abas. */
export function Page({ children, gap = 20, style, withTabBar = true }: { children: ReactNode; gap?: number; style?: StyleProp<ViewStyle>; withTabBar?: boolean }) {
  const { t } = useTheme();
  const insets = useSafeAreaInsets();
  return (
    <ScrollView
      style={{ flex: 1, backgroundColor: t.bg }}
      showsVerticalScrollIndicator={false}
      keyboardShouldPersistTaps="handled"
      contentContainerStyle={[
        {
          paddingTop: Math.max(insets.top, 12) + 12,
          paddingHorizontal: 20,
          paddingBottom: 44 + (withTabBar ? tabBarSpace(insets.bottom) : insets.bottom),
          gap,
        },
        style,
      ]}
    >
      {children}
    </ScrollView>
  );
}

/** Caixa tracejada de "nada aqui ainda" (Viagens sem reservas etc.). */
export function EmptyBox({ children }: { children: string }) {
  const { t } = useTheme();
  return (
    <View style={{ backgroundColor: t.s1, borderWidth: 1, borderStyle: 'dashed', borderColor: t.line, borderRadius: 20, padding: 20 }}>
      <Text size={15} leading={1.5} color={t.mut}>
        {children}
      </Text>
    </View>
  );
}

/** Foto de perfil (ou iniciais) com o anel azul: box-shadow 0 0 0 Npx bg, 0 0 0 Mpx accent. */
export function Avatar({ name, photoUrl, size = 44, ring = 2 }: { name?: string | null; photoUrl?: string | null; size?: number; ring?: number }) {
  const { t } = useTheme();
  const gap = ring;
  const outer = size + (gap + ring * 0.75) * 2;
  const uri = photoUrl ? (photoUrl.startsWith('http') ? photoUrl : API_URL + photoUrl) : null;
  return (
    <View style={{ width: outer, height: outer, borderRadius: outer / 2, borderWidth: ring * 0.75, borderColor: t.accent, alignItems: 'center', justifyContent: 'center' }}>
      <View style={{ width: size, height: size, borderRadius: size / 2, overflow: 'hidden', backgroundColor: t.s3, alignItems: 'center', justifyContent: 'center' }}>
        {uri ? (
          <Image source={{ uri }} style={{ width: size, height: size }} accessibilityIgnoresInvertColors />
        ) : (
          <Text size={Math.round(size * 0.34)} weight={600}>
            {initialsOf(name)}
          </Text>
        )}
      </View>
    </View>
  );
}

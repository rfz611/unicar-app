import { router } from 'expo-router';
import type { ReactNode } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '@/theme/ThemeProvider';
import { Text } from './Text';
import { IconButton } from './ui';

/**
 * Moldura das telas de entrada (e-mail, código, seu perfil): voltar, 3 barrinhas de progresso e "1/3"
 * no topo; conteúdo com padding 24 e o botão principal grudado embaixo (sobe junto com o teclado).
 */
export function AuthScreen({ step, children, footer, scroll }: { step: 0 | 1 | 2; children: ReactNode; footer: ReactNode; scroll?: boolean }) {
  const { t } = useTheme();
  const insets = useSafeAreaInsets();
  const top = Math.max(insets.top, 12) + 12;

  const body = (
    <View style={{ flex: 1, gap: scroll ? 16 : 0 }}>
      {children}
    </View>
  );

  return (
    <KeyboardAvoidingView style={{ flex: 1, backgroundColor: t.bg }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12, paddingTop: top, paddingHorizontal: 16 }}>
        <IconButton icon="back" label="Voltar" onPress={() => (router.canGoBack() ? router.back() : router.replace('/'))} />
        <View style={{ flex: 1, flexDirection: 'row', gap: 6 }}>
          {[0, 1, 2].map((i) => (
            <View key={i} style={{ flex: 1, height: 3, borderRadius: 99, backgroundColor: i <= step ? t.accent : t.line }} />
          ))}
        </View>
        <Text size={13} color={t.mut2} tabular align="right" style={{ width: 44 }}>
          {`${step + 1}/3`}
        </Text>
      </View>

      {scroll ? (
        <ScrollView
          style={{ flex: 1 }}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
          contentContainerStyle={{ flexGrow: 1, paddingHorizontal: 24, paddingTop: 20, paddingBottom: 16, gap: 16 }}
        >
          {children}
        </ScrollView>
      ) : (
        <View style={{ flex: 1, paddingHorizontal: 24, paddingTop: 24 }}>{body}</View>
      )}

      <View style={{ paddingHorizontal: 24, paddingTop: 12, paddingBottom: Math.max(insets.bottom, 16) + 16, gap: 16 }}>{footer}</View>
    </KeyboardAvoidingView>
  );
}

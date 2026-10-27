import { router } from 'expo-router';
import type { ReactNode } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '@/theme/ThemeProvider';
import { Text } from './Text';
import { IconButton } from './ui';

/**
 * Tela interna do perfil (.slide do protótipo): voltar + título com linha embaixo, conteúdo com
 * padding 20 e, se houver, o botão principal fixo no fim (Salvar, Enviar para análise).
 */
export function SubScreen({ title, children, footer, gap = 16 }: { title: string; children: ReactNode; footer?: ReactNode; gap?: number }) {
  const { t } = useTheme();
  const insets = useSafeAreaInsets();
  return (
    <KeyboardAvoidingView style={{ flex: 1, backgroundColor: t.bg }} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12, paddingTop: Math.max(insets.top, 12) + 8, paddingHorizontal: 16, paddingBottom: 12, borderBottomWidth: 1, borderBottomColor: t.line }}>
        <IconButton icon="back" label="Voltar" onPress={() => (router.canGoBack() ? router.back() : router.replace('/profile'))} />
        <Text size={18} weight={700} tracking={-0.02} style={{ flex: 1 }} numberOfLines={1}>
          {title}
        </Text>
      </View>
      <ScrollView
        style={{ flex: 1 }}
        keyboardShouldPersistTaps="handled"
        showsVerticalScrollIndicator={false}
        contentContainerStyle={{ padding: 20, gap, paddingBottom: footer ? 20 : Math.max(insets.bottom, 12) + 24 }}
      >
        {children}
      </ScrollView>
      {footer ? <View style={{ paddingHorizontal: 20, paddingTop: 8, paddingBottom: Math.max(insets.bottom, 12) + 12, gap: 12 }}>{footer}</View> : null}
    </KeyboardAvoidingView>
  );
}

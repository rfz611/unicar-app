import { useRef, useState } from 'react';
import { Modal, Pressable, ScrollView, useWindowDimensions, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import Svg, { Defs, LinearGradient, RadialGradient, Rect, Stop } from 'react-native-svg';
import { api, API_URL } from '@/api/client';
import type { AuthResponse } from '@/api/types';
import { Icon } from '@/components/Icon';
import { Logo, LogoMark } from '@/components/Logo';
import { Label, Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { Button, List, ListItem } from '@/components/ui';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';

type DemoAccount = { email: string; name: string; role: string };

/** Boas-vindas. Tocar 5 vezes no logo abre o modo apresentação (contas *.demo@ do backend). */
export default function Welcome() {
  const { t } = useTheme();
  const { say, sayError } = useToast();
  const insets = useSafeAreaInsets();
  const { width } = useWindowDimensions();
  const [demo, setDemo] = useState<DemoAccount[] | null>(null);
  const taps = useRef<number[]>([]);

  const tapLogo = async () => {
    const now = Date.now();
    taps.current = [...taps.current.filter((x) => now - x < 3000), now];
    if (taps.current.length < 5) return;
    taps.current = [];
    try {
      setDemo(await api.get<DemoAccount[]>('/api/dev/demo'));
    } catch (e) {
      sayError(e);
    }
  };

  return (
    <View style={{ flex: 1, backgroundColor: t.bg, overflow: 'hidden' }}>
      {/* brilho azul embaixo: radial-gradient(50% 50% at 50% 50%, accentGlow, transparente) */}
      <Svg pointerEvents="none" width={width + 120} height={520} style={{ position: 'absolute', left: -60, bottom: -220 }}>
        <Defs>
          <RadialGradient id="glow" cx="50%" cy="50%" rx="50%" ry="50%">
            <Stop offset="0" stopColor={t.accent} stopOpacity={t.name === 'dark' ? 0.5 : 0.28} />
            <Stop offset="1" stopColor={t.accent} stopOpacity={0} />
          </RadialGradient>
        </Defs>
        <Rect x={0} y={0} width={width + 120} height={520} fill="url(#glow)" />
      </Svg>
      {/* linha diagonal: linear-gradient(90deg, transparente, accent), rotate(-16deg) */}
      <Svg pointerEvents="none" width={520} height={2} style={{ position: 'absolute', left: -30, top: 560, transform: [{ translateX: -260 }, { rotate: '-16deg' }, { translateX: 260 }] }}>
        <Defs>
          <LinearGradient id="line" x1="0" y1="0" x2="1" y2="0">
            <Stop offset="0" stopColor={t.accent} stopOpacity={0} />
            <Stop offset="1" stopColor={t.accent} stopOpacity={1} />
          </LinearGradient>
        </Defs>
        <Rect x={0} y={0} width={520} height={1.5} fill="url(#line)" />
      </Svg>

      <View style={{ flex: 1, paddingTop: Math.max(insets.top, 20) + 28, paddingHorizontal: 24, paddingBottom: Math.max(insets.bottom, 16) + 24 }}>
        <Pressable onPress={tapLogo} accessibilityRole="image" accessibilityLabel="UniCar" style={{ alignSelf: 'flex-start' }}>
          <Logo />
        </Pressable>

        <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', gap: 40, paddingBottom: 56 }}>
          <AppIcon />
          <Text size={40} weight={700} tracking={-0.045} leading={1.02} align="center">
            {'Carona com quem\nestuda com você.'}
          </Text>
          <Button variant="inv" label="Entrar com e-mail da faculdade" style={{ alignSelf: 'stretch' }} onPress={() => say('O cadastro por e-mail chega na próxima etapa.')}>
            <Icon name="arrow" size={18} color={t.bg} strokeWidth={2.2} />
          </Button>
        </View>
      </View>

      <DemoSheet accounts={demo} onClose={() => setDemo(null)} />
    </View>
  );
}

/** Ícone de 168px: radial-gradient(130% 70% at 50% 118%, accent 0%, accentDeep 34%, #0B0B0C 66%). */
function AppIcon() {
  const { t } = useTheme();
  return (
    // box-shadow: 0 50px 110px -30px accent, inset 0 1px 0 rgba(255,255,255,.14)
    <View style={{ width: 168, height: 168, borderRadius: 44, boxShadow: `0 50px 110px -30px ${t.accent}` }}>
      <View style={{ flex: 1, borderRadius: 44, overflow: 'hidden', alignItems: 'center', justifyContent: 'center' }}>
        <Svg width={168} height={168} style={{ position: 'absolute', left: 0, top: 0 }}>
          <Defs>
            {/* elipse de 130% × 70% centrada em (50%, 118%) */}
            <RadialGradient id="icon" cx="0.5" cy="1.18" r="1" gradientTransform="translate(0.5 1.18) scale(1.3 0.7) translate(-0.5 -1.18)">
              <Stop offset="0" stopColor={t.accent} />
              <Stop offset="0.34" stopColor={t.accentDeep} />
              <Stop offset="0.66" stopColor="#0B0B0C" />
              <Stop offset="1" stopColor="#0B0B0C" />
            </RadialGradient>
          </Defs>
          <Rect x={0} y={0} width={168} height={168} fill="url(#icon)" />
          <Rect x={0} y={0} width={168} height={1} fill="rgba(255,255,255,0.14)" />
        </Svg>
        <View style={{ zIndex: 1 }}>
          <LogoMark size={96} color="#FFFFFF" dot="#FFFFFF" />
        </View>
      </View>
    </View>
  );
}

/** Lista das contas de demonstração (POST /api/dev/login), só com o backend em Development. */
function DemoSheet({ accounts, onClose }: { accounts: DemoAccount[] | null; onClose: () => void }) {
  const { t } = useTheme();
  const { signIn } = useSession();
  const { sayError, say } = useToast();
  const insets = useSafeAreaInsets();
  const [busy, setBusy] = useState<string | null>(null);

  const enter = async (email: string) => {
    setBusy(email);
    try {
      await signIn(await api.post<AuthResponse>('/api/dev/login', { email }));
      onClose();
    } catch (e) {
      sayError(e);
    } finally {
      setBusy(null);
    }
  };

  const reset = async () => {
    setBusy('reset');
    try {
      await api.post('/api/dev/demo/reset');
      say('Demonstração recriada com horários de agora.');
    } catch (e) {
      sayError(e);
    } finally {
      setBusy(null);
    }
  };

  return (
    <Modal visible={!!accounts} transparent animationType="slide" onRequestClose={onClose}>
      <Pressable style={{ flex: 1, backgroundColor: 'rgba(0,0,0,0.5)' }} onPress={onClose} accessibilityLabel="Fechar" />
      <View style={{ backgroundColor: t.bg, borderTopLeftRadius: 28, borderTopRightRadius: 28, borderTopWidth: 1, borderColor: t.line, maxHeight: '80%', paddingBottom: Math.max(insets.bottom, 16) + 8 }}>
        <View style={{ padding: 20, paddingBottom: 12, gap: 4 }}>
          <Text size={24} weight={700} tracking={-0.04}>
            Modo apresentação
          </Text>
          <Label>{API_URL}</Label>
        </View>
        <ScrollView contentContainerStyle={{ paddingHorizontal: 16, gap: 12 }}>
          <List>
            {(accounts ?? []).map((a, i, all) => (
              <ListItem key={a.email} last={i === all.length - 1} minHeight={64} onPress={() => enter(a.email)}>
                <View style={{ flex: 1, gap: 2, paddingVertical: 10 }}>
                  <Text size={15} weight={600}>
                    {a.name}
                  </Text>
                  <Text size={13} color={t.mut2}>
                    {a.role}
                  </Text>
                </View>
                {busy === a.email ? <Text size={13} color={t.mut2}>Entrando…</Text> : <Icon name="chevron" size={16} color={t.mut2} strokeWidth={2.2} />}
              </ListItem>
            ))}
          </List>
          <Button variant="ghost" label="Recriar demonstração" busy={busy === 'reset'} onPress={reset} />
        </ScrollView>
      </View>
    </Modal>
  );
}

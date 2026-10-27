import { router, useFocusEffect } from 'expo-router';
import { useCallback, useState } from 'react';
import { View } from 'react-native';
import { Icon } from '@/components/Icon';
import { Page } from '@/components/Page';
import { H1, Text } from '@/components/Text';
import { Button, Chip } from '@/components/ui';
import { OfferForm } from '@/features/rides/OfferForm';
import { DriverMode } from '@/features/trips/DriverMode';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';

/**
 * Aba Oferecer: bloqueada até virar motorista (User.DriverStatus). Motorista aprovado escolhe entre
 * publicar carona marcada (formulário do protótipo) e "Dirigir agora" (recebe os pedidos do pedir agora).
 */
export default function Offer() {
  const { t } = useTheme();
  const { user, refreshUser } = useSession();
  const [mode, setMode] = useState<'scheduled' | 'now'>('scheduled');
  const status = user?.driverStatus ?? 'None';
  // aprovado no painel enquanto o app estava aberto: libera ao voltar para a aba
  useFocusEffect(
    useCallback(() => {
      if (status !== 'Approved') refreshUser().catch(() => {});
    }, [status, refreshUser]),
  );
  if (status === 'Approved') {
    return (
      <DriverMode
        header={
          <>
            <H1>Oferecer carona</H1>
            <View accessibilityRole="radiogroup" accessibilityLabel="Tipo de carona" style={{ flexDirection: 'row', gap: 8, marginTop: -4 }}>
              <Chip label="Carona marcada" on={mode === 'scheduled'} minHeight={48} style={{ flex: 1 }} onPress={() => setMode('scheduled')} />
              <Chip label="Dirigir agora" on={mode === 'now'} minHeight={48} style={{ flex: 1 }} onPress={() => setMode('now')} />
            </View>
          </>
        }
        showDrive={mode === 'now'}
        footer={mode === 'scheduled' ? <OfferForm /> : null}
      />
    );
  }
  const review = status === 'InReview';
  return (
    <Page>
      <H1>Oferecer carona</H1>
      <View style={{ gap: 16, backgroundColor: t.s1, borderWidth: 1, borderColor: t.line, borderRadius: 24, padding: 22 }}>
        <View style={{ width: 48, height: 48, borderRadius: 16, backgroundColor: t.accent, alignItems: 'center', justifyContent: 'center' }}>
          <Icon name="car" size={24} color="#FFFFFF" />
        </View>
        <Text size={20} weight={700} tracking={-0.03}>
          {review ? 'Seu cadastro está em análise' : 'Cadastre-se como motorista'}
        </Text>
        <Text size={15} leading={1.5} color={t.mut}>
          {review
            ? 'Assim que a CNH e o veículo forem conferidos, você pode publicar caronas.'
            : 'Para oferecer caronas, envie sua CNH e os dados do veículo. É rápido e fica salvo no seu perfil.'}
        </Text>
        {!review ? <Button variant="acc" label="Cadastrar CNH e veículo" onPress={() => router.push('/driver')} /> : null}
      </View>
    </Page>
  );
}

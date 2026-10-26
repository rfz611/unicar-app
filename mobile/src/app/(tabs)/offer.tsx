import { View } from 'react-native';
import { Icon } from '@/components/Icon';
import { EmptyBox, Page } from '@/components/Page';
import { H1, Text } from '@/components/Text';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';

/** Aba Oferecer: bloqueada até virar motorista (User.DriverStatus). O formulário entra na etapa 5. */
export default function Offer() {
  const { t } = useTheme();
  const { user } = useSession();
  const status = user?.driverStatus ?? 'None';
  if (status === 'Approved') {
    return (
      <Page>
        <H1>Oferecer carona</H1>
        <EmptyBox>Você é motorista verificado. O formulário de oferecer carona chega na etapa 5.</EmptyBox>
      </Page>
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
      </View>
    </Page>
  );
}

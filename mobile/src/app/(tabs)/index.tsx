import { router } from 'expo-router';
import { Pressable, View } from 'react-native';
import { Logo } from '@/components/Logo';
import { Avatar, EmptyBox, Page } from '@/components/Page';
import { Text } from '@/components/Text';
import { useSession } from '@/session/SessionProvider';

/** Aba Caronas: "Para onde, {nome}?". A busca, o mapa e as caronas chegam na etapa 3. */
export default function Rides() {
  const { user } = useSession();
  const first = user?.fullName.split(' ')[0] ?? 'universitário';
  return (
    <Page gap={22}>
      <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
        <Logo mark={24} text={21} />
        <Pressable accessibilityRole="button" accessibilityLabel="Abrir perfil" onPress={() => router.navigate('/profile')}>
          <Avatar name={user?.fullName} photoUrl={user?.photoUrl} size={44} ring={2} />
        </Pressable>
      </View>
      <Text size={40} weight={700} tracking={-0.05} leading={1} style={{ marginTop: 6 }}>
        {`Para onde, ${first}?`}
      </Text>
      <EmptyBox>Busca de destino, mapa e caronas chegam na etapa 3.</EmptyBox>
    </Page>
  );
}

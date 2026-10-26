import { View } from 'react-native';
import { EmptyBox, Page } from '@/components/Page';
import { H1, Label, Text } from '@/components/Text';
import { useTheme } from '@/theme/ThemeProvider';

/** Aba Viagens. Os dados de GET /api/me/trips entram na etapa 5. */
export default function Trips() {
  const { t } = useTheme();
  return (
    <Page>
      <H1>Viagens</H1>
      <View style={{ gap: 10 }}>
        <Label>Próximas</Label>
        <EmptyBox>Nenhuma carona marcada. Reserve uma carona marcada ou ofereça a sua.</EmptyBox>
      </View>
      <View style={{ gap: 10 }}>
        <Label>Histórico</Label>
        <Text size={15} color={t.mut} style={{ padding: 4 }}>
          Suas caronas concluídas aparecem aqui.
        </Text>
      </View>
    </Page>
  );
}

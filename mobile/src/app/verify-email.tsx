import { View } from 'react-native';
import { Icon } from '@/components/Icon';
import { SubScreen } from '@/components/SubScreen';
import { Text } from '@/components/Text';
import { List, ListItem } from '@/components/ui';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';

/** E-mail da faculdade (isVerify do protótipo): o selo vem do login por código no e-mail institucional. */
export default function VerifyEmail() {
  const { t } = useTheme();
  const { user } = useSession();
  if (!user) return null;
  const verifiedAt = new Date(user.emailVerifiedAt).toLocaleDateString('pt-BR');
  return (
    <SubScreen title="E-mail da faculdade">
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 14, backgroundColor: t.s1, borderWidth: 1, borderColor: t.line, borderRadius: 20, padding: 18 }}>
        <View style={{ width: 44, height: 44, borderRadius: 22, backgroundColor: t.accent, alignItems: 'center', justifyContent: 'center' }}>
          <Icon name="check" size={22} color="#FFFFFF" strokeWidth={2.4} />
        </View>
        <View style={{ flex: 1, gap: 2 }}>
          <Text size={16} weight={600}>
            Universitário verificado
          </Text>
          <Text size={14} color={t.mut} numberOfLines={1}>
            {user.email}
          </Text>
        </View>
      </View>
      <List>
        <ListItem>
          <Text size={16}>Faculdade</Text>
          <Text size={14} color={t.mut} numberOfLines={1} style={{ flexShrink: 1 }}>
            {user.campus ? `${user.university} · ${user.campus.name}` : user.university}
          </Text>
        </ListItem>
        <ListItem last>
          <Text size={16}>Verificado em</Text>
          <Text size={14} color={t.mut}>
            {verifiedAt}
          </Text>
        </ListItem>
      </List>
      <Text size={14} leading={1.5} color={t.mut}>
        O selo aparece no seu perfil para motoristas e passageiros. Se você trocar de faculdade, confirme o novo e-mail.
      </Text>
    </SubScreen>
  );
}

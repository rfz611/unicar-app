import { useState } from 'react';
import { View } from 'react-native';
import { api } from '@/api/client';
import type { Preferences } from '@/api/types';
import { Icon } from '@/components/Icon';
import { Avatar, Page } from '@/components/Page';
import { Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { Button, List, ListItem } from '@/components/ui';
import { useSession } from '@/session/SessionProvider';
import { userTags } from '@/lib/user';
import { useTheme } from '@/theme/ThemeProvider';

/** Aba Perfil. Foto, editar, motorista e configurações completas entram na etapa 5. */
export default function Profile() {
  const { t, preference, setPreference } = useTheme();
  const { user, signOut } = useSession();
  const { sayError } = useToast();
  const [saving, setSaving] = useState(false);
  if (!user) return null;
  const light = t.name === 'light';

  // Aparência: troca na hora e salva em /api/users/me/preferences (vale no próximo login também).
  const toggleTheme = async () => {
    const next = light ? 'Dark' : 'Light';
    const before = preference;
    setPreference(next);
    setSaving(true);
    try {
      const prefs = await api.get<Preferences>('/api/users/me/preferences');
      await api.put<Preferences>('/api/users/me/preferences', { ...prefs, theme: next });
    } catch (e) {
      setPreference(before);
      sayError(e);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Page>
      <View style={{ alignItems: 'center', gap: 12, paddingTop: 8 }}>
        <Avatar name={user.fullName} photoUrl={user.photoUrl} size={104} ring={2.6} />
        <Text size={28} weight={700} tracking={-0.04} align="center" style={{ marginTop: 4 }}>
          {user.fullName}
        </Text>
        {user.bio ? (
          <Text size={15} leading={1.45} color={t.mut} align="center" style={{ maxWidth: 300 }}>
            {user.bio}
          </Text>
        ) : null}
        <View style={{ flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'center', gap: 8 }}>
          {userTags(user).map((label) => (
            <View key={label} style={{ borderWidth: 1, borderColor: t.line, borderRadius: 99, paddingVertical: 7, paddingHorizontal: 12, backgroundColor: t.s1 }}>
              <Text size={13} weight={500}>
                {label}
              </Text>
            </View>
          ))}
        </View>
      </View>

      <List>
        <ListItem>
          <Text size={16}>E-mail da faculdade</Text>
          <Text size={13} weight={600} color={t.accentText}>
            Verificado
          </Text>
        </ListItem>
        <ListItem onPress={saving ? undefined : toggleTheme} last>
          <Text size={16}>Aparência</Text>
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
            <Text size={13} color={t.mut2}>
              {light ? 'Claro' : 'Escuro'}
            </Text>
            <Icon name="chevron" size={16} color={t.mut2} strokeWidth={2.2} />
          </View>
        </ListItem>
      </List>

      <Button variant="ghost" label="Sair da conta" textColor={t.err} onPress={signOut} />
    </Page>
  );
}

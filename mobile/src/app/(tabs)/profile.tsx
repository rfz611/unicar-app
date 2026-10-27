import { router, useFocusEffect } from 'expo-router';
import { useCallback, useState } from 'react';
import { ActivityIndicator, Pressable, View } from 'react-native';
import { api } from '@/api/client';
import type { DriverApplication, MyTrips, Preferences, User } from '@/api/types';
import { Icon } from '@/components/Icon';
import { Avatar, Page } from '@/components/Page';
import { Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { IconButton, List, ListItem } from '@/components/ui';
import { imageForm, pickImage } from '@/lib/images';
import { userTags } from '@/lib/user';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';

type Car = { model: string; color: string; plate: string };

/** Aba Perfil (isProfile do protótipo): foto, etiquetas, virar motorista, editar, viagens, e-mail, aparência, configurações. */
export default function Profile() {
  const { t, preference, setPreference } = useTheme();
  const { user, refreshUser, setUser } = useSession();
  const { say, sayError } = useToast();
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [trips, setTrips] = useState<MyTrips | null>(null);
  const [car, setCar] = useState<Car | null>(null);

  // ao abrir a aba: status de motorista (pode ter sido aprovado), carro e resumo das viagens
  useFocusEffect(
    useCallback(() => {
      refreshUser().catch(() => {});
      api.get<MyTrips>('/api/me/trips').then(setTrips, () => {});
      api.get<DriverApplication>('/api/driver/application').then(
        (a) => setCar({ model: a.model, color: a.color, plate: a.plate }),
        () => api.get<Car>('/api/driver/vehicle').then(setCar, () => setCar(null)),
      );
    }, [refreshUser]),
  );

  if (!user) return null;
  const light = t.name === 'light';
  const dvs = user.driverStatus;

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

  const changePhoto = async () => {
    if (uploading) return;
    try {
      const image = await pickImage({ square: true });
      if (!image) return;
      setUploading(true);
      setUser(await api.upload<User>('/api/users/me/photo', await imageForm(image)));
      say('Foto atualizada.');
    } catch (e) {
      sayError(e);
    } finally {
      setUploading(false);
    }
  };

  const upcoming = trips?.upcoming.length ?? 0;
  const done = trips?.history.filter((h) => !h.status.includes('Cancelled')).length ?? 0;
  const tripsSummary = upcoming ? `${upcoming} ${upcoming === 1 ? 'próxima' : 'próximas'}` : done ? `${done} ${done === 1 ? 'concluída' : 'concluídas'}` : 'Nenhuma ainda';

  const none = dvs === 'None' || dvs === 'Rejected';
  const carText = car ? `${car.model} ${car.color.toLowerCase()}, placa ${car.plate}` : '';
  const driverTitle = dvs === 'Approved' ? 'Motorista verificado' : dvs === 'InReview' ? 'Cadastro em análise' : dvs === 'Rejected' ? 'Cadastro recusado' : 'Virar motorista';
  const driverSub = dvs === 'None' ? 'Cadastre CNH e veículo para oferecer caronas' : dvs === 'Rejected' ? 'Veja o motivo e envie de novo' : carText;

  return (
    <Page>
      <View style={{ flexDirection: 'row', justifyContent: 'flex-end', marginTop: -8, marginBottom: -44, zIndex: 1 }}>
        <IconButton icon="gear" label="Configurações" onPress={() => router.push('/settings')} />
      </View>
      <View style={{ alignItems: 'center', gap: 12, paddingTop: 8 }}>
        <Pressable accessibilityRole="button" accessibilityLabel="Alterar foto de perfil" onPress={changePhoto} style={{ borderRadius: 99 }}>
          <Avatar name={user.fullName} photoUrl={user.photoUrl} size={104} ring={2.6} />
          <View style={{ position: 'absolute', right: -2, bottom: -2, width: 34, height: 34, borderRadius: 17, backgroundColor: t.fg, borderWidth: 3, borderColor: t.bg, alignItems: 'center', justifyContent: 'center' }}>
            {uploading ? <ActivityIndicator size="small" color={t.bg} /> : <Icon name="camera" size={15} color={t.bg} strokeWidth={2.2} />}
          </View>
        </Pressable>
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

      <Pressable
        accessibilityRole="button"
        onPress={() => router.push('/driver')}
        style={({ pressed }) => ({
          flexDirection: 'row',
          alignItems: 'center',
          gap: 14,
          borderRadius: 20,
          padding: 18,
          opacity: pressed ? 0.9 : 1,
          ...(none ? { backgroundColor: t.accent } : { backgroundColor: t.s1, borderWidth: 1, borderColor: t.line }),
        })}
      >
        <Icon name="car" size={24} color={none ? '#FFFFFF' : t.fg} />
        <View style={{ flex: 1, gap: 2 }}>
          <Text size={16} weight={700} color={none ? '#FFFFFF' : t.fg}>
            {driverTitle}
          </Text>
          {driverSub ? (
            <Text size={13} weight={500} color={none ? '#FFFFFF' : t.fg} style={{ opacity: 0.85 }} numberOfLines={1}>
              {driverSub}
            </Text>
          ) : null}
        </View>
        <Icon name="chevron" size={18} color={none ? '#FFFFFF' : t.mut2} strokeWidth={2.2} />
      </Pressable>

      <List>
        <ListItem onPress={() => router.push('/edit-profile')}>
          <Text size={16}>Editar perfil</Text>
          <Icon name="chevron" size={16} color={t.mut2} strokeWidth={2.2} />
        </ListItem>
        <ListItem onPress={() => router.navigate('/trips')}>
          <Text size={16}>Minhas viagens</Text>
          <Text size={13} color={t.mut2}>
            {trips ? tripsSummary : ''}
          </Text>
        </ListItem>
        <ListItem onPress={() => router.push('/verify-email')}>
          <Text size={16}>E-mail da faculdade</Text>
          <Text size={13} weight={600} color={t.accentText}>
            Verificado
          </Text>
        </ListItem>
        <ListItem onPress={saving ? undefined : toggleTheme}>
          <Text size={16}>Aparência</Text>
          <Text size={13} color={t.mut2}>
            {light ? 'Claro' : 'Escuro'}
          </Text>
        </ListItem>
        <ListItem onPress={() => router.push('/settings')} last>
          <Text size={16}>Configurações</Text>
          <Icon name="chevron" size={16} color={t.mut2} strokeWidth={2.2} />
        </ListItem>
      </List>
    </Page>
  );
}

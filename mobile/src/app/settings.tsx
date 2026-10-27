import Constants from 'expo-constants';
import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { Pressable, View } from 'react-native';
import { api } from '@/api/client';
import type { Preferences, User } from '@/api/types';
import { SubScreen } from '@/components/SubScreen';
import { Label, Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { Button, Chip, Field, List, ListItem, Toggle } from '@/components/ui';
import { digits, mask } from '@/lib/format';
import { updateBody } from '@/lib/user';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';

const PREFS = [
  { key: 'rideAlerts', label: 'Avisos de carona', sub: 'Quando alguém aceitar ou mandar mensagem' },
  { key: 'alwaysShareTrip', label: 'Compartilhar trajeto sempre', sub: 'Envia o trajeto para seu contato a cada viagem' },
  { key: 'showCourse', label: 'Mostrar meu curso', sub: 'Aparece no seu perfil para os outros' },
] as const;

const DRIVER_SHORT = { Approved: 'Aprovado', InReview: 'Em análise', Rejected: 'Recusado', None: 'Não cadastrado' } as const;

/** Configurações (isSettings do protótipo): aparência, preferências, conta, contato de confiança, sobre, sair. */
export default function Settings() {
  const { t, setPreference } = useTheme();
  const { user, setUser, refreshUser, signOut } = useSession();
  const { say, sayError } = useToast();
  const [prefs, setPrefs] = useState<Preferences | null>(null);
  const [editing, setEditing] = useState(false);
  const [trusted, setTrusted] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    api.get<Preferences>('/api/users/me/preferences').then(setPrefs, sayError);
  }, [sayError]);

  if (!user) return null;
  const theme = t.name === 'light' ? 'Light' : 'Dark';

  /** Salva na hora; se a API recusar, volta como estava. */
  const savePrefs = async (next: Preferences) => {
    const before = prefs;
    setPrefs(next);
    if (next.theme !== before?.theme) setPreference(next.theme);
    try {
      setPrefs(await api.put<Preferences>('/api/users/me/preferences', next));
      // "Mostrar meu curso" muda as etiquetas do perfil
      if (next.showCourse !== before?.showCourse) await refreshUser();
    } catch (e) {
      setPrefs(before);
      if (before) setPreference(before.theme);
      sayError(e);
    }
  };

  const saveTrusted = async () => {
    if (digits(trusted).length !== 11) return say('Digite um telefone com DDD.');
    setBusy(true);
    try {
      setUser(await api.put<User>('/api/users/me', { ...updateBody(user), trustedContactPhone: trusted }));
      setEditing(false);
      say('Contato de confiança salvo.');
    } catch (e) {
      sayError(e);
    } finally {
      setBusy(false);
    }
  };

  return (
    <SubScreen title="Configurações" gap={22}>
      <View style={{ gap: 10 }}>
        <Label>Aparência</Label>
        <View accessibilityRole="radiogroup" accessibilityLabel="Aparência" style={{ flexDirection: 'row', gap: 8 }}>
          {(
            [
              ['Dark', 'Escuro'],
              ['Light', 'Claro'],
            ] as const
          ).map(([key, label]) => (
            <Chip key={key} label={label} on={theme === key} minHeight={52} style={{ flex: 1 }} onPress={() => prefs && savePrefs({ ...prefs, theme: key })} />
          ))}
        </View>
      </View>

      <View style={{ gap: 10 }}>
        <Label>Preferências</Label>
        <List>
          {PREFS.map((p, i) => (
            <ListItem key={p.key} minHeight={68} last={i === PREFS.length - 1} style={{ paddingVertical: 12 }}>
              <View style={{ flex: 1, gap: 2 }}>
                <Text size={15}>{p.label}</Text>
                <Text size={13} color={t.mut2}>
                  {p.sub}
                </Text>
              </View>
              <Toggle label={p.label} on={!!prefs?.[p.key]} onChange={(v) => prefs && savePrefs({ ...prefs, [p.key]: v })} />
            </ListItem>
          ))}
        </List>
      </View>

      <View style={{ gap: 10 }}>
        <Label>Conta</Label>
        <List>
          <ListItem onPress={() => router.push('/edit-profile')}>
            <Text size={16}>Dados pessoais</Text>
            <Text size={13} color={t.mut2}>
              Nome, curso e turno
            </Text>
          </ListItem>
          <ListItem onPress={() => router.push('/verify-email')}>
            <Text size={16}>E-mail da faculdade</Text>
            <Text size={13} weight={600} color={t.accentText}>
              Verificado
            </Text>
          </ListItem>
          <ListItem onPress={() => router.push('/driver')} last>
            <Text size={16}>Motorista e veículo</Text>
            <Text size={13} color={t.mut2}>
              {DRIVER_SHORT[user.driverStatus]}
            </Text>
          </ListItem>
        </List>
      </View>

      <View style={{ gap: 10 }}>
        <Label>Segurança</Label>
        <List>
          <ListItem minHeight={68} last={!editing} style={{ paddingVertical: 12 }}>
            <View style={{ flex: 1, gap: 2 }}>
              <Text size={16}>Contato de confiança</Text>
              <Text size={13} color={t.mut2}>
                {user.trustedContactPhone ? `Recebe seu trajeto: ${user.trustedContactPhone}` : 'Ninguém definido'}
              </Text>
            </View>
            <Pressable
              accessibilityRole="button"
              onPress={() => {
                setEditing(!editing);
                setTrusted(user.trustedContactPhone ?? '');
              }}
              style={{ minHeight: 44, justifyContent: 'center' }}
            >
              <Text size={14} weight={600} color={t.accentText}>
                {editing ? 'Fechar' : user.trustedContactPhone ? 'Alterar' : 'Adicionar'}
              </Text>
            </Pressable>
          </ListItem>
          {editing ? (
            <View style={{ flexDirection: 'row', gap: 8, paddingVertical: 12, paddingHorizontal: 18 }}>
              <Field
                containerStyle={{ flex: 1 }}
                style={{ minHeight: 48, flex: 1 }}
                value={trusted}
                onChangeText={(v) => setTrusted(mask(v, '(99) 99999-9999'))}
                onSubmitEditing={saveTrusted}
                placeholder="(41) 99999-0000"
                keyboardType="phone-pad"
                accessibilityLabel="Telefone do contato"
                maxLength={15}
              />
              <Button variant="inv" label="Salvar" size={15} minHeight={48} busy={busy} onPress={saveTrusted} />
            </View>
          ) : null}
        </List>
      </View>

      <View style={{ gap: 10 }}>
        <Label>Sobre</Label>
        <List>
          <ListItem onPress={() => say('Suporte: fale com a equipe UniCar pelo chat, em breve no app.')}>
            <Text size={16}>Ajuda e suporte</Text>
          </ListItem>
          <ListItem onPress={() => say('Termos e privacidade serão escritos antes do lançamento.')}>
            <Text size={16}>Termos e privacidade</Text>
          </ListItem>
          <ListItem last>
            <Text size={16}>Versão</Text>
            <Text size={13} color={t.mut2}>
              {Constants.expoConfig?.version ?? '0.1.0'}
            </Text>
          </ListItem>
        </List>
      </View>

      <Button variant="ghost" label="Sair da conta" textColor={t.err} onPress={signOut} />
    </SubScreen>
  );
}

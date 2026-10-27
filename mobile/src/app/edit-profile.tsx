import { router } from 'expo-router';
import { useState } from 'react';
import { View } from 'react-native';
import { api, messageOf } from '@/api/client';
import type { User } from '@/api/types';
import { missingField, ProfileFields, type ProfileDraft } from '@/components/ProfileForm';
import { SubScreen } from '@/components/SubScreen';
import { Label, Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { Button, Chip, ErrorText, Field } from '@/components/ui';
import { toBrDate, toIsoDate } from '@/lib/format';
import { updateBody } from '@/lib/user';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';

const GENDERS = ['Mulher', 'Homem', 'Prefiro não dizer'];

/** Editar perfil (isEdit do protótipo). CPF e e-mail não mudam; a API valida idade, celular e curso. */
export default function EditProfile() {
  const { user } = useSession();
  if (!user) return null;
  return <EditForm user={user} />;
}

function EditForm({ user }: { user: User }) {
  const { t } = useTheme();
  const { setUser } = useSession();
  const { say } = useToast();
  const [draft, setDraft] = useState<ProfileDraft>({
    name: user.fullName,
    birth: toBrDate(user.birthDate),
    phone: user.phone,
    cpf: '',
    courseQuery: user.course,
    course: user.course,
    shift: user.shift,
    period: String(user.period || ''),
  });
  const [gender, setGender] = useState(user.gender ?? '');
  const [bio, setBio] = useState(user.bio ?? '');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const save = async () => {
    if (busy) return;
    const missing = missingField(draft, false);
    if (missing) return setError(missing);
    setBusy(true);
    setError('');
    try {
      const updated = await api.put<User>('/api/users/me', {
        ...updateBody(user),
        fullName: draft.name.trim(),
        birthDate: toIsoDate(draft.birth),
        phone: draft.phone,
        course: draft.course,
        period: Number(draft.period),
        shift: draft.shift,
        gender: gender || null,
        bio: bio.trim() || null,
      });
      setUser(updated);
      say('Perfil atualizado.');
      router.back();
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <SubScreen
      title="Editar perfil"
      footer={
        <>
          <ErrorText>{error}</ErrorText>
          <Button variant="inv" label="Salvar" busy={busy} onPress={save} />
        </>
      }
    >
      <ProfileFields
        draft={draft}
        onChange={(d) => {
          setDraft(d);
          setError('');
        }}
        cpf={{ masked: user.cpfMasked }}
      />
      <View style={{ gap: 8 }}>
        <Label>Gênero (opcional)</Label>
        <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
          {GENDERS.map((g) => (
            <Chip key={g} label={g} on={gender === g} minHeight={44} onPress={() => setGender(gender === g ? '' : g)} />
          ))}
        </View>
        <Text size={12} leading={1.4} color={t.mut2}>
          Usado só para a opção de carona entre mulheres, se você quiser.
        </Text>
      </View>
      <View style={{ gap: 8 }}>
        <Field
          label="Sobre você"
          value={bio}
          onChangeText={(v) => setBio(v.slice(0, 140))}
          maxLength={140}
          multiline
          numberOfLines={3}
          placeholder="Ex.: Saio do Água Verde todo dia às 7h. Curto rock no carro."
          style={{ paddingVertical: 14, minHeight: 96, textAlignVertical: 'top', lineHeight: 24 }}
        />
        <Text size={12} color={t.mut2} align="right">{`${bio.length}/140`}</Text>
      </View>
    </SubScreen>
  );
}

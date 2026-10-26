import { router } from 'expo-router';
import { useState } from 'react';
import { api, messageOf } from '@/api/client';
import type { AuthResponse } from '@/api/types';
import { AuthScreen } from '@/components/AuthScreen';
import { emptyDraft, missingField, ProfileFields, type ProfileDraft } from '@/components/ProfileForm';
import { H1, Text } from '@/components/Text';
import { Button, ErrorText } from '@/components/ui';
import { toIsoDate } from '@/lib/format';
import { useAuthFlow } from '@/session/AuthFlow';
import { useTheme } from '@/theme/ThemeProvider';

/** Passo 3: Seu perfil → POST /api/auth/register (com o token de cadastro do passo 2). */
export default function Signup() {
  const { t } = useTheme();
  const { signupToken, setPending } = useAuthFlow();
  const [draft, setDraft] = useState<ProfileDraft>(emptyDraft);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const ok = !missingField(draft, true);

  const submit = async () => {
    if (busy) return;
    const missing = missingField(draft, true);
    if (missing) {
      setError(missing);
      return;
    }
    if (!signupToken) {
      setError('Confirme seu e-mail de novo para terminar o cadastro.');
      return;
    }
    setBusy(true);
    setError('');
    try {
      const auth = await api.post<AuthResponse>(
        '/api/auth/register',
        {
          fullName: draft.name.trim(),
          birthDate: toIsoDate(draft.birth),
          cpf: draft.cpf,
          phone: draft.phone,
          course: draft.course,
          period: Number(draft.period),
          shift: draft.shift,
        },
        { bearer: signupToken },
      );
      setPending(auth);
      router.replace('/done');
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <AuthScreen
      step={2}
      scroll
      footer={
        <>
          <ErrorText>{error}</ErrorText>
          <Button variant={ok ? 'acc' : 'off'} label="Criar meu perfil" busy={busy} onPress={submit} />
        </>
      }
    >
      <H1>Seu perfil</H1>
      <Text size={15} leading={1.5} color={t.mut} style={{ marginTop: -6, marginBottom: 4 }}>
        Use seu nome completo, como está na faculdade.
      </Text>
      <ProfileFields
        draft={draft}
        onChange={(d) => {
          setDraft(d);
          setError('');
        }}
        cpf="edit"
        onSubmit={submit}
      />
    </AuthScreen>
  );
}

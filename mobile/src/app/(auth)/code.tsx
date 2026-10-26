import { router } from 'expo-router';
import { useRef, useState, type ComponentRef } from 'react';
import { Pressable, TextInput, View } from 'react-native';
import { api, messageOf } from '@/api/client';
import { AuthScreen } from '@/components/AuthScreen';
import { H1, Label, Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { Button, ErrorText } from '@/components/ui';
import { useAuthFlow } from '@/session/AuthFlow';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';
import { fonts } from '@/theme/tokens';

type VerifyResponse = { isNewUser: boolean; accessToken: string | null; signupToken: string | null };

/** Passo 2: os 6 números → POST /api/auth/verify-code. Conta existente entra direto; nova vai para Seu perfil. */
export default function Code() {
  const { t } = useTheme();
  const { say } = useToast();
  const { signIn } = useSession();
  const { email, setSignupToken } = useAuthFlow();
  const [code, setCode] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [resent, setResent] = useState(false);
  const input = useRef<ComponentRef<typeof TextInput>>(null);

  const verify = async (value = code) => {
    if (busy || value.length !== 6) return;
    setBusy(true);
    setError('');
    try {
      const res = await api.post<VerifyResponse>('/api/auth/verify-code', { email: email.trim(), code: value });
      if (!res.isNewUser && res.accessToken) {
        await signIn({ accessToken: res.accessToken });
        return;
      }
      setSignupToken(res.signupToken);
      router.push('/signup');
    } catch (e) {
      setError(messageOf(e));
      setCode('');
    } finally {
      setBusy(false);
    }
  };

  const resend = async () => {
    try {
      await api.post('/api/auth/request-code', { email: email.trim() });
      setResent(true);
      say('Código reenviado.');
    } catch (e) {
      say(messageOf(e));
    }
  };

  return (
    <AuthScreen
      step={1}
      footer={
        <>
          {__DEV__ ? <Label>Em desenvolvimento, o código aparece no terminal da API.</Label> : null}
          <Button variant={code.length === 6 ? 'acc' : 'off'} label="Verificar" busy={busy} onPress={() => verify()} />
        </>
      }
    >
      <H1>{'Confere seu\ne-mail.'}</H1>
      <Text size={15} leading={1.5} color={t.mut} style={{ marginTop: 12 }}>
        {'Mandamos 6 números para '}
        <Text size={15} weight={500}>
          {email.trim()}
        </Text>
        .
      </Text>

      <Pressable onPress={() => input.current?.focus()} accessibilityLabel="Código de verificação" style={{ marginTop: 36, height: 68 }}>
        <View style={{ flexDirection: 'row', gap: 8, height: 68 }} pointerEvents="none">
          {[0, 1, 2, 3, 4, 5].map((i) => (
            <View
              key={i}
              style={{
                flex: 1,
                borderRadius: 14,
                backgroundColor: t.s1,
                borderWidth: 1.5,
                borderColor: i === code.length ? t.accent : error ? t.err : t.line,
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Text size={28} weight={600} tabular>
                {code[i] ?? ''}
              </Text>
            </View>
          ))}
        </View>
        <TextInput
          ref={input}
          value={code}
          onChangeText={(v) => {
            const clean = v.replace(/\D/g, '').slice(0, 6);
            setCode(clean);
            setError('');
            if (clean.length === 6) verify(clean);
          }}
          keyboardType="number-pad"
          textContentType="oneTimeCode"
          autoComplete="one-time-code"
          maxLength={6}
          autoFocus
          caretHidden
          style={{ position: 'absolute', left: 0, top: 0, right: 0, bottom: 0, opacity: 0, fontFamily: fonts[400], color: 'transparent' }}
        />
      </Pressable>

      <View style={{ marginTop: 20, flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
        <Text size={14} color={t.mut2}>
          Não chegou? Olhe o spam.
        </Text>
        <Pressable accessibilityRole="button" onPress={resend} style={{ minHeight: 44, paddingHorizontal: 4, justifyContent: 'center' }}>
          <Text size={14} weight={600}>
            {resent ? 'Reenviado' : 'Reenviar código'}
          </Text>
        </Pressable>
      </View>
      <View style={{ marginTop: 6 }}>
        <ErrorText>{error}</ErrorText>
      </View>
    </AuthScreen>
  );
}

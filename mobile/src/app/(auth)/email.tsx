import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { api, messageOf } from '@/api/client';
import { AuthScreen } from '@/components/AuthScreen';
import { Icon } from '@/components/Icon';
import { H1, Text } from '@/components/Text';
import { Button, ErrorText, Field } from '@/components/ui';
import { useAuthFlow, type University } from '@/session/AuthFlow';
import { useTheme } from '@/theme/ThemeProvider';

/** Passo 1: e-mail da faculdade → POST /api/auth/request-code. */
export default function Email() {
  const { t } = useTheme();
  const { email, setEmail, loadUniversities, universityOf } = useAuthFlow();
  const [unis, setUnis] = useState<University[]>([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    loadUniversities().then(setUnis, () => {});
  }, [loadUniversities]);

  const uni = universityOf(email, unis);

  const submit = async () => {
    if (busy) return;
    if (!uni) {
      setError('Esse e-mail não é de uma faculdade parceira. Use o institucional, por exemplo nome@ufpr.br.');
      return;
    }
    setBusy(true);
    setError('');
    try {
      await api.post('/api/auth/request-code', { email: email.trim() });
      router.push('/code');
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <AuthScreen step={0} footer={<Button variant={uni ? 'acc' : 'off'} label="Enviar código" busy={busy} onPress={submit} />}>
      <H1>{'Qual é o seu\ne-mail da faculdade?'}</H1>
      <Text size={15} leading={1.5} color={t.mut} style={{ marginTop: 12 }}>
        Mandamos um código para confirmar que você estuda lá.
      </Text>
      <Field
        containerStyle={{ marginTop: 32, gap: 10 }}
        label="E-mail institucional"
        value={email}
        onChangeText={(v) => {
          setEmail(v);
          setError('');
        }}
        onSubmitEditing={submit}
        placeholder="nome@ufpr.br"
        keyboardType="email-address"
        autoCapitalize="none"
        autoCorrect={false}
        autoComplete="email"
        textContentType="emailAddress"
        returnKeyType="send"
        autoFocus
        state={error ? 'error' : uni ? 'ok' : undefined}
      />
      {uni ? (
        <View
          style={{
            marginTop: 14,
            alignSelf: 'flex-start',
            flexDirection: 'row',
            alignItems: 'center',
            gap: 10,
            backgroundColor: t.s2,
            borderWidth: 1,
            borderColor: t.line,
            borderRadius: 99,
            paddingVertical: 8,
            paddingLeft: 10,
            paddingRight: 14,
          }}
        >
          <View style={{ width: 22, height: 22, borderRadius: 11, backgroundColor: t.accent, alignItems: 'center', justifyContent: 'center' }}>
            <Icon name="check" size={12} color="#FFFFFF" strokeWidth={3} />
          </View>
          <Text size={14} weight={500}>{`${uni.shortName} reconhecida`}</Text>
        </View>
      ) : null}
      <View style={{ marginTop: 14 }}>
        <ErrorText>{error}</ErrorText>
      </View>
    </AuthScreen>
  );
}

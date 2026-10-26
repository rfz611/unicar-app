import { Redirect } from 'expo-router';
import { useState } from 'react';
import { View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { Button, initialsOf } from '@/components/ui';
import { firstName, userTags } from '@/lib/user';
import { useAuthFlow } from '@/session/AuthFlow';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';

/** "Tudo pronto, {nome}." com as etiquetas do perfil criado. "Ver caronas" entra no app. */
export default function Done() {
  const { t } = useTheme();
  const insets = useSafeAreaInsets();
  const { pending } = useAuthFlow();
  const { signIn } = useSession();
  const { sayError } = useToast();
  const [busy, setBusy] = useState(false);

  if (!pending) return <Redirect href="/" />;
  const user = pending.user;

  const enter = async () => {
    setBusy(true);
    try {
      await signIn(pending);
    } catch (e) {
      sayError(e);
      setBusy(false);
    }
  };

  return (
    <View style={{ flex: 1, backgroundColor: t.bg, paddingTop: Math.max(insets.top, 20) + 28, paddingHorizontal: 24, paddingBottom: Math.max(insets.bottom, 16) + 24 }}>
      <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', gap: 24 }}>
        {/* box-shadow: 0 0 0 3px bg, 0 0 0 5px accent */}
        <View style={{ width: 130, height: 130, borderRadius: 65, borderWidth: 2, borderColor: t.accent, alignItems: 'center', justifyContent: 'center' }}>
          <View style={{ width: 120, height: 120, borderRadius: 60, backgroundColor: t.s3, alignItems: 'center', justifyContent: 'center' }}>
            <Text size={40} weight={600}>
              {initialsOf(user.fullName)}
            </Text>
          </View>
        </View>
        <Text size={36} weight={700} tracking={-0.045} leading={1.05} align="center">
          {`Tudo pronto,\n${firstName(user.fullName)}.`}
        </Text>
        <View style={{ flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'center', gap: 8 }}>
          {userTags(user).map((label) => (
            <View key={label} style={{ borderWidth: 1, borderColor: t.line, backgroundColor: t.s1, borderRadius: 99, paddingVertical: 8, paddingHorizontal: 14 }}>
              <Text size={14} weight={500}>
                {label}
              </Text>
            </View>
          ))}
        </View>
      </View>
      <Button variant="inv" label="Ver caronas" busy={busy} onPress={enter} />
    </View>
  );
}

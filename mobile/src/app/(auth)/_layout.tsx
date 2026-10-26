import { Stack } from 'expo-router';
import { AuthFlowProvider } from '@/session/AuthFlow';
import { useTheme } from '@/theme/ThemeProvider';

export default function AuthLayout() {
  const { t } = useTheme();
  return (
    <AuthFlowProvider>
      <Stack screenOptions={{ headerShown: false, contentStyle: { backgroundColor: t.bg }, animation: 'slide_from_right' }} />
    </AuthFlowProvider>
  );
}

import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from 'react';
import Animated, { useAnimatedStyle, useSharedValue, withTiming, Easing } from 'react-native-reanimated';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { messageOf } from '@/api/client';
import { useTheme } from '@/theme/ThemeProvider';
import { Text } from './Text';

type ToastValue = {
  /** Aviso curto no topo (2,6 s), como o say() do protótipo. */
  say: (text: string) => void;
  /** Mostra a mensagem de um erro da API (ApiError.message). */
  sayError: (e: unknown) => void;
};

const ToastContext = createContext<ToastValue | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const { t } = useTheme();
  const insets = useSafeAreaInsets();
  const [text, setText] = useState('');
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const rise = useSharedValue(0);

  const say = useCallback(
    (value: string) => {
      if (timer.current) clearTimeout(timer.current);
      setText(value);
      rise.set(0);
      // .rise do protótipo: sobe 36px em .45s com cubic-bezier(.2,.8,.2,1)
      rise.set(withTiming(1, { duration: 450, easing: Easing.bezier(0.2, 0.8, 0.2, 1) }));
      timer.current = setTimeout(() => setText(''), 2600);
    },
    [rise],
  );
  const sayError = useCallback((e: unknown) => say(messageOf(e)), [say]);
  useEffect(() => () => {
    if (timer.current) clearTimeout(timer.current);
  }, []);

  const style = useAnimatedStyle(() => ({
    opacity: rise.get(),
    transform: [{ translateY: (1 - rise.get()) * 36 }],
  }));

  return (
    <ToastContext.Provider value={{ say, sayError }}>
      {children}
      {text ? (
        <Animated.View
          accessibilityLiveRegion="polite"
          accessibilityRole="alert"
          pointerEvents="none"
          style={[
            {
              position: 'absolute',
              left: 16,
              right: 16,
              top: Math.max(insets.top, 12) + 8,
              zIndex: 9,
              backgroundColor: t.fg,
              borderRadius: 16,
              paddingVertical: 14,
              paddingHorizontal: 16,
              shadowColor: '#000',
              shadowOpacity: 0.35,
              shadowRadius: 20,
              shadowOffset: { width: 0, height: 20 },
              elevation: 10,
            },
            style,
          ]}
        >
          <Text size={15} weight={600} color={t.bg}>
            {text}
          </Text>
        </Animated.View>
      ) : null}
    </ToastContext.Provider>
  );
}

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast fora do ToastProvider');
  return ctx;
}

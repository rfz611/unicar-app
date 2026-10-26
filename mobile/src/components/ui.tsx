/**
 * Peças básicas, copiando as classes do protótipo:
 * .btn (.inv .acc .off .ghost), .field, .chip, .list/.li, .icb, .toggle.
 */
import { useState, type ComponentRef, type ReactNode, type Ref } from 'react';
import {
  ActivityIndicator,
  Platform,
  Pressable,
  TextInput,
  View,
  type PressableProps,
  type StyleProp,
  type TextInputProps,
  type ViewStyle,
} from 'react-native';
import { useTheme } from '@/theme/ThemeProvider';
import { fonts, HIT, radius } from '@/theme/tokens';
import { Icon, type IconName } from './Icon';
import { Label, Text } from './Text';

export type ButtonVariant = 'inv' | 'acc' | 'off' | 'ghost';

type ButtonProps = Omit<PressableProps, 'style' | 'children'> & {
  label?: string;
  children?: ReactNode;
  variant?: ButtonVariant;
  /** Mostra o carregando e ignora toques. */
  busy?: boolean;
  size?: number;
  minHeight?: number;
  style?: StyleProp<ViewStyle>;
  textColor?: string;
};

/** .btn: 56px, raio 18, 17px 600. inv = fundo do texto; acc = azul; off = desligado; ghost = contorno. */
export function Button({ label, children, variant = 'inv', busy, size = 17, minHeight = 56, style, textColor, disabled, ...rest }: ButtonProps) {
  const { t } = useTheme();
  const colors = {
    inv: { bg: t.fg, fg: t.bg, border: 'transparent' },
    acc: { bg: t.accent, fg: '#FFFFFF', border: 'transparent' },
    off: { bg: t.s3, fg: t.mut2, border: 'transparent' },
    ghost: { bg: t.s1, fg: t.fg, border: t.line },
  }[variant];
  const fg = textColor ?? colors.fg;
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ disabled: !!disabled || busy, busy }}
      disabled={disabled || busy}
      {...rest}
      style={({ pressed }) => [
        {
          minHeight,
          borderRadius: radius.btn,
          backgroundColor: colors.bg,
          borderWidth: variant === 'ghost' ? 1 : 0,
          borderColor: colors.border,
          flexDirection: 'row',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 10,
          paddingHorizontal: 16,
          opacity: pressed ? 0.85 : 1,
        },
        style,
      ]}
    >
      {busy ? (
        <ActivityIndicator color={fg} />
      ) : (
        <>
          {label ? (
            <Text size={size} weight={600} color={fg}>
              {label}
            </Text>
          ) : null}
          {children}
        </>
      )}
    </Pressable>
  );
}

type FieldProps = Omit<TextInputProps, 'ref'> & {
  ref?: Ref<ComponentRef<typeof TextInput>>;
  label?: string;
  /** Borda azul (campo confirmado) ou vermelha (erro). */
  state?: 'ok' | 'error';
  containerStyle?: StyleProp<ViewStyle>;
};

/** .field: 54px, raio 16, 17px, fundo s1, borda linha; borda azul quando confirmado, vermelha no erro. */
export function Field({ ref, label, state, containerStyle, style, onFocus, onBlur, ...rest }: FieldProps) {
  const { t } = useTheme();
  const [focused, setFocused] = useState(false);
  const borderColor = state === 'error' ? t.err : state === 'ok' || focused ? t.accent : t.line;
  const input = (
    <TextInput
      ref={ref}
      placeholderTextColor={t.mut2}
      selectionColor={t.accent}
      {...rest}
      onFocus={(e) => {
        setFocused(true);
        onFocus?.(e);
      }}
      onBlur={(e) => {
        setFocused(false);
        onBlur?.(e);
      }}
      style={[
        // no navegador: tira o contorno padrão; o foco é a borda azul, como no protótipo
        Platform.OS === 'web' ? ({ outlineWidth: 0 } as object) : null,
        {
          borderWidth: 1,
          borderColor,
          backgroundColor: t.s1,
          color: t.fg,
          borderRadius: radius.field,
          minHeight: 54,
          paddingHorizontal: 16,
          fontSize: 17,
          fontFamily: fonts[400],
        },
        style,
      ]}
    />
  );
  if (!label) return input;
  return (
    <View style={[{ gap: 8 }, containerStyle]}>
      <Label>{label}</Label>
      {input}
    </View>
  );
}

/** .chip: pílula 40px, 14px 600; ligada = fundo do texto. */
export function Chip({ label, on, onPress, minHeight = 40, style }: { label: string; on?: boolean; onPress?: () => void; minHeight?: number; style?: StyleProp<ViewStyle> }) {
  const { t } = useTheme();
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: !!on }}
      onPress={onPress}
      style={[
        {
          borderRadius: radius.pill,
          minHeight,
          paddingHorizontal: 16,
          alignItems: 'center',
          justifyContent: 'center',
          borderWidth: 1,
          borderColor: on ? t.fg : t.line,
          backgroundColor: on ? t.fg : t.s1,
        },
        style,
      ]}
    >
      <Text size={14} weight={600} color={on ? t.bg : t.fg}>
        {label}
      </Text>
    </Pressable>
  );
}

/** .list: bloco com borda e cantos de 20, itens separados por linha fina. */
export function List({ children, style }: { children: ReactNode; style?: StyleProp<ViewStyle> }) {
  const { t } = useTheme();
  return (
    <View style={[{ backgroundColor: t.s1, borderWidth: 1, borderColor: t.line, borderRadius: radius.list, overflow: 'hidden' }, style]}>
      {children}
    </View>
  );
}

/** .li: linha de 56px; o último item não tem separador (passe last). */
export function ListItem({ children, onPress, last, minHeight = 56, style }: { children: ReactNode; onPress?: () => void; last?: boolean; minHeight?: number; style?: StyleProp<ViewStyle> }) {
  const { t } = useTheme();
  const base: ViewStyle = {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 12,
    minHeight,
    paddingHorizontal: 18,
    borderBottomWidth: last ? 0 : 1,
    borderBottomColor: t.line,
  };
  if (!onPress) return <View style={[base, style]}>{children}</View>;
  return (
    <Pressable accessibilityRole="button" onPress={onPress} style={({ pressed }) => [base, { backgroundColor: pressed ? t.s2 : 'transparent' }, style]}>
      {children}
    </Pressable>
  );
}

/** .icb: botão redondo de 44px com vidro (voltar, configurações). */
export function IconButton({ icon, label, onPress, size = HIT }: { icon: IconName; label: string; onPress?: () => void; size?: number }) {
  const { t } = useTheme();
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      onPress={onPress}
      hitSlop={6}
      style={({ pressed }) => ({
        width: size,
        height: size,
        borderRadius: radius.pill,
        borderWidth: 1,
        borderColor: t.line,
        backgroundColor: pressed ? t.s2 : t.glass,
        alignItems: 'center',
        justifyContent: 'center',
      })}
    >
      <Icon name={icon} size={18} color={t.fg} strokeWidth={2.2} />
    </Pressable>
  );
}

/** .toggle: 52×32, bolinha branca de 26. */
export function Toggle({ on, onChange, label }: { on: boolean; onChange: (v: boolean) => void; label: string }) {
  const { t } = useTheme();
  return (
    <Pressable
      accessibilityRole="switch"
      accessibilityLabel={label}
      accessibilityState={{ checked: on }}
      onPress={() => onChange(!on)}
      style={{ width: 52, height: 32, borderRadius: radius.pill, backgroundColor: on ? t.accent : t.s3, padding: 3 }}
    >
      <View style={{ width: 26, height: 26, borderRadius: radius.pill, backgroundColor: '#FFFFFF', transform: [{ translateX: on ? 20 : 0 }] }} />
    </Pressable>
  );
}

/** Mensagem de erro (role=alert do protótipo): 14px, cor de erro. */
export function ErrorText({ children }: { children?: string | null }) {
  const { t } = useTheme();
  if (!children) return null;
  return (
    <Text accessibilityRole="alert" size={14} leading={1.45} color={t.err}>
      {children}
    </Text>
  );
}

/** Círculo com as iniciais (ou foto), contorno azul como no perfil. */
export function initialsOf(name?: string | null) {
  return (name ?? '')
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0]!.toUpperCase())
    .join('') || 'U';
}

import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import { palettes, type Palette, type ThemeName } from './tokens';

/** Como o backend guarda (UserPreferences.Theme). System = segue o padrão do app (escuro). */
export type ThemePreference = 'System' | 'Light' | 'Dark';

type ThemeContextValue = {
  t: Palette;
  preference: ThemePreference;
  setPreference: (p: ThemePreference) => void;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);

/** O protótipo abre no escuro; "System" fica no escuro também (decisão de produto: escuro é o padrão). */
function resolve(p: ThemePreference): ThemeName {
  return p === 'Light' ? 'light' : 'dark';
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [preference, setPreference] = useState<ThemePreference>('System');
  const value = useMemo(
    () => ({ t: palettes[resolve(preference)], preference, setPreference }),
    [preference],
  );
  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme fora do ThemeProvider');
  return ctx;
}

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, ApiError, setAuthToken, setUnauthorizedHandler } from '@/api/client';
import { loadToken, saveToken } from '@/api/storage';
import type { AuthResponse, Preferences, User } from '@/api/types';
import { useTheme } from '@/theme/ThemeProvider';

type Status = 'loading' | 'signedOut' | 'signedIn';

type SessionValue = {
  status: Status;
  user: User | null;
  /** Depois do register/verify/dev login: guarda o token e carrega o perfil. */
  signIn: (auth: AuthResponse | { accessToken: string; user?: User }) => Promise<void>;
  signOut: () => Promise<void>;
  /** Recarrega GET /api/users/me (depois de editar perfil, virar motorista...). */
  refreshUser: () => Promise<User | null>;
  setUser: (u: User) => void;
};

const SessionContext = createContext<SessionValue | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const { setPreference } = useTheme();
  const [status, setStatus] = useState<Status>('loading');
  const [user, setUser] = useState<User | null>(null);

  const signOut = useCallback(async () => {
    setAuthToken(null);
    await saveToken(null);
    setUser(null);
    setPreference('System');
    setStatus('signedOut');
  }, [setPreference]);

  /** Tema vem das Configurações salvas no backend. */
  const loadPreferences = useCallback(async () => {
    try {
      const prefs = await api.get<Preferences>('/api/users/me/preferences');
      setPreference(prefs.theme);
    } catch {
      /* sem preferências: fica no padrão */
    }
  }, [setPreference]);

  const refreshUser = useCallback(async () => {
    const me = await api.get<User>('/api/users/me');
    setUser(me);
    return me;
  }, []);

  const signIn = useCallback<SessionValue['signIn']>(
    async (auth) => {
      setAuthToken(auth.accessToken);
      await saveToken(auth.accessToken);
      const me = auth.user ?? (await api.get<User>('/api/users/me'));
      setUser(me);
      await loadPreferences();
      setStatus('signedIn');
    },
    [loadPreferences],
  );

  // Ao abrir o app: usa o token salvo, se ainda valer.
  useEffect(() => {
    let alive = true;
    (async () => {
      const saved = await loadToken();
      if (!saved) {
        if (alive) setStatus('signedOut');
        return;
      }
      setAuthToken(saved);
      try {
        const me = await api.get<User>('/api/users/me');
        if (!alive) return;
        setUser(me);
        await loadPreferences();
        setStatus('signedIn');
      } catch (e) {
        if (!alive) return;
        // Sem rede: não apaga o token, só volta para a entrada.
        if (e instanceof ApiError && e.status === 401) await saveToken(null);
        setAuthToken(null);
        setStatus('signedOut');
      }
    })();
    return () => {
      alive = false;
    };
  }, [loadPreferences]);

  useEffect(() => {
    setUnauthorizedHandler(() => void signOut());
    return () => setUnauthorizedHandler(null);
  }, [signOut]);

  const value = useMemo(
    () => ({ status, user, signIn, signOut, refreshUser, setUser }),
    [status, user, signIn, signOut, refreshUser],
  );
  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

export function useSession() {
  const ctx = useContext(SessionContext);
  if (!ctx) throw new Error('useSession fora do SessionProvider');
  return ctx;
}

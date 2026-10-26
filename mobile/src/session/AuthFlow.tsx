import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react';
import { api } from '@/api/client';
import type { AuthResponse } from '@/api/types';

export type University = { id: number; name: string; shortName: string; domains: string[] };

type AuthFlowValue = {
  email: string;
  setEmail: (v: string) => void;
  /** Token curto devolvido pelo verify-code para quem ainda não tem conta (fica só em memória). */
  signupToken: string | null;
  setSignupToken: (v: string | null) => void;
  /** Resposta do register, guardada até tocar em "Ver caronas" na tela Tudo pronto. */
  pending: AuthResponse | null;
  setPending: (v: AuthResponse | null) => void;
  /** Faculdades parceiras (GET /api/universities), para mostrar "UFPR reconhecida" enquanto digita. */
  loadUniversities: () => Promise<University[]>;
  universityOf: (email: string, list: University[]) => University | null;
};

const AuthFlowContext = createContext<AuthFlowValue | null>(null);

/** Mesma regra do backend: o domínio do e-mail ou um subdomínio dele (alunos.utfpr.edu.br). */
function universityOf(email: string, list: University[]) {
  const e = email.trim().toLowerCase();
  const at = e.lastIndexOf('@');
  if (at < 1 || at === e.length - 1) return null;
  const host = e.slice(at + 1);
  return list.find((u) => u.domains.some((d) => host === d || host.endsWith('.' + d))) ?? null;
}

/** Estado do fluxo e-mail → código → seu perfil → tudo pronto. Some quando a pessoa entra. */
export function AuthFlowProvider({ children }: { children: ReactNode }) {
  const [email, setEmail] = useState('');
  const [signupToken, setSignupToken] = useState<string | null>(null);
  const [pending, setPending] = useState<AuthResponse | null>(null);
  const cache = useRef<Promise<University[]> | null>(null);

  const loadUniversities = useCallback(() => {
    cache.current ??= api.get<University[]>('/api/universities').catch((e) => {
      cache.current = null;
      throw e;
    });
    return cache.current;
  }, []);

  const value = useMemo(
    () => ({ email, setEmail, signupToken, setSignupToken, pending, setPending, loadUniversities, universityOf }),
    [email, signupToken, pending, loadUniversities],
  );
  return <AuthFlowContext.Provider value={value}>{children}</AuthFlowContext.Provider>;
}

export function useAuthFlow() {
  const ctx = useContext(AuthFlowContext);
  if (!ctx) throw new Error('useAuthFlow fora do AuthFlowProvider');
  return ctx;
}

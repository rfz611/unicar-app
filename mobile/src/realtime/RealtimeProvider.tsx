import * as signalR from '@microsoft/signalr';
import { createContext, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { API_URL, getAuthToken } from '@/api/client';
import { useSession } from '@/session/SessionProvider';

/** Eventos do hub /hubs/trips (Realtime/Realtime.cs no backend). */
export type HubEvent =
  | 'RideRequested'
  | 'RideRequestClosed'
  | 'TripUpdated'
  | 'DriverLocation'
  | 'MessageReceived'
  | 'Typing'
  | 'RideUpdated'
  | 'TimeSuggested'
  | 'RouteAlertMatched';

type Listener = (payload: unknown) => void;

type RealtimeValue = {
  connected: boolean;
  /** Chama um método do hub (UpdateLocation, SendMessage, Typing...). */
  invoke: <T = unknown>(method: string, ...args: unknown[]) => Promise<T>;
  subscribe: (event: HubEvent, listener: Listener) => () => void;
};

const RealtimeContext = createContext<RealtimeValue | null>(null);
const EVENTS: HubEvent[] = ['RideRequested', 'RideRequestClosed', 'TripUpdated', 'DriverLocation', 'MessageReceived', 'Typing', 'RideUpdated', 'TimeSuggested', 'RouteAlertMatched'];

/** Uma conexão SignalR enquanto a pessoa está logada; reconecta sozinha quando a rede volta. */
export function RealtimeProvider({ children }: { children: ReactNode }) {
  const { status, user } = useSession();
  const [connected, setConnected] = useState(false);
  const connection = useRef<signalR.HubConnection | null>(null);
  const listeners = useRef(new Map<HubEvent, Set<Listener>>());

  useEffect(() => {
    if (status !== 'signedIn') return;
    const conn = new signalR.HubConnectionBuilder()
      // Sem cookies (o login é pelo token). Com withCredentials o navegador recusa o CORS "*" da API.
      .withUrl(`${API_URL}/hubs/trips`, { accessTokenFactory: () => getAuthToken() ?? '', withCredentials: false })
      .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();
    for (const name of EVENTS) {
      conn.on(name, (payload: unknown) => listeners.current.get(name)?.forEach((l) => l(payload)));
    }
    conn.onreconnecting(() => setConnected(false));
    conn.onreconnected(() => setConnected(true));
    conn.onclose(() => setConnected(false));
    connection.current = conn;

    let stopped = false;
    const start = async (attempt = 0) => {
      try {
        await conn.start();
        if (!stopped) setConnected(true);
      } catch {
        // API fora do ar: tenta de novo (até 30 s entre tentativas)
        if (!stopped) setTimeout(() => start(attempt + 1), Math.min(30000, 1000 * 2 ** attempt));
      }
    };
    void start();
    return () => {
      stopped = true;
      connection.current = null;
      setConnected(false);
      void conn.stop();
    };
  }, [status, user?.id]);

  const value = useMemo<RealtimeValue>(
    () => ({
      connected,
      invoke: async <T,>(method: string, ...args: unknown[]) => {
        const conn = connection.current;
        if (!conn || conn.state !== signalR.HubConnectionState.Connected) throw new Error('offline');
        return conn.invoke<T>(method, ...args);
      },
      subscribe: (event, listener) => {
        const set = listeners.current.get(event) ?? new Set<Listener>();
        set.add(listener);
        listeners.current.set(event, set);
        return () => set.delete(listener);
      },
    }),
    [connected],
  );

  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>;
}

export function useRealtime() {
  const ctx = useContext(RealtimeContext);
  if (!ctx) throw new Error('useRealtime fora do RealtimeProvider');
  return ctx;
}

/** Escuta um evento do hub enquanto a tela estiver aberta. */
export function useHubEvent<T>(event: HubEvent, handler: (payload: T) => void) {
  const { subscribe } = useRealtime();
  const ref = useRef(handler);
  useEffect(() => {
    ref.current = handler;
  });
  useEffect(() => subscribe(event, (p) => ref.current(p as T)), [event, subscribe]);
}

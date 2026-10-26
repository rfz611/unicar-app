import { useCallback, useEffect, useRef, useState } from 'react';
import { Platform, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { api } from '@/api/client';
import type { MyTrips, RideRequestOffer, SavedPlace, Trip } from '@/api/types';
import { Icon } from '@/components/Icon';
import { Page } from '@/components/Page';
import { PlaceField } from '@/components/PlaceField';
import { RideMap } from '@/components/RideMap';
import { tabBarSpace } from '@/components/TabBar';
import { Label, Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { Panel, ScanBar } from '@/components/TripParts';
import { Button, List } from '@/components/ui';
import { brl } from '@/lib/format';
import { km, type AppPlace, type LatLng } from '@/lib/geo';
import { currentPosition } from '@/lib/location';
import { useHubEvent, useRealtime } from '@/realtime/RealtimeProvider';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';
import { openChat } from './PassengerTrip';

const ACTIVE = ['Accepted', 'DriverArrived', 'InProgress'];
const TICK_MS = 3000;

/** Anda um pedaço em direção ao alvo (posição simulada sem GPS): ~12 passos até chegar, mínimo 80 m. */
function stepToward(from: LatLng, to: LatLng): LatLng {
  const d = km(from, to);
  if (d < 0.03) return to;
  const f = Math.min(1, Math.max(0.08, d / 12) / d);
  return { latitude: from.latitude + (to.latitude - from.latitude) * f, longitude: from.longitude + (to.longitude - from.longitude) * f };
}

/**
 * Lado do motorista do "Pedir agora" (aba Oferecer, motorista aprovado): avisa que está dirigindo,
 * recebe pedidos pelo SignalR, aceita e conduz a corrida mandando a posição a cada 3 s.
 */
export function DriverMode({ header, footer }: { header: React.ReactNode; footer?: React.ReactNode }) {
  const { t } = useTheme();
  const { user } = useSession();
  const { say, sayError } = useToast();
  const { connected } = useRealtime();
  const [from, setFrom] = useState<AppPlace | null>(null);
  const [to, setTo] = useState<AppPlace | null>(null);
  const [home, setHome] = useState<SavedPlace | null>(null);
  const [online, setOnline] = useState(false);
  const [offers, setOffers] = useState<RideRequestOffer[]>([]);
  const [trip, setTrip] = useState<Trip | null>(null);
  const [earned, setEarned] = useState<number | null>(null);
  const [gpsOk, setGpsOk] = useState<boolean | null>(null);
  const position = useRef<LatLng | null>(null);
  const [now, setNow] = useState(() => Date.now());

  // Saída: GPS; corrida em andamento (reabriu o app): volta para ela.
  useEffect(() => {
    let alive = true;
    (async () => {
      const trips = await api.get<MyTrips>('/api/me/trips').catch(() => null);
      const active = trips?.upcoming.find((i) => i.mode === 'Now' && i.kind === 'Driver' && ACTIVE.includes(i.status) && i.tripId);
      if (active?.tripId) {
        const tr = await api.get<Trip>(`/api/trips/${active.tripId}`).catch(() => null);
        if (alive && tr) {
          position.current = tr.driverLocation ?? null;
          setTrip(tr);
        }
      }
      setHome(await api.get<SavedPlace>('/api/me/places/home').catch(() => null));
      const pos = Platform.OS === 'web' ? null : await currentPosition();
      if (!alive) return;
      setGpsOk(!!pos);
      if (pos) setFrom((f) => f ?? { name: 'Localização atual', short: 'Localização atual', address: 'Pelo GPS do celular', ...pos });
    })();
    return () => {
      alive = false;
    };
  }, []);

  const shortcuts = [
    ...(user?.campus
      ? [{ label: 'Minha faculdade', place: { name: `${user.university} - ${user.campus.name}`, short: `${user.university} ${user.campus.name}`, address: user.campus.address, latitude: user.campus.latitude, longitude: user.campus.longitude } }]
      : []),
    ...(home ? [{ label: 'Casa', place: { name: 'Casa', short: home.neighborhood ?? 'Casa', address: home.address, latitude: home.latitude, longitude: home.longitude, neighborhood: home.neighborhood } }] : []),
  ];

  const goOnline = useCallback(async () => {
    if (!from || !to) return;
    const pos = position.current ?? from;
    try {
      const list = await api.post<RideRequestOffer[]>('/api/driver/online', {
        latitude: pos.latitude,
        longitude: pos.longitude,
        destinationLatitude: to.latitude,
        destinationLongitude: to.longitude,
      });
      position.current = pos;
      setOffers((old) => [...list, ...old.filter((o) => !list.some((n) => n.requestId === o.requestId))]);
      setOnline(true);
    } catch (e) {
      sayError(e);
      setOnline(false);
    }
  }, [from, to, sayError]);

  const goOffline = async () => {
    setOnline(false);
    setOffers([]);
    await api.post('/api/driver/offline').catch(() => {});
  };

  // Online: renova a cada 60 s (a API esquece depois de 2 min) e tira da lista o que venceu.
  useEffect(() => {
    if (!online) return;
    const renew = setInterval(goOnline, 60_000);
    const clock = setInterval(() => setNow(Date.now()), 5_000);
    return () => {
      clearInterval(renew);
      clearInterval(clock);
    };
  }, [online, goOnline]);

  // Sem tempo real (rede ruim): confere os pedidos abertos a cada 5 s.
  useEffect(() => {
    if (!online || connected) return;
    const poll = setInterval(async () => {
      const list = await api.get<RideRequestOffer[]>('/api/driver/ride-requests').catch(() => null);
      if (list) setOffers((old) => [...list, ...old.filter((o) => !list.some((n) => n.requestId === o.requestId))]);
    }, 5000);
    return () => clearInterval(poll);
  }, [online, connected]);

  useHubEvent<RideRequestOffer>('RideRequested', (o) => {
    if (online) setOffers((old) => [o, ...old.filter((x) => x.requestId !== o.requestId)]);
  });
  useHubEvent<{ requestId: string }>('RideRequestClosed', (e) => setOffers((old) => old.filter((o) => o.requestId !== e.requestId)));

  const accept = async (o: RideRequestOffer) => {
    try {
      const tr = await api.post<Trip>(`/api/ride-requests/${o.requestId}/accept`);
      setEarned(o.driverAmount);
      setOnline(false);
      setOffers([]);
      setTrip(tr);
    } catch (e) {
      sayError(e);
      setOffers((old) => old.filter((x) => x.requestId !== o.requestId));
    }
  };

  if (trip) {
    return (
      <DriverTrip
        trip={trip}
        onTrip={setTrip}
        earned={earned}
        simulate={gpsOk !== true}
        position={position}
        onClose={() => {
          setTrip(null);
          setEarned(null);
          say('Corrida encerrada. Para receber outro pedido, comece a dirigir de novo.');
        }}
      />
    );
  }

  const visible = offers.filter((o) => new Date(o.expiresAt).getTime() > now);

  return (
    <Page>
      {header}
      <View style={{ gap: 14 }}>
      <View style={{ gap: 6 }}>
        <Text size={20} weight={700} tracking={-0.03}>
          Dirigir agora
        </Text>
        <Text size={15} leading={1.5} color={t.mut}>
          Diga para onde está indo. Quem pedir carona no caminho aparece aqui.
        </Text>
      </View>
      {!online ? (
        <>
          <PlaceField label="Saindo de" value={from} onChange={setFrom} placeholder="Ex.: Batel" shortcuts={shortcuts} />
          <PlaceField label="Indo para" value={to} onChange={setTo} placeholder="Ex.: UFPR Politécnico" shortcuts={shortcuts} />
          <Button variant={from && to ? 'acc' : 'off'} label="Começar a dirigir" onPress={goOnline} disabled={!from || !to} />
        </>
      ) : (
        <>
          <View style={{ gap: 10, backgroundColor: t.s1, borderWidth: 1, borderColor: t.line, borderRadius: 20, padding: 16 }}>
            <Label color={t.accentText}>Dirigindo</Label>
            <Text size={17} weight={600}>{`${from?.short} → ${to?.short}`}</Text>
            <ScanBar />
            <Text size={14} color={t.mut}>
              {visible.length ? (visible.length === 1 ? '1 pedido no seu caminho' : `${visible.length} pedidos no seu caminho`) : 'Esperando pedidos no seu caminho…'}
            </Text>
          </View>
          {visible.map((o) => (
            <OfferCard key={o.requestId} offer={o} onAccept={() => accept(o)} onSkip={() => setOffers((old) => old.filter((x) => x.requestId !== o.requestId))} />
          ))}
          <Button variant="ghost" label="Parar de dirigir" onPress={goOffline} />
        </>
      )}
      </View>
      {footer}
    </Page>
  );
}

function OfferCard({ offer: o, onAccept, onSkip }: { offer: RideRequestOffer; onAccept: () => void; onSkip: () => void }) {
  const { t } = useTheme();
  const [busy, setBusy] = useState(false);
  return (
    <View style={{ backgroundColor: t.s1, borderWidth: 1, borderColor: t.accent, borderRadius: 22, padding: 16, gap: 14 }}>
      <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', gap: 12 }}>
        <View style={{ gap: 4, flexShrink: 1 }}>
          <Label size={13} color={t.accentText}>
            Pedido de carona
          </Label>
          <Text size={20} weight={700} tracking={-0.03}>{`${o.passengerFirstName} · ${o.passengerUniversity}`}</Text>
        </View>
        <View style={{ alignItems: 'flex-end' }}>
          <Text size={11} color={t.mut2}>
            você recebe
          </Text>
          <Text size={22} weight={700} tracking={-0.03}>
            {brl(o.driverAmount)}
          </Text>
        </View>
      </View>
      <List>
        <View style={{ paddingHorizontal: 16, paddingVertical: 12, gap: 2, borderBottomWidth: 1, borderBottomColor: t.line }}>
          <Text size={13} color={t.mut2}>{`Buscar a ${o.pickupKm.toFixed(1).replace('.', ',')} km`}</Text>
          <Text size={15} numberOfLines={1}>
            {o.origin.address ?? o.origin.label}
          </Text>
        </View>
        <View style={{ paddingHorizontal: 16, paddingVertical: 12, gap: 2 }}>
          <Text size={13} color={t.mut2}>{`Deixar · desvio de ${o.detourKm.toFixed(1).replace('.', ',')} km (${o.detourMinutes} min)`}</Text>
          <Text size={15} numberOfLines={1}>
            {o.destination.address ?? o.destination.label}
          </Text>
        </View>
      </List>
      <View style={{ flexDirection: 'row', gap: 8 }}>
        <Button
          variant="acc"
          label="Aceitar"
          busy={busy}
          style={{ flex: 1 }}
          onPress={async () => {
            setBusy(true);
            await onAccept();
            setBusy(false);
          }}
        />
        <Button variant="ghost" label="Agora não" style={{ paddingHorizontal: 18 }} onPress={onSkip} />
      </View>
    </View>
  );
}

/** Corrida do motorista: mapa, status (Cheguei → Embarcou → Finalizar) e posição ao vivo para o passageiro. */
function DriverTrip({
  trip,
  onTrip,
  earned,
  simulate,
  position,
  onClose,
}: {
  trip: Trip;
  onTrip: (t: Trip) => void;
  earned: number | null;
  simulate: boolean;
  position: React.RefObject<LatLng | null>;
  onClose: () => void;
}) {
  const { t } = useTheme();
  const insets = useSafeAreaInsets();
  const { say, sayError } = useToast();
  const { invoke } = useRealtime();
  const [busy, setBusy] = useState(false);
  const [car, setCar] = useState<LatLng | null>(trip.driverLocation ?? position.current);
  const passenger = trip.passengers[0]?.firstName ?? 'Passageiro';
  const pickup = { latitude: trip.origin.latitude!, longitude: trip.origin.longitude! };
  const dropoff = { latitude: trip.destination.latitude!, longitude: trip.destination.longitude! };

  // Mudanças vindas do passageiro ("Já embarquei", cancelou).
  useHubEvent<Trip>('TripUpdated', (e) => {
    if (e.id === trip.id) onTrip(e);
  });

  useEffect(() => {
    if (trip.status === 'Cancelled') {
      say(`${passenger} cancelou a corrida.`);
      onClose();
    }
  }, [trip.status, passenger, say, onClose]);

  // Posição a cada 3 s: GPS de verdade, ou simulada andando até o embarque / destino.
  useEffect(() => {
    if (!['Accepted', 'DriverArrived', 'InProgress'].includes(trip.status)) return;
    const timer = setInterval(async () => {
      let next: LatLng | null = null;
      if (!simulate) next = await currentPosition();
      if (!next) {
        const from = position.current ?? pickup;
        next = trip.status === 'Accepted' ? stepToward(from, pickup) : trip.status === 'InProgress' ? stepToward(from, dropoff) : from;
      }
      position.current = next;
      setCar(next);
      invoke('UpdateLocation', trip.id, next.latitude, next.longitude).catch(() => {});
    }, TICK_MS);
    return () => clearInterval(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [trip.id, trip.status, simulate]);

  const setStatus = async (status: 'DriverArrived' | 'InProgress' | 'Completed' | 'Cancelled') => {
    setBusy(true);
    try {
      const tr = await api.post<Trip>(`/api/trips/${trip.id}/status`, { status });
      onTrip(tr);
      if (status === 'Cancelled') onClose();
    } catch (e) {
      sayError(e);
    } finally {
      setBusy(false);
    }
  };

  const bottom = tabBarSpace(insets.bottom) + 26;
  const top = Math.max(insets.top, 12) + 8;
  const s = trip.status;
  const kicker = s === 'Accepted' ? 'Indo buscar' : s === 'DriverArrived' ? 'No ponto de embarque' : s === 'InProgress' ? 'Em viagem' : 'Corrida concluída';
  const title = s === 'Accepted' ? passenger : s === 'DriverArrived' ? `Esperando ${passenger}` : s === 'InProgress' ? (trip.destination.neighborhood ?? trip.destination.label) : `Você levou ${passenger}`;
  const where = s === 'Accepted' || s === 'DriverArrived' ? trip.origin.address : trip.destination.address;

  return (
    <View style={{ flex: 1, backgroundColor: t.bg }}>
      <RideMap origin={pickup} destination={dropoff} car={car} padding={{ top: top + 40, bottom: bottom + 280 }} />
      {simulate ? (
        <View style={{ position: 'absolute', top, alignSelf: 'center', backgroundColor: t.glass, borderWidth: 1, borderColor: t.line, borderRadius: 99, paddingVertical: 8, paddingHorizontal: 14 }}>
          <Text size={13} color={t.mut}>
            Posição simulada (sem GPS)
          </Text>
        </View>
      ) : null}
      <Panel bottom={bottom}>
        <View style={{ flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between', gap: 12 }}>
          <View style={{ gap: 4, flexShrink: 1 }}>
            <Label size={13} color={t.accentText}>
              {kicker}
            </Label>
            <Text size={22} weight={700} tracking={-0.035} numberOfLines={1}>
              {title}
            </Text>
            {where ? (
              <Text size={14} color={t.mut} numberOfLines={1}>
                {where}
              </Text>
            ) : null}
          </View>
          {s === 'Completed' && earned !== null ? (
            <View style={{ alignItems: 'flex-end' }}>
              <Text size={11} color={t.mut2}>
                você recebe
              </Text>
              <Text size={24} weight={700} tracking={-0.03}>
                {brl(earned)}
              </Text>
            </View>
          ) : null}
        </View>
        {s === 'Completed' ? (
          <Button variant="inv" label="Fechar" onPress={onClose} />
        ) : (
          <View style={{ flexDirection: 'row', gap: 8 }}>
            <Button
              variant="acc"
              label={s === 'Accepted' ? 'Cheguei' : s === 'DriverArrived' ? 'Passageiro embarcou' : 'Finalizar corrida'}
              busy={busy}
              style={{ flex: 1 }}
              onPress={() => setStatus(s === 'Accepted' ? 'DriverArrived' : s === 'DriverArrived' ? 'InProgress' : 'Completed')}
            />
            <Button variant="ghost" accessibilityLabel={`Mensagem para ${passenger}`} minHeight={56} style={{ width: 56, paddingHorizontal: 0 }} onPress={() => openChat(trip, passenger)}>
              <Icon name="chat" size={18} color={t.fg} />
            </Button>
          </View>
        )}
        {s === 'Accepted' || s === 'DriverArrived' ? <Button variant="ghost" label="Cancelar corrida" size={15} minHeight={48} onPress={() => setStatus('Cancelled')} /> : null}
      </Panel>
    </View>
  );
}

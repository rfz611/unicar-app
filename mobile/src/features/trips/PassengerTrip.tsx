import { router } from 'expo-router';
import { useCallback, useEffect, useState } from 'react';
import { Share, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { api } from '@/api/client';
import type { DriverLocationEvent, Trip } from '@/api/types';
import { Icon } from '@/components/Icon';
import { RideMap } from '@/components/RideMap';
import { tabBarSpace } from '@/components/TabBar';
import { Label, Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { carLabel, DriverCard, Panel, StarPicker } from '@/components/TripParts';
import { Button, Chip } from '@/components/ui';
import { brl } from '@/lib/format';
import { fmtKm, km, type LatLng } from '@/lib/geo';
import { useHubEvent } from '@/realtime/RealtimeProvider';
import { useTheme } from '@/theme/ThemeProvider';

const toLatLng = (p: { latitude: number | null; longitude: number | null }): LatLng | null =>
  p.latitude !== null && p.longitude !== null ? { latitude: p.latitude, longitude: p.longitude } : null;

const hhmm = (d: Date) => `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;

/** Abre a conversa da corrida (chat da Trip, ou da carona marcada). */
export function openChat(trip: Trip, title: string) {
  router.push({ pathname: '/chat/[id]', params: { id: trip.conversationId, title, context: `Carona agora para ${trip.destination.neighborhood ?? trip.destination.label}` } });
}

/**
 * Corrida do passageiro (isMatched → isRiding → isArrived do protótipo), com dados da API:
 * status e ETA por TripUpdated, carro ao vivo por DriverLocation; consulta a corrida a cada 8 s como reserva.
 */
export function PassengerTrip({ tripId, destinationName, onDone }: { tripId: string; destinationName?: string; onDone: () => void }) {
  const { t } = useTheme();
  const insets = useSafeAreaInsets();
  const { say, sayError } = useToast();
  const [trip, setTrip] = useState<Trip | null>(null);
  const [shared, setShared] = useState(false);
  const [stars, setStars] = useState(0);
  const [pay, setPay] = useState<'Pix' | 'Card' | ''>('');
  const [busy, setBusy] = useState(false);
  /** Relógio para o horário de chegada (atualiza a cada 10 s). */
  const [clock, setClock] = useState(() => Date.now());
  useEffect(() => {
    const timer = setInterval(() => setClock(Date.now()), 10_000);
    return () => clearInterval(timer);
  }, []);

  const load = useCallback(() => api.get<Trip>(`/api/trips/${tripId}`).then(setTrip, () => {}), [tripId]);

  useEffect(() => {
    void load();
    const timer = setInterval(load, 8000);
    return () => clearInterval(timer);
  }, [load]);

  // O evento não traz "meu pagamento"; mantém o que já tinha.
  useHubEvent<Trip>('TripUpdated', (e) => {
    if (e.id === tripId) setTrip((old) => ({ ...e, myPayment: old?.myPayment ?? e.myPayment }));
  });
  useHubEvent<DriverLocationEvent>('DriverLocation', (e) => {
    if (e.tripId === tripId) setTrip((old) => (old ? { ...old, driverLocation: { latitude: e.latitude, longitude: e.longitude, at: e.at } } : old));
  });

  useEffect(() => {
    if (trip?.status === 'Cancelled') {
      say(`${trip.driver.firstName} cancelou a corrida.`);
      onDone();
    }
    // já pagou (por exemplo, reabriu o app depois): volta para o início
    if (trip?.status === 'Completed' && trip.myPayment?.status === 'PendingPayment') onDone();
  }, [trip?.status, trip?.myPayment?.status, trip?.driver.firstName, say, onDone]);

  const setStatus = async (status: 'InProgress' | 'Cancelled') => {
    setBusy(true);
    try {
      setTrip(await api.post<Trip>(`/api/trips/${tripId}/status`, { status }));
      if (status === 'Cancelled') {
        say('Corrida cancelada.');
        onDone();
      }
    } catch (e) {
      sayError(e);
    } finally {
      setBusy(false);
    }
  };

  const share = async () => {
    if (shared) return;
    try {
      const link = await api.post<{ url: string }>(`/api/trips/${tripId}/share`);
      setShared(true);
      try {
        await Share.share({ message: `Acompanhe minha carona no UniCar: ${link.url}` });
      } catch {
        say('Link do trajeto: ' + link.url);
      }
    } catch (e) {
      sayError(e);
    }
  };

  const finish = async () => {
    if (!pay) return say('Escolha Pix ou cartão.');
    if (!stars) return say('Dê uma nota de 1 a 5.');
    setBusy(true);
    try {
      await api.post(`/api/trips/${tripId}/complete`, { paymentMethod: pay, rating: stars });
      say('Obrigado! A carona está no seu histórico.');
      onDone();
    } catch (e) {
      sayError(e);
    } finally {
      setBusy(false);
    }
  };

  const bottom = tabBarSpace(insets.bottom) + 26;
  const top = Math.max(insets.top, 12) + 8;
  if (!trip) return <View style={{ flex: 1, backgroundColor: t.bg }} />;

  const origin = toLatLng(trip.origin);
  const destination = toLatLng(trip.destination);
  const car = trip.driverLocation;
  const dest = destinationName ?? trip.destination.neighborhood ?? trip.destination.label;
  const name = trip.driver.firstName;
  const price = trip.myPayment ? brl(trip.myPayment.amount) : '';

  // Em viagem: quanto falta pela posição do carro (estimativa só para a barra; a ETA vem da API)
  const total = origin && destination ? km(origin, destination) : 1;
  const left = car && destination ? km(car, destination) : total;
  const progress = Math.max(0, Math.min(1, 1 - left / Math.max(total, 0.1)));
  const arrival = hhmm(new Date(clock + (trip.etaMinutes ?? trip.minutes) * 60000));

  return (
    <View style={{ flex: 1, backgroundColor: t.bg }}>
      <RideMap origin={origin} destination={destination} car={car} padding={{ top: top + 72, bottom: bottom + 300 }} />

      <View style={{ position: 'absolute', left: 16, right: 16, top, flexDirection: 'row', alignItems: 'center', gap: 10, borderWidth: 1, borderColor: t.line, backgroundColor: t.glass, borderRadius: 99, minHeight: 48, paddingHorizontal: 16 }}>
        <Text size={14} color={t.mut} numberOfLines={1} style={{ flexShrink: 1, maxWidth: '45%' }}>
          {trip.origin.neighborhood ?? 'Você'}
        </Text>
        <Icon name="arrow" size={14} color={t.mut} strokeWidth={2.2} />
        <Text size={14} weight={600} numberOfLines={1} style={{ flex: 1 }}>
          {dest}
        </Text>
      </View>

      {trip.status === 'Accepted' || trip.status === 'DriverArrived' ? (
        <Panel bottom={bottom}>
          <View style={{ flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between', gap: 12 }}>
            <View style={{ gap: 4, flexShrink: 1 }}>
              <Label size={13} color={t.accentText}>
                {trip.status === 'DriverArrived' ? 'Chegou' : 'Carona aceita'}
              </Label>
              <Text size={22} weight={700} tracking={-0.035}>
                {trip.status === 'DriverArrived' ? `${name} está te esperando` : `${name} está a caminho`}
              </Text>
            </View>
            {trip.status === 'Accepted' ? (
              <View style={{ flexDirection: 'row', alignItems: 'baseline', gap: 4 }}>
                <Text size={48} weight={600} tracking={-0.06} tabular style={{ lineHeight: 46 }}>
                  {trip.etaMinutes ?? '–'}
                </Text>
                <Text size={15} color={t.mut}>
                  min
                </Text>
              </View>
            ) : null}
          </View>
          <DriverCard driver={trip.driver} vehicle={trip.vehicle} />
          {trip.status === 'Accepted' ? (
            <View style={{ flexDirection: 'row', gap: 8 }}>
              <Button variant="inv" label="Mensagem" size={15} minHeight={52} style={{ flex: 1 }} onPress={() => openChat(trip, name)}>
                <Icon name="chat" size={18} color={t.bg} />
              </Button>
              <Button variant="ghost" label="Cancelar" size={15} minHeight={52} style={{ paddingHorizontal: 18 }} busy={busy} onPress={() => setStatus('Cancelled')} />
            </View>
          ) : (
            <Button variant="acc" label="Já embarquei" busy={busy} onPress={() => setStatus('InProgress')} />
          )}
        </Panel>
      ) : null}

      {trip.status === 'InProgress' ? (
        <Panel bottom={bottom}>
          <View style={{ flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between', gap: 12 }}>
            <View style={{ gap: 4, flexShrink: 1 }}>
              <Label size={13} color={t.accentText}>
                Em viagem
              </Label>
              <Text size={20} weight={700} tracking={-0.035} numberOfLines={1}>
                {dest}
              </Text>
            </View>
            <View style={{ alignItems: 'flex-end', gap: 2 }}>
              <Text size={12} color={t.mut2}>
                chegada
              </Text>
              <Text size={36} weight={600} tracking={-0.05} tabular style={{ lineHeight: 34 }}>
                {arrival}
              </Text>
            </View>
          </View>
          <View style={{ height: 6, borderRadius: 99, backgroundColor: t.s3, overflow: 'hidden' }}>
            <View style={{ height: '100%', width: `${Math.round(progress * 100)}%`, borderRadius: 99, backgroundColor: t.accent }} />
          </View>
          <View style={{ flexDirection: 'row', justifyContent: 'space-between' }}>
            <Text size={14} color={t.mut}>{`Faltam ${fmtKm(left * 1.35)}`}</Text>
            <Text size={14} color={t.mut} numberOfLines={1}>{`Com ${name} no ${carLabel(trip.vehicle)}`}</Text>
          </View>
          <View style={{ flexDirection: 'row', gap: 8 }}>
            <Button variant={shared ? 'ghost' : 'inv'} label={shared ? 'Trajeto compartilhado' : 'Compartilhar trajeto'} size={15} minHeight={52} style={{ flex: 1 }} onPress={share} />
            <Button variant="ghost" accessibilityLabel={`Mensagem para ${name}`} minHeight={52} style={{ width: 52, paddingHorizontal: 0 }} onPress={() => openChat(trip, name)}>
              <Icon name="chat" size={18} color={t.fg} />
            </Button>
          </View>
        </Panel>
      ) : null}

      {trip.status === 'Completed' ? (
        <Panel bottom={bottom}>
          <View style={{ gap: 6 }}>
            <Text size={26} weight={700} tracking={-0.04}>
              Você chegou
            </Text>
            <View style={{ flexDirection: 'row', justifyContent: 'space-between', gap: 12 }}>
              <Text size={15} color={t.mut} numberOfLines={1} style={{ flexShrink: 1 }}>
                {dest}
              </Text>
              <Text size={15} weight={600}>
                {price}
              </Text>
            </View>
          </View>
          <View style={{ gap: 10 }}>
            <Text size={15}>{`Como foi a carona com ${name}?`}</Text>
            <StarPicker value={stars} onChange={setStars} />
          </View>
          <View style={{ gap: 10 }}>
            <Text size={15}>Como você quer pagar?</Text>
            <View accessibilityRole="radiogroup" accessibilityLabel="Forma de pagamento" style={{ flexDirection: 'row', gap: 8 }}>
              <Chip label="Pix" on={pay === 'Pix'} minHeight={52} style={{ flex: 1 }} onPress={() => setPay('Pix')} />
              <Chip label="Cartão" on={pay === 'Card'} minHeight={52} style={{ flex: 1 }} onPress={() => setPay('Card')} />
            </View>
          </View>
          <Button
            variant={stars && pay ? 'acc' : 'off'}
            label={pay === 'Card' ? `Pagar ${price} no cartão` : pay === 'Pix' ? `Pagar ${price} com Pix` : `Pagar ${price}`}
            busy={busy}
            onPress={finish}
          />
        </Panel>
      ) : null}
    </View>
  );
}

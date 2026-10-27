import { router } from 'expo-router';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { api } from '@/api/client';
import type { Quote, Ride, RideRequest, RouteAlert, TimeSuggestion, Trip } from '@/api/types';
import { CalendarIcon, Icon } from '@/components/Icon';
import { Avatar } from '@/components/Page';
import { RideMap } from '@/components/RideMap';
import { tabBarSpace } from '@/components/TabBar';
import { Label, Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { Panel, ScanBar } from '@/components/TripParts';
import { Button, IconButton } from '@/components/ui';
import { brl, isoDay } from '@/lib/format';
import { decodePolyline, fmtKm, type AppPlace } from '@/lib/geo';
import { useHubEvent } from '@/realtime/RealtimeProvider';
import { useTheme } from '@/theme/ThemeProvider';

type Mode = 'now' | 'sched';
type Day = 'today' | 'tomorrow';

/** Rota no mapa (isRoute), "Procurando colegas" (isSearching) e caronas marcadas (isScheduled) do protótipo. */
export function RouteView({ origin, destination, onBack, onTrip }: { origin: AppPlace; destination: AppPlace; onBack: () => void; onTrip: (tripId: string) => void }) {
  const { t } = useTheme();
  const insets = useSafeAreaInsets();
  const { say, sayError } = useToast();
  const [step, setStep] = useState<'route' | 'scheduled' | 'searching'>('route');
  const [request, setRequest] = useState<RideRequest | null>(null);
  const [asking, setAsking] = useState(false);

  // Pedir agora: cria o pedido; quem estiver dirigindo na direção recebe pelo SignalR.
  const askNow = async () => {
    setAsking(true);
    try {
      const place = (p: AppPlace) => ({ address: p.address, neighborhood: p.neighborhood, latitude: p.latitude, longitude: p.longitude });
      const req = await api.post<RideRequest>('/api/ride-requests', { origin: place(origin), destination: place(destination) });
      setRequest(req);
      setStep('searching');
    } catch (e) {
      sayError(e);
    } finally {
      setAsking(false);
    }
  };

  const cancelRequest = async () => {
    if (request) await api.del(`/api/ride-requests/${request.id}`).catch(sayError);
    setRequest(null);
    setStep('route');
  };

  // Aceito: o hub avisa (TripUpdated); a cada 4 s confere o pedido, caso o aviso se perca.
  useHubEvent<Trip>('TripUpdated', (trip) => {
    if (request && trip.rideRequestId === request.id && trip.status === 'Accepted') onTrip(trip.id);
  });
  useEffect(() => {
    if (step !== 'searching' || !request) return;
    const timer = setInterval(async () => {
      const r = await api.get<RideRequest>(`/api/ride-requests/${request.id}`).catch(() => null);
      if (r?.tripId) onTrip(r.tripId);
      else if (r?.status === 'Expired') {
        say('Ninguém indo para lá agora. Tente uma carona marcada.');
        setRequest(null);
        setStep('route');
      }
    }, 4000);
    return () => clearInterval(timer);
  }, [step, request, onTrip, say]);
  const [mode, setMode] = useState<Mode>('now');
  const [quote, setQuote] = useState<Quote | null>(null);
  const [rides, setRides] = useState<Record<Day, Ride[] | null>>({ today: null, tomorrow: null });
  const [day, setDay] = useState<Day>('today');

  const searchUrl = useCallback(
    (d: Day) =>
      `/api/rides/search?destLat=${destination.latitude}&destLng=${destination.longitude}` +
      `&originLat=${origin.latitude}&originLng=${origin.longitude}&date=${isoDay(d === 'today' ? 0 : 1)}`,
    [origin, destination],
  );

  /** Recarrega depois de reservar ou cancelar. */
  const loadRides = useCallback(async () => {
    const [today, tomorrow] = await Promise.all([api.get<Ride[]>(searchUrl('today')), api.get<Ride[]>(searchUrl('tomorrow'))]);
    setRides({ today, tomorrow });
    return { today, tomorrow };
  }, [searchUrl]);

  // Preço e rota (POST /api/quote) + caronas marcadas de hoje e amanhã para esse destino.
  useEffect(() => {
    let alive = true;
    api
      .post<Quote>('/api/quote', {
        originLat: origin.latitude,
        originLng: origin.longitude,
        destinationLat: destination.latitude,
        destinationLng: destination.longitude,
      })
      .then((q) => alive && setQuote(q), sayError);
    Promise.all([api.get<Ride[]>(searchUrl('today')), api.get<Ride[]>(searchUrl('tomorrow'))]).then(([today, tomorrow]) => {
      if (!alive) return;
      setRides({ today, tomorrow });
      // abre no dia que tem saída (as caronas de demonstração costumam ser de amanhã)
      setDay(today.length === 0 && tomorrow.length > 0 ? 'tomorrow' : 'today');
    }, sayError);
    return () => {
      alive = false;
    };
  }, [origin, destination, searchUrl, sayError]);

  const route = useMemo(() => (quote?.encodedPolyline ? decodePolyline(quote.encodedPolyline) : null), [quote]);
  const todayCount = rides.today?.length ?? 0;
  const tomorrowCount = rides.tomorrow?.length ?? 0;
  // "a partir de": a mais barata do mesmo dia do subtítulo; sem nenhuma, o preço de carona marcada da rota
  const shown = todayCount ? rides.today! : tomorrowCount ? rides.tomorrow! : [];
  const cheapest = shown.length ? Math.min(...shown.map((r) => r.pricePerSeat)) : quote?.scheduled.total;
  const schedSub = todayCount
    ? todayCount === 1 ? '1 saída hoje' : `${todayCount} saídas hoje`
    : tomorrowCount
      ? tomorrowCount === 1 ? '1 saída amanhã' : `${tomorrowCount} saídas amanhã`
      : 'Nenhuma saída hoje';

  const panelBottom = tabBarSpace(insets.bottom) + 26;
  const top = Math.max(insets.top, 12) + 8;

  return (
    <View style={{ flex: 1, backgroundColor: t.bg }}>
      <RideMap origin={origin} destination={destination} route={route} padding={{ top: top + 72, bottom: step === 'route' ? panelBottom + 300 : tabBarSpace(insets.bottom) + 460 }} />

      {/* topo: voltar + "origem → destino" */}
      <View style={{ position: 'absolute', left: 16, right: 16, top, flexDirection: 'row', alignItems: 'center', gap: 10 }}>
        <IconButton
          icon={step === 'searching' ? 'close' : 'back'}
          label={step === 'searching' ? 'Cancelar' : 'Voltar'}
          size={48}
          onPress={step === 'scheduled' ? () => setStep('route') : step === 'searching' ? cancelRequest : onBack}
        />
        <View style={{ flex: 1, flexDirection: 'row', alignItems: 'center', gap: 10, borderWidth: 1, borderColor: t.line, backgroundColor: t.glass, borderRadius: 99, minHeight: 48, paddingHorizontal: 16 }}>
          <Text size={14} color={t.mut} numberOfLines={1} style={{ flexShrink: 1, maxWidth: '45%' }}>
            {origin.short}
          </Text>
          <Icon name="arrow" size={14} color={t.mut} strokeWidth={2.2} />
          <Text size={14} weight={600} numberOfLines={1} style={{ flex: 1 }}>
            {destination.short}
          </Text>
        </View>
      </View>

      {step === 'route' ? (
        <View
          style={{
            position: 'absolute',
            left: 12,
            right: 12,
            bottom: panelBottom,
            backgroundColor: t.s1,
            borderWidth: 1,
            borderColor: t.line,
            borderRadius: 28,
            padding: 20,
            gap: 16,
            boxShadow: `0 30px 60px -10px ${t.shadow}`,
          }}
        >
          <View style={{ flexDirection: 'row', gap: 28, paddingHorizontal: 4 }}>
            <Stat label="Distância" value={quote ? fmtKm(quote.km) : '…'} />
            <Stat label="Tempo" value={quote ? `${quote.minutes} min` : '…'} />
          </View>
          <View accessibilityRole="radiogroup" accessibilityLabel="Como você quer ir" style={{ gap: 8 }}>
            <ModeOption
              on={mode === 'now'}
              onPress={() => setMode('now')}
              icon={<Icon name="bolt" size={20} color={t.fg} />}
              title="Pedir agora"
              sub="Quem já vai para lá"
              price={quote ? brl(quote.now.total) : '…'}
            />
            <ModeOption
              on={mode === 'sched'}
              onPress={() => setMode('sched')}
              icon={<CalendarIcon size={20} color={t.fg} />}
              title="Caronas marcadas"
              sub={rides.today ? schedSub : '…'}
              price={cheapest !== undefined ? brl(cheapest) : '…'}
              from
            />
          </View>
          <Button
            variant="inv"
            label={mode === 'now' ? 'Pedir carona' : 'Ver caronas marcadas'}
            busy={asking}
            onPress={() => (mode === 'sched' ? setStep('scheduled') : askNow())}
          />
        </View>
      ) : step === 'searching' ? (
        <Panel bottom={panelBottom}>
          <View style={{ gap: 6 }}>
            <Text size={22} weight={700} tracking={-0.035}>
              Procurando colegas
            </Text>
            <Text size={15} leading={1.45} color={t.mut}>{`Avisamos quem está dirigindo na direção de ${destination.short}.`}</Text>
          </View>
          <ScanBar />
          <Button variant="ghost" label="Cancelar pedido" onPress={cancelRequest} />
        </Panel>
      ) : (
        <ScheduledSheet
          rides={rides[day]}
          day={day}
          onDay={setDay}
          destination={destination}
          onChanged={() => loadRides().catch(sayError)}
        />
      )}
    </View>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <View style={{ gap: 2 }}>
      <Label size={13}>{label}</Label>
      <Text size={24} weight={600} tracking={-0.04}>
        {value}
      </Text>
    </View>
  );
}

function ModeOption({ on, onPress, icon, title, sub, price, from }: { on: boolean; onPress: () => void; icon: React.ReactNode; title: string; sub: string; price: string; from?: boolean }) {
  const { t } = useTheme();
  return (
    <Pressable
      accessibilityRole="radio"
      accessibilityState={{ checked: on }}
      onPress={onPress}
      style={{ flexDirection: 'row', alignItems: 'center', gap: 14, borderRadius: 20, paddingVertical: 12, paddingHorizontal: 14, backgroundColor: t.s2, borderWidth: 1.5, borderColor: on ? t.accent : 'transparent' }}
    >
      <View style={{ width: 44, height: 44, borderRadius: 14, backgroundColor: t.s3, alignItems: 'center', justifyContent: 'center' }}>{icon}</View>
      <View style={{ flex: 1, gap: 2 }}>
        <Text size={16} weight={600}>
          {title}
        </Text>
        <Text size={13} color={t.mut}>
          {sub}
        </Text>
      </View>
      <View style={{ alignItems: 'flex-end' }}>
        {from ? (
          <Text size={11} color={t.mut2}>
            a partir de
          </Text>
        ) : null}
        <Text size={20} weight={700} tracking={-0.03}>
          {price}
        </Text>
      </View>
    </Pressable>
  );
}

/** Folha "Caronas marcadas" (section do protótipo), 450px acima da barra de abas. */
function ScheduledSheet({ rides, day, onDay, destination, onChanged }: { rides: Ride[] | null; day: Day; onDay: (d: Day) => void; destination: AppPlace; onChanged: () => void }) {
  const { t } = useTheme();
  const insets = useSafeAreaInsets();
  const { say, sayError } = useToast();
  const [selected, setSelected] = useState<string | null>(null);
  const [suggested, setSuggested] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState<string | null>(null);
  const [alert, setAlert] = useState<RouteAlert | null>(null);

  const reserve = async (r: Ride) => {
    setBusy(r.id);
    try {
      if (r.reservedByMe) {
        await api.del(`/api/rides/${r.id}/reservations/mine`);
        say('Reserva cancelada.');
      } else {
        await api.post(`/api/rides/${r.id}/reservations`);
        say('Lugar reservado. Está em Viagens.');
      }
      onChanged();
    } catch (e) {
      sayError(e);
      onChanged();
    } finally {
      setBusy(null);
    }
  };

  const suggest = async (r: Ride, minutes: -10 | 10) => {
    try {
      await api.post<TimeSuggestion>(`/api/rides/${r.id}/time-suggestions`, { minutes });
      setSuggested((s) => ({ ...s, [r.id]: minutes < 0 ? '10 min antes' : '10 min depois' }));
    } catch (e) {
      sayError(e);
    }
  };

  const toggleAlert = async () => {
    try {
      if (alert) {
        await api.del(`/api/route-alerts/${alert.id}`);
        setAlert(null);
      } else {
        setAlert(await api.post<RouteAlert>('/api/route-alerts', { label: destination.name, latitude: destination.latitude, longitude: destination.longitude }));
        say('Avisaremos quando aparecer.');
      }
    } catch (e) {
      sayError(e);
    }
  };

  return (
    <View
      accessibilityLabel="Caronas marcadas"
      style={{
        position: 'absolute',
        left: 0,
        right: 0,
        bottom: tabBarSpace(insets.bottom),
        height: 450,
        backgroundColor: t.bg,
        borderTopLeftRadius: 28,
        borderTopRightRadius: 28,
        borderTopWidth: 1,
        borderColor: t.line,
        boxShadow: `0 -30px 60px -20px ${t.shadow}`,
      }}
    >
      <View style={{ flexDirection: 'row', alignItems: 'baseline', justifyContent: 'space-between', paddingTop: 22, paddingHorizontal: 20, paddingBottom: 14 }}>
        <Text accessibilityRole="header" size={24} weight={700} tracking={-0.04}>
          Caronas marcadas
        </Text>
        {/* Hoje / Amanhã: o protótipo só tinha "Hoje"; as caronas do seed costumam ser de amanhã */}
        <View style={{ flexDirection: 'row', gap: 14 }}>
          {(['today', 'tomorrow'] as const).map((d) => (
            <Pressable key={d} accessibilityRole="button" accessibilityState={{ selected: day === d }} onPress={() => onDay(d)} hitSlop={10}>
              <Label weight={day === d ? 600 : 500} color={day === d ? t.fg : t.mut2}>
                {d === 'today' ? 'Hoje' : 'Amanhã'}
              </Label>
            </Pressable>
          ))}
        </View>
      </View>
      <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ paddingHorizontal: 16, paddingBottom: 20, gap: 10 }}>
        {rides === null ? <ActivityIndicator color={t.mut} style={{ marginTop: 24 }} /> : null}
        {rides?.map((r) => (
          <RideCard
            key={r.id}
            ride={r}
            selected={selected === r.id}
            busy={busy === r.id}
            suggestion={suggested[r.id]}
            onSelect={() => setSelected(r.id)}
            onReserve={() => reserve(r)}
            onChat={() =>
              r.reservedByMe
                ? router.push({ pathname: '/chat/[id]', params: { id: r.id, title: r.driver.firstName, context: `Carona das ${r.departureTime} para ${destination.short}` } })
                : say(`Reserve um lugar para conversar com ${r.driver.firstName}.`)
            }
            onSuggest={(m) => suggest(r, m)}
          />
        ))}
        {rides && rides.length === 0 ? (
          <View style={{ paddingVertical: 28, paddingHorizontal: 12, alignItems: 'center', gap: 12 }}>
            <Text size={18} weight={600} align="center">
              Nenhuma saída marcada para lá
            </Text>
            <Text size={14} leading={1.5} color={t.mut} align="center">
              Quando alguém marcar essa rota, a gente te avisa.
            </Text>
            <Button variant={alert ? 'ghost' : 'inv'} label={alert ? 'Aviso ligado' : 'Me avise'} size={15} minHeight={48} style={{ paddingHorizontal: 22 }} onPress={toggleAlert} />
          </View>
        ) : null}
      </ScrollView>
    </View>
  );
}

function RideCard({
  ride: r,
  selected,
  busy,
  suggestion,
  onSelect,
  onReserve,
  onChat,
  onSuggest,
}: {
  ride: Ride;
  selected: boolean;
  busy: boolean;
  suggestion?: string;
  onSelect: () => void;
  onReserve: () => void;
  onChat: () => void;
  onSuggest: (m: -10 | 10) => void;
}) {
  const { t } = useTheme();
  const away = r.pickup ? r.pickup.minutesAfterDeparture : null;
  const pickup = away === null ? r.origin.label : away <= 1 ? 'Sai perto de você' : `Passa por você em ${away} min`;
  const left = r.seatsLeft;
  const seats = left <= 0 ? 'Lotada' : left === 1 ? '1 vaga' : `${left} vagas`;
  const meta = r.driver.course ? `${r.driver.course} na ${r.driver.university}` : r.driver.university;
  const rating = r.driver.rating !== null ? r.driver.rating.toFixed(1).replace('.', ',') : 'Nova';

  return (
    <View style={{ borderRadius: 22, backgroundColor: t.s1, borderWidth: 1, borderColor: selected ? t.accent : t.line }}>
      <Pressable accessibilityRole="button" accessibilityLabel={`Carona das ${r.departureTime} com ${r.driver.firstName}`} onPress={onSelect} style={{ padding: 16, gap: 14 }}>
        <View style={{ flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: 12 }}>
          <View style={{ gap: 8, flexShrink: 1 }}>
            <Text size={44} weight={600} tracking={-0.06} tabular style={{ lineHeight: 44 }}>
              {r.departureTime}
            </Text>
            <Text size={14} color={t.mut}>
              {pickup}
            </Text>
          </View>
          <View style={{ alignItems: 'flex-end', gap: 6 }}>
            <Text size={22} weight={700} tracking={-0.03}>
              {brl(r.pricePerSeat)}
            </Text>
            <View
              style={
                left === 1
                  ? { borderRadius: 99, paddingVertical: 5, paddingHorizontal: 10, backgroundColor: t.accent }
                  : { borderRadius: 99, paddingVertical: 5, paddingHorizontal: 10, borderWidth: 1, borderColor: t.line }
              }
            >
              <Text size={12} weight={600} color={left === 1 ? '#FFFFFF' : t.fg}>
                {seats}
              </Text>
            </View>
          </View>
        </View>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 10, borderTopWidth: 1, borderTopColor: t.line, paddingTop: 12 }}>
          <Avatar name={r.driver.firstName} photoUrl={r.driver.photoUrl} size={36} ring={0} />
          <View style={{ flex: 1, gap: 2 }}>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
              <Text size={15} weight={600}>
                {r.driver.firstName}
              </Text>
              <Icon name="star" size={12} color={t.accent} fill={t.accent} strokeWidth={0} />
              <Text size={13} weight={500} color={t.mut}>
                {rating}
              </Text>
            </View>
            <Text size={13} color={t.mut2} numberOfLines={1}>
              {meta}
            </Text>
          </View>
          <Text size={13} color={t.mut} numberOfLines={1}>
            {`${r.vehicle.model.split(' ').slice(-1)[0]} ${r.vehicle.color.toLowerCase()}`}
          </Text>
        </View>
      </Pressable>
      {selected ? (
        <View style={{ gap: 10, paddingHorizontal: 16, paddingBottom: 16 }}>
          <View style={{ flexDirection: 'row', gap: 8 }}>
            <Button
              variant={r.reservedByMe ? 'ghost' : left <= 0 ? 'off' : 'inv'}
              label={r.reservedByMe ? 'Reservado' : left <= 0 ? 'Lotada' : 'Reservar lugar'}
              size={16}
              minHeight={52}
              busy={busy}
              disabled={!r.reservedByMe && left <= 0}
              style={{ flex: 1 }}
              onPress={onReserve}
            />
            <Button variant="ghost" accessibilityLabel={`Mensagem para ${r.driver.firstName}`} minHeight={52} style={{ width: 52, paddingHorizontal: 0 }} onPress={onChat}>
              <Icon name="chat" size={18} color={t.fg} />
            </Button>
          </View>
          {r.reservedByMe ? (
            <View style={{ gap: 8 }}>
              <Text size={13} color={t.mut}>
                {suggestion ? `Sugestão enviada para ${r.driver.firstName}: sair ${suggestion}.` : `Precisa de outro horário? Sugira para ${r.driver.firstName}.`}
              </Text>
              <View style={{ flexDirection: 'row', gap: 8 }}>
                <Button variant="ghost" label="10 min antes" size={14} minHeight={44} style={{ flex: 1 }} onPress={() => onSuggest(-10)} />
                <Button variant="ghost" label="10 min depois" size={14} minHeight={44} style={{ flex: 1 }} onPress={() => onSuggest(10)} />
              </View>
            </View>
          ) : null}
        </View>
      ) : null}
    </View>
  );
}

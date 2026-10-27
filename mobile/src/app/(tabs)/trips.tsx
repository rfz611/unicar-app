import { router, useFocusEffect } from 'expo-router';
import { useCallback, useState } from 'react';
import { View } from 'react-native';
import { api } from '@/api/client';
import type { MyTrips, TripItem } from '@/api/types';
import { EmptyBox, Page } from '@/components/Page';
import { H1, Label, Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { Button, List, ListItem } from '@/components/ui';
import { brl, dayLabel } from '@/lib/format';
import { useTheme } from '@/theme/ThemeProvider';

const ACTIVE_NOW = ['Accepted', 'DriverArrived', 'InProgress'];

/** "Onix Plus prata · ABC-1D23" → "Onix Plus prata". */
const carOf = (vehicle: string) => vehicle.split(' · ')[0];

function kindOf(i: TripItem) {
  if (i.mode === 'Now') return i.kind === 'Driver' ? 'Você dirige agora' : 'Carona agora';
  return i.kind === 'Driver' ? 'Você dirige' : 'Reserva';
}

function subOf(i: TripItem) {
  if (i.kind === 'Passenger') return `Com ${i.with[0] ?? 'o motorista'} no ${carOf(i.vehicle)}`;
  if (i.mode === 'Now') return `Com ${i.with.join(', ')} · você recebe ${brl(i.amount)}`;
  if (i.seatsTaken === 0) return `${i.seats} ${i.seats === 1 ? 'vaga' : 'vagas'} · ninguém reservou ainda`;
  return `${i.with.join(', ')} · você recebe ${brl(i.amount)}`;
}

function historyStatus(i: TripItem) {
  if (i.status === 'Cancelled' || i.status === 'RideCancelled') return 'Cancelada';
  return i.kind === 'Driver' ? 'Você dirigiu' : `Com ${i.with[0] ?? 'o motorista'}`;
}

/** Aba Viagens (isTrips do protótipo): Próximas e Histórico de GET /api/me/trips, recarregado ao abrir a aba. */
export default function Trips() {
  const { t } = useTheme();
  const { say, sayError } = useToast();
  const [data, setData] = useState<MyTrips | null>(null);

  const load = useCallback(() => api.get<MyTrips>('/api/me/trips').then(setData, sayError), [sayError]);
  useFocusEffect(
    useCallback(() => {
      void load();
    }, [load]),
  );

  const cancel = async (i: TripItem) => {
    try {
      if (i.kind === 'Passenger') {
        await api.del(`/api/rides/${i.rideId}/reservations/mine`);
        say('Reserva cancelada.');
      } else {
        await api.del(`/api/rides/${i.rideId}`);
        say('Carona removida.');
      }
      await load();
    } catch (e) {
      sayError(e);
    }
  };

  const chat = (i: TripItem) => {
    if (i.mode === 'Now') {
      // a corrida ao vivo continua na aba de onde começou
      router.navigate(i.kind === 'Driver' ? '/offer' : '/');
      return;
    }
    if (i.kind === 'Driver' && i.seatsTaken === 0) return say('Ninguém reservou ainda.');
    router.push({
      pathname: '/chat/[id]',
      params: { id: i.rideId!, title: i.kind === 'Driver' ? 'Passageiros' : (i.with[0] ?? 'Conversa'), context: `Carona das ${i.departureTime} para ${i.destination}` },
    });
  };

  const upcoming = data?.upcoming ?? [];
  const history = data?.history ?? [];

  return (
    <Page>
      <H1>Viagens</H1>
      <View style={{ gap: 10 }}>
        <Label>Próximas</Label>
        {upcoming.map((i) => (
          <UpcomingCard key={`${i.mode}-${i.rideId ?? i.tripId}-${i.kind}`} item={i} onChat={() => chat(i)} onCancel={() => cancel(i)} />
        ))}
        {data && upcoming.length === 0 ? <EmptyBox>Nenhuma carona marcada. Reserve uma carona marcada ou ofereça a sua.</EmptyBox> : null}
      </View>
      <View style={{ gap: 10 }}>
        <Label>Histórico</Label>
        {history.length > 0 ? (
          <List>
            {history.map((h, n) => (
              <ListItem key={`${h.mode}-${h.rideId ?? h.tripId}-${h.kind}`} last={n === history.length - 1} minHeight={72} style={{ paddingVertical: 12 }}>
                <View style={{ flex: 1, gap: 2 }}>
                  <Text size={15} weight={500} numberOfLines={1}>{`${h.origin} para ${h.destination}`}</Text>
                  <Text size={13} color={t.mut2} numberOfLines={1}>{`${dayLabel(h.departureDate)}, ${h.departureTime} · ${historyStatus(h)}`}</Text>
                </View>
                <Text size={15} weight={600} color={h.status.includes('Cancelled') ? t.mut2 : t.fg}>
                  {brl(h.amount)}
                </Text>
              </ListItem>
            ))}
          </List>
        ) : (
          <Text size={15} color={t.mut} style={{ padding: 4 }}>
            Suas caronas concluídas aparecem aqui.
          </Text>
        )}
      </View>
    </Page>
  );
}

function UpcomingCard({ item: i, onChat, onCancel }: { item: TripItem; onChat: () => void; onCancel: () => Promise<void> }) {
  const { t } = useTheme();
  const [busy, setBusy] = useState(false);
  const live = i.mode === 'Now' && ACTIVE_NOW.includes(i.status);
  const day = dayLabel(i.departureDate);
  return (
    <View style={{ backgroundColor: t.s1, borderWidth: 1, borderColor: t.line, borderRadius: 20, padding: 16, gap: 12 }}>
      <View style={{ flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: 12 }}>
        <View style={{ gap: 6, flex: 1 }}>
          <Label size={13} color={t.accentText}>
            {i.mode === 'Now' ? kindOf(i) : `${kindOf(i)} · ${day}`}
          </Label>
          <Text size={17} weight={600} leading={1.3}>{`${i.origin} para ${i.destination}`}</Text>
          <Text size={14} color={t.mut}>
            {subOf(i)}
          </Text>
        </View>
        <Text size={32} weight={600} tracking={-0.05} tabular style={{ lineHeight: 32 }}>
          {i.departureTime}
        </Text>
      </View>
      <View style={{ flexDirection: 'row', gap: 8 }}>
        <Button
          variant="inv"
          label={live ? 'Abrir corrida' : i.kind === 'Driver' ? 'Passageiros' : 'Mensagem'}
          size={15}
          minHeight={48}
          style={{ flex: 1 }}
          onPress={onChat}
        />
        {i.canCancel ? (
          <Button
            variant="ghost"
            label="Cancelar"
            size={15}
            minHeight={48}
            busy={busy}
            style={{ paddingHorizontal: 18 }}
            onPress={async () => {
              setBusy(true);
              await onCancel();
              setBusy(false);
            }}
          />
        ) : null}
      </View>
    </View>
  );
}

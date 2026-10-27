import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { Pressable, View } from 'react-native';
import { api, messageOf } from '@/api/client';
import type { Ride } from '@/api/types';
import { PlaceField, usePlaceShortcuts } from '@/components/PlaceField';
import { Label, Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { Button, Chip, ErrorText, Field } from '@/components/ui';
import { brl, isoDay } from '@/lib/format';
import { fmtKm, type AppPlace } from '@/lib/geo';
import { useTheme } from '@/theme/ThemeProvider';

type Estimate = { km: number; minutes: number; pricePerSeat: number; driverAmountPerSeat: number; estimated: boolean };

const TIME_OK = /^([01]\d|2[0-3]):[0-5]\d$/;
const toInput = (p: AppPlace) => ({ address: p.address, neighborhood: p.neighborhood ?? null, latitude: p.latitude, longitude: p.longitude });

/**
 * Publicar carona marcada (offerOpen do protótipo): saindo de, indo para, dia, saída e vagas.
 * "Você recebe por passageiro" vem de POST /api/rides/estimate; o preço final é da API.
 */
export function OfferForm() {
  const { t } = useTheme();
  const { say } = useToast();
  const shortcuts = usePlaceShortcuts();
  const [from, setFrom] = useState<AppPlace | null>(null);
  const [to, setTo] = useState<AppPlace | null>(null);
  const [day, setDay] = useState<0 | 1>(0);
  const [time, setTime] = useState('');
  const [seats, setSeats] = useState(3);
  const [estimate, setEstimate] = useState<{ key: string; value: Estimate } | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  // remonta os campos de lugar depois de publicar (limpa o texto digitado)
  const [round, setRound] = useState(0);

  const routeKey = from && to ? `${from.latitude},${from.longitude}>${to.latitude},${to.longitude}` : '';
  useEffect(() => {
    if (!from || !to) return;
    const key = `${from.latitude},${from.longitude}>${to.latitude},${to.longitude}`;
    let alive = true;
    api.post<Estimate>('/api/rides/estimate', { origin: toInput(from), destination: toInput(to) }).then(
      (value) => alive && setEstimate({ key, value }),
      (e) => alive && setError(messageOf(e)),
    );
    return () => {
      alive = false;
    };
  }, [from, to]);
  const est = estimate?.key === routeKey ? estimate.value : null;

  const ok = !!from && !!to && TIME_OK.test(time);

  const publish = async () => {
    if (busy) return;
    if (!ok) {
      setError(!from ? 'Escolha de onde você sai na lista.' : !to ? 'Escolha para onde vai na lista.' : 'Digite o horário como 17:50.');
      return;
    }
    setBusy(true);
    setError('');
    try {
      await api.post<Ride>('/api/rides', { origin: toInput(from), destination: toInput(to), date: isoDay(day), time, seats });
      setFrom(null);
      setTo(null);
      setTime('');
      setRound((r) => r + 1);
      say('Carona publicada.');
      router.navigate('/trips');
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <View style={{ gap: 14 }}>
      <Text size={15} leading={1.5} color={t.mut}>
        Publique o horário e as vagas. Quem vai para o mesmo lado reserva pelo app.
      </Text>
      <PlaceField key={`f${round}`} label="Saindo de" value={from} onChange={(p) => { setFrom(p); setError(''); }} placeholder="Ex.: PUCPR" shortcuts={shortcuts} />
      <PlaceField key={`t${round}`} label="Indo para" value={to} onChange={(p) => { setTo(p); setError(''); }} placeholder="Ex.: Centro Cívico" shortcuts={shortcuts} />
      <View style={{ gap: 8 }}>
        <Label>Dia</Label>
        <View accessibilityRole="radiogroup" accessibilityLabel="Dia da carona" style={{ flexDirection: 'row', gap: 8 }}>
          <Chip label="Hoje" on={day === 0} minHeight={48} style={{ flex: 1 }} onPress={() => setDay(0)} />
          <Chip label="Amanhã" on={day === 1} minHeight={48} style={{ flex: 1 }} onPress={() => setDay(1)} />
        </View>
      </View>
      <View style={{ flexDirection: 'row', gap: 12 }}>
        <Field
          containerStyle={{ flex: 1 }}
          label="Saída"
          value={time}
          onChangeText={(v) => {
            let d = v.replace(/\D/g, '').slice(0, 4);
            if (d.length > 2) d = d.slice(0, 2) + ':' + d.slice(2);
            setTime(d);
            setError('');
          }}
          placeholder="17:50"
          keyboardType="number-pad"
          maxLength={5}
          onSubmitEditing={publish}
          style={{ minWidth: 0 }}
        />
        <View style={{ flex: 1, gap: 8 }}>
          <Label>Vagas</Label>
          <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', borderWidth: 1, borderColor: t.line, backgroundColor: t.s1, borderRadius: 16, minHeight: 54, paddingHorizontal: 6 }}>
            <Stepper label="Menos uma vaga" text="−" onPress={() => setSeats((s) => Math.max(1, s - 1))} />
            <Text size={20} weight={600} tabular>
              {seats}
            </Text>
            <Stepper label="Mais uma vaga" text="+" onPress={() => setSeats((s) => Math.min(4, s + 1))} />
          </View>
        </View>
      </View>
      {est ? (
        <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 12, backgroundColor: t.s2, borderRadius: 18, padding: 16 }}>
          <View style={{ gap: 2, flexShrink: 1 }}>
            <Text size={14} color={t.mut}>
              Você recebe por passageiro
            </Text>
            <Text size={13} color={t.mut2}>{`${fmtKm(est.km)}, cerca de ${est.minutes} min${est.estimated ? ' (estimativa)' : ''}`}</Text>
          </View>
          <Text size={24} weight={700} tracking={-0.03}>
            {brl(est.driverAmountPerSeat)}
          </Text>
        </View>
      ) : null}
      <ErrorText>{error}</ErrorText>
      <Button variant={ok ? 'acc' : 'off'} label="Publicar carona" busy={busy} onPress={publish} style={{ marginTop: 4 }} />
    </View>
  );
}

function Stepper({ label, text, onPress }: { label: string; text: string; onPress: () => void }) {
  const { t } = useTheme();
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={label}
      onPress={onPress}
      style={({ pressed }) => ({ width: 44, height: 44, borderRadius: 12, backgroundColor: pressed ? t.s2 : t.s3, alignItems: 'center', justifyContent: 'center' })}
    >
      <Text size={20}>{text}</Text>
    </Pressable>
  );
}

import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, View } from 'react-native';
import { api, messageOf } from '@/api/client';
import type { DriverApplication } from '@/api/types';
import { Icon } from '@/components/Icon';
import { SubScreen } from '@/components/SubScreen';
import { Label, Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { PlateBadge, ScanBar } from '@/components/TripParts';
import { Button, ErrorText, Field, List, ListItem } from '@/components/ui';
import { imageForm, pickImage, type PickedImage } from '@/lib/images';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';

const VAL_OK = /^(0[1-9]|1[0-2])\/\d{4}$/;
const PLATE_OK = /^[A-Z]{3}-?\d[A-Z0-9]\d{2}$/;

/**
 * Virar motorista (isDriver do protótipo): dados da CNH e do carro + foto da CNH e selfie.
 * Completo, vai para análise manual no painel; esta tela confere a cada 8 s até sair a resposta.
 */
export default function Driver() {
  const { user, refreshUser } = useSession();
  const { say } = useToast();
  // undefined = carregando; null = ainda não pediu (404)
  const [app, setApp] = useState<DriverApplication | null | undefined>(undefined);

  useEffect(() => {
    api.get<DriverApplication>('/api/driver/application').then(setApp, () => setApp(null));
  }, []);

  const status = app?.status ?? user?.driverStatus ?? 'None';

  // Em análise: espera a resposta do painel
  useEffect(() => {
    if (status !== 'InReview') return;
    const timer = setInterval(async () => {
      const next = await api.get<DriverApplication>('/api/driver/application').catch(() => null);
      if (!next || next.status === 'InReview') return;
      setApp(next);
      await refreshUser().catch(() => {});
      if (next.status === 'Approved') say('Cadastro aprovado. Você já pode oferecer caronas.');
      else if (next.status === 'Rejected') say('Seu cadastro precisa de ajustes. Veja o motivo.');
    }, 8000);
    return () => clearInterval(timer);
  }, [status, refreshUser, say]);

  return (
    <SubScreen title="Virar motorista" gap={14}>
      {app === undefined && user?.driverStatus !== 'None' ? (
        <ActivityIndicator style={{ marginTop: 40 }} />
      ) : status === 'InReview' || status === 'Approved' ? (
        <StatusView app={app ?? null} approved={status === 'Approved'} />
      ) : (
        <DriverForm key={app ? 'app' : 'new'} app={app ?? null} onSaved={setApp} />
      )}
    </SubScreen>
  );
}

function StatusView({ app, approved }: { app: DriverApplication | null; approved: boolean }) {
  const { t } = useTheme();
  return (
    <View style={{ gap: 16 }}>
      <View style={{ gap: 12, backgroundColor: t.s1, borderWidth: 1, borderColor: t.line, borderRadius: 22, padding: 20 }}>
        <Label color={t.accentText}>{approved ? 'Aprovado' : 'Em análise'}</Label>
        <Text size={22} weight={700} tracking={-0.03}>
          {approved ? 'Você é motorista UniCar' : 'Estamos conferindo seus documentos'}
        </Text>
        <Text size={15} leading={1.5} color={t.mut}>
          {approved ? 'Sua CNH e seu veículo foram conferidos. Agora você pode publicar caronas.' : 'Isso leva pouco tempo. Avisamos assim que terminar.'}
        </Text>
        {!approved ? <ScanBar /> : null}
      </View>
      {app ? (
        <List>
          <ListItem>
            <Text size={16}>Veículo</Text>
            <Text size={14} color={t.mut}>{`${app.model} ${app.color.toLowerCase()}`}</Text>
          </ListItem>
          <ListItem last>
            <Text size={16}>Placa</Text>
            <PlateBadge plate={app.plate.replace('-', '')} size={13} />
          </ListItem>
        </List>
      ) : null}
      {approved ? <Button variant="inv" label="Oferecer uma carona" onPress={() => router.navigate('/offer')} /> : null}
    </View>
  );
}

function DriverForm({ app, onSaved }: { app: DriverApplication | null; onSaved: (a: DriverApplication) => void }) {
  const { t } = useTheme();
  const { refreshUser } = useSession();
  const { say, sayError } = useToast();
  const [cnh, setCnh] = useState('');
  const [val, setVal] = useState(app?.cnhExpiry ?? '');
  const [plate, setPlate] = useState(app?.plate.replace('-', '') ?? '');
  const [color, setColor] = useState(app?.color ?? '');
  const [model, setModel] = useState(app?.model ?? '');
  const [cnhPhoto, setCnhPhoto] = useState<PickedImage | null>(null);
  const [selfie, setSelfie] = useState<PickedImage | null>(null);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const rejected = app?.status === 'Rejected';

  const pick = async (kind: 'cnh' | 'selfie') => {
    try {
      const image = await pickImage({ camera: kind === 'selfie' });
      if (!image) return;
      if (kind === 'cnh') setCnhPhoto(image);
      else setSelfie(image);
      setError('');
    } catch (e) {
      sayError(e);
    }
  };

  const submit = async () => {
    if (busy) return;
    const p = plate.toUpperCase();
    const dataChanged = !app || !!cnh || val !== app.cnhExpiry || p !== app.plate.replace('-', '') || model.trim() !== app.model || color.trim() !== app.color;
    const err =
      dataChanged && cnh.length !== 11
        ? 'A CNH tem 11 números.'
        : !VAL_OK.test(val)
          ? 'Validade no formato MM/AAAA.'
          : !PLATE_OK.test(p)
            ? 'Placa inválida. Ex.: ABC1D23.'
            : !model.trim()
              ? 'Informe o modelo do carro.'
              : !color.trim()
                ? 'Informe a cor do carro.'
                : !cnhPhoto && !app?.hasCnhPhoto
                  ? 'Adicione a foto da CNH.'
                  : !selfie && !app?.hasSelfie
                    ? 'Tire uma selfie para conferirmos com a CNH.'
                    : '';
    if (err) return setError(err);

    // Dados primeiro (as fotos só entram depois do pedido criado). Reenvio depois de recusa: fotos novas
    // primeiro, porque com tudo completo a API já manda para análise e trava o resto.
    const sendData = () => api.post<DriverApplication>('/api/driver/application', { cnh, cnhExpiry: val, plate: p, model: model.trim(), color: color.trim() });
    const photoSteps = [
      cnhPhoto ? async () => api.upload<DriverApplication>('/api/driver/application/cnh-photo', await imageForm(cnhPhoto)) : null,
      selfie ? async () => api.upload<DriverApplication>('/api/driver/application/selfie', await imageForm(selfie)) : null,
    ].filter((s): s is () => Promise<DriverApplication> => !!s);
    const dataSteps = dataChanged ? [sendData] : [];
    const steps = rejected ? [...photoSteps, ...dataSteps] : [...dataSteps, ...photoSteps];

    setBusy(true);
    setError('');
    try {
      let last = app;
      for (const step of steps) {
        last = await step();
        if (last.status === 'InReview') break;
      }
      if (last) onSaved(last);
      await refreshUser().catch(() => {});
      if (last?.status === 'InReview') say('Enviado para análise.');
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      {rejected ? (
        <View style={{ gap: 6, backgroundColor: t.s1, borderWidth: 1, borderColor: t.err, borderRadius: 20, padding: 16 }}>
          <Label color={t.err}>Cadastro recusado</Label>
          <Text size={15} leading={1.5}>
            {app?.rejectionReason ?? 'Confira os dados e as fotos e envie de novo.'}
          </Text>
        </View>
      ) : null}
      <Text size={15} leading={1.5} color={t.mut}>
        Conferimos a CNH e o veículo antes de liberar suas caronas.
      </Text>
      <Field
        label="Número da CNH"
        value={cnh}
        onChangeText={(v) => {
          setCnh(v.replace(/\D/g, '').slice(0, 11));
          setError('');
        }}
        placeholder={app ? `${app.cnhMasked} (digite para trocar)` : '11 dígitos'}
        keyboardType="number-pad"
        maxLength={11}
      />
      <Field
        label="Validade"
        value={val}
        onChangeText={(v) => {
          let d = v.replace(/\D/g, '').slice(0, 6);
          if (d.length > 2) d = d.slice(0, 2) + '/' + d.slice(2);
          setVal(d);
          setError('');
        }}
        placeholder="MM/AAAA"
        keyboardType="number-pad"
        maxLength={7}
      />
      <View style={{ flexDirection: 'row', gap: 12 }}>
        <Field
          containerStyle={{ flex: 1 }}
          label="Placa"
          value={plate}
          onChangeText={(v) => {
            setPlate(v.toUpperCase().replace(/[^A-Z0-9-]/g, '').slice(0, 8));
            setError('');
          }}
          placeholder="ABC1D23"
          autoCapitalize="characters"
          autoCorrect={false}
          maxLength={8}
          style={{ minWidth: 0 }}
        />
        <Field
          containerStyle={{ flex: 1 }}
          label="Cor"
          value={color}
          onChangeText={(v) => {
            setColor(v);
            setError('');
          }}
          placeholder="Prata"
          style={{ minWidth: 0 }}
        />
      </View>
      <Field
        label="Modelo"
        value={model}
        onChangeText={(v) => {
          setModel(v);
          setError('');
        }}
        placeholder="Ex.: Gol 2019"
      />
      <PhotoRow
        label={cnhPhoto ? 'Foto da CNH adicionada' : app?.hasCnhPhoto ? 'Foto da CNH enviada (toque para trocar)' : 'Adicionar foto da CNH'}
        done={!!cnhPhoto || !!app?.hasCnhPhoto}
        onPress={() => pick('cnh')}
      />
      <PhotoRow
        label={selfie ? 'Selfie adicionada' : app?.hasSelfie ? 'Selfie enviada (toque para trocar)' : 'Tirar uma selfie'}
        done={!!selfie || !!app?.hasSelfie}
        onPress={() => pick('selfie')}
      />
      <ErrorText>{error}</ErrorText>
      <Button variant="acc" label="Enviar para análise" busy={busy} onPress={submit} style={{ marginTop: 6 }} />
    </>
  );
}

/** Caixa tracejada "Adicionar foto" do protótipo. */
function PhotoRow({ label, done, onPress }: { label: string; done: boolean; onPress: () => void }) {
  const { t } = useTheme();
  return (
    <Pressable
      accessibilityRole="button"
      onPress={onPress}
      style={({ pressed }) => ({
        flexDirection: 'row',
        alignItems: 'center',
        gap: 12,
        backgroundColor: pressed ? t.s2 : t.s1,
        borderWidth: 1,
        borderStyle: done ? 'solid' : 'dashed',
        borderColor: done ? t.accent : t.line,
        borderRadius: 16,
        minHeight: 64,
        paddingHorizontal: 16,
      })}
    >
      <Icon name={done ? 'check' : 'camera'} size={20} color={done ? t.accentText : t.mut} strokeWidth={2.2} />
      <Text size={15} style={{ flex: 1 }}>
        {label}
      </Text>
    </Pressable>
  );
}

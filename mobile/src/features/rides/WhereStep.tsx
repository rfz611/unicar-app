import { router } from 'expo-router';
import { useEffect, useRef, useState, type ComponentRef, type Ref } from 'react';
import { Pressable, TextInput, View } from 'react-native';
import { api } from '@/api/client';
import type { PlaceDetails, PlaceSuggestion, RecentPlace, SavedPlace } from '@/api/types';
import { Icon, icons } from '@/components/Icon';
import { Logo } from '@/components/Logo';
import { Avatar, Page } from '@/components/Page';
import { Label, Text } from '@/components/Text';
import { useToast } from '@/components/Toast';
import { fmtKm, km, shortOf, uuid, type AppPlace } from '@/lib/geo';
import { firstName } from '@/lib/user';
import { useSession } from '@/session/SessionProvider';
import { useTheme } from '@/theme/ThemeProvider';
import { fonts } from '@/theme/tokens';

type Picking = 'dest' | 'origin' | 'home';

type Row = { key: string; name: string; address: string; icon: string; km?: string; pick: () => void };

const CAMPUS_RE = /UFPR|PUCPR|UTFPR|Universidade|Faculdade|FAE|Positivo|Câmpus|Campus/i;
const iconOf = (name: string) => (CAMPUS_RE.test(name) ? icons.campus : /shopping/i.test(name) ? icons.shop : icons.pin);

export const placeFromDetails = (p: PlaceDetails): AppPlace => ({
  name: p.name,
  short: shortOf(p.name, p.address),
  address: p.address,
  neighborhood: p.neighborhood,
  latitude: p.latitude,
  longitude: p.longitude,
});

export const placeFromSaved = (h: SavedPlace): AppPlace => ({
  name: h.label,
  short: h.neighborhood ?? shortOf(h.label, h.address),
  address: h.address,
  neighborhood: h.neighborhood,
  latitude: h.latitude,
  longitude: h.longitude,
});

/** Passo "Para onde, {nome}?" da aba Caronas (isWhere no protótipo). */
export function WhereStep({
  origin,
  gpsOrigin,
  onOrigin,
  onDestination,
  home,
  onHome,
}: {
  origin: AppPlace | null;
  /** A posição do GPS, para a opção "Localização atual" ao alterar a saída. */
  gpsOrigin: AppPlace | null;
  onOrigin: (p: AppPlace) => void;
  onDestination: (p: AppPlace) => void;
  home: SavedPlace | null | undefined;
  onHome: (h: SavedPlace) => void;
}) {
  const { t } = useTheme();
  const { user } = useSession();
  const { say, sayError } = useToast();
  const [picking, setPicking] = useState<Picking>('dest');
  const [query, setQuery] = useState('');
  /** Resultados da busca, junto com o texto que os gerou (resultado velho não aparece). */
  const [found, setFound] = useState<{ q: string; list: PlaceSuggestion[] } | null>(null);
  const [recents, setRecents] = useState<RecentPlace[]>([]);
  /** sessionToken do Google: o mesmo durante uma busca; troca quando escolhe o lugar. */
  const [session, setSession] = useState(uuid);
  const input = useRef<ComponentRef<typeof TextInput>>(null);

  useEffect(() => {
    api.get<RecentPlace[]>('/api/me/places/recent').then(setRecents, () => {});
  }, []);

  // Busca enquanto digita (espera 250 ms). Sem chave do Google, a API usa o catálogo local.
  useEffect(() => {
    const q = query.trim();
    if (q.length < 2) return;
    const ctrl = new AbortController();
    const timer = setTimeout(() => {
      const near = origin ? `&lat=${origin.latitude}&lng=${origin.longitude}` : '';
      api
        .get<PlaceSuggestion[]>(`/api/places/autocomplete?q=${encodeURIComponent(q)}&session=${session}${near}`, { signal: ctrl.signal })
        .then((list) => setFound({ q, list }), (e) => {
          if ((e as Error)?.name !== 'AbortError') sayError(e);
        });
    }, 250);
    return () => {
      clearTimeout(timer);
      ctrl.abort();
    };
  }, [query, origin, session, sayError]);

  const results = query.trim().length >= 2 && found?.q === query.trim() ? found.list : null;

  const choose = async (place: AppPlace) => {
    setSession(uuid());
    setQuery('');
    if (picking === 'origin') {
      onOrigin(place);
      setPicking('dest');
      return;
    }
    if (picking === 'home') {
      try {
        const saved = await api.put<SavedPlace>('/api/me/places/home', {
          address: place.address,
          neighborhood: place.neighborhood,
          latitude: place.latitude,
          longitude: place.longitude,
        });
        onHome(saved);
        say('Casa salva: ' + (saved.neighborhood ?? place.short));
      } catch (e) {
        sayError(e);
      }
      setPicking('dest');
      return;
    }
    onDestination(place);
  };

  const pickSuggestion = async (s: PlaceSuggestion) => {
    try {
      const details = await api.get<PlaceDetails>(`/api/places/${encodeURIComponent(s.placeId)}?session=${session}`);
      await choose(placeFromDetails(details));
    } catch (e) {
      sayError(e);
    }
  };

  const rows: Row[] = (() => {
    const list: Row[] = query.trim()
      ? (results ?? []).map((s) => ({ key: s.placeId, name: s.name, address: s.address, icon: iconOf(s.name), pick: () => pickSuggestion(s) }))
      : recents.map((r) => {
          // A API guarda o endereço da viagem; o nome é a rua, o curto é o bairro.
          const street = r.address.split(',')[0] ?? r.address;
          const place: AppPlace = {
            name: street,
            short: r.neighborhood ?? street,
            address: r.address,
            neighborhood: r.neighborhood,
            latitude: r.latitude,
            longitude: r.longitude,
          };
          return { key: r.address, name: place.name, address: r.address, icon: icons.clock, km: origin ? fmtKm(km(origin, r)) : undefined, pick: () => choose(place) };
        });
    if (picking === 'origin' && gpsOrigin && !query.trim()) {
      list.unshift({ key: 'gps', name: 'Localização atual', address: 'Pelo GPS do celular', icon: icons.gps, pick: () => choose(gpsOrigin) });
    }
    return list;
  })();

  const campus = user?.campus;
  const goCampus = () => {
    if (!campus) {
      say('Defina sua faculdade no perfil.');
      return;
    }
    void choose({
      name: `${user!.university} - ${campus.name}`,
      short: `${user!.university} ${campus.name.split(' ').slice(-1)[0]}`,
      address: campus.address,
      latitude: campus.latitude,
      longitude: campus.longitude,
    });
  };
  const goHome = () => {
    if (home) void choose(placeFromSaved(home));
    else {
      setPicking('home');
      setQuery('');
      input.current?.focus();
    }
  };

  const title = picking === 'home' ? 'Onde fica sua casa?' : picking === 'origin' ? 'De onde você sai?' : `Para onde, ${firstName(user?.fullName) || 'universitário'}?`;
  const searching = !!query.trim();

  return (
    <Page gap={22}>
      <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
        <Logo mark={24} text={21} />
        <Pressable accessibilityRole="button" accessibilityLabel="Abrir perfil" onPress={() => router.navigate('/profile')}>
          <Avatar name={user?.fullName} photoUrl={user?.photoUrl} size={44} ring={2} />
        </Pressable>
      </View>

      <Text size={40} weight={700} tracking={-0.05} leading={1} style={{ marginTop: 6 }}>
        {title}
      </Text>

      {/* Saída e destino */}
      <View style={{ backgroundColor: t.s1, borderWidth: 1, borderColor: t.line, borderRadius: 22, paddingVertical: 4, paddingHorizontal: 16 }}>
        {picking === 'origin' ? (
          <SearchRow ref={input} value={query} onChange={setQuery} placeholder="De onde você sai?" label="Origem" dot="ring" border onSubmit={() => rows[0]?.pick()} />
        ) : (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel="Alterar saída"
            onPress={() => {
              setPicking('origin');
              setQuery('');
            }}
            style={{ flexDirection: 'row', alignItems: 'center', gap: 12, minHeight: 56, borderBottomWidth: 1, borderBottomColor: t.line }}
          >
            <Dot kind="ring" />
            <View style={{ flex: 1, gap: 2 }}>
              <Text size={15} numberOfLines={1}>
                {origin?.name ?? 'Procurando sua localização…'}
              </Text>
              <Text size={12} color={t.mut2} numberOfLines={1}>
                {origin ? (origin === gpsOrigin ? 'Pelo GPS do celular' : origin.address) : ' '}
              </Text>
            </View>
            <Text size={13} weight={600} color={t.accentText}>
              Alterar
            </Text>
          </Pressable>
        )}
        {picking !== 'origin' ? (
          <SearchRow
            ref={input}
            value={query}
            onChange={setQuery}
            placeholder={picking === 'home' ? 'Busque seu endereço ou bairro' : 'Faculdade, lugar ou endereço'}
            label="Destino"
            dot="fill"
            onSubmit={() => rows[0]?.pick()}
          />
        ) : (
          <Pressable
            accessibilityRole="button"
            onPress={() => {
              setPicking('dest');
              setQuery('');
            }}
            style={{ flexDirection: 'row', alignItems: 'center', gap: 12, minHeight: 56 }}
          >
            <Dot kind="fill" />
            <Text size={15} color={t.mut2}>
              Para onde?
            </Text>
          </Pressable>
        )}
      </View>

      {!searching && picking === 'dest' ? (
        <View style={{ flexDirection: 'row', gap: 10 }}>
          <Shortcut accent icon="campus" title="Minha faculdade" sub={campus ? `${user!.university} ${campus.name}` : 'Defina no perfil'} onPress={goCampus} />
          <Shortcut
            icon="home"
            title="Casa"
            sub={home === undefined ? ' ' : home ? (home.neighborhood ?? shortOf(home.label, home.address)) : 'Toque para definir'}
            onPress={goHome}
          />
        </View>
      ) : null}

      {picking === 'home' ? (
        <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 12, backgroundColor: t.s2, borderRadius: 16, paddingVertical: 12, paddingHorizontal: 14 }}>
          <Text size={14} color={t.mut} style={{ flex: 1 }}>
            Escolha onde fica sua casa. Os outros só veem o bairro.
          </Text>
          <Pressable accessibilityRole="button" onPress={() => setPicking('dest')} style={{ minHeight: 44, justifyContent: 'center' }}>
            <Text size={14} weight={600}>
              Cancelar
            </Text>
          </Pressable>
        </View>
      ) : null}

      <View style={{ gap: 4 }}>
        {rows.length > 0 || searching ? (
          <Label style={{ paddingHorizontal: 2, paddingBottom: 2 }}>{searching ? 'Resultados' : 'Recentes'}</Label>
        ) : null}
        {rows.map((r) => (
          <Pressable
            key={r.key}
            accessibilityRole="button"
            onPress={r.pick}
            style={({ pressed }) => ({ flexDirection: 'row', alignItems: 'center', gap: 14, borderBottomWidth: 1, borderBottomColor: t.line, minHeight: 64, paddingVertical: 10, paddingHorizontal: 2, opacity: pressed ? 0.7 : 1 })}
          >
            <Icon d={r.icon} size={18} color={t.mut2} strokeWidth={1.8} />
            <View style={{ flex: 1, gap: 2 }}>
              <Text size={15} weight={500} leading={1.3}>
                {r.name}
              </Text>
              <Text size={13} color={t.mut2} numberOfLines={1}>
                {r.address}
              </Text>
            </View>
            {r.km ? (
              <Text size={13} color={t.mut2} tabular>
                {r.km}
              </Text>
            ) : null}
          </Pressable>
        ))}
        {searching && results && results.length === 0 ? (
          <Text size={15} color={t.mut} style={{ paddingVertical: 16, paddingHorizontal: 4 }}>
            Nada encontrado. Tente o nome do lugar ou da rua.
          </Text>
        ) : null}
      </View>
    </Page>
  );
}

function Dot({ kind }: { kind: 'ring' | 'fill' }) {
  const { t } = useTheme();
  return kind === 'ring' ? (
    <View style={{ width: 10, height: 10, borderRadius: 5, borderWidth: 2.5, borderColor: t.fg }} />
  ) : (
    <View style={{ width: 10, height: 10, borderRadius: 5, backgroundColor: t.accent }} />
  );
}

function SearchRow({
  ref,
  value,
  onChange,
  placeholder,
  label,
  dot,
  border,
  onSubmit,
}: {
  ref: Ref<ComponentRef<typeof TextInput>>;
  value: string;
  onChange: (v: string) => void;
  placeholder: string;
  label: string;
  dot: 'ring' | 'fill';
  border?: boolean;
  onSubmit: () => void;
}) {
  const { t } = useTheme();
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12, minHeight: 56, borderBottomWidth: border ? 1 : 0, borderBottomColor: t.line }}>
      <Dot kind={dot} />
      <TextInput
        ref={ref}
        value={value}
        onChangeText={onChange}
        placeholder={placeholder}
        accessibilityLabel={label}
        placeholderTextColor={t.mut2}
        selectionColor={t.accent}
        autoCorrect={false}
        returnKeyType="search"
        onSubmitEditing={onSubmit}
        style={[{ flex: 1, minHeight: 54, color: t.fg, fontSize: 17, fontFamily: fonts[400] }, { outlineWidth: 0 } as object]}
      />
    </View>
  );
}

function Shortcut({ icon, title, sub, onPress, accent }: { icon: 'campus' | 'home'; title: string; sub: string; onPress: () => void; accent?: boolean }) {
  const { t } = useTheme();
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityLabel={title}
      onPress={onPress}
      style={({ pressed }) => ({ flex: 1, borderWidth: 1, borderColor: t.line, borderRadius: 20, backgroundColor: pressed ? t.s2 : t.s1, padding: 16, minHeight: 104, justifyContent: 'space-between', gap: 12 })}
    >
      <View style={{ width: 36, height: 36, borderRadius: 12, backgroundColor: accent ? t.accent : t.s3, alignItems: 'center', justifyContent: 'center' }}>
        <Icon name={icon} size={20} color={accent ? '#FFFFFF' : t.fg} />
      </View>
      <View style={{ gap: 2 }}>
        <Text size={16} weight={700}>
          {title}
        </Text>
        <Text size={13} color={t.mut} numberOfLines={1}>
          {sub}
        </Text>
      </View>
    </Pressable>
  );
}

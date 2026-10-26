import { useEffect, useState } from 'react';
import { Pressable, View } from 'react-native';
import { api } from '@/api/client';
import type { PlaceDetails, PlaceSuggestion } from '@/api/types';
import { placeFromDetails } from '@/features/rides/WhereStep';
import { uuid, type AppPlace } from '@/lib/geo';
import { useTheme } from '@/theme/ThemeProvider';
import { Text } from './Text';
import { useToast } from './Toast';
import { Chip, Field, List } from './ui';

/**
 * Campo de lugar do formulário "Oferecer" do protótipo ("Saindo de", "Indo para"): digita, aparecem
 * sugestões da busca da API; escolhido, a borda fica azul. Atalhos opcionais em chips (Minha faculdade, Casa).
 */
export function PlaceField({
  label,
  value,
  onChange,
  placeholder,
  shortcuts = [],
}: {
  label: string;
  value: AppPlace | null;
  onChange: (p: AppPlace | null) => void;
  placeholder: string;
  shortcuts?: { label: string; place: AppPlace }[];
}) {
  const { t } = useTheme();
  const { sayError } = useToast();
  const [query, setQuery] = useState(value?.short ?? '');
  const [found, setFound] = useState<{ q: string; list: PlaceSuggestion[] } | null>(null);
  const [session, setSession] = useState(uuid);

  useEffect(() => {
    const q = query.trim();
    if (value || q.length < 2) return;
    const ctrl = new AbortController();
    const timer = setTimeout(() => {
      api
        .get<PlaceSuggestion[]>(`/api/places/autocomplete?q=${encodeURIComponent(q)}&session=${session}`, { signal: ctrl.signal })
        .then((list) => setFound({ q, list: list.slice(0, 4) }), () => {});
    }, 250);
    return () => {
      clearTimeout(timer);
      ctrl.abort();
    };
  }, [query, value, session]);

  const suggestions = !value && found?.q === query.trim() ? found.list : [];

  const pick = async (s: PlaceSuggestion) => {
    try {
      const place = placeFromDetails(await api.get<PlaceDetails>(`/api/places/${encodeURIComponent(s.placeId)}?session=${session}`));
      setSession(uuid());
      setQuery(place.short);
      onChange(place);
    } catch (e) {
      sayError(e);
    }
  };

  return (
    <View style={{ gap: 8 }}>
      <Field
        label={label}
        value={query}
        onChangeText={(v) => {
          setQuery(v);
          if (value) onChange(null);
        }}
        placeholder={placeholder}
        autoCorrect={false}
        state={value ? 'ok' : undefined}
      />
      {shortcuts.length > 0 && !value ? (
        <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
          {shortcuts.map((s) => (
            <Chip
              key={s.label}
              label={s.label}
              onPress={() => {
                setQuery(s.place.short);
                onChange(s.place);
              }}
            />
          ))}
        </View>
      ) : null}
      {suggestions.length > 0 ? (
        <List>
          {suggestions.map((s, i) => (
            <Pressable
              key={s.placeId}
              accessibilityRole="button"
              onPress={() => pick(s)}
              style={({ pressed }) => ({ minHeight: 56, paddingVertical: 8, paddingHorizontal: 16, justifyContent: 'center', gap: 2, borderBottomWidth: i === suggestions.length - 1 ? 0 : 1, borderBottomColor: t.line, backgroundColor: pressed ? t.s2 : 'transparent' })}
            >
              <Text size={15}>{s.name}</Text>
              <Text size={12} color={t.mut2} numberOfLines={1}>
                {s.address}
              </Text>
            </Pressable>
          ))}
        </List>
      ) : null}
    </View>
  );
}

import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { api } from '@/api/client';
import { digits, mask, matchCourses } from '@/lib/format';
import { useTheme } from '@/theme/ThemeProvider';
import { Label, Text } from './Text';
import { Chip, Field, List, ListItem } from './ui';

export const SHIFTS = [
  { label: 'Manhã', value: 'Manha' },
  { label: 'Tarde', value: 'Tarde' },
  { label: 'Noite', value: 'Noite' },
] as const;

export type ShiftValue = (typeof SHIFTS)[number]['value'];

export type ProfileDraft = {
  name: string;
  birth: string; // DD/MM/AAAA
  phone: string; // (41) 99999-0000
  cpf: string; // 000.000.000-00 (só no cadastro)
  courseQuery: string;
  course: string; // curso escolhido da lista
  shift: ShiftValue | '';
  period: string;
};

export const emptyDraft: ProfileDraft = { name: '', birth: '', phone: '', cpf: '', courseQuery: '', course: '', shift: '', period: '' };

/** Mesmas contas do protótipo, só para o botão e a primeira mensagem. Quem decide é a API. */
function cpfValid(v: string) {
  const d = digits(v);
  if (d.length !== 11 || /^(\d)\1{10}$/.test(d)) return false;
  const calc = (len: number) => {
    let sum = 0;
    for (let i = 0; i < len; i++) sum += Number(d[i]) * (len + 1 - i);
    const r = (sum * 10) % 11;
    return r === 10 ? 0 : r;
  };
  return calc(9) === Number(d[9]) && calc(10) === Number(d[10]);
}

function ageOf(v: string) {
  const m = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(v);
  if (!m) return -1;
  const b = new Date(Number(m[3]), Number(m[2]) - 1, Number(m[1]));
  if (b.getDate() !== Number(m[1]) || b.getMonth() !== Number(m[2]) - 1) return -1;
  const now = new Date();
  let a = now.getFullYear() - b.getFullYear();
  if (now.getMonth() < b.getMonth() || (now.getMonth() === b.getMonth() && now.getDate() < b.getDate())) a--;
  return a;
}

/** Primeira coisa que falta, com o texto do protótipo; '' quando dá para enviar. */
export function missingField(d: ProfileDraft, withCpf: boolean) {
  const age = ageOf(d.birth);
  const period = Number(d.period);
  if (d.name.trim().split(/\s+/).filter(Boolean).length < 2) return 'Escreva nome e sobrenome.';
  if (age < 0 || age >= 100) return 'Data de nascimento no formato DD/MM/AAAA.';
  if (age < 18) return 'O UniCar é para maiores de 18 anos.';
  if (digits(d.phone).length !== 11) return 'Celular com DDD, 11 números.';
  if (withCpf && !cpfValid(d.cpf)) return 'CPF inválido. Confira os números.';
  if (!d.course) return 'Escolha um curso da lista.';
  if (!(period >= 1 && period <= 12)) return 'Período entre 1 e 12.';
  if (!d.shift) return 'Escolha o turno.';
  return '';
}

/**
 * Campos de "Seu perfil" (e de Editar perfil): nome, nascimento + celular, CPF, curso com busca
 * na lista fechada (GET /api/courses), turno e período.
 */
export function ProfileFields({
  draft,
  onChange,
  cpf,
  onSubmit,
}: {
  draft: ProfileDraft;
  onChange: (d: ProfileDraft) => void;
  /** 'edit' = digitável (cadastro); 'readonly' = mostra o mascarado; undefined = esconde. */
  cpf?: 'edit' | { masked: string };
  onSubmit?: () => void;
}) {
  const { t } = useTheme();
  const [courses, setCourses] = useState<string[]>([]);
  const set = (patch: Partial<ProfileDraft>) => onChange({ ...draft, ...patch });

  useEffect(() => {
    api.get<string[]>('/api/courses').then(setCourses, () => {});
  }, []);

  const suggestions = draft.course ? [] : matchCourses(courses, draft.courseQuery);

  return (
    <>
      <Field label="Nome completo" value={draft.name} onChangeText={(name) => set({ name })} placeholder="Seu nome completo" autoComplete="name" textContentType="name" autoCapitalize="words" returnKeyType="next" onSubmitEditing={onSubmit} />
      <View style={{ flexDirection: 'row', gap: 12 }}>
        <Field
          containerStyle={{ flex: 1 }}
          label="Nascimento"
          value={draft.birth}
          onChangeText={(v) => set({ birth: mask(v, '99/99/9999') })}
          placeholder="DD/MM/AAAA"
          keyboardType="number-pad"
          maxLength={10}
        />
        <Field
          containerStyle={{ flex: 1 }}
          label="Celular"
          value={draft.phone}
          onChangeText={(v) => set({ phone: mask(v, '(99) 99999-9999') })}
          placeholder="(41) 99999-0000"
          keyboardType="phone-pad"
          textContentType="telephoneNumber"
          maxLength={15}
        />
      </View>
      {cpf === 'edit' ? (
        <Field label="CPF" value={draft.cpf} onChangeText={(v) => set({ cpf: mask(v, '999.999.999-99') })} placeholder="000.000.000-00" keyboardType="number-pad" maxLength={14} />
      ) : cpf ? (
        <Field label="CPF" value={cpf.masked} editable={false} style={{ color: t.mut }} />
      ) : null}
      <View style={{ gap: 8 }}>
        <Field
          label="Curso"
          value={draft.courseQuery}
          onChangeText={(courseQuery) => set({ courseQuery, course: '' })}
          placeholder="Busque seu curso"
          autoCorrect={false}
          state={draft.course ? 'ok' : undefined}
        />
        {suggestions.length > 0 ? (
          <List>
            {suggestions.map((name, i) => (
              <ListItem key={name} last={i === suggestions.length - 1} onPress={() => set({ course: name, courseQuery: name })} style={{ paddingHorizontal: 16, paddingVertical: 8 }}>
                <Text size={16}>{name}</Text>
              </ListItem>
            ))}
          </List>
        ) : null}
      </View>
      <View style={{ gap: 8 }}>
        <Label>Turno</Label>
        <View style={{ flexDirection: 'row', gap: 8 }}>
          {SHIFTS.map((s) => (
            <Chip key={s.value} label={s.label} on={draft.shift === s.value} minHeight={48} style={{ flex: 1 }} onPress={() => set({ shift: s.value })} />
          ))}
        </View>
      </View>
      <Field
        label="Período"
        value={draft.period}
        onChangeText={(v) => set({ period: v.replace(/\D/g, '').slice(0, 2) })}
        placeholder="Em que período você está? Ex.: 3"
        keyboardType="number-pad"
        maxLength={2}
      />
    </>
  );
}

/** Máscara simples: 9 = dígito, o resto é literal (mesma função do protótipo). */
export function mask(value: string, pattern: string) {
  const d = value.replace(/\D/g, '');
  let out = '';
  let j = 0;
  for (const ch of pattern) {
    if (j >= d.length) break;
    if (ch === '9') out += d[j++];
    else out += ch;
  }
  return out;
}

export const digits = (value: string) => value.replace(/\D/g, '');

/** "DD/MM/AAAA" → "AAAA-MM-DD" (formato da API); null se incompleto. */
export function toIsoDate(br: string) {
  const m = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(br);
  return m ? `${m[3]}-${m[2]}-${m[1]}` : null;
}

/** "AAAA-MM-DD" → "DD/MM/AAAA". */
export function toBrDate(iso: string) {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso);
  return m ? `${m[3]}/${m[2]}/${m[1]}` : '';
}

/** Ignora acento, caixa e pontuação (busca tolerante do protótipo). */
export function norm(text?: string | null) {
  return (text ?? '')
    .toLowerCase()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9 ]/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

/** Diferença de no máximo 1 letra (troca, sobra ou falta). */
export function lev1(a: string, b: string) {
  if (Math.abs(a.length - b.length) > 1) return false;
  let i = 0;
  let j = 0;
  let d = 0;
  while (i < a.length && j < b.length) {
    if (a[i] === b[j]) {
      i++;
      j++;
      continue;
    }
    if (++d > 1) return false;
    if (a.length > b.length) i++;
    else if (b.length > a.length) j++;
    else {
      i++;
      j++;
    }
  }
  return d + (a.length - i) + (b.length - j) <= 1;
}

/**
 * Cursos que combinam com o que foi digitado (até 5). Cada pedaço digitado precisa bater com alguma
 * palavra do curso: começo da palavra, pedaço no meio (3+ letras) ou 1 letra errada (4+ letras).
 * "eng sof" → Engenharia de Software; "medicna" → Medicina.
 */
export function matchCourses(all: string[], query: string) {
  const terms = norm(query).split(' ').filter(Boolean);
  if (!terms.length) return [];
  const fits = (term: string, word: string) =>
    word.startsWith(term) || (term.length >= 3 && word.includes(term)) || (term.length >= 4 && (lev1(term, word) || lev1(term, word.slice(0, term.length))));
  return all.filter((c) => {
    const words = norm(c).split(' ');
    return terms.every((term) => words.some((w) => fits(term, w)));
  }).slice(0, 5);
}

/** R$ 12,50 */
export const brl = (value: number) => 'R$ ' + value.toFixed(2).replace('.', ',');

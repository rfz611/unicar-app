import type { User } from '@/api/types';

export const SHIFT_LABEL = { Manha: 'Manhã', Tarde: 'Tarde', Noite: 'Noite' } as const;

/** Etiquetas do perfil, na ordem do protótipo: curso, período, faculdade, turno. */
export function userTags(u: Pick<User, 'course' | 'showCourse' | 'period' | 'university' | 'shift'>) {
  return [u.showCourse ? u.course : '', u.period ? `${u.period}º período` : '', u.university, SHIFT_LABEL[u.shift]].filter(Boolean);
}

export const firstName = (fullName?: string | null) => fullName?.trim().split(/\s+/)[0] ?? '';

/** Corpo do PUT /api/users/me a partir do usuário atual (o PUT troca tudo de uma vez). */
export function updateBody(u: User) {
  return {
    fullName: u.fullName,
    birthDate: u.birthDate,
    phone: u.phone,
    course: u.course,
    period: u.period,
    shift: u.shift,
    gender: u.gender,
    bio: u.bio,
    trustedContactPhone: u.trustedContactPhone,
  };
}

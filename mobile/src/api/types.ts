/** Formatos que a API devolve (espelham os DTOs do C#, em camelCase). */

export type Campus = { id: number; name: string; address: string; latitude: number; longitude: number };

export type User = {
  id: string;
  email: string;
  fullName: string;
  university: string;
  campus: Campus | null;
  canChooseCampus: boolean;
  birthDate: string;
  phone: string;
  cpfMasked: string;
  course: string;
  showCourse: boolean;
  period: number;
  shift: 'Manha' | 'Tarde' | 'Noite';
  gender: string | null;
  bio: string | null;
  photoUrl: string | null;
  trustedContactPhone: string | null;
  driverStatus: 'None' | 'InReview' | 'Approved' | 'Rejected';
  emailVerifiedAt: string;
};

export type AuthResponse = { accessToken: string; user: User };

export type Preferences = {
  theme: 'System' | 'Light' | 'Dark';
  rideAlerts: boolean;
  alwaysShareTrip: boolean;
  showCourse: boolean;
};

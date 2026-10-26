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

// ---------- lugares ----------

export type PlaceSuggestion = { placeId: string; name: string; address: string };

export type PlaceDetails = {
  placeId: string;
  name: string;
  address: string;
  neighborhood: string | null;
  latitude: number;
  longitude: number;
};

export type SavedPlace = {
  id: number;
  kind: 'Home' | 'Work' | 'Other';
  label: string;
  address: string;
  neighborhood: string | null;
  googlePlaceId: string | null;
  latitude: number;
  longitude: number;
};

export type RecentPlace = { address: string; neighborhood: string | null; latitude: number; longitude: number; departureAt: string };

// ---------- preço e caronas ----------

export type PriceQuote = { total: number; driverAmount: number; platformAmount: number; km: number; minutes: number; mode: number };

export type Quote = { km: number; minutes: number; encodedPolyline: string | null; now: PriceQuote; scheduled: PriceQuote; estimated: boolean };

export type RidePlace = { label: string; address: string | null; neighborhood: string | null; latitude: number | null; longitude: number | null };

export type Ride = {
  id: string;
  status: 'Scheduled' | 'Cancelled';
  departureAt: string;
  departureDate: string;
  departureTime: string;
  origin: RidePlace;
  destination: RidePlace;
  pricePerSeat: number;
  seats: number;
  seatsLeft: number;
  km: number;
  minutes: number;
  driver: { id: string; firstName: string; course: string | null; university: string; photoUrl: string | null; rating: number | null; ratingsCount: number };
  vehicle: { id: number; plate: string; model: string; color: string };
  pickup: { at: string; time: string; minutesAfterDeparture: number; estimated: boolean } | null;
  distanceToDestinationKm: number | null;
  isMine: boolean;
  reservedByMe: boolean;
};

export type TimeSuggestion = {
  id: string;
  rideId: string;
  passengerId: string;
  passengerFirstName: string;
  minutes: number;
  status: 'Pending' | 'Accepted' | 'Declined';
  currentDepartureTime: string;
  suggestedDepartureTime: string;
  createdAt: string;
};

export type RouteAlert = {
  id: string;
  label: string;
  latitude: number;
  longitude: number;
  radiusKm: number;
  matchedRideId: string | null;
  matchedAt: string | null;
  createdAt: string;
};

export type Preferences = {
  theme: 'System' | 'Light' | 'Dark';
  rideAlerts: boolean;
  alwaysShareTrip: boolean;
  showCourse: boolean;
};

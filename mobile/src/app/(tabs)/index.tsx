import { useCallback, useEffect, useState } from 'react';
import { api } from '@/api/client';
import type { MyTrips, PlaceDetails, SavedPlace } from '@/api/types';
import { RouteView } from '@/features/rides/RouteView';
import { placeFromSaved, WhereStep } from '@/features/rides/WhereStep';
import { PassengerTrip } from '@/features/trips/PassengerTrip';
import type { AppPlace } from '@/lib/geo';
import { currentPosition } from '@/lib/location';
import { useSession } from '@/session/SessionProvider';

const ACTIVE = ['Accepted', 'DriverArrived', 'InProgress', 'Completed'];

/**
 * Aba Caronas: "Para onde?" → rota com preços → Pedir agora (corrida ao vivo) ou caronas marcadas.
 * Saída padrão: GPS; sem GPS, a casa; sem casa, o campus. Reabrindo o app no meio de uma corrida, volta para ela.
 */
export default function Rides() {
  const { user } = useSession();
  const [gps, setGps] = useState<AppPlace | null>(null);
  const [origin, setOrigin] = useState<AppPlace | null>(null);
  const [destination, setDestination] = useState<AppPlace | null>(null);
  /** undefined = carregando; null = não definida. */
  const [home, setHome] = useState<SavedPlace | null | undefined>(undefined);
  const [tripId, setTripId] = useState<string | null>(null);

  useEffect(() => {
    let alive = true;
    (async () => {
      // Corrida de "pedir agora" em andamento (ou esperando o pagamento)?
      const trips = await api.get<MyTrips>('/api/me/trips').catch(() => null);
      const active = trips?.upcoming.find((i) => i.mode === 'Now' && i.kind === 'Passenger' && ACTIVE.includes(i.status) && i.tripId);
      if (alive && active?.tripId) setTripId(active.tripId);

      const savedHome = await api.get<SavedPlace>('/api/me/places/home').catch(() => null);
      if (!alive) return;
      setHome(savedHome);

      const pos = await currentPosition();
      if (!alive) return;
      if (pos) {
        const here = await api.get<PlaceDetails>(`/api/places/reverse?lat=${pos.latitude}&lng=${pos.longitude}`).catch(() => null);
        const place: AppPlace = { name: 'Localização atual', short: 'Você', address: here?.address ?? 'Pelo GPS do celular', neighborhood: here?.neighborhood, ...pos };
        setGps(place);
        setOrigin((o) => o ?? place);
        return;
      }
      // Sem GPS (permissão negada, navegador): sai de casa ou do campus.
      if (savedHome) setOrigin((o) => o ?? { ...placeFromSaved(savedHome), name: 'Casa', short: 'Casa' });
      else if (user?.campus)
        setOrigin((o) => o ?? { name: `${user.university} - ${user.campus!.name}`, short: user.university, address: user.campus!.address, latitude: user.campus!.latitude, longitude: user.campus!.longitude });
    })();
    return () => {
      alive = false;
    };
  }, [user?.campus, user?.university]);

  const tripDone = useCallback(() => {
    setTripId(null);
    setDestination(null);
  }, []);

  if (tripId) return <PassengerTrip tripId={tripId} destinationName={destination?.short} onDone={tripDone} />;

  if (destination && origin) {
    // key: cada rota começa do zero (preço e caronas novos)
    const key = `${origin.latitude},${origin.longitude}>${destination.latitude},${destination.longitude}`;
    return <RouteView key={key} origin={origin} destination={destination} onBack={() => setDestination(null)} onTrip={setTripId} />;
  }
  return <WhereStep origin={origin} gpsOrigin={gps} onOrigin={setOrigin} onDestination={setDestination} home={home} onHome={setHome} />;
}

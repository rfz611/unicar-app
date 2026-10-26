import * as Location from 'expo-location';
import type { LatLng } from './geo';

/**
 * Posição atual do celular. null se a pessoa negar a permissão ou o GPS não responder em 8 s
 * (o app então usa a casa ou o campus como saída).
 */
export async function currentPosition(): Promise<LatLng | null> {
  try {
    const perm = await Location.requestForegroundPermissionsAsync();
    if (perm.status !== 'granted') return null;
    const last = await Location.getLastKnownPositionAsync({ maxAge: 60_000 }).catch(() => null);
    if (last) return { latitude: last.coords.latitude, longitude: last.coords.longitude };
    const pos = await Promise.race([
      Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.Balanced }),
      new Promise<null>((resolve) => setTimeout(() => resolve(null), 8000)),
    ]);
    return pos ? { latitude: pos.coords.latitude, longitude: pos.coords.longitude } : null;
  } catch {
    return null;
  }
}

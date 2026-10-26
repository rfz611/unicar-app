export type LatLng = { latitude: number; longitude: number };

/** Lugar escolhido no app (origem ou destino). */
export type AppPlace = LatLng & {
  /** Nome como aparece na lista ("UFPR - Centro Politécnico", "Localização atual"). */
  name: string;
  /** Nome curto para a barra do mapa ("UFPR Politécnico", "Você", "Batel"). */
  short: string;
  address: string;
  neighborhood?: string | null;
};

/** Distância em linha reta, só para mostrar ("2,3 km" na lista). O preço e a rota vêm da API. */
export function km(a: LatLng, b: LatLng) {
  const R = 6371;
  const rad = (d: number) => (d * Math.PI) / 180;
  const dLat = rad(b.latitude - a.latitude);
  const dLng = rad(b.longitude - a.longitude);
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(rad(a.latitude)) * Math.cos(rad(b.latitude)) * Math.sin(dLng / 2) ** 2;
  return 2 * R * Math.asin(Math.sqrt(h));
}

/** "7,2 km" */
export const fmtKm = (value: number) => value.toFixed(1).replace('.', ',') + ' km';

/** Polyline codificada do Google (Routes API) → pontos. */
export function decodePolyline(encoded: string): LatLng[] {
  const points: LatLng[] = [];
  let index = 0;
  let lat = 0;
  let lng = 0;
  while (index < encoded.length) {
    for (const axis of [0, 1]) {
      let shift = 0;
      let result = 0;
      let byte: number;
      do {
        byte = encoded.charCodeAt(index++) - 63;
        result |= (byte & 0x1f) << shift;
        shift += 5;
      } while (byte >= 0x20);
      const delta = result & 1 ? ~(result >> 1) : result >> 1;
      if (axis === 0) lat += delta;
      else lng += delta;
    }
    points.push({ latitude: lat / 1e5, longitude: lng / 1e5 });
  }
  return points;
}

/** sessionToken da busca do Google: um por busca, até escolher o lugar (cobra a busca como uma só). */
export function uuid() {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16);
  });
}

/** Nome curto: bairro, senão o começo do endereço. */
export function shortOf(name: string, address?: string | null) {
  const n = name.replace(/^(UFPR|PUCPR|UTFPR) - /, '$1 ').replace(' - Câmpus Curitiba, Sede Centro', ' Centro');
  if (n.length <= 26) return n;
  return address?.split(',')[0] ?? n.slice(0, 24) + '…';
}

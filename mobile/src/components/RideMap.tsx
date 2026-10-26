import { useEffect, useRef } from 'react';
import { View } from 'react-native';
import MapView, { Marker, Polyline } from 'react-native-maps';
import type { LatLng } from '@/lib/geo';
import { useTheme } from '@/theme/ThemeProvider';

export type RideMapProps = {
  origin: LatLng | null;
  destination: LatLng | null;
  /** Rota pelas ruas (polyline da API). Sem ela, uma linha reta entre os pontos. */
  route?: LatLng[] | null;
  /** Carro do motorista (etapa 4). */
  car?: LatLng | null;
  /** Espaço que os painéis ocupam por cima do mapa, para a rota não ficar escondida. */
  padding: { top: number; bottom: number };
};

/** Estilo escuro do Google (Android). No iOS o Apple Maps usa userInterfaceStyle. */
const DARK = [
  { elementType: 'geometry', stylers: [{ color: '#18181B' }] },
  { elementType: 'labels.text.fill', stylers: [{ color: '#7D7D85' }] },
  { elementType: 'labels.text.stroke', stylers: [{ color: '#18181B' }] },
  { featureType: 'road', elementType: 'geometry', stylers: [{ color: '#2C2C32' }] },
  { featureType: 'road', elementType: 'geometry.stroke', stylers: [{ color: '#202025' }] },
  { featureType: 'poi.park', elementType: 'geometry', stylers: [{ color: '#1A231D' }] },
  { featureType: 'water', elementType: 'geometry', stylers: [{ color: '#1D2B40' }] },
  { featureType: 'poi', elementType: 'labels.icon', stylers: [{ visibility: 'off' }] },
  { featureType: 'transit', stylers: [{ visibility: 'off' }] },
];

/** Mapa real (Apple Maps no iOS, Google no Android), com saída, destino, rota e carro. */
export function RideMap({ origin, destination, route, car, padding }: RideMapProps) {
  const { t } = useTheme();
  const ref = useRef<MapView>(null);
  const line = route && route.length > 1 ? route : origin && destination ? [origin, destination] : [];

  useEffect(() => {
    const pts = [origin, destination, car].filter(Boolean) as LatLng[];
    if (pts.length === 0) return;
    const timer = setTimeout(() => {
      ref.current?.fitToCoordinates(line.length ? [...line, ...pts] : pts, {
        edgePadding: { top: padding.top, bottom: padding.bottom, left: 56, right: 56 },
        animated: true,
      });
    }, 250);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [origin?.latitude, origin?.longitude, destination?.latitude, destination?.longitude, line.length, padding.bottom]);

  return (
    <MapView
      ref={ref}
      style={{ position: 'absolute', left: 0, right: 0, top: 0, bottom: 0 }}
      userInterfaceStyle={t.name}
      customMapStyle={t.name === 'dark' ? DARK : []}
      showsPointsOfInterests={false}
      toolbarEnabled={false}
      initialRegion={{ latitude: -25.4372, longitude: -49.2700, latitudeDelta: 0.12, longitudeDelta: 0.12 }}
    >
      {line.length > 1 ? (
        <>
          <Polyline coordinates={line} strokeColor={t.accent + '33'} strokeWidth={12} />
          <Polyline coordinates={line} strokeColor={t.accent} strokeWidth={4} />
        </>
      ) : null}
      {origin ? (
        <Marker coordinate={origin} anchor={{ x: 0.5, y: 0.5 }} tracksViewChanges={false}>
          <View style={{ width: 16, height: 16, borderRadius: 8, borderWidth: 3, borderColor: t.fg, backgroundColor: t.bg }} />
        </Marker>
      ) : null}
      {destination ? (
        <Marker coordinate={destination} anchor={{ x: 0.5, y: 0.5 }} tracksViewChanges={false}>
          <View style={{ width: 44, height: 44, borderRadius: 22, backgroundColor: t.accent + '33', alignItems: 'center', justifyContent: 'center' }}>
            <View style={{ width: 18, height: 18, borderRadius: 9, backgroundColor: t.accent, alignItems: 'center', justifyContent: 'center' }}>
              <View style={{ width: 6, height: 6, borderRadius: 3, backgroundColor: '#FFFFFF' }} />
            </View>
          </View>
        </Marker>
      ) : null}
      {car ? (
        <Marker coordinate={car} anchor={{ x: 0.5, y: 0.5 }}>
          <View style={{ width: 24, height: 24, borderRadius: 8, backgroundColor: t.fg }} />
        </Marker>
      ) : null}
    </MapView>
  );
}

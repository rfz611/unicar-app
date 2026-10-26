import { useState } from 'react';
import { View, type LayoutChangeEvent } from 'react-native';
import Svg, { Circle, Path, Rect } from 'react-native-svg';
import type { LatLng } from '@/lib/geo';
import { useTheme } from '@/theme/ThemeProvider';
import type { RideMapProps } from './RideMap';
import { Text } from './Text';

/**
 * No navegador não há mapa nativo: um espaço reservado no estilo do mapa do protótipo,
 * com a rota desenhada em escala (saída, destino, carro). No celular aparece o mapa real.
 */
export function RideMap({ origin, destination, route, car, padding }: RideMapProps) {
  const { t } = useTheme();
  const [size, setSize] = useState({ w: 0, h: 0 });
  const line = route && route.length > 1 ? route : origin && destination ? [origin, destination] : [];
  const pts = [...line, origin, destination, car].filter(Boolean) as LatLng[];

  // Projeção simples dentro da área livre entre os painéis.
  const box = { left: 56, right: size.w - 56, top: padding.top, bottom: Math.max(padding.top + 80, size.h - padding.bottom) };
  const lats = pts.map((p) => p.latitude);
  const lngs = pts.map((p) => p.longitude);
  const minLat = Math.min(...lats), maxLat = Math.max(...lats), minLng = Math.min(...lngs), maxLng = Math.max(...lngs);
  const spanLat = Math.max(maxLat - minLat, 0.005);
  const spanLng = Math.max(maxLng - minLng, 0.005);
  const scale = Math.min((box.right - box.left) / spanLng, (box.bottom - box.top) / spanLat);
  const cx = (box.left + box.right) / 2, cy = (box.top + box.bottom) / 2;
  const midLat = (minLat + maxLat) / 2, midLng = (minLng + maxLng) / 2;
  const xy = (p: LatLng) => ({ x: cx + (p.longitude - midLng) * scale, y: cy - (p.latitude - midLat) * scale });
  const d = line.map((p, i) => `${i ? 'L' : 'M'}${xy(p).x.toFixed(1)} ${xy(p).y.toFixed(1)}`).join(' ');

  return (
    <View
      style={{ position: 'absolute', left: 0, right: 0, top: 0, bottom: 0, backgroundColor: t.mapBg }}
      onLayout={(e: LayoutChangeEvent) => setSize({ w: e.nativeEvent.layout.width, h: e.nativeEvent.layout.height })}
    >
      {size.w > 0 && pts.length > 0 ? (
        <Svg width={size.w} height={size.h}>
          {/* grade de ruas, como o fundo do protótipo */}
          {Array.from({ length: Math.ceil(size.h / 20) }, (_, i) => (
            <Rect key={'h' + i} x={0} y={i * 20} width={size.w} height={1.6} fill={t.mapRoad2} />
          ))}
          {Array.from({ length: Math.ceil(size.w / 24) }, (_, i) => (
            <Rect key={'v' + i} x={i * 24} y={0} width={1.6} height={size.h} fill={t.mapRoad2} />
          ))}
          {d ? <Path d={d} stroke={t.accent} strokeOpacity={0.2} strokeWidth={12} fill="none" strokeLinecap="round" strokeLinejoin="round" /> : null}
          {d ? <Path d={d} stroke={t.accent} strokeWidth={4} fill="none" strokeLinecap="round" strokeLinejoin="round" /> : null}
          {destination ? (
            <>
              <Circle cx={xy(destination).x} cy={xy(destination).y} r={22} fill={t.accent} opacity={0.2} />
              <Circle cx={xy(destination).x} cy={xy(destination).y} r={9} fill={t.accent} />
              <Circle cx={xy(destination).x} cy={xy(destination).y} r={3} fill="#FFFFFF" />
            </>
          ) : null}
          {origin ? <Circle cx={xy(origin).x} cy={xy(origin).y} r={8} fill={t.bg} stroke={t.fg} strokeWidth={3} /> : null}
          {car ? <Rect x={xy(car).x - 12} y={xy(car).y - 12} width={24} height={24} rx={8} fill={t.fg} /> : null}
        </Svg>
      ) : null}
      <View pointerEvents="none" style={{ position: 'absolute', left: 0, right: 0, top: padding.top - 8, alignItems: 'center' }}>
        <Text size={12} color={t.mapLabel}>
          Mapa real no celular
        </Text>
      </View>
    </View>
  );
}

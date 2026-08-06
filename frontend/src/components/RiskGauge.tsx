import { Box, Typography, useTheme } from '@mui/material';

interface RiskGaugeProps {
  /** 0-100 arası, ne kadar yüksekse o kadar riskli. */
  value: number;
  /** Göstergenin altında/ortasında gösterilecek kısa etiket, ör. "3 / 7 açık sipariş". */
  caption?: string;
  size?: number;
}

function polarToCartesian(cx: number, cy: number, r: number, angleDeg: number) {
  const angleRad = (angleDeg * Math.PI) / 180;
  return { x: cx + r * Math.cos(angleRad), y: cy + r * Math.sin(angleRad) };
}

// 180°(sol) -> 0°(sağ) yarım daire yayı için SVG path 'd' değeri üretir.
function describeArc(cx: number, cy: number, r: number, startAngle: number, endAngle: number) {
  const start = polarToCartesian(cx, cy, r, startAngle);
  const end = polarToCartesian(cx, cy, r, endAngle);
  const largeArcFlag = endAngle - startAngle <= 180 ? 0 : 1;
  return `M ${start.x} ${start.y} A ${r} ${r} 0 ${largeArcFlag} 1 ${end.x} ${end.y}`;
}

/**
 * Basit, statik yarım-daire risk göstergesi. Kütüphane kullanmaz, tek bir
 * değeri (ör. açık siparişlerin gecikme oranı) düşük/orta/yüksek renk
 * bölgeleriyle görselleştirir — SAD'in yasakladığı "interaktif/gelişmiş
 * grafik" kapsamına girmez, statik bir SVG göstergedir.
 */
export function RiskGauge({ value, caption, size = 88 }: RiskGaugeProps) {
  const theme = useTheme();
  const clamped = Math.max(0, Math.min(100, value));

  const trackColor = theme.palette.mode === 'dark' ? 'rgba(255,255,255,0.08)' : theme.palette.borderDefault;
  const fillColor = clamped <= 25
    ? theme.palette.success.main
    : clamped <= 60
      ? theme.palette.warning.main
      : theme.palette.error.main;

  const width = size;
  const height = size / 2 + 10;
  const cx = width / 2;
  const cy = size / 2 + 2;
  const r = size / 2 - 8;
  const sweepAngle = 180 * (clamped / 100);

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
      <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`}>
        <path
          d={describeArc(cx, cy, r, 180, 360)}
          fill="none"
          stroke={trackColor}
          strokeWidth={8}
          strokeLinecap="round"
        />
        {clamped > 0 && (
          <path
            d={describeArc(cx, cy, r, 180, 180 + sweepAngle)}
            fill="none"
            stroke={fillColor}
            strokeWidth={8}
            strokeLinecap="round"
          />
        )}
      </svg>
      {caption && (
        <Typography sx={{ fontSize: '11px', color: 'textSecondary', mt: -0.5 }}>
          {caption}
        </Typography>
      )}
    </Box>
  );
}

import { ResponsiveContainer, LineChart, Line } from 'recharts'
import type { SparklinePoint } from '../api/sparklines'

type Props = {
  points: SparklinePoint[] | undefined
  width?: number
  height?: number
}

export function Sparkline({ points, width = 80, height = 28 }: Props) {
  if (!points || points.length < 2) {
    return <div style={{ width, height }} className="text-muted small d-flex align-items-center justify-content-center">—</div>
  }

  const first = points[0].close
  const last = points[points.length - 1].close
  const color = last >= first ? '#198754' : '#dc3545'

  return (
    <div style={{ width, height }}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={points} margin={{ top: 1, right: 1, bottom: 1, left: 1 }}>
          <Line
            type="monotone"
            dataKey="close"
            stroke={color}
            strokeWidth={1.5}
            dot={false}
            isAnimationActive={false}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}

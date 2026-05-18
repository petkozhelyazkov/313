import { useState } from 'react'
import { Card, Row, Col, Spinner, OverlayTrigger, Tooltip } from 'react-bootstrap'
import { useTranslation } from 'react-i18next'
import { useAnalyticsAdvanced, type AnalyticsRange } from '../../api/analytics'
import { plClassName } from '../../lib/format'
import { Button, ButtonGroup } from '../Button'

const RANGES: AnalyticsRange[] = ['1M', '3M', '6M', '1Y', 'MAX']

function fmtPct(v: number | null): string {
  if (v === null || v === undefined || Number.isNaN(v)) return '—'
  return (v >= 0 ? '+' : '') + v.toFixed(2) + '%'
}

function fmtRatio(v: number | null): string {
  if (v === null || v === undefined || Number.isNaN(v)) return '—'
  return v.toFixed(2)
}

function fmtDate(iso: string | null, locale: string): string {
  if (!iso) return '—'
  return new Date(iso + 'T00:00:00Z').toLocaleDateString(locale, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

export function AdvancedMetricsCard() {
  const { t, i18n } = useTranslation()
  const [range, setRange] = useState<AnalyticsRange>('1Y')
  const { data, isLoading, isError } = useAnalyticsAdvanced(range)

  const insufficient = data && data.dataPoints < 5

  return (
    <Card>
      <Card.Header className="d-flex justify-content-between align-items-center flex-wrap gap-2">
        <strong>{t('analytics.advanced.title')}</strong>
        <ButtonGroup size="sm">
          {RANGES.map((r) => (
            <Button
              key={r}
              variant={r === range ? 'primary' : 'secondary'}
              outline={r !== range}
              size="sm"
              onClick={() => setRange(r)}
            >
              {r}
            </Button>
          ))}
        </ButtonGroup>
      </Card.Header>
      <Card.Body>
        {isLoading ? (
          <div className="text-center py-4"><Spinner animation="border" size="sm" /></div>
        ) : isError || !data ? (
          <div className="text-muted text-center py-4 small">{t('analytics.advanced.couldNotLoad')}</div>
        ) : insufficient ? (
          <div className="text-muted text-center py-4 small">
            {t('analytics.advanced.insufficient', { count: data.dataPoints })}
          </div>
        ) : (
          <Row className="g-3">
            <Col xs={6} md={3}>
              <OverlayTrigger placement="top" overlay={<Tooltip>{t('analytics.advanced.twrHint')}</Tooltip>}>
                <div>
                  <div className="text-muted small text-uppercase">{t('analytics.advanced.twr')}</div>
                  <strong className={`fs-4 ${plClassName(data.timeWeightedReturn)}`}>{fmtPct(data.timeWeightedReturn)}</strong>
                </div>
              </OverlayTrigger>
            </Col>
            <Col xs={6} md={3}>
              <OverlayTrigger placement="top" overlay={<Tooltip>{t('analytics.advanced.mwrHint')}</Tooltip>}>
                <div>
                  <div className="text-muted small text-uppercase">{t('analytics.advanced.mwr')}</div>
                  <strong className={`fs-4 ${plClassName(data.moneyWeightedReturn)}`}>{fmtPct(data.moneyWeightedReturn)}</strong>
                </div>
              </OverlayTrigger>
            </Col>
            <Col xs={6} md={3}>
              <OverlayTrigger placement="top" overlay={<Tooltip>{t('analytics.advanced.sortinoHint')}</Tooltip>}>
                <div>
                  <div className="text-muted small text-uppercase">{t('analytics.advanced.sortino')}</div>
                  <strong className={`fs-4 ${plClassName(data.sortinoRatio)}`}>{fmtRatio(data.sortinoRatio)}</strong>
                </div>
              </OverlayTrigger>
            </Col>
            <Col xs={6} md={3}>
              <OverlayTrigger placement="top" overlay={<Tooltip>{t('analytics.advanced.winRateHint')}</Tooltip>}>
                <div>
                  <div className="text-muted small text-uppercase">{t('analytics.advanced.winRate')}</div>
                  <strong className="fs-4">{data.winRate === null ? '—' : data.winRate.toFixed(1) + '%'}</strong>
                  <div className="small text-muted mt-1">
                    {t('analytics.advanced.daysSplit', { up: data.positiveDays, down: data.negativeDays })}
                  </div>
                </div>
              </OverlayTrigger>
            </Col>
            <Col xs={6} md={3}>
              <div>
                <div className="text-muted small text-uppercase">{t('analytics.advanced.bestDay')}</div>
                <strong className="fs-5 text-success">{fmtPct(data.bestDayReturn)}</strong>
                <div className="small text-muted">{fmtDate(data.bestDayDate, i18n.language)}</div>
              </div>
            </Col>
            <Col xs={6} md={3}>
              <div>
                <div className="text-muted small text-uppercase">{t('analytics.advanced.worstDay')}</div>
                <strong className="fs-5 text-danger">{fmtPct(data.worstDayReturn)}</strong>
                <div className="small text-muted">{fmtDate(data.worstDayDate, i18n.language)}</div>
              </div>
            </Col>
            <Col xs={6} md={3}>
              <div>
                <div className="text-muted small text-uppercase">{t('analytics.advanced.avgDaily')}</div>
                <strong className={`fs-5 ${plClassName(data.averageDailyReturn)}`}>{fmtPct(data.averageDailyReturn)}</strong>
              </div>
            </Col>
            <Col xs={6} md={3}>
              <div>
                <div className="text-muted small text-uppercase">{t('analytics.advanced.samples')}</div>
                <strong className="fs-5">{data.dataPoints}</strong>
                <div className="small text-muted">{t('analytics.advanced.snapshotDays')}</div>
              </div>
            </Col>
          </Row>
        )}
      </Card.Body>
    </Card>
  )
}

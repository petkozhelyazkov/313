import { useState, useEffect } from 'react'
import { Container, Card, Table, Spinner, Form, Row, Col } from 'react-bootstrap'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useTaxYears, useTaxReport } from '../api/portfolio'
import { useDocumentTitle } from '../hooks/useDocumentTitle'
import { Button } from '../components/Button'
import { downloadFile } from '../lib/download'
import { formatCurrency, plClassName } from '../lib/format'
import { toast } from 'react-hot-toast'

export function TaxReportPage() {
  const { t, i18n } = useTranslation()
  useDocumentTitle(t('tax.title'))
  const { data: years } = useTaxYears()
  const [year, setYear] = useState<number | null>(null)

  useEffect(() => {
    if (year === null && years && years.length > 0) {
      setYear(years[0])
    }
  }, [years, year])

  const { data, isLoading } = useTaxReport(year)

  const fmt = (v: number) => formatCurrency(v)
  const fmtDate = (s: string) =>
    new Date(s).toLocaleDateString(i18n.language, { year: 'numeric', month: 'short', day: 'numeric' })

  const printPdf = () => window.print()
  const downloadCsv = async () => {
    if (year === null) return
    try {
      await downloadFile(`/api/portfolio/tax-report/${year}.csv`, `tax-report-${year}.csv`)
    } catch {
      toast.error(t('tax.downloadFailed'))
    }
  }

  return (
    <Container className="py-4 tax-report-page">
      <div className="d-flex justify-content-between align-items-center flex-wrap gap-3 mb-3 no-print">
        <h1 className="h3 mb-0">{t('tax.title')}</h1>
        <div className="d-flex gap-2 align-items-center flex-wrap">
          <Form.Select
            size="sm"
            value={year ?? ''}
            onChange={(e) => setYear(Number(e.target.value))}
            style={{ width: 120 }}
            disabled={!years || years.length === 0}
          >
            {(years ?? []).map((y) => (
              <option key={y} value={y}>{y}</option>
            ))}
          </Form.Select>
          <Button variant="primary" outline onClick={downloadCsv} disabled={!data}>
            {t('tax.downloadCsv')}
          </Button>
          <Button variant="primary" onClick={printPdf} disabled={!data}>
            {t('tax.printPdf')}
          </Button>
          <Link to="/profile" className="btn btn-link btn-sm">{t('common.back')}</Link>
        </div>
      </div>

      {isLoading || !data ? (
        <Card body className="text-center py-5"><Spinner /></Card>
      ) : (
        <>
          <div className="print-header d-none">
            <h1>Trading313 · {t('tax.title')} {data.year}</h1>
          </div>

          <Row className="g-3 mb-4">
            <Col md={6} lg={3}>
              <Card>
                <Card.Body>
                  <div className="text-muted small text-uppercase">{t('tax.shortTermNet')}</div>
                  <div className={`h4 ${plClassName(data.shortTermNet)}`}>{fmt(data.shortTermNet)}</div>
                  <small className="text-muted">
                    {t('tax.gains')}: <span className="text-success">{fmt(data.shortTermGains)}</span> ·{' '}
                    {t('tax.losses')}: <span className="text-danger">{fmt(-data.shortTermLosses)}</span>
                  </small>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={3}>
              <Card>
                <Card.Body>
                  <div className="text-muted small text-uppercase">{t('tax.longTermNet')}</div>
                  <div className={`h4 ${plClassName(data.longTermNet)}`}>{fmt(data.longTermNet)}</div>
                  <small className="text-muted">
                    {t('tax.gains')}: <span className="text-success">{fmt(data.longTermGains)}</span> ·{' '}
                    {t('tax.losses')}: <span className="text-danger">{fmt(-data.longTermLosses)}</span>
                  </small>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={3}>
              <Card>
                <Card.Body>
                  <div className="text-muted small text-uppercase">{t('tax.dividends')}</div>
                  <div className="h4">{fmt(data.dividendsReceived)}</div>
                  <small className="text-muted">{t('tax.feesPaid')}: {fmt(data.feesPaid)}</small>
                </Card.Body>
              </Card>
            </Col>
            <Col md={6} lg={3}>
              <Card>
                <Card.Body>
                  <div className="text-muted small text-uppercase">{t('tax.netTotal')}</div>
                  <div className={`h4 ${plClassName(data.netTotal)}`}>{fmt(data.netTotal)}</div>
                  <small className="text-muted">{t('tax.netHint')}</small>
                </Card.Body>
              </Card>
            </Col>
          </Row>

          <Card className="mb-3">
            <Card.Header><strong>{t('tax.realizedSales')}</strong> <small className="text-muted">· {data.sellRows.length}</small></Card.Header>
            {data.sellRows.length === 0 ? (
              <Card.Body className="text-muted">{t('tax.noSells')}</Card.Body>
            ) : (
              <Table responsive hover className="align-middle mb-0" size="sm">
                <thead>
                  <tr>
                    <th>{t('tax.symbol')}</th>
                    <th>{t('tax.acquired')}</th>
                    <th>{t('tax.sold')}</th>
                    <th className="text-end">{t('tax.quantity')}</th>
                    <th className="text-end">{t('tax.costBasis')}</th>
                    <th className="text-end">{t('tax.proceeds')}</th>
                    <th className="text-end">{t('tax.gain')}</th>
                    <th>{t('tax.term')}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.sellRows.map((r, i) => (
                    <tr key={i}>
                      <td className="fw-semibold">{r.symbol}</td>
                      <td className="small">{fmtDate(r.acquiredAt)}</td>
                      <td className="small">{fmtDate(r.soldAt)}</td>
                      <td className="text-end">{r.quantity}</td>
                      <td className="text-end">{fmt(r.costBasis)}</td>
                      <td className="text-end">{fmt(r.proceeds)}</td>
                      <td className={`text-end ${plClassName(r.gain)}`}>{fmt(r.gain)}</td>
                      <td>
                        <span className={`badge ${r.isLongTerm ? 'bg-info' : 'bg-secondary'}`}>
                          {r.isLongTerm ? t('tax.long') : t('tax.short')}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            )}
          </Card>

          {data.dividendRows.length > 0 && (
            <Card>
              <Card.Header><strong>{t('tax.dividendsTable')}</strong> <small className="text-muted">· {data.dividendRows.length}</small></Card.Header>
              <Table responsive className="align-middle mb-0" size="sm">
                <thead>
                  <tr>
                    <th>{t('tax.symbol')}</th>
                    <th>{t('tax.exDate')}</th>
                    <th className="text-end">{t('tax.perShare')}</th>
                    <th className="text-end">{t('tax.quantity')}</th>
                    <th className="text-end">{t('tax.received')}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.dividendRows.map((d, i) => (
                    <tr key={i}>
                      <td className="fw-semibold">{d.symbol}</td>
                      <td className="small">{d.exDate}</td>
                      <td className="text-end">{fmt(d.amountPerShare)}</td>
                      <td className="text-end">{d.quantityAtExDate}</td>
                      <td className="text-end text-success">{fmt(d.totalReceived)}</td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </Card>
          )}
        </>
      )}
    </Container>
  )
}

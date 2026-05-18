#!/usr/bin/env python3
"""
Pre-seeds 5 years of SPY (S&P 500 ETF) historical prices into HistoricalPrices.
The Analytics "vs S&P 500" overlay needs this; relying on the live history
service often fails because the per-minute rate limit is already saturated by
QuoteRefreshService.
"""

import pymysql
import urllib.request
import json
import sys
from datetime import datetime
import os

DB = dict(host='127.0.0.1', port=3310, user='trading212', password='trading212pass',
          database='trading212_dev', autocommit=False, charset='utf8mb4')

API_KEY = os.environ.get('TWELVE_DATA_KEY') or 'c109d9d99e4d410d9544cf61e72f3067'
SYMBOL = 'SPY'


def fetch_history():
    url = (f'https://api.twelvedata.com/time_series?'
           f'symbol={SYMBOL}&interval=1day&outputsize=5000&apikey={API_KEY}')
    print(f'Fetching {SYMBOL} from Twelve Data…')
    with urllib.request.urlopen(url, timeout=30) as r:
        body = json.load(r)
    if body.get('status') == 'error':
        raise SystemExit(f"Twelve Data error: {body.get('code')} {body.get('message')}")
    return body.get('values', [])


def main():
    points = fetch_history()
    if not points:
        raise SystemExit('No data returned for SPY')
    print(f'  got {len(points)} bars ({points[-1]["datetime"]} → {points[0]["datetime"]})')

    conn = pymysql.connect(**DB)
    cur = conn.cursor()
    try:
        cur.execute("SELECT Date FROM HistoricalPrices WHERE Symbol=%s", (SYMBOL,))
        existing = {row[0] for row in cur.fetchall()}
        print(f'  {len(existing)} bars already in DB')

        rows = []
        for p in points:
            date = datetime.strptime(p['datetime'], '%Y-%m-%d').date()
            if date in existing:
                continue
            rows.append((SYMBOL, date,
                         float(p['open']), float(p['high']),
                         float(p['low']), float(p['close']),
                         int(float(p.get('volume', 0) or 0))))
        if not rows:
            print('  nothing new to insert')
            return
        cur.executemany(
            "INSERT INTO HistoricalPrices (Symbol, Date, Open, High, Low, Close, Volume) "
            "VALUES (%s,%s,%s,%s,%s,%s,%s)", rows)
        conn.commit()
        print(f'  inserted {len(rows)} new bars')

        # Make sure SPY is in the Stocks catalog too so the stock detail page works.
        cur.execute("SELECT 1 FROM Stocks WHERE Symbol=%s", (SYMBOL,))
        if not cur.fetchone():
            cur.execute(
                "INSERT INTO Stocks (Symbol, Name, Exchange, Currency, InstrumentType, IsActive, CreatedAt) "
                "VALUES (%s, %s, %s, %s, %s, %s, %s)",
                (SYMBOL, 'SPDR S&P 500 ETF Trust', 'NYSE Arca', 'USD', 'ETF', 1, datetime.utcnow()))
            conn.commit()
            print('  added SPY to Stocks catalog')
    except Exception:
        conn.rollback()
        raise
    finally:
        cur.close()
        conn.close()


if __name__ == '__main__':
    main()

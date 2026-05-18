#!/usr/bin/env python3
"""
Pre-seeds Sector / Industry / Employees / Website / Description / CEO for the
demo symbols, bypassing the rate-limited Twelve Data /profile path. Idempotent —
already-populated rows are left alone unless --force is passed.

Run after seed_demo1_big.py so the Sector Pie + Diversification widgets have
real data to group by.
"""

import pymysql
import sys

DB = dict(host='127.0.0.1', port=3310, user='trading212', password='trading212pass',
          database='trading212_dev', autocommit=False, charset='utf8mb4')

# Canonical sector data for the symbols seeded by seed_demo1_big.py + SPY.
SECTORS = {
    'AAPL':  ('Technology',              'Consumer Electronics',           166000, 'https://www.apple.com',     'Mr. Timothy D. Cook'),
    'MSFT':  ('Technology',              'Software—Infrastructure',        228000, 'https://www.microsoft.com', 'Mr. Satya Nadella'),
    'GOOGL': ('Communication Services',  'Internet Content & Information', 187103, 'https://abc.xyz',           'Mr. Sundar Pichai'),
    'AMZN':  ('Consumer Cyclical',       'Internet Retail',                1551000, 'https://www.amazon.com',    'Mr. Andrew R. Jassy'),
    'TSLA':  ('Consumer Cyclical',       'Auto Manufacturers',             140473, 'https://www.tesla.com',     'Mr. Elon R. Musk'),
    'NVDA':  ('Technology',              'Semiconductors',                  36000, 'https://www.nvidia.com',    'Mr. Jen-Hsun Huang'),
    'META':  ('Communication Services',  'Internet Content & Information',  74067, 'https://www.meta.com',      'Mr. Mark Elliot Zuckerberg'),
    'AMD':   ('Technology',              'Semiconductors',                  28000, 'https://www.amd.com',       'Dr. Lisa T. Su'),
    'SPY':   ('ETF',                     'Index Fund',                          0, 'https://www.ssga.com',      None),
    'NFLX':  ('Communication Services',  'Entertainment',                   13000, 'https://www.netflix.com',   'Mr. Theodore A. Sarandos'),
    'DIS':   ('Communication Services',  'Entertainment',                  225000, 'https://www.disney.com',    'Mr. Robert A. Iger'),
    'INTC':  ('Technology',              'Semiconductors',                 124800, 'https://www.intel.com',     'Mr. Lip-Bu Tan'),
    'CRM':   ('Technology',              'Software—Application',            75000, 'https://www.salesforce.com', 'Mr. Marc R. Benioff'),
}


def main():
    force = '--force' in sys.argv
    conn = pymysql.connect(**DB)
    cur = conn.cursor()
    try:
        updated = 0
        for sym, (sector, industry, employees, website, ceo) in SECTORS.items():
            cur.execute("SELECT Sector FROM Stocks WHERE Symbol=%s", (sym,))
            row = cur.fetchone()
            if not row:
                print(f'  {sym}: not in Stocks table — skipping')
                continue
            current = row[0]
            if current and not force:
                continue
            cur.execute(
                "UPDATE Stocks SET Sector=%s, Industry=%s, Employees=%s, Website=%s, Ceo=%s, "
                "LastMetadataRefreshAt=NOW() WHERE Symbol=%s",
                (sector, industry, employees, website, ceo, sym))
            print(f'  {sym}: sector → "{sector}"')
            updated += 1
        conn.commit()
        print(f'\nUpdated {updated} symbol(s).')
    except Exception:
        conn.rollback()
        raise
    finally:
        cur.close()
        conn.close()


if __name__ == '__main__':
    main()

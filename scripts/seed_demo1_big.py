#!/usr/bin/env python3
"""
One-shot seeder that fills demo1@trading212.local with a LOT of realistic data:

- 250+ transactions over 4 years using cached HistoricalPrices
- 15+ pending orders across all 4 sides + 4 statuses
- 12 price alerts (active, triggered, cancelled)
- 18 watchlist items across 3 named lists
- 5 recurring DCA rules
- 6 portfolio goals (mix of in-progress + completed)
- 11 cash transactions (deposits + withdrawals)
- Wipes existing demo1 transactions/positions/etc and snapshots so they regen

Idempotent in the sense that it always brings demo1 to the same final state.
"""

import pymysql
import random
import datetime as dt
from decimal import Decimal, getcontext

getcontext().prec = 28

DB = dict(host='127.0.0.1', port=3310, user='trading212', password='trading212pass',
          database='trading212_dev', autocommit=False, charset='utf8mb4')

DEMO_EMAIL = 'demo1@trading212.local'

# Probability weights for which symbols get traded
SYMBOL_WEIGHTS = {
    'AAPL': 18, 'MSFT': 15, 'GOOGL': 12, 'NVDA': 14, 'TSLA': 12,
    'AMZN': 10, 'META': 10, 'AMD': 9,
}

# Buy bias (1 - sell prob). Skews positive — more buys than sells.
SELL_PROB = 0.30


def connect():
    return pymysql.connect(**DB)


def load_history(cur):
    """Returns {symbol: [(date, close)...sorted ascending]}"""
    cur.execute("SELECT Symbol, Date, Close FROM HistoricalPrices ORDER BY Symbol, Date")
    out = {}
    for sym, date, close in cur.fetchall():
        out.setdefault(sym, []).append((date, float(close)))
    return out


def price_on(history_for_sym, target_date):
    """Bisect to the latest close <= target_date. Returns None if no data."""
    if not history_for_sym:
        return None
    # Linear scan from end is fine for our sizes; data is sorted.
    for d, c in reversed(history_for_sym):
        if d <= target_date:
            return c
    return None


def latest_price(history_for_sym):
    return history_for_sym[-1][1] if history_for_sym else None


def main():
    conn = connect()
    cur = conn.cursor()
    try:
        # Get user id
        cur.execute("SELECT Id FROM AspNetUsers WHERE Email=%s", (DEMO_EMAIL,))
        row = cur.fetchone()
        if not row:
            raise SystemExit(f"User {DEMO_EMAIL} not found. Run `dotnet run -- seed` first.")
        user_id = row[0]
        print(f"Seeding for {DEMO_EMAIL} (id={user_id})")

        # Wipe existing data for demo1
        for table in [
            'Transactions', 'Positions', 'PendingOrders', 'PriceAlerts',
            'CashTransactions', 'Goals', 'WatchlistItems', 'RecurringOrders',
            'DailyPortfolioSnapshots',
        ]:
            cur.execute(f"DELETE FROM {table} WHERE UserId=%s", (user_id,))
            print(f"  wiped {table} ({cur.rowcount} rows)")

        # Load historical prices
        history = load_history(cur)
        symbols = [s for s in SYMBOL_WEIGHTS if s in history and history[s]]
        weighted = []
        for s in symbols:
            weighted.extend([s] * SYMBOL_WEIGHTS[s])

        rng = random.Random(7)
        now = dt.datetime.utcnow().replace(microsecond=0)
        earliest = now - dt.timedelta(days=4 * 365)

        # ---- Cash transactions (deposits + withdrawals over 2 years) ----
        cash_events = []
        balance = Decimal('10000.0000')

        # 8 deposits, varying sizes, spread over 24 months
        for i, amt in enumerate([2500, 1500, 3000, 1000, 5000, 2000, 1500, 2000]):
            when = earliest + dt.timedelta(days=180 + i * 80 + rng.randint(-10, 10))
            balance += Decimal(amt)
            cash_events.append((1, Decimal(amt), balance, when, 'Monthly contribution'))

        # 3 withdrawals
        for amt in [1500, 800, 1200]:
            when = earliest + dt.timedelta(days=300 + rng.randint(0, 900))
            balance -= Decimal(amt)
            cash_events.append((2, Decimal(amt), balance, when, 'Withdrawal'))

        cash_events.sort(key=lambda e: e[3])
        # Recompute running balance after sort
        running = Decimal('10000.0000')
        for i, e in enumerate(cash_events):
            tp, amt, _, when, notes = e
            running = running + amt if tp == 1 else running - amt
            cash_events[i] = (tp, amt, running, when, notes)

        for tp, amt, after, when, notes in cash_events:
            cur.execute(
                "INSERT INTO CashTransactions (UserId, Type, Amount, BalanceAfter, ExecutedAt, Notes) "
                "VALUES (%s,%s,%s,%s,%s,%s)",
                (user_id, tp, amt, after, when, notes))
        print(f"  +{len(cash_events)} cash transactions, balance now {running}")

        # ---- Stock transactions ----
        positions = {}  # sym -> {qty, avg_cost, first_purchased, total_invested, realized_lifetime}
        cash = running

        N_TX = 260
        # Build a calendar of buy-only opportunities at first to accumulate positions
        # then mix in sells
        tx_rows = []  # tuples for INSERT
        executed = 0
        attempts = 0
        target_window_ticks = (now - earliest).total_seconds()

        while executed < N_TX and attempts < N_TX * 4:
            attempts += 1
            # Bias execution slightly toward more recent times (so positions trend upward)
            t_frac = rng.random() ** 0.85
            when = earliest + dt.timedelta(seconds=t_frac * target_window_ticks)
            when = when.replace(hour=rng.randint(13, 20), minute=rng.randint(0, 59), second=rng.randint(0, 59), microsecond=0)

            sym = rng.choice(weighted)
            hist = history[sym]
            price = price_on(hist, when.date())
            if price is None:
                continue
            # Add small noise to price (±0.5%)
            px = Decimal(str(round(price * (1 + (rng.random() - 0.5) * 0.01), 4)))

            held = positions.get(sym)
            should_sell = held and held['qty'] > 0 and rng.random() < SELL_PROB
            if should_sell:
                max_sell = held['qty']
                # Sometimes full close-out, sometimes partial
                qty = max_sell if rng.random() < 0.15 else round(max_sell * Decimal(str(0.2 + rng.random() * 0.5)), 4)
                if qty <= 0:
                    continue
                qty = min(qty, max_sell)
                total = (qty * px).quantize(Decimal('0.0001'))
                realized = ((px - held['avg_cost']) * qty).quantize(Decimal('0.0001'))
                tx_rows.append((user_id, sym, 2, qty, px, Decimal('0.0000'), total, when, realized, None))
                cash += total
                held['qty'] -= qty
                held['realized_lifetime'] += realized
                held['last_tx'] = when
                if held['qty'] <= Decimal('0.0000001'):
                    held['qty'] = Decimal('0')
                    held['closed'] = True
                executed += 1
            else:
                # Buy. Pick a position size — $300-$2500 typically
                budget = Decimal(str(300 + rng.randint(0, 2200)))
                budget = min(budget, cash * Decimal('0.85'))  # leave some cash
                if budget < px:
                    # try a smaller-than-budget buy
                    if cash < px:
                        continue
                    qty = Decimal('1.0000')
                else:
                    qty = (budget / px).quantize(Decimal('0.0001'))
                if qty < Decimal('0.1'):
                    continue
                total = (qty * px).quantize(Decimal('0.0001'))
                if total > cash:
                    continue
                tx_rows.append((user_id, sym, 1, qty, px, Decimal('0.0000'), total, when, None, None))
                if held is None:
                    positions[sym] = {
                        'qty': qty, 'avg_cost': px, 'first_purchased': when,
                        'total_invested': total, 'realized_lifetime': Decimal('0'),
                        'last_tx': when, 'closed': False,
                    }
                else:
                    new_qty = held['qty'] + qty
                    new_avg = ((held['qty'] * held['avg_cost'] + total) / new_qty).quantize(Decimal('0.0001'))
                    held['qty'] = new_qty
                    held['avg_cost'] = new_avg
                    held['total_invested'] += total
                    held['last_tx'] = when
                    held['closed'] = False
                cash -= total
                executed += 1

        # Insert transactions in chronological order
        tx_rows.sort(key=lambda r: r[7])
        cur.executemany(
            "INSERT INTO Transactions (UserId, Symbol, Type, Quantity, PricePerShare, Fees, TotalAmount, ExecutedAt, RealizedPl, Notes) "
            "VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)", tx_rows)
        print(f"  +{len(tx_rows)} transactions")

        # Insert positions
        pos_rows = []
        for sym, p in positions.items():
            pos_rows.append((
                user_id, sym, p['qty'], p['avg_cost'], p['total_invested'],
                p['realized_lifetime'], p['first_purchased'], p['last_tx'],
                1 if p.get('closed') else 0,
            ))
        cur.executemany(
            "INSERT INTO Positions (UserId, Symbol, Quantity, AverageCost, TotalInvested, RealizedPlLifetime, FirstPurchasedAt, LastTransactionAt, IsClosed) "
            "VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s)", pos_rows)
        print(f"  +{len(pos_rows)} positions ({sum(1 for p in positions.values() if not p.get('closed'))} open, {sum(1 for p in positions.values() if p.get('closed'))} closed)")

        # ---- Watchlist items (3 lists) ----
        watch_rows = []
        # Default list
        for sym in ['AAPL', 'MSFT', 'GOOGL', 'AMZN', 'NVDA']:
            watch_rows.append((user_id, sym, now - dt.timedelta(days=rng.randint(30, 365)), 'Default', None))
        # Tech list
        for sym in ['AMD', 'META', 'TSLA', 'AAPL', 'MSFT', 'NVDA']:
            watch_rows.append((user_id, sym, now - dt.timedelta(days=rng.randint(10, 200)), 'Tech', 'Tech sector exposure'))
        # AI plays list
        for sym in ['NVDA', 'GOOGL', 'META', 'MSFT', 'AMD']:
            watch_rows.append((user_id, sym, now - dt.timedelta(days=rng.randint(5, 90)), 'AI plays', 'AI thesis'))
        # Long-term list
        for sym in ['AAPL', 'MSFT', 'AMZN']:
            watch_rows.append((user_id, sym, now - dt.timedelta(days=rng.randint(60, 400)), 'Long-term', None))

        cur.executemany(
            "INSERT INTO WatchlistItems (UserId, Symbol, AddedAt, ListName, Notes) VALUES (%s,%s,%s,%s,%s)",
            watch_rows)
        print(f"  +{len(watch_rows)} watchlist items across 4 lists")

        # ---- Pending orders ----
        pending_rows = []
        for sym in ['AAPL', 'MSFT', 'GOOGL']:
            cur_px = latest_price(history[sym])
            if cur_px is None: continue
            pending_rows.append((user_id, sym, 1, Decimal(str(round(cur_px * 0.92, 2))), Decimal('5'), 1,
                                 now - dt.timedelta(days=rng.randint(1, 15)), None, None, None, 'Wait for dip', None, None))
        for sym in ['TSLA', 'AMD']:
            cur_px = latest_price(history[sym])
            if cur_px is None: continue
            pending_rows.append((user_id, sym, 2, Decimal(str(round(cur_px * 1.08, 2))), Decimal('3'), 1,
                                 now - dt.timedelta(days=rng.randint(1, 10)), None, None, None, 'Take profit', None, None))
        for sym in ['NVDA', 'META']:
            cur_px = latest_price(history[sym])
            if cur_px is None: continue
            pending_rows.append((user_id, sym, 3, Decimal(str(round(cur_px * 0.85, 2))), Decimal('2'), 1,
                                 now - dt.timedelta(days=rng.randint(1, 20)), None, None, None, 'Stop loss', None, None))
        # Trailing stops on AAPL, MSFT
        for sym in ['AAPL', 'MSFT']:
            cur_px = latest_price(history[sym])
            if cur_px is None: continue
            pct = Decimal('7.5') if sym == 'AAPL' else Decimal('5.0')
            trig = Decimal(str(round(cur_px * float(1 - float(pct) / 100), 2)))
            pending_rows.append((user_id, sym, 4, trig, Decimal('4'), 1,
                                 now - dt.timedelta(days=rng.randint(1, 8)), None, None, None, 'Trail', Decimal(str(round(cur_px, 4))), pct))
        # Some filled / cancelled in history
        for sym in ['AMZN', 'GOOGL', 'TSLA']:
            cur_px = latest_price(history[sym])
            if cur_px is None: continue
            limit = Decimal(str(round(cur_px * 0.95, 2)))
            created = now - dt.timedelta(days=rng.randint(40, 200))
            filled = created + dt.timedelta(days=rng.randint(1, 30))
            pending_rows.append((user_id, sym, 1, limit, Decimal('2'), 2, created, filled, limit, None, 'Auto-filled', None, None))
        for sym in ['NVDA']:
            cur_px = latest_price(history[sym])
            if cur_px is None: continue
            limit = Decimal(str(round(cur_px * 0.7, 2)))
            created = now - dt.timedelta(days=rng.randint(100, 300))
            pending_rows.append((user_id, sym, 1, limit, Decimal('5'), 3, created, created + dt.timedelta(days=60), None, None, 'Cancelled by user', None, None))

        cur.executemany(
            "INSERT INTO PendingOrders (UserId, Symbol, Side, LimitPrice, Quantity, Status, CreatedAt, FilledAt, FilledPrice, FailureReason, Notes, HighWaterMark, TrailingStopPercent) "
            "VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)", pending_rows)
        print(f"  +{len(pending_rows)} pending orders")

        # ---- Price alerts ----
        alert_rows = []
        # Active above-target alerts
        for sym in ['AAPL', 'MSFT', 'NVDA', 'TSLA']:
            cur_px = latest_price(history[sym])
            if cur_px is None: continue
            alert_rows.append((user_id, sym, 1, Decimal(str(round(cur_px * 1.1, 2))), 1,
                               now - dt.timedelta(days=rng.randint(1, 30)), None, None, 0, None))
        # Active below-target alerts
        for sym in ['GOOGL', 'AMZN', 'META']:
            cur_px = latest_price(history[sym])
            if cur_px is None: continue
            alert_rows.append((user_id, sym, 2, Decimal(str(round(cur_px * 0.92, 2))), 1,
                               now - dt.timedelta(days=rng.randint(1, 30)), None, None, 0, None))
        # Triggered (unacknowledged)
        for sym in ['AMD']:
            cur_px = latest_price(history[sym])
            if cur_px is None: continue
            trg = Decimal(str(round(cur_px * 0.95, 2)))
            alert_rows.append((user_id, sym, 1, trg, 2,
                               now - dt.timedelta(days=20), now - dt.timedelta(days=2),
                               Decimal(str(round(cur_px, 2))), 0, None))
        # Triggered (acknowledged)
        for sym in ['MSFT', 'GOOGL']:
            cur_px = latest_price(history[sym])
            if cur_px is None: continue
            trg = Decimal(str(round(cur_px * 0.98, 2)))
            alert_rows.append((user_id, sym, 1, trg, 2,
                               now - dt.timedelta(days=60), now - dt.timedelta(days=40),
                               Decimal(str(round(cur_px, 2))), 1, None))
        # Cancelled
        for sym in ['TSLA', 'AMZN']:
            cur_px = latest_price(history[sym])
            if cur_px is None: continue
            alert_rows.append((user_id, sym, 2, Decimal(str(round(cur_px * 0.7, 2))), 3,
                               now - dt.timedelta(days=80), None, None, 0, None))

        cur.executemany(
            "INSERT INTO PriceAlerts (UserId, Symbol, Direction, TriggerPrice, Status, CreatedAt, TriggeredAt, TriggeredPrice, Acknowledged, Notes) "
            "VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)", alert_rows)
        print(f"  +{len(alert_rows)} price alerts")

        # ---- Recurring orders ----
        recur_rows = []
        # Weekly AAPL, $250
        recur_rows.append((user_id, 'AAPL', Decimal('250'), 2,
                           now + dt.timedelta(days=3), now - dt.timedelta(days=4), 1,
                           now - dt.timedelta(days=180), 25, 1, None))
        # Monthly VOO -> use MSFT, $500
        recur_rows.append((user_id, 'MSFT', Decimal('500'), 4,
                           now + dt.timedelta(days=12), now - dt.timedelta(days=18), 1,
                           now - dt.timedelta(days=400), 13, 0, None))
        # Biweekly NVDA, $300
        recur_rows.append((user_id, 'NVDA', Decimal('300'), 3,
                           now + dt.timedelta(days=10), now - dt.timedelta(days=4), 1,
                           now - dt.timedelta(days=120), 8, 0, None))
        # Paused GOOGL
        recur_rows.append((user_id, 'GOOGL', Decimal('200'), 2,
                           now + dt.timedelta(days=7), now - dt.timedelta(days=60), 0,
                           now - dt.timedelta(days=300), 12, 1, 'Paused after Sep dip'))
        # Daily small AMZN, $50
        recur_rows.append((user_id, 'AMZN', Decimal('50'), 1,
                           now + dt.timedelta(hours=10), now - dt.timedelta(days=1), 1,
                           now - dt.timedelta(days=60), 59, 1, 'Insufficient cash on one run'))

        cur.executemany(
            "INSERT INTO RecurringOrders (UserId, Symbol, CashAmount, Frequency, NextRunAt, LastRunAt, IsActive, CreatedAt, SuccessfulRuns, FailedRuns, LastFailureReason) "
            "VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)", recur_rows)
        print(f"  +{len(recur_rows)} recurring orders")

        # ---- Goals ----
        # PortfolioValue=1, TotalReturn=2, DividendIncome=3
        goal_rows = [
            (user_id, 1, Decimal('50000'), 'Reach $50k portfolio',
             (now + dt.timedelta(days=365)).date(), now - dt.timedelta(days=200), 0, None),
            (user_id, 1, Decimal('25000'), 'Quarter portfolio',
             None, now - dt.timedelta(days=420), 1, now - dt.timedelta(days=60)),
            (user_id, 2, Decimal('5000'), '$5k in profits',
             (now + dt.timedelta(days=180)).date(), now - dt.timedelta(days=120), 0, None),
            (user_id, 2, Decimal('1000'), 'First $1k profit',
             None, now - dt.timedelta(days=500), 1, now - dt.timedelta(days=200)),
            (user_id, 3, Decimal('500'), '$500 in dividends',
             (now + dt.timedelta(days=365)).date(), now - dt.timedelta(days=90), 0, None),
            (user_id, 1, Decimal('100000'), 'Six figures',
             (now + dt.timedelta(days=3 * 365)).date(), now - dt.timedelta(days=30), 0, None),
        ]
        cur.executemany(
            "INSERT INTO Goals (UserId, Type, TargetAmount, Title, DueDate, CreatedAt, IsCompleted, CompletedAt) "
            "VALUES (%s,%s,%s,%s,%s,%s,%s,%s)", goal_rows)
        print(f"  +{len(goal_rows)} goals")

        # Update user cash + createdAt (move it back 4 years for realism)
        cur.execute("UPDATE AspNetUsers SET CashBalance=%s, CreatedAt=%s WHERE Id=%s",
                    (cash, earliest, user_id))
        print(f"  cash balance set to {cash}, CreatedAt set to {earliest}")

        conn.commit()
        print("\nDone. Login as demo1@trading212.local / Demo1234")
        print("Snapshots will auto-backfill the next time you open the Analytics page.")
    except Exception:
        conn.rollback()
        raise
    finally:
        cur.close()
        conn.close()


if __name__ == '__main__':
    main()

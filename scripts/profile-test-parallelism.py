#!/usr/bin/env python3
"""Per-CLASS wall-clock profile of a test assembly, from a `--logger trx` run — plan §11 A13(b).

    python scripts/profile-test-parallelism.py <run.trx> [top-N]
    python scripts/profile-test-parallelism.py --slowest <N> <run.trx>...

The second form lists the N slowest individual TESTS across every trx it is given — `scripts/battery.sh` prints
the five slowest of its three legs in the battery summary (DESIGN-test-build-ci.md §3.14.5, kb/Work PB1716), so
the next long pole is seen when it appears. It is a REPORT, never a wall-clock assertion (a timing ceiling flakes
under load, and `NoWallClockAssertionDriftTests` forbids one), and it exits 0 whatever it finds; an unreadable trx
is named in its output rather than failing the battery, whose leg verdicts already report a missing leg.

⛔ WHY. xUnit 2.9.2 parallelizes at TEST-COLLECTION granularity, and by default **each test CLASS is one
collection** — so every test in a class, including every row of a `[Theory]`, runs SERIALLY ON ONE THREAD. A
single fat class therefore caps an entire assembly's wall clock while the other cores idle, and nothing in the
normal output says so: `dotnet test` reports totals and duration, never concurrency.

That is not a theory. The first run of this script on the greenfield UNIT assembly measured **3,634 tests across
63 classes in 210 s wall for 279 s of total test time — 1.3x average concurrency on a 32-core box** — and found
that ONE class (`StorageFormEquivalenceTests`, 6 tests, 210 s) *was* the whole wall clock.

⚠ READ THE TWO NUMBERS TOGETHER. `sum` is the class's total test time — the serial cost if it is one collection.
`span` is first-start to last-end, i.e. what it actually occupied. `sum ≈ span` means the class ran serially and
is a splitting candidate; `span << sum` means it was already spread across threads. A class whose tests report a
zero-width span (some runners stamp identical start/end) is flagged on `sum` alone — treat a large `sum` as the
signal either way.

`scripts/battery.sh` already emits a trx for the Conformance and Unit legs, so this profile costs nothing extra.
"""
from __future__ import annotations

import collections
import datetime
import pathlib
import sys
import xml.etree.ElementTree as ET

NS = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}


def _parse_time(stamp: str | None) -> datetime.datetime | None:
    return datetime.datetime.fromisoformat(stamp.replace('Z', '+00:00')) if stamp else None


def _seconds(duration: str | None) -> float:
    h, m, s = (duration or '0:0:0').split(':')
    return int(h) * 3600 + int(m) * 60 + float(s)


def slowest_tests(paths: list[str], top: int) -> int:
    """The `top` slowest individual tests across `paths`, slowest first, each with the trx (leg) it came from."""
    rows: list[tuple[float, str, str]] = []
    for path in paths:
        try:
            root = ET.parse(path).getroot()
        except (OSError, ET.ParseError) as e:
            print(f'  !! {path}: no readable trx ({e.__class__.__name__}: {e})')
            continue
        leg = pathlib.Path(path).stem
        for r in root.iter(f'{{{NS["t"]}}}UnitTestResult'):
            rows.append((_seconds(r.get('duration')), leg, r.get('testName', '?')))
    if not rows:
        print(f'  !! no test results in {" ".join(paths)}')
    for secs, leg, name in sorted(rows, key=lambda row: -row[0])[:top]:
        print(f'  {secs:8.1f}s  {leg:<17} {name}')
    return 0


def main(argv: list[str]) -> int:
    # The Windows console defaults to cp1252, which cannot encode the ⚠/⛔ this report uses — and the failure is
    # an UnhandledException AFTER the first lines have printed, i.e. a tool that looks like it half-worked.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding='utf-8', errors='replace')
        except (AttributeError, ValueError):                  # already wrapped, or not a real stream
            pass
    if not argv:
        sys.exit(__doc__)
    if argv[0] == '--slowest':
        if len(argv) < 3:
            sys.exit(__doc__)
        return slowest_tests(argv[2:], int(argv[1]))
    path, top = argv[0], int(argv[1]) if len(argv) > 1 else 15
    root = ET.parse(path).getroot()

    by_id = {}
    for ut in root.iter(f'{{{NS["t"]}}}UnitTest'):
        method = ut.find(f'{{{NS["t"]}}}TestMethod')
        if method is not None:
            by_id[ut.get('id')] = method.get('className', '?').split(',')[0]

    agg: dict[str, dict] = collections.defaultdict(
        lambda: {'n': 0, 'sum': 0.0, 'start': None, 'end': None})
    for r in root.iter(f'{{{NS["t"]}}}UnitTestResult'):
        a = agg[by_id.get(r.get('testId'), '?')]
        a['n'] += 1
        a['sum'] += _seconds(r.get('duration'))
        start, end = _parse_time(r.get('startTime')), _parse_time(r.get('endTime'))
        if start and (a['start'] is None or start < a['start']):
            a['start'] = start
        if end and (a['end'] is None or end > a['end']):
            a['end'] = end

    if not agg:
        print(f'!! no test results in {path}', file=sys.stderr)
        return 2

    total = sum(a['sum'] for a in agg.values())
    starts = [a['start'] for a in agg.values() if a['start']]
    ends = [a['end'] for a in agg.values() if a['end']]
    wall = (max(ends) - min(starts)).total_seconds() if starts and ends else 0.0
    tests = sum(a['n'] for a in agg.values())
    conc = total / wall if wall else 0.0
    print(f'{path}')
    print(f'  classes {len(agg)}  tests {tests}  wall {wall:.0f}s  '
          f'sum-of-test-time {total:.0f}s  =>  average concurrency {conc:.1f}x')
    if conc < 4:
        print(f'  ⚠ {conc:.1f}x means this assembly is effectively SERIAL — the classes below are the reason.')

    print(f"\n  {'class':<52} {'tests':>6} {'sum(s)':>8} {'span(s)':>8}  note")
    for name, a in sorted(agg.items(), key=lambda kv: -kv[1]['sum'])[:top]:
        span = (a['end'] - a['start']).total_seconds() if a['start'] and a['end'] else 0.0
        share = a['sum'] / wall if wall else 0
        note = 'SERIAL' if span > 0 and a['sum'] / span > 0.75 else ''
        if share > 0.25:
            note = (note + ' ⛔ IS THE WALL CLOCK').strip()
        print(f"  {name.split('.')[-1]:<52} {a['n']:>6} {a['sum']:>8.1f} {span:>8.1f}  {note}")
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))

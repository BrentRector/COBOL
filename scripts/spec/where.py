#!/usr/bin/env python3
"""where.py — where is a spec clause implemented?  Orientation in one call instead of a grep survey.

Every rule the compiler implements carries its § citation in a comment (project rule: spec-to-code traceability),
so the citations ARE the index. Given a clause (and optionally a rule), print the source files that cite it,
ranked by citation count, with the first few cited lines each — plus the kb/Work notes and goldens that name it.

    python scripts/spec/where.py 13.18.63            # every file citing §13.18.63 or a sub-clause
    python scripts/spec/where.py 13.18.63.3 SR2      # only lines that also name the rule
    python scripts/spec/where.py 14.9.39 --top 5 --lines 2

Measured motivation (waves 45–57, 81 implementer/finisher transcripts): searching and reading the codebase was
46 % of their tokens, the single largest cost; each agent re-surveyed the same subsystems from scratch.
"""
import argparse, collections, pathlib, re, sys

ROOT = pathlib.Path(__file__).resolve().parents[2]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('clause', help='e.g. 13.18.63 or 13.18.63.3')
    ap.add_argument('rule', nargs='?', help='optional rule token, e.g. SR2, GR4, r6')
    ap.add_argument('--top', type=int, default=12)
    ap.add_argument('--lines', type=int, default=3)
    # The cited lines it prints carry characters a cp1252 console cannot encode (a source comment's ⛔, a §), and
    # printing one raised UnicodeEncodeError mid-listing; cite.py does the same before its parse_args.
    try:
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    except Exception:  # noqa: BLE001
        pass
    a = ap.parse_args()
    clause = re.escape(a.clause.lstrip('§'))
    cpat = re.compile(r'§\s?' + clause + r'(?![0-9])')
    rpat = re.compile(r'\b' + re.escape(a.rule) + r'\b', re.I) if a.rule else None
    hits = collections.defaultdict(list)
    for base, globs in (('src', ('*.cs', '*.g4')), ('tests', ('*.cs',)), ('kb/Work', ('*.md',)), ('docs', ('*.md',))):
        for g in globs:
            for p in (ROOT / base).rglob(g):
                if 'Generated' in p.parts or 'bin' in p.parts or 'obj' in p.parts:
                    continue
                try:
                    text = p.read_text(encoding='utf-8', errors='ignore')
                except OSError:
                    continue
                if not cpat.search(text):
                    continue
                for i, line in enumerate(text.splitlines(), 1):
                    if cpat.search(line) and (rpat is None or rpat.search(line)):
                        hits[p.relative_to(ROOT).as_posix()].append((i, line.strip()))
    if not hits:
        print(f'no citation of §{a.clause}' + (f' {a.rule}' if a.rule else '') + ' — the rule may be unimplemented; '
              'check the traceability inventory row and kb/Work before surveying')
        return 1
    groups = collections.OrderedDict((k, []) for k in ('src', 'tests', 'kb/Work', 'docs'))
    for f, ls in hits.items():
        key = 'kb/Work' if f.startswith('kb/Work') else f.split('/')[0]
        groups.setdefault(key, []).append((f, ls))
    for key, items in groups.items():
        if not items:
            continue
        items.sort(key=lambda x: -len(x[1]))
        print(f'== {key}: {len(items)} file(s)')
        for f, ls in items[:a.top]:
            print(f'  {f}  ({len(ls)})')
            for i, line in ls[:a.lines]:
                print(f'      :{i}  {line[:150]}')
        if len(items) > a.top:
            print(f'  … {len(items) - a.top} more (--top)')
    return 0


if __name__ == '__main__':
    sys.exit(main())

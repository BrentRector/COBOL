#!/usr/bin/env python3
"""Extract GnuCOBOL autotest (`.at`) groups into runnable (source, expectation) cases.

Plan §11 A4 / PHASE-14 Step 13 — the EXTERNAL DIFFERENTIAL CORPUS.

⚖ LICENSING (load-bearing — do not deviate): GnuCOBOL and its testsuite are GPL-3.0; this repo is BSL 1.1.
This script is OURS and is committed; the corpus it reads is NOT and lives in the git-ignored
`tests/external/gnucobol/`. Extracted case text is written ONLY under that ignored tree (or held in memory).

WHERE THE LINE SITS (owner decision 2026-07-19): never emit their COBOL test SOURCE or their EXPECTED
OUTPUT/diagnostic text into a committed file — that is the substantial expressive content. Short factual
group TITLES and KEYWORDS are citable identification (the nominative use any write-up describing the suite
would make) and MAY appear in our committed reports and ledger, which is what makes them usable for triage.

The `.at` format is m4/autotest:

    AT_SETUP([title])
    AT_KEYWORDS([kw ...])
    AT_DATA([prog.cob], [ <COBOL source> ])
    AT_CHECK([$COMPILE_ONLY prog.cob], [exit-status], [expected-stdout], [expected-stderr])
    AT_CLEANUP

Bracket quoting nests, so all payload extraction is bracket-balanced rather than regex-based.
"""
from __future__ import annotations
import argparse, json, os, re, sys
from dataclasses import dataclass, field, asdict

# ── bracket-balanced m4 payload reader ────────────────────────────────────────────────────────────────────
def read_bracketed(s: str, i: int) -> tuple[str, int]:
    """s[i] must be '['. Return (payload_without_outer_brackets, index_after_closing_bracket)."""
    assert s[i] == '[', f'expected [ at {i}'
    depth, start = 0, i
    while i < len(s):
        if s[i] == '[':
            depth += 1
        elif s[i] == ']':
            depth -= 1
            if depth == 0:
                return s[start + 1:i], i + 1
        i += 1
    raise ValueError('unbalanced brackets')


def split_args(s: str, i: int, maxargs: int) -> tuple[list[str], int]:
    """Read up to maxargs comma-separated bracketed args starting at s[i]=='('."""
    assert s[i] == '(', f'expected ( at {i}'
    i += 1
    args: list[str] = []
    while i < len(s) and len(args) < maxargs:
        while i < len(s) and s[i] in ' \t\r\n':
            i += 1
        if i < len(s) and s[i] == '[':
            payload, i = read_bracketed(s, i)
            args.append(payload)
        elif i < len(s) and s[i] == ')':
            break
        else:  # bare (unbracketed) argument, e.g. AT_CHECK([...], 0, ...)
            j = i
            while j < len(s) and s[j] not in ',)':
                j += 1
            args.append(s[i:j].strip())
            i = j
        while i < len(s) and s[i] in ' \t\r\n':
            i += 1
        if i < len(s) and s[i] == ',':
            i += 1
            continue
        if i < len(s) and s[i] == ')':
            i += 1
            break
    return args, i


@dataclass
class Check:
    command: str
    status: str = ''
    stdout: str = ''
    stderr: str = ''
    # derived
    kind: str = ''          # compile | compile-and-run | run | other
    expects_failure: bool = False


@dataclass
class Group:
    id: str
    file: str
    line: int
    title: str
    keywords: list[str] = field(default_factory=list)
    data: dict[str, str] = field(default_factory=dict)   # filename -> content
    checks: list[Check] = field(default_factory=list)
    xfail: bool = False
    skipped_reason: str = ''


COMPILE_TOKENS = ('$COMPILE_ONLY', '$COMPILE ', '$COBC', '$COMPILE_MODULE', '$COMPILE_LISTING')
RUN_TOKENS = ('$COBCRUN', '$COBCRUN_DIRECT', './prog')


def classify_check(cmd: str) -> str:
    has_c = any(t in cmd for t in COMPILE_TOKENS)
    has_r = any(t in cmd for t in RUN_TOKENS)
    if has_c and has_r:
        return 'compile-and-run'
    if has_c:
        return 'compile'
    if has_r:
        return 'run'
    return 'other'


def parse_file(path: str) -> list[Group]:
    text = open(path, encoding='utf-8', errors='replace').read()
    groups: list[Group] = []
    cur: Group | None = None
    i = 0
    fname = os.path.basename(path)
    while i < len(text):
        m = re.compile(r'\bAT_(SETUP|KEYWORDS|DATA|CHECK|CLEANUP|XFAIL_IF|SKIP_IF)\b').search(text, i)
        if not m:
            break
        macro = m.group(1)
        j = m.end()
        while j < len(text) and text[j] in ' \t':
            j += 1
        line_no = text.count('\n', 0, m.start()) + 1

        if macro == 'CLEANUP':
            if cur:
                groups.append(cur)
                cur = None
            i = j
            continue

        if j >= len(text) or text[j] != '(':
            i = j
            continue

        try:
            args, nxt = split_args(text, j, 4)
        except ValueError:
            i = j + 1
            continue

        if macro == 'SETUP':
            if cur:
                groups.append(cur)
            title = (args[0] if args else '').strip()
            gid = f'{fname[:-3]}:{line_no}'
            cur = Group(id=gid, file=fname, line=line_no, title=title)
        elif cur is not None:
            if macro == 'KEYWORDS':
                cur.keywords = (args[0] if args else '').split()
            elif macro == 'DATA':
                if len(args) >= 2:
                    cur.data[args[0].strip()] = args[1]
            elif macro == 'CHECK':
                c = Check(command=(args[0] if args else '').strip(),
                          status=(args[1].strip() if len(args) > 1 else ''),
                          stdout=(args[2] if len(args) > 2 else ''),
                          stderr=(args[3] if len(args) > 3 else ''))
                c.kind = classify_check(c.command)
                c.expects_failure = c.status not in ('', '0')
                cur.checks.append(c)
            elif macro == 'XFAIL_IF':
                cur.xfail = True
        i = nxt
    if cur:
        groups.append(cur)
    return groups


# ── THE ONE EXTERNAL-CORPUS POPULATION (PB209) ────────────────────────────────────────────────────────────
#
# ⛔ WHY THIS SECTION EXISTS. Until 2026-08-31 the differential defined "the corpus" INSIDE its own worker
# (`run_case` re-derived "has a COBOL member AND has a compile check" inline), while every reachability sweep
# defined it with a `find … -name '*.cob'` over `tests/external/gnucobol/`. Those two definitions disagree by
# three orders of magnitude: the whole external tree holds 75 files of which exactly TWO are `.cob`, because
# the 1,323 programs live as `AT_DATA` heredocs inside the `.at` wrappers this module parses. A wave therefore
# proved a shape ABSENT from a corpus the gate then found it in — twice, and one of the cases is titled
# "REDEFINES: with OCCURS" (PB209; §13.18.44.3 SR5 s.1 / §13.18.63.3 SR13 s.2).
#
# So the population is defined ONCE, here, and BOTH readers call it: `gnucobol_differential.py` for the cases
# it compiles, `corpus_sweep.py` for the programs it greps. A sweep over "the corpus" now mechanically includes
# the extracted programs, and `ExternalCorpusPopulationDriftTests` fails if the two ever diverge again.
#
# The canonical currency is the `asdict(Group)` DICT, not the dataclass: the differential ships groups across a
# ProcessPoolExecutor, so they have to be picklable plain data anyway, and one shape means the predicate below
# cannot acquire a second spelling.

DEFAULT_SRC = 'tests/external/gnucobol/tests/testsuite.src'
COBOL_SUFFIXES = ('.cob', '.cbl')


@dataclass(frozen=True)
class CorpusProgram:
    """One COBOL program text in the external corpus, with the coordinate that names it."""
    case_id: str        # the differential's case id, e.g. 'syn_redefines:172'
    at_file: str        # the .at wrapper it was extracted from
    filename: str       # the AT_DATA member name, e.g. 'prog.cob'
    title: str          # the AT_SETUP title (citable identification, never their source)
    text: str           # ⚖ GPL — hold in memory, never write to a committed file


def cobol_members(g: dict) -> list[str]:
    """The AT_DATA member names that are COBOL source, in declaration order."""
    return [f for f in g['data'] if f.lower().endswith(COBOL_SUFFIXES)]


def primary_source(g: dict) -> tuple[str, dict] | None:
    """The (member, compile-check) pair the differential compiles, or None if this group is not a case.

    ⛔ THE ONE DEFINITION OF "IS THIS A CORPUS CASE". A group qualifies when it carries COBOL source AND a
    check that compiles it; the primary member is the one that check names, else the first. Returning the
    pair rather than a bool is deliberate — `run_case` needs exactly this and must not re-derive it.
    """
    srcs = cobol_members(g)
    if not srcs:
        return None
    compile_checks = [c for c in g['checks'] if c['kind'].startswith('compile')]
    if not compile_checks:
        return None
    chk = compile_checks[0]
    return next((s for s in srcs if s in chk['command']), srcs[0]), chk


def case_path(case_dir: str, member: str) -> str | None:
    """Where an AT_DATA member lives under its case directory: at its OWN relative path (`SUB/copy.inc` in SUB/),
    exactly where GnuCOBOL's testsuite writes it — flattening it to its basename hides every OF/IN library. None
    for a member that would land outside the case directory."""
    rel = os.path.normpath(member)
    if os.path.isabs(rel) or rel == '..' or rel.startswith('..' + os.sep):
        return None
    return os.path.join(case_dir, rel)


def materialize(g: dict, case_dir: str) -> bool:
    """Write every AT_DATA member of group `g` under `case_dir` at its own relative path (THE one materializer,
    shared by the differential and `--out`). False when a member cannot be placed there."""
    for fn, content in g['data'].items():
        p = case_path(case_dir, fn)
        if p is None:
            return False
        try:
            os.makedirs(os.path.dirname(p), exist_ok=True)
            with open(p, 'w', encoding='utf-8', newline='\n') as fh:
                fh.write(content.lstrip('\n'))
        except OSError:
            return False
    return True


def is_case(g: dict) -> bool:
    return primary_source(g) is not None


def load_groups(src: str = DEFAULT_SRC, only: str = '') -> list[dict]:
    """Every AT_SETUP group in every `.at` wrapper under `src`, as plain dicts."""
    if not os.path.isdir(src):
        raise FileNotFoundError(src)
    groups: list[dict] = []
    for f in sorted(x for x in os.listdir(src) if x.endswith('.at') and x.startswith(only)):
        groups.extend(asdict(g) for g in parse_file(os.path.join(src, f)))
    return groups


def differential_cases(src: str = DEFAULT_SRC, only: str = '') -> list[dict]:
    """THE external-corpus population: the groups the differential compiles. `len()` is its `cases run`."""
    return [g for g in load_groups(src, only) if is_case(g)]


def iter_programs(src: str = DEFAULT_SRC, only: str = '') -> "list[CorpusProgram]":
    """Every COBOL program text in that population — what a reachability sweep must actually read.

    A case may carry more than one COBOL member (a CALLed subprogram, a COPY book compiled separately), and a
    sweep asking "does any corpus program have this shape" must see all of them, not just the primary.
    """
    out: list[CorpusProgram] = []
    for g in differential_cases(src, only):
        for fn in cobol_members(g):
            out.append(CorpusProgram(case_id=g['id'], at_file=g['file'], filename=fn,
                                     title=g['title'], text=g['data'][fn]))
    return out


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--src', default=DEFAULT_SRC)
    ap.add_argument('--out', default='', help='ignored-tree dir to materialize cases into')
    ap.add_argument('--only', default='', help='filename prefix filter, e.g. syn_')
    ap.add_argument('--summary', action='store_true')
    a = ap.parse_args()

    if not os.path.isdir(a.src):
        print(f'!! corpus not present: {a.src}\n'
              f'   run python scripts/external_corpus.py ensure first (the corpus is GPL and is never committed).',
              file=sys.stderr)
        return 2

    files = sorted(f for f in os.listdir(a.src)
                   if f.endswith('.at') and f.startswith(a.only))
    allg = load_groups(a.src, a.only)
    cobol_groups = [g for g in allg if is_case(g)]

    if a.summary:
        from collections import Counter
        print(f'files              : {len(files)}')
        print(f'groups parsed      : {len(allg)}')
        print(f'corpus cases       : {len(cobol_groups)}   (differential_cases — the ONE population)')
        print(f'programs extracted : {len(iter_programs(a.src, a.only))}')
        print(f'checks total       : {sum(len(g["checks"]) for g in allg)}')
        print('check kinds        :', dict(Counter(c['kind'] for g in allg for c in g['checks'])))
        print('compile-expect-FAIL:', sum(1 for g in allg for c in g['checks']
                                          if c['kind'].startswith('compile') and c['expects_failure']))
        print('compile-expect-OK  :', sum(1 for g in allg for c in g['checks']
                                          if c['kind'].startswith('compile') and not c['expects_failure']))
        print('xfail groups       :', sum(1 for g in allg if g['xfail']))

    if a.out:
        os.makedirs(a.out, exist_ok=True)
        # materialize ONLY under the ignored tree
        idx = []
        for g in cobol_groups:
            d = os.path.join(a.out, re.sub(r'[^A-Za-z0-9_.:-]', '_', g['id']).replace(':', '_'))
            if not materialize(g, d):
                print(f'!! {g["id"]}: a member would land outside its case directory; skipped', file=sys.stderr)
                continue
            idx.append({**g, 'dir': d})
        with open(os.path.join(a.out, '_index.json'), 'w', encoding='utf-8') as fh:
            json.dump(idx, fh, indent=1)
        print(f'materialized {len(idx)} groups into {a.out} (ignored tree)')
    return 0


if __name__ == '__main__':
    sys.exit(main())

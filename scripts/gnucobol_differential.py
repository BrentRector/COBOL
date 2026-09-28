#!/usr/bin/env python3
"""Run the extracted GnuCOBOL corpus through cobol.exe and bucket the accept/reject divergences.

Plan §11 A4 / PHASE-14 Step 13 — the EXTERNAL DIFFERENTIAL CORPUS.

⚖ LICENSING: the corpus is GPL-3.0 and lives ONLY in the git-ignored `tests/external/gnucobol/`; this repo is
BSL 1.1. This script is ours. The line (owner decision 2026-07-19): their COBOL test SOURCE and their EXPECTED
OUTPUT/diagnostic text are the substantial expressive content and are NEVER committed or reproduced. Short
factual group TITLES and KEYWORDS are citable identification — the nominative use any article describing the
suite would make — so this report carries them alongside our own verdicts, codes, and counts.

WHAT A DIVERGENCE MEANS (read before triaging — the naive reading is wrong):
GnuCOBOL's DEFAULT dialect is `default`, i.e. ISO **plus GnuCOBOL extensions**; the testsuite only pins an
ISO edition on a minority of groups (-std=cobol85/cobol2002/cobol2014) and pins VENDOR dialects (mf/ibm/acu)
on others. Therefore:
  * WE_REJECT_THEY_ACCEPT is NOT automatically our bug — it is our bug only where the construct is ISO.
    Where the construct is a GnuCOBOL/vendor extension, rejecting it is OUR CORRECT BEHAVIOUR.
  * WE_ACCEPT_THEY_REJECT is the higher-confidence signal: GnuCOBOL's permissive default rejected it, so
    an ISO-conforming compiler almost certainly should too. These are prime "our grammar is too lax" leads.
Confidence tiers are emitted so triage can start where the signal is cleanest.
"""
from __future__ import annotations
import argparse, json, os, re, subprocess, sys, tempfile
from concurrent.futures import ProcessPoolExecutor

# ⛔ MODULE-LEVEL, not inside main(): `run_case` is shipped to a ProcessPoolExecutor, and on Windows the
# spawned child re-imports this module rather than inheriting main()'s locals. An import made inside main()
# leaves every worker with no `gx` — 1,323 identical RUNNER_ERRORs, which is precisely the shape the harness
# rules below refuse to launder into compiler verdicts.
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gnucobol_extract as gx                                                              # noqa: E402

STD_MAP = {'cobol85': '85', 'cobol2002': '2002', 'cobol2014': '2014', 'cobol2023': '2023'}
VENDOR_STDS = ('mf', 'ibm', 'acu', 'rm', 'xopen', 'mvs', 'bs2000', 'realia')


def std_of(cmd: str) -> tuple[str, str]:
    """Return (our_std, tier) for a GnuCOBOL compile command."""
    m = re.search(r'-std=([a-z0-9-]+)', cmd)
    if not m:
        return '2023', 'DEFAULT_DIALECT'      # their default = ISO + extensions
    s = m.group(1)
    if s in STD_MAP:
        return STD_MAP[s], 'ISO_PINNED'       # directly comparable
    base = s.split('-')[0]
    if base in VENDOR_STDS:
        return '2023', 'VENDOR_DIALECT'       # not ISO; rejecting is correct for us
    return '2023', 'DEFAULT_DIALECT'


def copy_dirs(cmd: str, case_dir: str) -> list[str]:
    """The copybook directories a GnuCOBOL compile command names with `-I DIR` / `-IDIR`, resolved against the case
    directory the command runs in. GnuCOBOL's `-I` and our `--copy` are the same thing — a place of the default
    COBOL library after the working directory (DOC-A.1-40) — so a case that names one compiles with it here too. A
    directory spelled with a shell variable is not the case's own and is left out."""
    return [os.path.join(case_dir, m) for m in re.findall(r'(?:^|\s)-I\s*([^\s$]+)(?=\s|$)', cmd)]


def compile_once(exe: str, src: str, std: str, out_dll: str, cwd: str | None = None,
                 copy: list[str] | tuple[str, ...] = ()):
    """One compile attempt → (returncode, diagnostics, artifact_exists, runner_error).

    Everything the evidence rule needs, and nothing inferred. `runner_error` is non-empty only when the
    PROCESS could not be observed at all (timeout, launch failure) — distinct from a process that ran and
    said something.

    `cwd` is the case directory: GnuCOBOL's testsuite runs `cobc` inside the test directory its AT_DATA files were
    written to, so that is where a case's copybooks sit, and the working directory is the first place of the
    default COBOL library (docs/CONFORMANCE.md DOC-A.1-40) — the source file's own directory is not one (kb/Work
    PB1355). Without it the compiler runs in the source's directory. `copy` is the case's `-I` directories
    (`copy_dirs`), each passed as `--copy`.
    """
    try:
        r = subprocess.run([exe, src, '--std', std, '-o', out_dll, *(a for c in copy for a in ('--copy', c))],
                           capture_output=True, text=True, timeout=60,
                           cwd=cwd or os.path.dirname(os.path.abspath(src)))
        return r.returncode, r.stdout + r.stderr, os.path.exists(out_dll), ''
    except subprocess.TimeoutExpired:
        return -1, '<<TIMEOUT>>', False, 'TimeoutExpired'
    except Exception as e:                                    # noqa: BLE001
        return -1, '', False, f'{type(e).__name__}: {e}'


def has_evidence(rc: int, diag: str, artifact: bool) -> bool:
    """Does this compile support a VERDICT about the compiler?

    Accept needs the artifact it claims to have produced; reject needs the reason it claims to have found.
    An exit status on its own is not evidence: under load a process can be killed or fail to start and
    return non-zero having compiled nothing and said nothing. Verified against this build by
    evidence_control(): a rejection is exit 65 with ~300 bytes of diagnostic, an acceptance is exit 0 with
    the .dll — there is no third shape.
    """
    return bool(artifact) if rc == 0 else bool(diag.strip())


EVIDENCE_CONTROL_GOOD = ('IDENTIFICATION DIVISION.\nPROGRAM-ID. EVCTLOK.\n'
                         'PROCEDURE DIVISION.\nMAIN.\n    DISPLAY "OK".\n    STOP RUN.\n')
EVIDENCE_CONTROL_BAD = ('IDENTIFICATION DIVISION.\nPROGRAM-ID. EVCTLBAD.\n'
                        'PROCEDURE DIVISION.\nMAIN.\n    MOVE TO.\n    STOP RUN.\n')


def evidence_control(exe: str) -> list[str]:
    """Prove — on THIS build, before any case is scored — that the evidence rule can tell accept from reject.

    `feedback_green_gates_arent_evidence`: a rule that has never been shown to discriminate is not a rule. If
    the compiler ever rejects silently, `has_evidence` would classify every genuine rejection as a lost result
    and the differential would go quietly, uselessly clean. This is the check that makes that loud instead.
    Returns a list of failure descriptions (empty = the rule holds).
    """
    fails = []
    with tempfile.TemporaryDirectory(prefix='gcdiff_ctl_') as d:
        for tag, src_text, want_ok in (('accept', EVIDENCE_CONTROL_GOOD, True),
                                       ('reject', EVIDENCE_CONTROL_BAD, False)):
            src = os.path.join(d, f'{tag}.cob')
            with open(src, 'w', encoding='utf-8', newline='\n') as fh:
                fh.write(src_text)
            rc, diag, artifact, err = compile_once(exe, src, '2023', os.path.join(d, f'{tag}.dll'))
            if err:
                fails.append(f'{tag} control could not be run: {err}')
            elif (rc == 0) != want_ok:
                fails.append(f'{tag} control returned exit {rc} (expected {"0" if want_ok else "non-zero"})')
            elif not has_evidence(rc, diag, artifact):
                fails.append(f'{tag} control produced NO EVIDENCE (exit {rc}, {len(diag.strip())} diag bytes, '
                             f'artifact={artifact}) — the evidence rule cannot classify this build')
    return fails


def run_case(args):
    exe, group, workroot = args
    gid = group['id']
    # ⛔ THE POPULATION PREDICATE IS NOT SPELLED HERE (PB209). It used to be — "has a COBOL member AND a
    # compile check", re-derived inline — while every reachability sweep spelled "the corpus" as a `*.cob`
    # glob that found TWO files. One definition now, in gnucobol_extract, shared with corpus_sweep.py.
    sel = gx.primary_source(group)
    if sel is None:
        return None
    prim, chk = sel

    d = os.path.join(workroot, re.sub(r'[^A-Za-z0-9_.-]', '_', gid))
    os.makedirs(d, exist_ok=True)
    # Each AT_DATA file lands at ITS OWN relative path under the case directory (`SUB/copy.inc` in SUB/), exactly
    # where GnuCOBOL's testsuite writes it: flattening it to its basename used to hide every OF/IN library.
    if not gx.materialize(group, d):
        return None

    std, tier = std_of(chk['command'])
    out_dll = os.path.join(d, '_out.dll')
    copy = copy_dirs(chk['command'], d)

    # ⛔ EVIDENCE-REQUIRED VERDICTS (plan §11 A12e). A compile is retried ONCE when it produced no evidence,
    # because the first attempt made no observation to retry the interpretation of — see compile_once().
    rc, diag, artifact, runner_error = compile_once(exe, gx.case_path(d, prim), std, out_dll, cwd=d, copy=copy)
    if not runner_error and not has_evidence(rc, diag, artifact):
        try:
            os.remove(out_dll)
        except OSError:
            pass
        rc, diag, artifact, runner_error = compile_once(exe, gx.case_path(d, prim), std, out_dll, cwd=d, copy=copy)

    def _rec(verdict, codes, first):
        return {'id': gid, 'file': group['file'], 'title': group['title'][:160],
                'keywords': group['keywords'], 'std': std, 'tier': tier,
                'verdict': verdict, 'ourCodes': codes, 'ourFirstError': first,
                'xfail': group.get('xfail', False)}

    # A harness failure is NOT a compiler verdict. Laundering one into WE_REJECT_THEY_ACCEPT is how a
    # totally broken run reports as 1046 "compiler bugs" — which is exactly what happened on the first
    # run of this script (a relative exe path that Windows CreateProcess could not resolve).
    if runner_error:
        return _rec('RUNNER_ERROR', [], runner_error)

    # ⛔ AND NEITHER IS AN EVIDENCE-FREE EXIT STATUS. Observed 2026-08-02: `run_fundamental.at :: "Numeric
    # operations (6)"` was scored WE_REJECT_THEY_ACCEPT — the 0-tolerance direction — with `ourCodes: []`, an
    # EMPTY `ourFirstError`, and a case that compiles clean when run directly. A "rejection" with no diagnostic
    # is not a rejection; it is a LOST RESULT, and scoring it as one manufactures a regression in the one
    # direction the gate treats as unforgivable. Same root cause as A12b/A12c: a missing observation was being
    # read as a negative observation. The premise is checked at startup by evidence_control(), so this rule
    # cannot quietly become wrong if the compiler ever stops printing a reason for a rejection.
    if not has_evidence(rc, diag, artifact):
        why = (f'exit {rc} with NO diagnostic output' if rc != 0
               else 'exit 0 but no output artifact was produced')
        return _rec('NO_COMPILER_EVIDENCE', [], f'{why} (retried once)')

    we_ok = (rc == 0)
    they_ok = not chk['expects_failure']
    if we_ok and they_ok:
        verdict = 'AGREE_ACCEPT'
    elif not we_ok and not they_ok:
        verdict = 'AGREE_REJECT'
    elif we_ok and not they_ok:
        verdict = 'WE_ACCEPT_THEY_REJECT'
    else:
        verdict = 'WE_REJECT_THEY_ACCEPT'

    codes = sorted(set(re.findall(r'\b(COBOLNET\d{4}|COBOL\d{4})\b', diag)))
    first = ''
    for line in diag.splitlines():
        if 'error' in line.lower():
            first = line.strip()[:300]
            break
    return {'id': gid, 'file': group['file'], 'title': group['title'][:160],
            'keywords': group['keywords'], 'std': std, 'tier': tier,
            'verdict': verdict, 'ourCodes': codes, 'ourFirstError': first,
            'xfail': group.get('xfail', False)}


BASELINE_HEADER = """\
# WiseOwl COBOL GnuCOBOL external-differential PER-CASE VERDICT BASELINE.
#
# ⛔ WHY THIS FILE EXISTS. The differential's four TOTALS matching the previous run is NOT evidence of zero
# flips — offsetting flips look identical, and for a long time no per-case report was kept, so "0 per-case
# flips" was asserted from totals alone. This manifest makes the claim mechanical: one row per case, so a
# flip is a ONE-LINE git diff a reviewer can see, exactly as tests/nist/corpus.tsv does for NIST.
#
# ⚖ LICENSING (owner decisions 2026-07-19 and 2026-08-04). Every column here is OURS: the case ID is a
# coordinate we compute, the tier is our classification, the verdict is our compiler's behaviour compared
# with theirs. Their COBOL test SOURCE and their EXPECTED OUTPUT are never committed and are not here.
# Titles and keywords are deliberately OMITTED — the diff does not need them, so this file carries nothing
# but our own generated results.
#
# ⛔ REGENERATING IS A DELIBERATE ACT, NEVER A FIX FOR A RED. `--write-baseline` rewrites it; the commit that
# does so must ATTRIBUTE every changed row. A flip is either a fix (we now accept ISO source we rejected), a
# regression, or a corpus refresh — and only the first two are visible here. Rewriting to make a red go away
# destroys the only record that the behaviour moved.
#
# Tab-separated, sorted by id; a leading # is a comment. Columns: id<TAB>tier<TAB>verdict
"""


def read_baseline(path: str) -> dict[str, tuple[str, str]] | None:
    """The committed manifest as {id: (tier, verdict)}, or None when absent (first run / corpus not fetched)."""
    if not os.path.exists(path):
        return None
    out: dict[str, tuple[str, str]] = {}
    with open(path, encoding='utf-8') as fh:
        for line in fh:
            line = line.rstrip('\n')
            if not line or line.startswith('#'):
                continue
            parts = line.split('\t')
            if len(parts) != 3:
                continue
            out[parts[0]] = (parts[1], parts[2])
    return out


def write_baseline(path: str, results: list[dict]) -> None:
    os.makedirs(os.path.dirname(path) or '.', exist_ok=True)
    with open(path, 'w', encoding='utf-8', newline='\n') as fh:
        fh.write(BASELINE_HEADER)
        for r in sorted(results, key=lambda x: x['id']):
            fh.write(f"{r['id']}\t{r['tier']}\t{r['verdict']}\n")


def diff_baseline(base: dict[str, tuple[str, str]], results: list[dict], path: str) -> int:
    """Print the PER-CASE diff and return the number of VERDICT FLIPS (0 = clean).

    NEW / REMOVED cases are reported but are NOT flips: they mean the fetched corpus changed, which is a
    legitimate event the compiler did not cause. Conflating the two is how a corpus refresh would either mask
    a real regression or manufacture 1300 false ones.
    """
    cur = {r['id']: r for r in results}
    flips = [(i, base[i][1], cur[i]['verdict']) for i in sorted(base.keys() & cur.keys())
             if base[i][1] != cur[i]['verdict']]
    new, gone = sorted(cur.keys() - base.keys()), sorted(base.keys() - cur.keys())

    print(f'\n=== PER-CASE DIFF vs {path} ===')
    if new or gone:
        print(f'  corpus changed: {len(new)} NEW case(s), {len(gone)} REMOVED — not flips, but explain them.')
        for i in new[:10]:
            print(f'    NEW      {i}  -> {cur[i]["verdict"]}')
        for i in gone[:10]:
            print(f'    REMOVED  {i}  (was {base[i][1]})')
    for i, was, now in flips:
        why = (cur[i].get('ourFirstError') or '').replace('\n', ' ')[:100]
        print(f'    FLIP     {i:<28} {was} -> {now}   {why}')
    print(f'=== DIFFERENTIAL: {len(flips)} PER-CASE FLIP(S) ===')
    return len(flips)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--exe', default='src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe')
    ap.add_argument('--src', default='tests/external/gnucobol/tests/testsuite.src')
    ap.add_argument('--only', default='')
    ap.add_argument('--jobs', type=int, default=max(2, (os.cpu_count() or 4) - 2))
    ap.add_argument('--report', default='tests/external/gnucobol-differential-report.json')
    ap.add_argument('--baseline', default='tests/external/gnucobol-verdict-baseline.tsv',
                    help='committed per-case verdict manifest to diff against (see write_baseline)')
    ap.add_argument('--write-baseline', action='store_true',
                    help='REWRITE --baseline from this run instead of diffing against it. Deliberate only: '
                         'every accepted flip must be attributed in the commit message that rewrites it.')
    a = ap.parse_args()

    if not os.path.exists(a.exe):
        print(f'!! compiler not found: {a.exe}', file=sys.stderr); return 2
    # MUST be absolute: on Windows, CreateProcess does not resolve a relative forward-slash path, so
    # subprocess.run() raises FileNotFoundError (WinError 2) even though os.path.exists() is True. Left
    # relative, EVERY case fails identically and the run reports a corpus-wide false "we reject everything".
    a.exe = os.path.abspath(a.exe)

    # ⛔ THE EVIDENCE RULE IS PROVEN BEFORE IT IS APPLIED (plan §11 A12e). If this build cannot be told to
    # reject with a reason, every scored rejection below would be indistinguishable from a lost result.
    ctl = evidence_control(a.exe)
    if ctl:
        print('!! THE EVIDENCE CONTROL FAILED — verdicts from this run would not be trustworthy:', file=sys.stderr)
        for m in ctl:
            print(f'   {m}', file=sys.stderr)
        return 4
    print('evidence control: accept and reject are distinguishable on this build.')

    if not os.path.isdir(a.src):
        print(f'!! corpus absent: {a.src}\n   run scripts/fetch-gnucobol-tests.ps1 (GPL, never committed).',
              file=sys.stderr)
        return 2

    # THE population, from the ONE definition — pre-filtered, so `len(payload)` IS `cases run` and the
    # progress counter below stops counting groups it will silently drop (PB209).
    groups = gx.differential_cases(a.src, a.only)
    print(f'external corpus population: {len(groups)} case(s) from '
          f'{len([x for x in os.listdir(a.src) if x.endswith(".at") and x.startswith(a.only)])} .at wrapper(s) '
          f'- gnucobol_extract.differential_cases')

    work = tempfile.mkdtemp(prefix='gcdiff_')
    payload = [(a.exe, g, work) for g in groups]
    results = []
    with ProcessPoolExecutor(max_workers=a.jobs) as ex:
        for i, r in enumerate(ex.map(run_case, payload, chunksize=4), 1):
            if r: results.append(r)
            if i % 200 == 0: print(f'  ... {i}/{len(payload)}', flush=True)

    from collections import Counter
    by_v = Counter(r['verdict'] for r in results)

    # LOUD failure on a broken harness — silence is not success. If a meaningful share of cases never
    # reached the compiler, every downstream number is noise and must not be read as a finding.
    # ⛔ NO_COMPILER_EVIDENCE COUNTS AS A HARNESS FAILURE, NOT A VERDICT. It is a case whose compile produced
    # neither an artifact nor a reason; folding it into WE_REJECT_THEY_ACCEPT is precisely the A12e defect.
    # Each one is a case to RE-RUN, never a divergence to triage — and each is named, because a lost result
    # that nobody looks at is how the previous version of this block stayed quiet.
    NON_VERDICTS = ('RUNNER_ERROR', 'NO_COMPILER_EVIDENCE')
    errs = sum(by_v.get(v, 0) for v in NON_VERDICTS)
    if errs:
        pct = 100.0 * errs / max(1, len(results))
        print(f'\n!! {errs} of {len(results)} cases ({pct:.1f}%) PRODUCED NO COMPILER VERDICT '
              f'(harness, not compiler).')
        for v in NON_VERDICTS:
            named = [r for r in results if r['verdict'] == v]
            if not named:
                continue
            print(f'   {v} ({len(named)}):')
            for m, n in Counter(r['ourFirstError'] for r in named).most_common(5):
                print(f'     {n:5}  {m[:110]}')
            for r in named[:10]:
                print(f'     re-run: {r["file"]} :: {r["title"][:80]}')
        if pct > 5.0:
            print('!! ABORTING: >5% harness failures — the verdict counts below would be meaningless.',
                  file=sys.stderr)
            return 3
    by_vt = Counter((r['verdict'], r['tier']) for r in results)
    print(f'\ncases run: {len(results)}')
    for v, n in by_v.most_common():
        print(f'  {v:24} {n}')
    print('\nby confidence tier:')
    for (v, t), n in sorted(by_vt.items()):
        if v.startswith('WE_'):
            print(f'  {v:24} {t:16} {n}')

    os.makedirs(os.path.dirname(a.report), exist_ok=True)
    with open(a.report, 'w', encoding='utf-8') as fh:
        json.dump({'summary': {f'{v}': n for v, n in by_v.items()},
                   'byTier': {f'{v}|{t}': n for (v, t), n in by_vt.items()},
                   'cases': results}, fh, indent=1)
    print(f'\nreport -> {a.report}')

    # ── PER-CASE FLIP DETECTION (the claim the four totals above cannot make) ────────────────────────────
    if a.write_baseline:
        write_baseline(a.baseline, results)
        print(f'baseline REWRITTEN -> {a.baseline}  ({len(results)} cases). '
              'Attribute every changed row in the commit that does this.')
        return 0
    base = read_baseline(a.baseline)
    if base is None:
        # Absent baseline is a LOUD gap, not a pass: without it this run proves nothing per-case, and
        # silently returning 0 is what let "0 per-case flips" be asserted from totals for weeks.
        print(f'\n!! NO VERDICT BASELINE at {a.baseline} — this run CANNOT claim zero per-case flips.\n'
              f'   Create it deliberately: --write-baseline', file=sys.stderr)
        return 5
    return 6 if diff_baseline(base, results, a.baseline) else 0


if __name__ == '__main__':
    sys.exit(main())

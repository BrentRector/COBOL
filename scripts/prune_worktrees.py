#!/usr/bin/env python3
"""Classify every local branch and agent worktree against origin/main, and (with --apply) archive and remove the ones whose work is on main.

    python scripts/prune_worktrees.py                      # dry run: a table, nothing touched
    python scripts/prune_worktrees.py --apply [--include-check] [--bundle <file>]

Why this exists (2026-09-30): `git worktree remove` after `git merge-base --is-ancestor` clears only branches whose commits are literally
in main. A train re-applies, rebases and conflict-resolves each cluster's commits, so a LANDED cluster's branch is never an ancestor and 40+
branches and 19 worktrees piled up from earlier waves. The test that does decide it is content, and it is derived on each run:

  MERGED   the branch tip is an ancestor of origin/main.
  LANDED   not an ancestor, but EVERY file the branch added under src/ or tests/conformance exists on origin/main, and every C# type, method and
           test name it declared exists in origin/main's src, tests or scripts.
  CHECK    every added file exists, but some declared names do not. A rename or a deliberate redesign (the PB1683 derived test filter was
           replaced on 2026-09-28) looks the same as lost work: read the listed names against the branch's kb/Work notes before removing.
  UNLANDED an added file is absent from origin/main: preserved work that never landed (PB1210's golden, found this way). Never removed.

--apply bundles every branch it removes into one verified git bundle OUTSIDE the repository (default: the system temp directory), unlocks and
removes its worktree, then deletes the branch. MERGED and LANDED are removed; CHECK only with --include-check; UNLANDED never. A dirty,
locked or recently touched worktree is skipped (a running agent's worktree is locked and fresh, and sits at main's tip, which classifies as
MERGED: never remove it; --include-locked and --min-age override). On Windows a worktree can refuse to delete ("Invalid argument", "Access is denied") because the C# language server
(csharp-ls) loaded an analyzer DLL from it: stop csharp-ls and re-run.
"""
import argparse
import pathlib
import re
import subprocess
import sys
import tempfile
import time

REPO = pathlib.Path(__file__).resolve().parents[1]
DECL = re.compile(r'^\+\s*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|override|readonly|async|virtual|unsafe|new)\s+)*'
                  r'(?:class|record|struct|enum|interface)\s+(?:struct\s+)?(\w+)')
METH = re.compile(r'^\+\s*(?:(?:public|internal|private|protected|static|sealed|abstract|override|readonly|async|virtual|unsafe)\s+)+'
                  r'[\w<>\[\],.?()\s]+?\s+(\w+)\s*\(')
TEST = re.compile(r'^\+\s*public (?:async )?(?:void|Task)\s+(\w+)\s*\(')


def git(*args, cwd=REPO):
    return subprocess.run(['git', '-C', str(cwd), *args], capture_output=True, text=True, encoding='utf-8', errors='replace')


def main_names():
    out = git('grep', '-h', '-I', '-E', '-o', r'[A-Za-z_][A-Za-z0-9_]{3,}', 'origin/main', '--', 'src', 'tests', 'scripts').stdout
    return set(out.split())


def classify(branch, names):
    if git('merge-base', '--is-ancestor', branch, 'origin/main').returncode == 0:
        return 'MERGED', [], []
    absent = [f for f in git('diff', '--name-only', '--diff-filter=A', 'origin/main...' + branch).stdout.split()
              if not f.startswith(('TestResults/', 'tools/')) and f != 'STATUS.md'
              and git('cat-file', '-e', 'origin/main:' + f).returncode != 0]   # ANY added file absent from main counts (2026-10-04: a branch that added only scripts/ and docs/ was classified LANDED and would have been deleted)
    declared = set()
    for c in git('rev-list', '--no-merges', 'origin/main..' + branch).stdout.split():
        for line in git('show', '--format=', '-U0', c, '--', '*.cs').stdout.split('\n'):
            for rx in (DECL, METH, TEST):
                m = rx.match(line)
                if m and len(m.group(1)) > 3:
                    declared.add(m.group(1))
    lost = sorted(n for n in declared if n not in names)
    return ('UNLANDED' if absent else 'CHECK' if lost else 'LANDED'), absent, lost


def worktrees():
    found = {}
    for blk in [b for b in git('worktree', 'list', '--porcelain').stdout.split('\n\n') if b.strip()]:
        path = re.search(r'^worktree (.+)$', blk, re.M).group(1)
        br = re.search(r'^branch refs/heads/(.+)$', blk, re.M)
        if br and br.group(1) != 'main':
            found[br.group(1)] = (path, bool(re.search(r'^locked', blk, re.M)))
    return found


def age_minutes(path):
    log = pathlib.Path(git('rev-parse', '--absolute-git-dir', cwd=path).stdout.strip()) / 'logs' / 'HEAD'
    try:
        return (time.time() - log.stat().st_mtime) / 60
    except OSError:
        return 0.0


def main():
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--apply', action='store_true')
    ap.add_argument('--include-check', action='store_true')
    ap.add_argument('--include-locked', action='store_true')
    ap.add_argument('--min-age', type=int, default=180, help='skip a worktree touched in the last N minutes (default 180)')
    ap.add_argument('--bundle', default=str(pathlib.Path(tempfile.gettempdir()) / f'pruned-branches-{time.strftime("%Y%m%d-%H%M%S")}.bundle'))
    a = ap.parse_args()
    git('fetch', '-q', 'origin')
    names = main_names()
    wts = worktrees()
    branches = [b for b in git('branch', '--format=%(refname:short)').stdout.split() if b != 'main']
    rows = [(b, *classify(b, names)) for b in branches]
    for b, kind, absent, lost in rows:
        ahead = git('rev-list', '--count', 'origin/main..' + b).stdout.strip()
        wt = ' [worktree]' if b in wts else ''
        print(f'{kind:9} ahead {ahead:>3}  {b}{wt}')
        for f in absent[:3]:
            print(f'            absent on main: {f}')
        if kind == 'CHECK':
            print(f'            names not on main: {" ".join(lost[:8])}')
    if not a.apply:
        print('\ndry run: nothing removed. --apply archives and removes MERGED and LANDED (--include-check adds CHECK).')
        return 0
    take = {'MERGED', 'LANDED'} | ({'CHECK'} if a.include_check else set())
    doomed = [b for b, kind, _, _ in rows if kind in take]
    unmerged = [b for b in doomed if git('rev-list', '--count', 'origin/main..' + b).stdout.strip() != '0']
    if unmerged:
        if git('bundle', 'create', a.bundle, *unmerged).returncode != 0 or git('bundle', 'verify', a.bundle).returncode != 0:
            print('the archive bundle did not verify; nothing removed')
            return 1
        print(f'archived {len(unmerged)} unmerged tips in {a.bundle}')
    removed = 0
    for b in doomed:
        if b in wts:
            path, locked = wts[b]
            if locked and not a.include_locked:
                print(f'skipped (locked, probably a running agent): {b}')
                continue
            if age_minutes(path) < a.min_age:
                print(f'skipped (touched in the last {a.min_age} min): {b}')
                continue
            if git('status', '--short', '--', '.', ':!.claude/settings.local.json', cwd=path).stdout.strip():
                print(f'skipped (dirty worktree): {b}')
                continue
            if locked:
                git('worktree', 'unlock', path)
            r = git('worktree', 'remove', '--force', path)
            if r.returncode != 0:
                print(f'could not remove the worktree of {b}: {r.stderr.strip()[:120]} (stop csharp-ls and re-run)')
                continue
        git('branch', '-D', b)
        removed += 1
    git('worktree', 'prune')
    print(f'removed {removed} branches and their worktrees')
    return 0


if __name__ == '__main__':
    sys.exit(main())

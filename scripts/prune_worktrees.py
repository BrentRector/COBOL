#!/usr/bin/env python3
"""Every local branch and worktree, by CONTENT against origin/main and by LIFECYCLE (who acts on it next); with --apply,
remove the ones whose work is on main, and the ones abandoned by a recorded decision.

    python scripts/prune_worktrees.py                      # dry run: the lifecycle table, nothing touched
    python scripts/prune_worktrees.py --brief              # the verdict line and only the rows someone must act on
    python scripts/prune_worktrees.py --json               # the same survey as JSON (the operator tick reads it)
    python scripts/prune_worktrees.py --train              # THE NEXT TRAIN: at most TRAIN_MAX WAITING TO LAND rows
    python scripts/prune_worktrees.py --apply [--abandon <branch> ...]
    python scripts/prune_worktrees.py --self-test          # a scratch repository with one branch in every state

⛔ THE OWNER'S RULE (2026-10-08 00:00 PDT, kb/Work PB2600): "We should never have unused branches left around. We use a
branch to test a change. If the change is accepted, we eventually merge the branch to main and delete the branch. If the
change is not accepted, we either fix it, then merge, or decide it's not the right solution and typically delete the
branch if we decide to abandon that approach." So every branch is in exactly one LIFECYCLE state, and each state names
who acts next:

  IN FLIGHT        a live dispatch owns it: its worktree is locked by an agent whose process is alive (the harness
                   writes `claude agent <id> (pid N)`; a lock with no pid was placed by hand and counts as live), a
                   dispatch-ledger wave group still in flight names it (`<coord>/inflight-groups.json`, the planner's
                   ledger, read through plan_wave.inflight_file_sets; the branch name starts with the group's slug
                   `w<wave><letter>`), the train whose lander holds the landing lease carries it (its manifest,
                   landing_train_members), or it was touched in the last --min-age minutes (its HEAD reflog or STATUS.md).
                   Nobody touches it.
  WAITING TO LAND  not on main, and its newest fix-lane report (`w<wave><letter>-PB<lead>-report.md`, plan_wave.py
                   REPORT_NAME, under coord.reports_dir()) says `Status: DONE`: a land unit's train takes it. A train
                   carries at most TRAIN_MAX of them, the most harmful first (`--train`, kb/Work PB2981); the rest wait
                   for the land unit after it.
  LANDED           its work is on main by content (MERGED or LANDED below) and its worktree holds nothing uncommitted:
                   `--apply` deletes it.
  DECISION         everything else: a person or the operator decides — land it (via a finisher when SPLIT), or ABANDON it
                   with the decision recorded in its kb/Work note — and then it is deleted. Each row prints its evidence.

The CONTENT class (why this exists, 2026-09-30): `git merge-base --is-ancestor` clears only branches whose commits are
literally in main. A train re-applies, rebases and conflict-resolves each cluster's commits, so a LANDED cluster's branch
is never an ancestor and 40+ branches and 19 worktrees piled up from earlier waves. The test that does decide it is
content, and it is derived on each run:

  MERGED   the branch tip is an ancestor of origin/main.
  LANDED   not an ancestor, but MERGING it into origin/main would change nothing: `git merge-tree --write-tree
           origin/main <branch>` merges cleanly to origin/main's own tree. That is the one proof (train 1039b review):
           the earlier test (every added file and every declared C# name on main) called a branch that only MODIFIED
           existing files LANDED, so an unlanded method-body fix would have been deleted.
           Also LANDED when its own merge would change main but its work reached main as OTHER commits (carried(), kb/Work
           PB2708): a train_apply.py commit on origin/main brought in exactly its head (a lander that edits a cluster in
           the train changes the merge), or every one of its commits is patch-equivalent to one on main (`git cherry`, a
           finisher's cherry-pick) and it declares no name main lacks. The row's evidence says which.
  CHECK    every added file exists, but merging it would still change main: an unlanded fix to existing files, a
           train's re-resolved landing, a rename or a deliberate redesign (the PB1683 derived test filter was replaced
           on 2026-09-28) all look alike. The C# names it declared that main lacks are listed as evidence: read them,
           and the branch's kb/Work notes, before removing.
  UNLANDED an added file is absent from origin/main: preserved work that never landed (PB1210's golden, found this way).
           Never removed.

--apply unlocks and removes the worktree, then deletes the branch, of exactly the LANDED rows: never an IN FLIGHT branch,
never a worktree with an uncommitted edit or an untracked file, never UNLANDED or CHECK (owner 2026-10-07: "I do not want
to accidentally lose useful work"). The classification IS that protection; nothing is archived (owner 2026-10-08 00:52
PDT: "Archiving to a bundle serves no use and wastes disk space"). A branch that is not landed is deleted only when it
is ABANDONED: `--apply --abandon <branch>` removes a clean, idle DECISION row only when a kb/Work note already records
the decision (a line naming the branch and the word ABANDONED, with the reason). Each removed branch's `origin/<branch>`
goes with it unless it holds commits the local branch did not (drop_remote). --include-locked also takes a landed
worktree locked by hand (no pid). A DETACHED worktree (a read-only fleet's pin)
has no branch: it is listed with its lifecycle and never removed here. On Windows a worktree can refuse to delete
("Invalid argument", "Access is denied") because the C# language server (csharp-ls) loaded an analyzer DLL from it:
stop csharp-ls and re-run.
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import tempfile
import time
from concurrent.futures import ThreadPoolExecutor
from typing import Callable

REPO = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / 'scripts' / 'orchestrator'))
import coord  # noqa: E402  (the coordination directory, the live-process probe)

DECL = re.compile(r'^\+\s*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|override|readonly|async|virtual|unsafe|new)\s+)*'
                  r'(?:class|record|struct|enum|interface)\s+(?:struct\s+)?(\w+)')
METH = re.compile(r'^\+\s*(?:(?:public|internal|private|protected|static|sealed|abstract|override|readonly|async|virtual|unsafe)\s+)+'
                  r'[\w<>\[\],.?()\s]+?\s+(\w+)\s*\(')
TEST = re.compile(r'^\+\s*public (?:async )?(?:void|Task)\s+(\w+)\s*\(')
LANDED_CONTENT = ('MERGED', 'LANDED')
STATES = ('IN FLIGHT', 'WAITING TO LAND', 'LANDED', 'DECISION')
LOCK_PID = re.compile(r'\(pid (\d+)\)')
SETTINGS = ':!.claude/settings.local.json'   # always dirty in an agent's worktree, never work (2026-10-03)


def git(*args, cwd=REPO):
    return subprocess.run(['git', '-C', str(cwd), *args], capture_output=True, text=True, encoding='utf-8', errors='replace')


def main_names(repo=REPO):
    out = git('grep', '-h', '-I', '-E', '-o', r'[A-Za-z_][A-Za-z0-9_]{3,}', 'origin/main', '--', 'src', 'tests', 'scripts', cwd=repo).stdout
    return set(out.split())


def merges_to_main_unchanged(branch, repo=REPO) -> bool:
    """Would merging `branch` into origin/main leave main's tree exactly as it is? Any git failure (unrelated histories, a
    conflict, a missing ref) is a NO: an unproven landing is never LANDED."""
    merged = git('merge-tree', '--write-tree', 'origin/main', branch, cwd=repo)
    tree = git('rev-parse', 'origin/main^{tree}', cwd=repo)
    return (merged.returncode == 0 and tree.returncode == 0 and bool(tree.stdout.strip())
            and merged.stdout.split('\n', 1)[0].strip() == tree.stdout.strip())


def uncommitted(path) -> int:
    """The worktree's uncommitted paths (the settings file excepted). A status git cannot produce (a corrupt index, a
    dubious-ownership refusal) counts as ONE dirty path: an unmeasured worktree is never removed (train 1039b review)."""
    r = git('status', '--porcelain', '--', '.', SETTINGS, cwd=path)
    if r.returncode != 0:
        return 1
    return len([l for l in r.stdout.splitlines() if l.strip()])


# A TRAIN CARRIED IT (kb/Work PB2708). A lander that edits a cluster in the train makes the branch's own merge change
# main, so the merge test alone reads CHECK: after train 1042b landed, three of its members read WAITING TO LAND (the next
# land unit would have re-carried them) and seven read DECISION; after train 1053, branch-records-r1716 (53Y) read
# WAITING TO LAND. The record of what a train took is its MANIFEST (`<coord>/scratch/train<label>-manifest.json`: per
# cluster its branch, base and head) plus, on origin/main, the train's commits: train_apply.py gives each cluster's
# commit a `Train-Cluster: <cluster>` trailer, and the train ends in a `Train <label>: ...` commit. A member whose
# trailer commit is in its train's range (train_clusters: back from the train's last commit to the member's base or to
# another train's commit, whichever is nearer, since trains reuse cluster letters) was brought in, its diff is in main's
# history, and deleting the branch loses nothing even where a later commit withdrew the fix. The commit BODY is not the
# record: landers rewrite it (train 1053's 53A and 53G kept only the trailer). A member with no trailer commit (a train
# before train_apply.py, or a lander that dropped the trailer: train 1053's 53P) stays a hand decision.
_trains: dict[tuple[str, str], dict[str, list[tuple[str, str]]]] = {}


def train_heads(repo=REPO, coord_dir: pathlib.Path | None = None) -> dict[str, list[tuple[str, str]]]:
    """{branch: [(the head a LANDED train took, the evidence)]}: the members of every train manifest whose train's
    `Train <label>:` commit is on origin/main and whose `Train-Cluster:` trailer commit is in that train's range. Read
    once per repository and coordination directory per process."""
    cdir = pathlib.Path(coord_dir) if coord_dir else coord.coord_path()
    key = (str(repo), str(cdir))
    if key in _trains:
        return _trains[key]
    final: dict[str, str] = {}             # train label -> its newest `Train <label>:` commit on origin/main
    for line in git('log', 'origin/main', '--format=%H %s', cwd=repo).stdout.splitlines():
        sha, _, subject = line.partition(' ')
        if (m := re.match(r'Train (\S+?):', subject)) and m.group(1) not in final:
            final[m.group(1)] = sha
    heads: dict[str, list[tuple[str, str]]] = {}
    ranges: dict[tuple[str, str], dict[str, str]] = {}
    for f in sorted((cdir / 'scratch').glob('train*-manifest.json')) if (cdir / 'scratch').is_dir() else ():
        label = f.name[len('train'):-len('-manifest.json')]
        if label not in final:
            continue
        try:
            members = coord.read_json(f, []) or []
        except ValueError:
            continue
        for m in members if isinstance(members, list) else ():
            b, h, c, base = (str(m.get(k) or '') for k in ('branch', 'head', 'cluster', 'base')) if isinstance(m, dict) else ('',) * 4
            if not (b and h and c and base):
                continue
            if (base, label) not in ranges:
                ranges[(base, label)] = train_clusters(base, final[label], label, repo)
            if sha := ranges[(base, label)].get(c):
                heads.setdefault(b, []).append((h, f'carried by train {label} as cluster {c} ({sha}), which took its '
                                                   f'head {h[:12]}'))
    _trains[key] = heads
    return heads


def train_clusters(base: str, last: str, label: str, repo=REPO) -> dict[str, str]:
    """{cluster: its `Train-Cluster:` trailer commit} of ONE train: walking back along the first parent from the train's
    last commit to the member's base or to the nearest commit of ANOTHER train (`Train <other>:`), whichever comes first.
    A member's base can predate an earlier train (the wave-1055 branches were cut before train 1056 landed) and every
    train reuses the letters A, B, C, so the whole base..last range credited train 1055's members with train 1056's B to F
    (2026-10-11): a member its lander ejected would read LANDED by another train's commit, and --apply delete it."""
    out = git('log', '--first-parent', f'{base}..{last}',
              '--format=%h%x00%s%x00%(trailers:key=Train-Cluster,valueonly)%x01', cwd=repo).stdout
    found: dict[str, str] = {}
    for rec in out.split('\x01'):
        sha, _, rest = rec.strip().partition('\x00')
        subject, _, value = rest.partition('\x00')
        if (m := re.match(r'Train (\S+?):', subject)) and m.group(1) != label:
            break
        if value.strip():
            found.setdefault(value.strip(), sha)
    return found


def carried(branch, lost, repo=REPO, coord_dir: pathlib.Path | None = None) -> str:
    """How a branch's work reached main as OTHER commits, or '' (kb/Work PB2708): a landed train took exactly its head
    (train_heads), or every one of its commits is patch-equivalent to a commit on main (`git cherry`: a finisher
    cherry-picked them, the five SPLIT predecessors of 2026-10-10) and it declares no name main lacks. A branch that
    moved past the head a train took is not carried (its later commits never were), and a cherry-picked branch with a
    name main lacks stays a DECISION (`worktree-wf_0cae47bc-172-2`: HolderState, StateOf)."""
    head = git('rev-parse', branch, cwd=repo).stdout.strip()
    for took, evidence in train_heads(repo, coord_dir).get(branch, []):
        if head and head.startswith(took):
            return evidence
    marks = git('cherry', 'origin/main', branch, cwd=repo)
    signs = [line[:1] for line in marks.stdout.splitlines() if line.strip()]
    if marks.returncode == 0 and signs and set(signs) == {'-'} and not lost:
        return f'every one of its {len(signs)} commit(s) is patch-equivalent to a commit on main (git cherry)'
    return ''


def classify(branch, names, repo=REPO, coord_dir: pathlib.Path | None = None):
    """(content class, added files absent from main, declared C# names main lacks, how a LANDED row's work reached main
    when its own merge would still change main: carried(), else '')."""
    if git('merge-base', '--is-ancestor', branch, 'origin/main', cwd=repo).returncode == 0:
        return 'MERGED', [], [], ''
    absent = [f for f in git('diff', '--name-only', '--diff-filter=A', 'origin/main...' + branch, cwd=repo).stdout.split()
              if not f.startswith(('TestResults/', 'tools/')) and f != 'STATUS.md'
              and git('cat-file', '-e', 'origin/main:' + f, cwd=repo).returncode != 0]   # ANY added file absent from main counts (2026-10-04: a branch that added only scripts/ and docs/ was classified LANDED and would have been deleted)
    # The names the branch's NET change declares (merge base to tip), never each commit's: a name one WIP commit added
    # and a later commit deleted is not lost (kb/Work PB2708: 908-1's RunsOfEqual, added in fb4c17001, gone at its head).
    declared = set()
    for line in git('diff', '-U0', 'origin/main...' + branch, '--', '*.cs', cwd=repo).stdout.split('\n'):
        for rx in (DECL, METH, TEST):
            m = rx.match(line)
            if m and len(m.group(1)) > 3:
                declared.add(m.group(1))
    lost = sorted(n for n in declared if n not in names)
    if absent:
        return 'UNLANDED', absent, lost, ''
    if merges_to_main_unchanged(branch, repo):
        return 'LANDED', absent, lost, ''
    how = carried(branch, lost, repo, coord_dir)
    return ('LANDED' if how else 'CHECK'), absent, lost, how


@dataclasses.dataclass
class Tree:
    path: str
    branch: str | None       # None: a detached worktree
    lock: str | None         # None: not locked; '' : locked with no reason; else the lock reason
    head: str


def worktrees(repo=REPO) -> list[Tree]:
    """Every linked worktree but the main checkout, with its branch (None when detached) and its lock reason."""
    found = []
    for blk in [b for b in git('worktree', 'list', '--porcelain', cwd=repo).stdout.split('\n\n') if b.strip()][1:]:
        path = re.search(r'^worktree (.+)$', blk, re.M).group(1)
        br = re.search(r'^branch refs/heads/(.+)$', blk, re.M)
        lk = re.search(r'^locked(?: (.*))?$', blk, re.M)
        head = re.search(r'^HEAD ([0-9a-f]+)$', blk, re.M)
        found.append(Tree(path, br.group(1) if br else None, (lk.group(1) or '') if lk else None, head.group(1) if head else ''))
    return found


def live_lock(reason: str | None, alive: Callable[[int], str | None] = coord.process_name) -> bool:
    """Is this lock held by a live agent? A harness lock names its agent's pid, which must be a live claude process (a
    reused pid is some other image: verify-pid-before-kill, 2026-10-07); a lock with no pid was placed by hand and is
    honoured as live."""
    if reason is None:
        return False
    m = LOCK_PID.search(reason)
    if not m:
        return True
    name = alive(int(m.group(1)))
    return bool(name) and 'claude' in name.lower()


def age_minutes(*paths: pathlib.Path) -> float:
    """Minutes since the newest of `paths` changed (a missing path is ignored; none present = 0, i.e. fresh)."""
    stamps = []
    for p in paths:
        try:
            stamps.append(p.stat().st_mtime)
        except OSError:
            pass
    return (time.time() - max(stamps)) / 60 if stamps else 0.0


@dataclasses.dataclass
class Row:
    name: str                       # the branch, or '(detached) <path name>'
    content: str                    # MERGED | LANDED | CHECK | UNLANDED | DETACHED
    absent: list[str]
    lost: list[str]
    ahead: int
    worktree: str | None
    lock: str | None
    dirty: int
    age_min: float                  # minutes since its last activity (branch reflog, worktree HEAD log, STATUS.md)
    last_commit: str
    report: Report | None = None    # the newest fix-lane report that names it
    state: str = ''
    why: str = ''
    how: str = ''                   # a LANDED row whose own merge would change main: how its work got there (carried)


@dataclasses.dataclass
class Report:
    name: str                       # the file name, w<wave><letter>-PB<lead>-report.md
    status: str                     # DONE | SPLIT | DISCHARGED | BLOCKED | NOT STARTED | '' (plan_wave.parse_report)
    lead: str                       # its lead note
    lead_terminal: bool             # the lead note is landed or retired: the work reached main some other way
    age_min: float                  # minutes since the report was written
    header: str = dataclasses.field(default='', repr=False)


REPORT_SLACK_MIN = 10   # an agent writes its report right after its last STATUS.md rewrite; older than that = resumed since


def lifecycle(row: Row, *, in_flight_slugs: set[str], min_age: int, stale_hours: float = 24,
              live: Callable[[str | None], bool] = live_lock, in_train: dict[str, str] | None = None) -> tuple[str, str]:
    """THE lifecycle rule (module doc), as a pure function of what the survey measured: (state, the evidence)."""
    if live(row.lock):
        return 'IN FLIGHT', f'worktree locked by a live agent ({row.lock or "locked by hand"})'
    if in_train and row.name in in_train and row.content not in LANDED_CONTENT:
        return 'IN FLIGHT', f'a member of train {in_train[row.name]}, whose lander holds the landing lease now'
    slug = next((s for s in sorted(in_flight_slugs) if re.match(re.escape(s) + r'(?![a-z0-9])', row.name.lower())), None)
    if slug:
        return 'IN FLIGHT', f'dispatch ledger: group {slug} is still in flight'
    fresh = row.age_min < min_age
    touched = f'touched {row.age_min:.0f} min ago (under --min-age {min_age})'
    if row.content == 'DETACHED':
        return ('IN FLIGHT', touched) if fresh else (
            'DECISION', f'detached worktree at {row.last_commit}, idle {row.age_min / 60:.1f} h'
            + (f', {row.dirty} uncommitted path(s)' if row.dirty else '') + ': remove it when no fleet reads it')
    if row.content in LANDED_CONTENT and not fresh and not row.dirty:
        return 'LANDED', f'{row.content}{f" ({row.how})" if row.how else ""}: its work is on main; --apply deletes it'
    rep = row.report
    if row.content not in LANDED_CONTENT and rep and rep.status == 'DONE' and rep.age_min <= row.age_min + REPORT_SLACK_MIN:
        if rep.lead_terminal:
            return 'DECISION', (f'{rep.name}: DONE, but {rep.lead} is already landed: superseded (its work reached main '
                                f'through another branch); confirm nothing unique remains, then delete it; ' + evidence(row))
        if rep.age_min > stale_hours * 60:
            return 'DECISION', (f'{rep.name}: DONE {rep.age_min / 60:.0f} h ago and never landed (over {stale_hours:g} h): '
                                f'land it or record why not; ' + evidence(row))
        return 'WAITING TO LAND', f'{rep.name}: Status DONE; a land unit takes it (`--train` names the next train)'
    if fresh:
        return 'IN FLIGHT', touched
    return 'DECISION', evidence(row)


def evidence(row: Row) -> str:
    ev = [row.content + (f' ({len(row.absent)} added file(s) absent from main, e.g. {row.absent[0]})' if row.absent else '')
          + (f' (names not on main: {" ".join(row.lost[:5])})' if row.content == 'CHECK' else ''),
          f'{row.ahead} commit(s) ahead', f'idle {row.age_min / 60:.1f} h', f'last: {row.last_commit}']
    if row.dirty:
        ev.append(f'{row.dirty} uncommitted path(s) in its worktree')
    if row.lock is not None:
        ev.append(f'stale lock ({row.lock})')
    rep = row.report
    ev.append(f'{rep.name}: {rep.status or "no status"} ({rep.lead} {"landed" if rep.lead_terminal else "open"})'
              if rep else 'no fix-lane report names it')
    return '; '.join(ev)


def note_terminal(work_dir: pathlib.Path | None = None) -> Callable[[str], bool]:
    """`terminal(note id)`: the note is landed or retired (work.py's one frontmatter reader and status set)."""
    sys.path.insert(0, str(REPO / 'scripts' / 'spec'))
    import work  # noqa: PLC0415
    status = {i.get('id'): i.get('status') for i in (work.load(work_dir) if work_dir else work.load())}
    return lambda n: status.get(n) in work.TERMINAL_STATUSES


def report_index(reports_dir: pathlib.Path | None, terminal: Callable[[str], bool]) -> list[Report]:
    """Every fix-lane report, oldest first, through plan_wave's reader (REPORT_NAME, the status line), reused and never
    forked: a suffixed name is invisible here exactly as it is to the planner (kb/Work PB2602). The header (up to the
    first `## ` heading, as plan_wave.parse_report reads it) is where a report names its branch and worktree."""
    if not reports_dir or not reports_dir.is_dir():
        return []
    import plan_wave  # noqa: PLC0415  (it imports this module back, lazily: no cycle at import time)
    out = []
    for r in plan_wave.load_reports(reports_dir):
        lines = r.path.read_text(encoding='utf-8', errors='replace').splitlines()
        head = next((i for i, l in enumerate(lines) if i > 0 and l.startswith('## ')), len(lines))
        out.append(Report(r.path.name, r.status, r.lead, terminal(r.lead), age_minutes(r.path), '\n'.join(lines[:head])))
    return out


BRANCH_FIELD = re.compile(r'(?i)\bbranch\b[\s:*`(]*([A-Za-z0-9_][\w.\-/]*[\w])')


def newest_report(reports: list[Report], branch: str | None, path: str | None) -> Report | None:
    """The newest report about this branch. A header with a `branch` field (`Branch: x`, `**branch:** `x``, `(branch x)`)
    is about exactly the branch(es) it names: a header that ALSO names another branch, e.g. as its base or its
    predecessor, is not about that one (2026-10-08: w1037r's report names landing-lease as its base). A header with no
    branch field is about any branch it names as a whole word, or whose worktree directory it names (either slash)."""
    word = re.compile(r'(?<![\w.\-/\\])' + re.escape(branch) + r'(?![\w.\-])') if branch else None
    tree = re.compile(r'worktrees[\\/]+' + re.escape(pathlib.PurePath(path).name) + r'(?![\w.\-])') if path else None
    hit = None
    for rep in reports:
        named = set(BRANCH_FIELD.findall(rep.header))
        if (branch in named) if named else any(p and p.search(rep.header) for p in (word, tree)):
            hit = rep
    return hit


def landing_train_members(coord_dir: pathlib.Path | None = None) -> dict[str, str]:
    """{branch: train label} for the members of the train whose lander holds the landing lease NOW: the manifest
    `<coord>/scratch/train<label>-manifest.json` whose label the lease holder names (`train <label> lander`). They are
    being carried, so no other train takes them (kb/Work PB2708, 2026-10-10: train 1056's five members read WAITING TO
    LAND while their lander carried them, and `--train` picked four of them again)."""
    import landing_lease  # noqa: PLC0415
    cdir = coord_dir or coord.coord_path()
    lease = landing_lease.current(cdir) if cdir.is_dir() else None
    out: dict[str, str] = {}
    for f in (cdir / 'scratch').glob('train*-manifest.json') if lease else ():
        label = f.name[len('train'):-len('-manifest.json')]
        if re.search(rf'\btrain {re.escape(label)}\b', str(lease.get('holder', ''))):
            members = coord.read_json(f, []) or []
            out.update({m['branch']: label for m in members if isinstance(m, dict) and m.get('branch')})
    return out


def in_flight_slugs(coord_dir: pathlib.Path | None, terminal: Callable[[str], bool]) -> set[str]:
    """The `w<wave><letter>` slugs of the dispatch-ledger wave groups still in flight, by the planner's own liveness rule
    (plan_wave.inflight_file_sets, source 3: a group with a note that is not landed or retired). A hand entry has no
    slug (its label is free text); a hand dispatch's worktree is caught by its live lock or its recency instead."""
    import plan_wave  # noqa: PLC0415
    live = plan_wave.inflight_file_sets(coord_dir=coord_dir, with_git=False, terminal=terminal)
    return {m.group(1).lower() for o in live if (m := re.match(r'dispatched (w\d+[A-Za-z0-9]+) \(', o))}


def survey(repo=REPO, *, reports: list[Report] = (), slugs: set[str] = frozenset(), min_age: int = 180,
           stale_hours: float = 24, live: Callable[[str | None], bool] = live_lock,
           in_train: dict[str, str] | None = None, coord_dir: pathlib.Path | None = None) -> list[Row]:
    """Every local branch but main, and every detached worktree, measured and given its lifecycle state. `in_train`:
    {branch: train label} of the train landing now (landing_train_members)."""
    names = main_names(repo)
    trees = worktrees(repo)
    by_branch = {t.branch: t for t in trees if t.branch}
    common = pathlib.Path(git('rev-parse', '--git-common-dir', cwd=repo).stdout.strip())
    if not common.is_absolute():
        common = pathlib.Path(repo) / common

    def measure(b: str) -> Row:
        kind, absent, lost, how = classify(b, names, repo, coord_dir)
        t = by_branch.get(b)
        ahead = int(git('rev-list', '--count', 'origin/main..' + b, cwd=repo).stdout.strip() or 0)
        last = git('log', '-1', '--format=%h %cs %s', b, cwd=repo).stdout.strip()[:90]
        stamps = [common / 'logs' / 'refs' / 'heads' / b]
        dirty = 0
        if t:
            gitdir = pathlib.Path(git('rev-parse', '--absolute-git-dir', cwd=t.path).stdout.strip() or t.path)
            stamps += [gitdir / 'logs' / 'HEAD', pathlib.Path(t.path) / 'STATUS.md']
            dirty = uncommitted(t.path)
        return Row(b, kind, absent, lost, ahead, t.path if t else None, t.lock if t else None, dirty,
                   age_minutes(*stamps), last, newest_report(list(reports), b, t.path if t else None), how=how)

    def measure_detached(t: Tree) -> Row:
        gitdir = pathlib.Path(git('rev-parse', '--absolute-git-dir', cwd=t.path).stdout.strip() or t.path)
        dirty = uncommitted(t.path)
        last = git('log', '-1', '--format=%h %cs', t.head, cwd=repo).stdout.strip() if t.head else '?'
        return Row(f'(detached) {pathlib.PurePath(t.path).name}', 'DETACHED', [], [], 0, t.path, t.lock, dirty,
                   age_minutes(gitdir / 'logs' / 'HEAD', pathlib.Path(t.path) / 'STATUS.md'), last)

    branches = [b for b in git('branch', '--format=%(refname:short)', cwd=repo).stdout.split() if b != 'main']
    with ThreadPoolExecutor(max_workers=16) as pool:   # subprocess-bound: ~1-3 s a branch, 35 s for 29 one by one
        rows = list(pool.map(measure, branches)) + list(pool.map(measure_detached, [t for t in trees if not t.branch]))
    for r in rows:
        r.state, r.why = lifecycle(r, in_flight_slugs=set(slugs), min_age=min_age, stale_hours=stale_hours, live=live,
                                   in_train=in_train)
    return rows


def verdict(rows: list[Row]) -> str:
    n = {s: sum(1 for r in rows if r.state == s) for s in STATES}
    return (f'=== BRANCH LIFECYCLE: {len(rows)} · {n["IN FLIGHT"]} in flight · {n["WAITING TO LAND"]} waiting to land · '
            f'{n["LANDED"]} landed (--apply deletes) · {n["DECISION"]} need a DECISION'
            + (' (owner rule 2026-10-08, kb/Work PB2600: land it or abandon it in its kb/Work note, then delete) ===' if n['DECISION'] else ' ==='))


# ⛔ THE TRAIN SIZE (owner 2026-10-10, kb/Work PB2981): a train carries 4-6 clusters, never every waiting branch. On the
# morning of 2026-10-10 the land unit took all 15 WAITING TO LAND branches (13 clusters) into ONE train: its lander split
# it into three serial agents and nothing reached main for hours; that evening 15 were waiting again ("do not try and do
# 15 branches at once. We'll just hit the same issues as this morning"). So the next train is computed here, and the
# land unit carries exactly it.
TRAIN_MAX = 6


def note_rank(work_dir: pathlib.Path | None = None) -> Callable[[str], tuple[int, int]]:
    """`rank(note id)`: a sort key, most urgent first: minus the note's harm weight (plan_wave.harm_weights, the
    planner's weights), then a compiler note (0) before a process-only one (1). A note main does not hold yet (filed on
    its own branch) ranks as harmless process work."""
    sys.path.insert(0, str(REPO / 'scripts' / 'spec'))
    import work  # noqa: PLC0415
    import plan_wave  # noqa: PLC0415
    weights = plan_wave.harm_weights()
    items = {i.get('id'): i for i in (work.load(work_dir) if work_dir else work.load())}

    def rank(note: str) -> tuple[int, int]:
        i = items.get(note)
        if i is None:
            return (0, 1)
        return (-sum(w for k, w in weights.items() if i.get(k) is True), 1 if i.get('process_only') is True else 0)
    return rank


def next_train(rows: list[Row], rank: Callable[[str], tuple[int, int]], size: int = TRAIN_MAX) -> tuple[list[Row], list[Row]]:
    """(the next train, the rows it leaves WAITING for a later one): the WAITING TO LAND rows ordered by their report's
    lead note (`rank`), then the OLDEST report first, then the name; the first `size` of them."""
    waiting = sorted((r for r in rows if r.state == 'WAITING TO LAND'),
                     key=lambda r: (*rank(r.report.lead), -r.report.age_min, r.name))
    return waiting[:size], waiting[size:]


ABANDONED = re.compile(r'(?i)(?<!not )\babandoned\b')   # "not abandoned" records the opposite decision


def abandon_record(branch: str, work_dir: pathlib.Path) -> str | None:
    """The kb/Work note that records the decision to abandon `branch`: a line naming the branch (as a whole word) and
    the word ABANDONED. None when no note says so (the owner's rule: the decision is recorded BEFORE the delete)."""
    name = re.compile(r'(?<![\w.\-/\\])' + re.escape(branch) + r'(?![\w.\-])')
    for p in sorted(work_dir.glob('*.md')):
        if any(name.search(l) and ABANDONED.search(l) for l in p.read_text(encoding='utf-8', errors='replace').splitlines()):
            return p.stem
    return None


def drop_remote(branch: str, tip: str, *, repo=REPO, say: Callable[[str], None] = print) -> None:
    """Delete `origin/<branch>` with the local branch just removed, when it holds nothing the local one did not (its
    commit is the local tip or an ancestor of it). A squash landing leaves the pushed branch on origin, and nothing else
    ever deleted one (kb/Work PB2708: `note-budget-borrow`, `r1-pb2118`). A remote that moved on is kept, said so."""
    remote = git('rev-parse', '--verify', '--quiet', f'refs/remotes/origin/{branch}', cwd=repo).stdout.strip()
    if not remote:
        return
    if not tip or git('merge-base', '--is-ancestor', remote, tip, cwd=repo).returncode != 0:
        say(f'kept origin/{branch}: it holds commits the local branch did not ({remote[:12]})')
        return
    res = git('push', '-q', 'origin', '--delete', branch, cwd=repo)
    say(f'deleted origin/{branch}' if res.returncode == 0 else
        f'could not delete origin/{branch}: {res.stderr.strip()[:120]}')


def apply(rows: list[Row], *, repo=REPO, include_locked: bool = False, abandon: list[str] = (), min_age: int = 180,
          work_dir: pathlib.Path = REPO / 'kb' / 'Work', say: Callable[[str], None] = print) -> int:
    """Remove the LANDED rows; with --include-locked also a landed, clean, idle worktree that is IN FLIGHT only because
    it was locked by hand (no pid); and each branch named by --abandon that is a clean, idle DECISION row whose kb/Work
    note records the decision to abandon it (abandon_record). Nothing is archived: the classification IS the protection
    (owner 2026-10-08 00:52 PDT, kb/Work PB2600: "Archiving to a bundle serves no use and wastes disk space").
    Returns the count removed."""
    def hand_locked(r: Row) -> bool:
        return r.lock == '' and r.content in LANDED_CONTENT and not r.dirty and r.age_min >= min_age
    doomed = [r for r in rows if r.content != 'DETACHED' and (
        r.state == 'LANDED' or (include_locked and r.state == 'IN FLIGHT' and hand_locked(r)))]
    by_name = {r.name: r for r in rows}
    for b in abandon:
        r = by_name.get(b)
        if r is None or r.state != 'DECISION' or r.dirty or r.content == 'DETACHED':
            say(f'not abandoned: {b} is {"no branch" if r is None else r.state + (" with uncommitted edits" if r and r.dirty else "")}'
                ' (only a clean DECISION row can be abandoned)')
        elif not (note := abandon_record(b, work_dir)):
            say(f'not abandoned: no kb/Work note records the decision to abandon {b} (a line naming it and the word '
                f'ABANDONED, with the reason); record it there first')
        else:
            say(f'abandoning {b}: decision recorded in kb/Work/{note}.md')
            doomed.append(r)
    removed = 0
    for r in doomed:
        if r.worktree:
            # re-measured at the moment of removal: an agent may have started writing since the survey
            if uncommitted(r.worktree):
                say(f'skipped (dirty worktree): {r.name}')
                continue
            if r.lock is not None:
                git('worktree', 'unlock', r.worktree, cwd=repo)
            res = git('worktree', 'remove', '--force', r.worktree, cwd=repo)
            if res.returncode != 0:
                say(f'could not remove the worktree of {r.name}: {res.stderr.strip()[:120]} (stop csharp-ls and re-run)')
                continue
        tip = git('rev-parse', '--verify', '--quiet', f'refs/heads/{r.name}', cwd=repo).stdout.strip()
        res = git('branch', '-D', r.name, cwd=repo)
        if res.returncode != 0:
            say(f'could not delete the branch {r.name}: {res.stderr.strip()[:120]}')
            continue
        removed += 1
        drop_remote(r.name, tip, repo=repo, say=say)
    git('worktree', 'prune', cwd=repo)
    say(f'removed {removed} branches and their worktrees')
    return removed


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('--apply', action='store_true', help='remove the LANDED rows (and each --abandon branch)')
    ap.add_argument('--abandon', action='append', default=[], metavar='BRANCH',
                    help='with --apply: also remove this DECISION branch, once a kb/Work note records it ABANDONED')
    ap.add_argument('--include-locked', action='store_true', help='also take a LANDED worktree locked by hand (no pid)')
    ap.add_argument('--min-age', type=int, default=180, help='a branch touched in the last N minutes is IN FLIGHT (default 180)')
    ap.add_argument('--stale-hours', type=float, default=24,
                    help='a DONE report older than this that never landed needs a DECISION (default 24)')
    ap.add_argument('--reports', type=pathlib.Path, help='the fix-lane reports directory (default coord.reports_dir())')
    ap.add_argument('--brief', action='store_true', help='the verdict line and the WAITING TO LAND and DECISION rows only')
    ap.add_argument('--train', action='store_true',
                    help=f'the next train: at most {TRAIN_MAX} WAITING TO LAND rows, the most harmful first (kb/Work PB2981)')
    ap.add_argument('--json', action='store_true', help='the survey as JSON: every row with its state and evidence')
    ap.add_argument('--no-fetch', action='store_true')
    ap.add_argument('--self-test', action='store_true')
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if not a.no_fetch:
        git('fetch', '-q', 'origin')
    terminal = note_terminal()
    rows = survey(reports=report_index(a.reports or coord.reports_dir(), terminal),
                  slugs=in_flight_slugs(None, terminal), min_age=a.min_age, stale_hours=a.stale_hours,
                  in_train=landing_train_members())
    rank = note_rank()
    train, held = next_train(rows, rank)
    if a.json:
        print(json.dumps({'verdict': verdict(rows), 'rows': [dataclasses.asdict(r) for r in rows],
                          'train': [r.name for r in train], 'held': [r.name for r in held]}, indent=1))
        return 0
    if a.train:
        print(f'=== NEXT TRAIN: {len(train)} of {len(train) + len(held)} WAITING TO LAND (at most {TRAIN_MAX} per train, '
              f'the most harmful lead first; kb/Work PB2981) ===')
        for tag, group in (('TRAIN', train), ('HELD', held)):
            for r in group:
                harm, process = rank(r.report.lead)
                print(f'{tag:6} {r.name} — {r.report.name} ({r.report.lead}: harm {-harm}, '
                      f'{"process" if process else "compiler"}){"; waits for the land unit after this one" if tag == "HELD" else ""}')
        return 0
    for st in STATES:
        for r in sorted((r for r in rows if r.state == st), key=lambda r: r.name):
            if a.brief and st in ('IN FLIGHT', 'LANDED'):
                continue
            print(f'{st:15} {r.name}{" [worktree]" if r.worktree and r.content != "DETACHED" else ""} — {r.why}')
    print(verdict(rows))
    if not a.apply:
        if not a.brief:
            print('dry run: nothing removed. --apply removes the LANDED rows; --apply --abandon <branch> a DECISION row '
                  'whose kb/Work note records it ABANDONED.')
        return 0
    apply(rows, include_locked=a.include_locked, abandon=a.abandon, min_age=a.min_age)
    return 0


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    """A scratch repository with one branch in every lifecycle state, surveyed and then pruned (--apply)."""
    fails = []
    check = lambda ok, what: None if ok else fails.append(what)  # noqa: E731
    tmp = pathlib.Path(tempfile.mkdtemp(prefix='prune-selftest-'))
    try:
        env_strip = subprocess.run(['git', 'rev-parse', '--local-env-vars'], capture_output=True, text=True).stdout.split()
        for v in env_strip:      # never act on the caller's repository (kb/Work PB1719)
            os.environ.pop(v, None)
        origin, repo = tmp / 'origin.git', tmp / 'repo'
        subprocess.run(['git', 'init', '-q', '--bare', '-b', 'main', str(origin)], check=True)
        subprocess.run(['git', 'init', '-q', '-b', 'main', str(repo)], check=True)
        g = lambda *x, cwd=repo: git(*x, cwd=cwd)  # noqa: E731
        for k, v in (('user.name', 'self-test'), ('user.email', 'self-test@example.invalid'), ('commit.gpgsign', 'false')):
            g('config', k, v)

        def commit(path, text, msg, cwd=repo):
            p = pathlib.Path(cwd) / path
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(text, encoding='utf-8')
            g('add', path, cwd=cwd)
            g('commit', '-q', '-m', msg, cwd=cwd)

        commit('src/A.cs', 'class Alpha {}\n', 'base')
        g('remote', 'add', 'origin', str(origin))
        g('push', '-q', 'origin', 'main')
        g('fetch', '-q', 'origin')
        # merged: an ancestor of main. landed-by-content: re-applied on main as a different commit.
        g('branch', 'merged')
        g('switch', '-q', '-c', 'landed-content')
        commit('src/B.cs', 'class Bravo {}\n', 'bravo')
        g('switch', '-q', 'main')
        commit('src/B.cs', 'class Bravo {}\n', 'bravo, as the train re-applied it')
        g('push', '-q', 'origin', 'main')
        g('fetch', '-q', 'origin')
        # unlanded with no report (DECISION), unlanded with a DONE report (WAITING TO LAND), a SPLIT report (DECISION),
        # a DONE report whose lead note is already landed (superseded: DECISION), a DONE report 30 h old (stale:
        # DECISION), a DONE report the branch moved past by an hour (resumed since: not waiting), a ledger slug, a fresh one
        for b, f in (('orphan', 'src/C.cs'), ('done-branch', 'src/D.cs'), ('split-branch', 'src/E.cs'),
                     ('w9999a-ledger', 'src/F.cs'), ('fresh', 'src/G.cs'), ('superseded', 'src/H.cs'),
                     ('stale-done', 'src/I.cs'), ('resumed', 'src/J.cs')):
            g('switch', '-q', '-c', b, 'main')
            commit(f, f'class {b.replace("-", "_").title()}X {{}}\n', b)
        # modified-only: an unlanded change to an EXISTING file that declares no new name (train 1039b review: the
        # name test called it LANDED, and --apply would have deleted it)
        g('switch', '-q', '-c', 'modified-only', 'main')
        commit('src/A.cs', 'class Alpha { int Fixed() => 1; }\n', 'a method-body fix')
        # THE WORK REACHED MAIN AS OTHER COMMITS (kb/Work PB2708). `carried`: train 9999 (its manifest, its trailer
        # commit, its `Train 9999:` commit) took its head and the lander edited it in the train, so its own merge would
        # change main; `carried-moved`: the same, but the branch got a commit after the head the train took;
        # `unlanded-train`: named with its head by the manifest of train 7777, which never landed; `add-del`: a name one
        # commit added and the next deleted; `picked`: its commit cherry-picked onto main, which then edited the file;
        # `picked-lost`: the same, but main renamed the class it declares
        landed_coord = tmp / 'coord-landed'
        (landed_coord / 'scratch').mkdir(parents=True)
        manifest: list[dict] = []

        def train_commit(branch, path, text, cluster):
            tip = g('rev-parse', branch).stdout.strip()
            base = g('merge-base', branch, 'main').stdout.strip()      # where the branch was cut, as train_apply records
            manifest.append({'cluster': cluster, 'branch': branch, 'base': base, 'head': tip})
            # the body a lander rewrote: only the trailer survives (train 1053's 53A and 53G)
            commit(path, text, f'{cluster} PB1: the cluster as the lander edited it\n\nTrain-Cluster: {cluster}\n')
        g('switch', '-q', '-c', 'unlanded-train', 'main')
        commit('src/U.cs', 'class UniformUnlanded {}\n', 'unlanded-train')
        (landed_coord / 'scratch' / 'train7777-manifest.json').write_text(json.dumps(
            [{'cluster': 'K', 'branch': 'unlanded-train', 'base': g('rev-parse', 'main').stdout.strip(),
              'head': g('rev-parse', 'unlanded-train').stdout.strip()}]), encoding='utf-8')
        for b, f, name in (('carried', 'src/K.cs', 'KiloCarried'), ('carried-moved', 'src/M.cs', 'MikeMoved'),
                           ('picked', 'src/P.cs', 'PapaPicked'), ('picked-lost', 'src/Q.cs', 'QuebecLost')):
            g('switch', '-q', '-c', b, 'main')
            commit(f, f'class {name} {{}}\n', b)
        g('switch', '-q', '-c', 'add-del', 'main')
        commit('src/A.cs', 'class Alpha {}\nclass TransientName {}\n', 'a WIP name')
        commit('src/A.cs', 'class Alpha { int Kept() => 2; }\n', 'the WIP name deleted, a body fix kept')
        # `ejected` changes only an existing file (no added file to read UNLANDED by), so the train record decides it
        g('switch', '-q', '-c', 'ejected', 'main')
        commit('src/A.cs', 'class Alpha { int Ejected() => 3; }\n', 'a method-body fix its train ejected')
        g('switch', '-q', 'main')
        # an EARLIER train reusing the letters lands after train 9999's members were cut (train 1056 before train 1055,
        # 2026-10-11), so their base..last range holds its K and E; `ejected` is train 9999's cluster E in the manifest,
        # but its lander ejected it: no E commit in train 9999, and the earlier train's E must not carry it
        commit('src/K0.cs', 'class KiloEarlier {}\n', 'K PB2: an earlier train\'s cluster K\n\nTrain-Cluster: K\n')
        commit('src/E0.cs', 'class EchoEarlier {}\n', 'E PB3: an earlier train\'s cluster E\n\nTrain-Cluster: E\n')
        commit('DEVLOG/9998-train.md', 'train 9998\n', 'Train 9998: DEVLOG 0, the earlier train\'s last commit')
        train_commit('carried', 'src/K.cs', 'class KiloCarried { int EditedInTrain; }\n', 'K')
        k9999 = g('log', '-1', '--format=%h').stdout.strip()
        train_commit('carried-moved', 'src/M.cs', 'class MikeMoved { int EditedInTrain; }\n', 'M')
        manifest.append({'cluster': 'E', 'branch': 'ejected', 'base': g('merge-base', 'ejected', 'main').stdout.strip(),
                         'head': g('rev-parse', 'ejected').stdout.strip()})
        commit('DEVLOG/9999-train.md', 'train 9999\n', 'Train 9999: DEVLOG 1, the train\'s last commit')
        (landed_coord / 'scratch' / 'train9999-manifest.json').write_text(json.dumps(manifest), encoding='utf-8')
        for b, f, text in (('picked', 'src/P.cs', 'class PapaPicked { int LaterOnMain; }\n'),
                           ('picked-lost', 'src/Q.cs', 'class QuebecRenamed {}\n')):
            g('cherry-pick', b)
            commit(f, text, f'main edits the file {b} added')
        g('switch', '-q', 'carried-moved')
        commit('src/M.cs', 'class MikeMoved { int AfterTheTrain; }\n', 'a commit after the head the train took')
        # a pushed branch that lands is deleted on origin too; a remote that moved on is kept
        g('push', '-q', 'origin', 'landed-content')
        g('switch', '-q', '-c', 'tmp-remote', 'merged')
        commit('src/R.cs', 'class RomeoRemoteOnly {}\n', 'a commit only the remote holds')
        g('push', '-q', 'origin', 'tmp-remote:merged')
        g('switch', '-q', 'main')
        g('branch', '-D', 'tmp-remote')
        g('push', '-q', 'origin', 'main')
        g('fetch', '-q', 'origin')
        wt = tmp / 'worktrees'
        # worktrees: dirty-but-merged (DECISION), locked by a live agent, locked by a dead pid (LANDED), detached
        g('worktree', 'add', '-q', '-b', 'dirty-merged', str(wt / 'dirty-merged'), 'main')
        (wt / 'dirty-merged' / 'scratch.txt').write_text('uncommitted\n', encoding='utf-8')
        g('worktree', 'add', '-q', '-b', 'agent-live', str(wt / 'agent-live'), 'main')
        g('worktree', 'lock', '--reason', 'claude agent live (pid 101)', str(wt / 'agent-live'))
        g('worktree', 'add', '-q', '-b', 'agent-dead', str(wt / 'agent-dead'), 'main')
        g('worktree', 'lock', '--reason', 'claude agent dead (pid 102)', str(wt / 'agent-dead'))
        g('worktree', 'add', '-q', '--detach', str(wt / 'pin'), 'main')
        # unreadable: a landed worktree whose status git cannot produce (a corrupt index) holding an untracked file
        g('worktree', 'add', '-q', '-b', 'unreadable', str(wt / 'unreadable'), 'main')
        (wt / 'unreadable' / 'precious.txt').write_text('uncommitted\n', encoding='utf-8')
        (repo / '.git' / 'worktrees' / 'unreadable' / 'index').write_bytes(b'not an index')
        rdir = tmp / 'reports'
        rdir.mkdir()
        hours = lambda h: time.time() - h * 3600  # noqa: E731
        for name, text, h in (
                # the attended shape (kb/Work PB2602): Status, Worktree (a Windows path), Branch, HEAD, Base
                ('w9998a-PB1-report.md', 'Status: DONE\nWorktree: E:\\x\\.claude\\worktrees\\done-wt\n'
                                         'Branch: `done-branch`\nHEAD: abcdef1\nBase: 1234567\n', 10),
                ('w9998b-PB2-report.md', 'SPLIT\nBranch: split-branch\n', 10),
                ('w9998c-PB3-report-v2.md', 'Status: DONE\nBranch: orphan\n', 10),        # suffixed: invisible
                ('w9998d-PB4-report.md', '**Status:** DONE · **branch:** `superseded`\n', 10),
                ('w9998e-PB5-report.md', 'DONE\n**worktree:** `E:/x/.claude/worktrees/stale-wt` · branch stale-done\n', 30),
                ('w9998f-PB6-report.md', 'Status: DONE\nBranch: resumed\n', 11),
                ('w9998g-PB7-report.md', 'Status: DONE\nBranch: other\n\n## Notes\nits predecessor was orphan\n', 10),
                # a header whose branch field names another branch is not about the one it names as its base
                ('w9998h-PB9-report.md', 'Status: DONE\nWorktree: E:\\x\\worktrees\\zz (branch zz)\nBase: orphan\n', 10),
                # PB2708: a carried branch's lead note is still open, so without the train record it read WAITING TO LAND
                ('w9998i-PB10-report.md', 'Status: DONE\nBranch: carried\n', 10),
                ('w9998j-PB11-report.md', 'Status: DONE\nBranch: carried-moved\n', 11)):
            (rdir / name).write_text(text, encoding='utf-8')
            os.utime(rdir / name, (hours(h), hours(h)))
        # age everything but `fresh` past --min-age: the branch reflogs, each worktree's HEAD log
        common = repo / '.git'
        for p in list((common / 'logs').rglob('*')):
            if p.is_file() and p.name != 'fresh':
                os.utime(p, (hours(10), hours(10)))
        for d in (common / 'worktrees').iterdir():
            os.utime(d / 'logs' / 'HEAD', (hours(10), hours(10)))
        os.utime(common / 'logs' / 'refs' / 'heads' / 'stale-done', (hours(40), hours(40)))  # idle since before its report
        alive = {101: 'claude.exe', 102: None}
        live = lambda reason: live_lock(reason, alive=lambda pid: alive.get(pid))  # noqa: E731
        terminal = lambda n: n == 'PB4'  # noqa: E731
        rows = {r.name: r for r in survey(repo, reports=report_index(rdir, terminal), slugs={'w9999a'}, min_age=180,
                                          stale_hours=24, live=live, coord_dir=landed_coord)}
        want = {'merged': 'LANDED', 'landed-content': 'LANDED', 'orphan': 'DECISION', 'done-branch': 'WAITING TO LAND',
                'split-branch': 'DECISION', 'w9999a-ledger': 'IN FLIGHT', 'fresh': 'IN FLIGHT', 'superseded': 'DECISION',
                'stale-done': 'DECISION', 'resumed': 'DECISION',
                'dirty-merged': 'DECISION', 'agent-live': 'IN FLIGHT', 'agent-dead': 'LANDED', '(detached) pin': 'DECISION',
                'modified-only': 'DECISION', 'unreadable': 'DECISION',
                'carried': 'LANDED', 'carried-moved': 'DECISION', 'add-del': 'DECISION', 'picked': 'LANDED',
                'picked-lost': 'DECISION', 'unlanded-train': 'DECISION', 'ejected': 'DECISION'}
        for name, state in want.items():
            got = rows.get(name)
            check(got is not None and got.state == state, f'{name}: want {state}, got {got.state if got else "no row"} ({got.why if got else ""})')
        check(rows['landed-content'].content == 'LANDED', 'a branch re-applied on main classifies LANDED by content')
        check('no fix-lane report' in rows['orphan'].why,
              'a suffixed report name is invisible (plan_wave REPORT_NAME), and a name below the header binds nothing')
        check('SPLIT' in rows['split-branch'].why, 'a DECISION row names its report status')
        check('already landed' in rows['superseded'].why, 'a DONE report whose lead note landed is superseded')
        check('never landed' in rows['stale-done'].why, 'a DONE report over --stale-hours is a DECISION, said so')
        check('uncommitted' in rows['dirty-merged'].why, 'a dirty landed worktree is a DECISION, with the reason')
        check(set(rows) == set(want), f'every branch and detached worktree has a row: {sorted(set(rows) ^ set(want))}')
        check(rows['modified-only'].content == 'CHECK', 'a branch that only modifies an existing file is not LANDED')
        check(rows['unreadable'].dirty > 0, 'a worktree whose status git cannot produce counts as dirty')
        check(f'carried by train 9999 as cluster K ({k9999})' in rows['carried'].why,
              f'a train-carried branch names its own train\'s commit ({k9999}), not an earlier train\'s: {rows["carried"].why}')
        check(rows['ejected'].content == 'CHECK' and not rows['ejected'].how,
              f'an ejected member is not carried by an earlier train\'s commit of its letter: {rows["ejected"].why}')
        check(rows['carried-moved'].content == 'CHECK', 'a branch that moved past the head its train took is not carried')
        check('TransientName' not in rows['add-del'].lost,
              f'a name one commit added and the next deleted is not lost: {rows["add-del"].lost}')
        check('patch-equivalent' in rows['picked'].why, f'a cherry-picked branch says so: {rows["picked"].why}')
        check(rows['picked-lost'].content == 'CHECK' and rows['picked-lost'].lost == ['QuebecLost'],
              f'a cherry-picked branch with a name main lacks stays CHECK: {rows["picked-lost"].lost}')
        # THE TRAIN LANDING NOW (PB2708): its manifest's members are IN FLIGHT, never WAITING TO LAND; the label binds
        # only at a word boundary (train 99990 is not train 9999)
        cdir = tmp / 'coord'
        (cdir / 'scratch').mkdir(parents=True)
        (cdir / 'landing-lease.json').write_text(json.dumps({'holder': 'train 9999 lander', 'worktree': str(tmp),
                                                             'expires_at': '2099-01-01T00:00:00+00:00'}), encoding='utf-8')
        (cdir / 'scratch' / 'train9999-manifest.json').write_text(json.dumps([{'branch': 'done-branch'}]), encoding='utf-8')
        (cdir / 'scratch' / 'train99990-manifest.json').write_text(json.dumps([{'branch': 'orphan'}]), encoding='utf-8')
        in_train = landing_train_members(cdir)
        check(in_train == {'done-branch': '9999'}, f'the members of the train holding the lease: {in_train}')
        check(lifecycle(rows['done-branch'], in_flight_slugs=set(), min_age=180, live=live, in_train=in_train)[0]
              == 'IN FLIGHT', 'a member of the train landing now is IN FLIGHT, not WAITING TO LAND')
        (cdir / 'landing-lease.json').write_text(json.dumps({'holder': 'train 9999 lander', 'worktree': str(tmp),
                                                             'expires_at': '2000-01-01T00:00:00+00:00'}), encoding='utf-8')
        check(landing_train_members(cdir) == {}, 'an expired lease carries no train')
        # THE TRAIN SIZE (kb/Work PB2981): of eight waiting rows the next train is TRAIN_MAX, the most harmful lead
        # first, a compiler fix before process work at equal harm, the oldest report first; the rest are held
        def waiting(name, lead, age):
            return Row(name, 'CHECK', [], [], 1, None, None, 0, 999.0, '',
                       Report(f'w1a-{lead}-report.md', 'DONE', lead, False, age), 'WAITING TO LAND')
        ranks = {'PB1': (-8, 0), 'PB2': (-4, 0), 'PB3': (0, 0), 'PB4': (0, 1)}
        eight = [waiting(f'b{i}', lead, age) for i, (lead, age) in enumerate(
            (('PB4', 500), ('PB3', 10), ('PB3', 90), ('PB1', 5), ('PB4', 50), ('PB2', 7), ('PB3', 30), ('PB4', 900)))]
        train, held = next_train(eight + [dataclasses.replace(eight[0], name='landed-row', state='LANDED')],
                                 lambda n: ranks.get(n, (0, 1)))
        check([r.name for r in train] == ['b3', 'b5', 'b2', 'b6', 'b1', 'b7'] and [r.name for r in held] == ['b0', 'b4'],
              f'the next train is the {TRAIN_MAX} most harmful waiting rows, compiler before process, oldest first: '
              f'{[r.name for r in train]}, held {[r.name for r in held]}')
        check(verdict(list(rows.values())).startswith('=== BRANCH LIFECYCLE: 23 · 3 in flight · 1 waiting to land · 5 landed'),
              f'verdict line: {verdict(list(rows.values()))}')
        # the pure rule: a lock with no pid is honoured; a ledger slug matches only at a word boundary
        r0 = dataclasses.replace(rows['orphan'], lock='')
        check(lifecycle(r0, in_flight_slugs=set(), min_age=180)[0] == 'IN FLIGHT', 'a hand lock (no pid) is live')
        r1 = dataclasses.replace(rows['orphan'], name='w99990-other')
        check(lifecycle(r1, in_flight_slugs={'w9999'}, min_age=180, live=live)[0] == 'DECISION', 'slug w9999 does not bind w99990-…')
        # --apply removes exactly the LANDED rows plus the --abandon branches whose decision a kb/Work note records,
        # archives nothing (owner 2026-10-08 00:52 PDT), and loses nothing else
        notes = tmp / 'Work'
        notes.mkdir()
        (notes / 'PB8.md').write_text('---\nid: PB8\n---\nThe orphan branch: ABANDONED 2026-10-08, the approach was '
                                      'replaced by PB9.\nsplit-branch: not abandoned, a finisher takes it.\n', encoding='utf-8')
        out = []
        n = apply(list(rows.values()), repo=repo, abandon=['orphan', 'split-branch', 'fresh', 'dirty-merged'],
                  work_dir=notes, say=out.append)
        left = set(g('branch', '--format=%(refname:short)').stdout.split())
        check(n == 6, f'--apply removed {n}, want 6 (5 LANDED + orphan): {out}')
        check(not {'merged', 'landed-content', 'agent-dead', 'orphan', 'carried', 'picked'} & left, f'LANDED/abandoned branches remain: {sorted(left)}')
        check({b for b, s in want.items() if s != 'LANDED' and b != 'orphan' and not b.startswith('(')} <= left,
              f'a branch neither landed nor recorded abandoned was removed: {sorted(left)}')
        check(any('no kb/Work note records' in l and 'split-branch' in l for l in out),
              'an --abandon with no recorded decision is refused, said so')
        check(any('fresh is IN FLIGHT' in l for l in out) and any('uncommitted' in l for l in out),
              'an --abandon of an IN FLIGHT or dirty branch is refused, said so')
        check(not list(tmp.rglob('*.bundle')), 'nothing was archived')
        check((wt / 'dirty-merged' / 'scratch.txt').exists(), 'an uncommitted file survived')
        check((wt / 'unreadable' / 'precious.txt').exists(), 'a file in an unmeasurable worktree survived')
        heads = set(g('ls-remote', '--heads', 'origin').stdout.split())
        check('refs/heads/landed-content' not in heads and any('deleted origin/landed-content' in l for l in out),
              f'a landed branch is deleted on origin too: {out}')
        check('refs/heads/merged' in heads and any('kept origin/merged' in l for l in out),
              f'a remote branch holding commits the local one did not is kept: {out}')
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    for f in fails:
        print(f'FAIL  {f}')
    print(f'=== PRUNE_WORKTREES SELF-TEST: {"GREEN" if not fails else f"RED ({len(fails)})"} ===')
    return 1 if fails else 0


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')   # the rows carry '·' and '—'; session-probe reads them as UTF-8
    sys.exit(main())

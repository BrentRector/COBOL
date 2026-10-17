#!/usr/bin/env python3
"""Every local branch and worktree, by CONTENT against origin/main and by LIFECYCLE (who acts on it next); with --apply,
remove the ones whose work is on main, and the ones abandoned by a recorded decision.

    python scripts/prune_worktrees.py                      # dry run: the lifecycle table, nothing touched
    python scripts/prune_worktrees.py --brief              # the verdict line and only the rows someone must act on
    python scripts/prune_worktrees.py --json               # the same survey as JSON (the operator tick reads it)
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
                   `w<wave><letter>`), or it was touched in the last --min-age minutes (its HEAD reflog or STATUS.md).
                   Nobody touches it.
                   A branch whose tip is contained in a fix-lane branch that is IN FLIGHT, waiting or STOPPED (a
                   same-file predecessor, a branch a review finisher merged; contained()) is IN FLIGHT too: it lands
                   through that branch (its row names it, `carrier`), and reads LANDED once it has. Only the first
                   kind has a live owner of its own (owned(): what the planner's FINDINGS arm waits on).
  A FINISHED BRANCH is decided by ONE rule of three coverages, never by a report's age (kb/Work PB3181, PB3125):
    1. its newest fix-lane report (`w<wave><letter>-PB<lead>-report.md`, plan_wave.py REPORT_NAME, under
       coord.reports_dir()) is FINISHED (plan_wave.Report.finished: `Status: DONE`, or DISCHARGED with a HEAD past its
       base) and COVERS the tip: its HEAD is the tip, or every commit after it is mechanical (main merged in cleanly,
       the DEVLOG stamp: `unreviewed_since`, the one coverage rule the review stamp and `enqueue.py land` read). A
       report naming no HEAD, or followed by a commit no report describes (an agent resumed after writing it), covers
       nothing: DECISION, with the count;
    2. its header's review stamp is CLEAN at the tip, or at an older head the same coverage reaches;
    3. the landing lane's newest attempt AT THE TIP (land_lane.view: its head, or the end head its run left).
  WAITING FOR REVIEW  coverage 1 holds, but the header carries no `Review: CLEAN` stamp covering the tip
                   (plan_wave.REVIEW, written by review_findings.py stamp): ONE read-only cobol-reviewer reviews it (the
                   `review` skill's branch form, kb/Work PB3027). The landing step never reviews and never fixes a
                   finding. A stamp at an older head is unreviewed work.
  WAITING TO LAND  coverages 1 and 2 hold and no attempt stopped at this tip (none ran here, one is running, or its exit
                   is one the lane retries by itself: 3 once landing_check.py passes, 2 or 9 once, 8 after the halt; or
                   the enqueue unit re-armed it): the loop's landing lane runs `enqueue.py land <branch>` on it
                   (land_lane.py, kb/Work PB3103, PB3023), one branch at a time, the most harmful lead first (`--brief`
                   lists them in that order, kb/Work PB2981's ranking). (A stamp that says FINDINGS is a DECISION row:
                   the next wave's finisher answers them, plan_wave.py.)
  STOPPED          coverages 1 and 2 hold and the lane's attempt at this tip STOPPED (land_lane's class: a conflict 4, a
                   red gate 5, a DEVLOG refusal 6, UNREVIEWED 11, a repeated exit 2 or 9, a runner that DIED twice): the
                   lane never relaunches it at this head; the enqueue unit sends it to its finisher (or reviewer), whose
                   new tip is reviewed and landed, or re-arms a stop resolved with no commit. The evidence names the
                   attempt and land_lane.ACTION's owner. A branch it contains lands through it (IN FLIGHT).
  ENQUEUED         its pull request is open in GitHub's merge queue, or set to join it (auto-merge on): the queue tests it
                   in a merge group and lands it (kb/Work PB3024); offline, the lane's exit-0 attempt at the tip says
                   the same. Nobody touches it.
  BOUNCED          its pull request is open but the queue REMOVED it (a red merge group, a conflict, a timeout: the
                   reason is GitHub's) and its head is still the branch's tip: the enqueue unit dispatches ONE finisher
                   with the reason named. A finisher that fixes the branch returns DONE and lands nothing: its new tip
                   is reviewed again, then the landing lane lands it. A bounce resolved with no commit (infrastructure,
                   a red already on main) is re-armed by the enqueue unit (`land_lane.py rearm`, kb/Work PB3103), and
                   the lane lands it again at the same head. A tip that moved past the bounced head is the fix, and
                   reads by its report again.
                   The pull-request states come from ONE GraphQL read (`pull_requests`); offline, unauthenticated or with
                   no gh it is empty and the lifecycle is git-only (a non-observation, never a claim that nothing is
                   queued).
  LANDED           its work is on main by content (MERGED or LANDED below) and its worktree holds nothing uncommitted:
                   `--apply` deletes it.
  DECISION         everything else: a person or the operator decides — land it (via a finisher when SPLIT), or ABANDON it
                   with the decision recorded in its kb/Work note — and then it is deleted. Each row prints its evidence.

The CONTENT class (why this exists, 2026-09-30): `git merge-base --is-ancestor` clears only branches whose commits are
literally in main. A train re-applies, rebases and conflict-resolves each cluster's commits, so a LANDED cluster's branch
is never an ancestor and 40+ branches and 19 worktrees piled up from earlier waves. The test that does decide it is
content, and it is derived on each run (a merge-queue landing is a merge commit, so from PB3023 on a branch's
commits are ancestors of main and MERGED decides; the rest stays for the branches trains landed before it):

  MERGED   the branch tip is an ancestor of origin/main.
  LANDED   not an ancestor, but MERGING it into origin/main would change nothing: `git merge-tree --write-tree
           origin/main <branch>` merges cleanly to origin/main's own tree. That is the one proof (train 1039b review):
           the earlier test (every added file and every declared C# name on main) called a branch that only MODIFIED
           existing files LANDED, so an unlanded method-body fix would have been deleted.
           Also LANDED when its own merge would change main but its work reached main as OTHER commits (carried(), kb/Work
           PB2708): a commit of the retired train applier on origin/main brought in exactly its head (a lander that edits a cluster in
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
the decision (a line where the word ABANDONED directly follows the branch's name, with the reason; the word abandons
that branch, never a successor the line names). Each removed branch's `origin/<branch>`
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
from typing import TYPE_CHECKING, Callable

REPO = pathlib.Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / 'scripts' / 'orchestrator'))
import coord  # noqa: E402  (the coordination directory, the live-process probe)
if TYPE_CHECKING:
    from plan_wave import Review  # noqa: E402  (imported lazily at run time: plan_wave imports this module back)
    from land_lane import Lane  # noqa: E402  (likewise: land_lane imports this module back)

DECL = re.compile(r'^\+\s*(?:(?:public|internal|private|protected|static|sealed|abstract|partial|override|readonly|async|virtual|unsafe|new)\s+)*'
                  r'(?:class|record|struct|enum|interface)\s+(?:struct\s+)?(\w+)')
METH = re.compile(r'^\+\s*(?:(?:public|internal|private|protected|static|sealed|abstract|override|readonly|async|virtual|unsafe)\s+)+'
                  r'[\w<>\[\],.?()\s]+?\s+(\w+)\s*\(')
TEST = re.compile(r'^\+\s*public (?:async )?(?:void|Task)\s+(\w+)\s*\(')
LANDED_CONTENT = ('MERGED', 'LANDED')
STATES = ('IN FLIGHT', 'WAITING FOR REVIEW', 'WAITING TO LAND', 'STOPPED', 'ENQUEUED', 'BOUNCED', 'LANDED', 'DECISION')
LOCK_PID = re.compile(r'\(pid (\d+)\)')
SETTINGS = ':!.claude/settings.local.json'   # always dirty in an agent's worktree, never work (2026-10-03)


def git(*args, cwd=REPO, stdin: str | None = None):
    return subprocess.run(['git', '-C', str(cwd), *args], input=stdin, capture_output=True, text=True, encoding='utf-8',
                          errors='replace')


def absent_from(ref: str, paths: list[str], repo=REPO) -> list[str]:
    """The paths `ref` does not hold, from ONE `git cat-file --batch-check` (one output line per input line, `<ref>:<path>
    missing` for an absent one). One `cat-file -e` per path spawned a process per added file: a branch that splits the
    DEVLOG into 1,984 entry files (kb/Work PB3025) held the whole survey past five minutes (kb/Work PB3027, found there)."""
    if not paths:
        return []
    out = git('cat-file', '--batch-check', cwd=repo, stdin=''.join(f'{ref}:{p}\n' for p in paths)).stdout.splitlines()
    if len(out) != len(paths):
        raise RuntimeError(f'git cat-file --batch-check answered {len(out)} lines for {len(paths)} paths')
    return [p for p, line in zip(paths, out) if line.endswith(' missing')]


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
# cluster its branch, base and head) plus, on origin/main, the train's commits: the retired train applier gave each
# cluster's commit a `Train-Cluster: <cluster>` trailer, and the train ends in a `Train <label>: ...` commit. A member whose
# trailer commit is in its train's range (train_clusters: back from the train's last commit to the member's base or to
# another train's commit, whichever is nearer, since trains reuse cluster letters) was brought in, its diff is in main's
# history, and deleting the branch loses nothing even where a later commit withdrew the fix. The commit BODY is not the
# record: landers rewrite it (train 1053's 53A and 53G kept only the trailer). A member with no trailer commit (a train
# before the train applier, or a lander that dropped the trailer: train 1053's 53P) stays a hand decision.
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
    # ANY added file absent from main counts (2026-10-04: a branch that added only scripts/ and docs/ was classified
    # LANDED and would have been deleted)
    absent = absent_from('origin/main', [
        f for f in git('diff', '--name-only', '--diff-filter=A', 'origin/main...' + branch, cwd=repo).stdout.splitlines()
        if f and not f.startswith(('TestResults/', 'tools/')) and f != 'STATUS.md'], repo)
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
    tip: str = ''                   # the branch's tip sha (a review stamp covers exactly one head)
    review_holds: bool = False      # its CLEAN stamp names an older head and every commit since is mechanical
                                    # (unreviewed_since: main's, a clean merge of main, DEVLOG-only)
    report_since: list[str] | None = None   # unreviewed_since(the report's HEAD, tip): [] = the report covers the tip,
                                            # None = it names no head of this branch (kb/Work PB3181)
    carrier: str = ''               # contained(): the branch it lands THROUGH; its IN FLIGHT is that branch's, and
                                    # nobody works on THIS one (owned() excludes it; kb/Work PB3311 review R1-1)


def mechanical(commit: str, repo=REPO) -> bool:
    """A commit that changes nothing a reviewer reads: a merge whose tree IS the clean merge of its two parents (no hand
    resolution: what `enqueue.py land`'s own `git merge origin/main` makes), or a commit that changes only DEVLOG/ (an
    entry, its stamp)."""
    parents = git('rev-list', '--parents', '-n', '1', commit, cwd=repo).stdout.split()[1:]
    if len(parents) == 2:
        clean = git('merge-tree', '--write-tree', *parents, cwd=repo)
        return clean.returncode == 0 and clean.stdout.split()[:1] == [
            git('rev-parse', f'{commit}^{{tree}}', cwd=repo).stdout.strip()]
    if len(parents) == 1:
        paths = git('diff-tree', '--no-commit-id', '--name-only', '-r', commit, cwd=repo).stdout.split()
        return bool(paths) and all(p.startswith('DEVLOG/') for p in paths)
    return False


def unreviewed_since(reviewed: str, tip: str, *, repo=REPO, base: str = 'refs/remotes/origin/main') -> list[str] | None:
    """THE REVIEW'S COVERAGE (MANDATORY-PRACTICES I10, L9; kb/Work PB3027, PB3023), one rule read by this survey and by
    `enqueue.py land`: the commits of `tip` after the `reviewed` head that no reviewer read, newest first — every commit
    not on `base` but a mechanical one (above). [] = the review still covers the tip; None = `reviewed` is not a commit
    of `tip` (unreadable, or another history), so it covers nothing."""
    full = git('rev-parse', '--verify', '--quiet', f'{reviewed}^{{commit}}', cwd=repo).stdout.strip()
    if not full or not tip or git('merge-base', '--is-ancestor', full, tip, cwd=repo).returncode != 0:
        return None
    return [c for c in git('rev-list', tip, f'^{full}', f'^{base}', cwd=repo).stdout.split() if not mechanical(c, repo)]


@dataclasses.dataclass
class Report:
    name: str                       # the file name, w<wave><letter>-PB<lead>-report.md
    status: str                     # DONE | SPLIT | DISCHARGED | BLOCKED | NOT STARTED | '' (plan_wave.parse_report)
    lead: str                       # its lead note
    lead_terminal: bool             # the lead note is landed or retired: the work reached main some other way
    age_min: float                  # minutes since the report was written
    header: str = dataclasses.field(default='', repr=False)
    review: Review | None = None    # the header's branch-review stamp (plan_wave.parse_review, kb/Work PB3027)
    head: str | None = None         # the header's HEAD: the branch tip the report describes (plan_wave.HEAD)
    finished: bool = False          # plan_wave.Report.finished: DONE, or DISCHARGED with a HEAD past its base


def landing_state(row: Row, rep: Report, rv: Review, lane: Lane | None) -> tuple[str, str]:
    """A report-covered, review-CLEAN-covered branch, by the landing lane's newest attempt AT THIS TIP (its head, or the
    end head its run left: main merged in, the DEVLOG stamp) — the third coverage of lifecycle() (kb/Work PB3181,
    PB3125). The lane's own classes (land_lane.classify/effective) and its own ACTION text decide; its status text is
    never parsed. A STOPPED attempt is never WAITING TO LAND: the lane does not relaunch a stop at the same head, so
    only a new head (its finisher's) or the enqueue unit's re-arm lands it."""
    since = '' if rv.covers(row.tip) else ' (later commits mechanical: main merged in, the DEVLOG stamp)'
    clean = f'{rep.name}: Status {rep.status}, Review CLEAN at {rv.sha[:12]} ({rv.reviewer}){since}'
    waiting = ('WAITING TO LAND', clean + '; the landing lane lands it (enqueue.py land)')
    latest = next(((a, c) for a, c in (lane.latest if lane else ()) if a.get('branch') == row.name), None)
    if latest is None:
        return waiting
    import land_lane  # noqa: PLC0415  (scripts/orchestrator: it imports this module back, lazily)
    att, cls = latest
    if cls == 'running':
        return 'WAITING TO LAND', clean + f'; being landed (attempt {att["id"]}, pid {att.get("pid")})'
    if not any(row.tip and h and (row.tip.startswith(h) or h.startswith(row.tip))
               for h in (att.get('head'), att.get('end_head'))):
        return waiting                                  # an attempt at another head: this tip was never tried
    rearm = (lane.rearms or {}).get(row.name) if lane else None
    tried = f'landing attempt {att["id"]} {land_lane.outcome(att)} ({att.get("verdict") or "no verdict line"})'
    if rearm and rearm.get('head') == row.tip:
        return 'WAITING TO LAND', (f'{clean}; {tried}, re-armed by the enqueue unit at this head '
                                   f'({rearm.get("why") or "no reason given"}): the lane lands it again')
    if cls == land_lane.STOPPED:
        return 'STOPPED', (f'{clean}; {tried}: {land_lane.ACTION[land_lane.STOPPED]}'
                           + (' [collected: the enqueue unit has dispatched its owner]' if att.get('collected') else ''))
    if cls == land_lane.DONE:
        return 'ENQUEUED', (f'{tried} at this tip: its pull request went to the merge queue (no pull-request read '
                            'says otherwise): it lands when the queue merges it')
    return 'WAITING TO LAND', f'{clean}; {tried}: {land_lane.ACTION[cls]}'


def lifecycle(row: Row, *, in_flight_slugs: set[str], min_age: int, stale_hours: float = 24,
              live: Callable[[str | None], bool] = live_lock,
              prs: dict[str, PullRequest] | None = None, lane: Lane | None = None) -> tuple[str, str]:
    """THE lifecycle rule (module doc), as a pure function of what the survey measured: (state, the evidence). `lane`:
    the landing lane's view (land_lane.view: each branch's newest attempt and its class, the pending re-arms)."""
    if live(row.lock):
        return 'IN FLIGHT', f'worktree locked by a live agent ({row.lock or "locked by hand"})'
    pr = (prs or {}).get(row.name)
    if pr and row.content not in LANDED_CONTENT:
        if pr.queued:
            return 'ENQUEUED', f'pull request #{pr.number} is in the merge queue ({pr.url}): it lands when the queue merges it'
        if pr.bounced and (not row.tip or row.tip == pr.head):
            return 'BOUNCED', (f'pull request #{pr.number} was removed from the merge queue ({pr.bounced}; {pr.url}): ONE '
                               'finisher fixes the branch and returns DONE; its new tip is reviewed before enqueue.py '
                               'land takes it (kb/Work PB3023)')
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
    if row.content not in LANDED_CONTENT and rep and rep.finished:
        # A FINISHED report (plan_wave.Report.finished) decides the branch only where it COVERS the tip: its HEAD is the
        # tip, or every commit after it is mechanical (unreviewed_since, the coverage rule of the review stamp and of
        # `enqueue.py land`). Never the report's age (kb/Work PB3181: enqueue.py's merge and DEVLOG stamp postdate it).
        if rep.lead_terminal:
            return 'DECISION', (f'{rep.name}: {rep.status}, but {rep.lead} is already landed: superseded (its work '
                                'reached main through another branch); confirm nothing unique remains, then delete it; '
                                + evidence(row))
        if rep.age_min > stale_hours * 60:
            return 'DECISION', (f'{rep.name}: {rep.status} {rep.age_min / 60:.0f} h ago and never landed (over '
                                f'{stale_hours:g} h): land it or record why not; ' + evidence(row))
        uncovered = ('the report names no HEAD, so it describes no commit of the branch' if not rep.head else
                     f'the report\'s HEAD {rep.head[:12]} is not a commit of this branch' if row.report_since is None else
                     f'{len(row.report_since)} commit(s) since the report\'s HEAD {rep.head[:12]} that no report '
                     f'describes (newest {row.report_since[0][:12]}): a resumed agent rewrites its report'
                     if row.report_since else '')
        if uncovered:
            return ('IN FLIGHT', touched) if fresh else ('DECISION', f'{rep.name}: {rep.status}, but {uncovered}; '
                                                         + evidence(row))
        rv = rep.review
        if rv and rv.verdict == 'CLEAN' and (rv.covers(row.tip) or row.review_holds):
            return landing_state(row, rep, rv, lane)
        if rv and rv.verdict == 'FINDINGS' and rv.covers(row.tip):
            return 'DECISION', (f'{rep.name}: {rep.status}, but its branch review found {rv.findings} finding(s) '
                                f'({rv.file}): a finisher answers them, and the next wave plans it (plan_wave.py, '
                                'kb/Work PB3027); ' + evidence(row))
        unreviewed = ('no review stamp' if rv is None else
                      f'Review {rv.verdict} at {rv.sha[:12]}, but the branch is at {row.tip[:12] or "?"}')
        return 'WAITING FOR REVIEW', (f'{rep.name}: Status {rep.status}, {unreviewed}: one cobol-reviewer reviews it (the '
                                      'review skill\'s branch form, kb/Work PB3027)')
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
        header = plan_wave.header_lines(r.path.read_text(encoding='utf-8', errors='replace').splitlines())
        out.append(Report(r.path.name, r.status, r.lead, terminal(r.lead), age_minutes(r.path), '\n'.join(header),
                          r.review, r.head, r.finished))
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


@dataclasses.dataclass
class PullRequest:
    number: int
    url: str
    head: str                       # the PR's head commit
    queued: bool                    # in the merge queue, or auto-merge on (it joins the queue when its checks allow)
    bounced: str = ''               # the queue removed it: GitHub's reason (the newest RemovedFromMergeQueueEvent)


PR_QUERY = ('query($owner:String!,$name:String!){repository(owner:$owner,name:$name){pullRequests(states:OPEN,'
            'baseRefName:"main",first:100){nodes{number url headRefName headRefOid isInMergeQueue '
            'autoMergeRequest{enabledAt} timelineItems(itemTypes:[REMOVED_FROM_MERGE_QUEUE_EVENT],last:1){nodes{'
            '... on RemovedFromMergeQueueEvent{reason createdAt}}}}}}}')


def pull_requests(gh=None) -> dict[str, PullRequest]:
    """{head branch: its OPEN pull request into main}, from ONE GraphQL read (kb/Work PB3023): whether the merge queue
    holds it and, when the queue removed it, GitHub's reason. Offline, unauthenticated or with no gh it is {} and the
    lifecycle is git-only (a non-observation, never a claim that nothing is queued)."""
    import ruleset  # noqa: PLC0415  (its gh runner: a hang guard, and Unobserved on any failure to read)
    run = gh or ruleset.real_gh
    try:
        rc, out, _ = run(['repo', 'view', '--json', 'nameWithOwner', '--jq', '.nameWithOwner'])
        owner, _, name = out.strip().partition('/')
        if rc != 0 or not name:
            return {}
        rc, out, _ = run(['api', 'graphql', '-f', f'query={PR_QUERY}', '-F', f'owner={owner}', '-F', f'name={name}'])
        nodes = json.loads(out)['data']['repository']['pullRequests']['nodes'] if rc == 0 else []
    except (ruleset.Unobserved, ValueError, KeyError, TypeError):
        return {}
    prs: dict[str, PullRequest] = {}
    for n in nodes if isinstance(nodes, list) else ():
        if not isinstance(n, dict) or not n.get('headRefName'):
            continue
        removed = [e for e in ((n.get('timelineItems') or {}).get('nodes') or []) if isinstance(e, dict) and e]
        queued = bool(n.get('isInMergeQueue')) or bool(n.get('autoMergeRequest'))
        prs[str(n['headRefName'])] = PullRequest(
            int(n.get('number') or 0), str(n.get('url') or ''), str(n.get('headRefOid') or ''), queued,
            '' if queued or not removed else f"removed from the queue: {removed[-1].get('reason') or 'no reason given'} "
                                             f"at {removed[-1].get('createdAt') or '?'}")
    return prs


def in_flight_slugs(coord_dir: pathlib.Path | None, terminal: Callable[[str], bool],
                    reports_dir: pathlib.Path | None = None) -> set[str]:
    """The `w<wave><letter>` slugs of the dispatch-ledger wave groups still in flight, by the planner's own liveness rule
    (plan_wave.inflight_file_sets, source 3: a group with a note that is not landed or retired and that no report in
    `reports_dir` answers, kb/Work PB2795), read from its `meta` (each entry's recorded slug), never parsed back out of
    an owner label. A dispatch made without a rendered group has no ledger entry (kb/Work PB3186); its worktree is
    caught by its live lock or its recency instead."""
    import plan_wave  # noqa: PLC0415
    meta: dict[str, dict] = {}
    plan_wave.inflight_file_sets(coord_dir=coord_dir, with_git=False, terminal=terminal, meta=meta,
                                 reports=plan_wave.load_reports(reports_dir) if reports_dir else ())
    return {m['slug'] for m in meta.values() if m.get('slug')}


def live_survey(repo=REPO, *, coord_dir: pathlib.Path | None = None, reports_dir: pathlib.Path | None = None,
                terminal: Callable[[str], bool] | None = None, min_age: int = 180, stale_hours: float = 24,
                live: Callable[[str | None], bool] = live_lock, gh=None) -> list[Row]:
    """THE lifecycle as every production reader takes it: this module's main, the landing lane (land_lane.survey) and
    the planner's file-set partition (plan_wave.main) all call this, never `survey` with arguments of their own, so
    each reads the fix-lane reports, the dispatch ledger's in-flight slugs and the OPEN PULL REQUESTS the same way. A
    reader that surveyed without the pull requests could never see ENQUEUED from GitHub or BOUNCED at all, and would
    plan over a branch the merge queue rejected as if it were still queued (kb/Work PB2807, branch review R1-2).
    `gh`: pull_requests' runner (a self-test fake). The caller fetches origin first when it needs a current main."""
    terminal = terminal or note_terminal()
    rdir = reports_dir or coord.reports_dir(str(coord_dir) if coord_dir else None)
    return survey(repo, reports=report_index(rdir, terminal), slugs=in_flight_slugs(coord_dir, terminal, rdir),
                  min_age=min_age, stale_hours=stale_hours, live=live, prs=pull_requests(gh), coord_dir=coord_dir)


def survey(repo=REPO, *, prs: dict[str, PullRequest], reports: list[Report] = (), slugs: set[str] = frozenset(),
           min_age: int = 180, stale_hours: float = 24, live: Callable[[str | None], bool] = live_lock,
           coord_dir: pathlib.Path | None = None) -> list[Row]:
    """Every local branch but main, and every detached worktree, measured and given its lifecycle state. `prs`:
    {branch: its open pull request into main} (pull_requests), REQUIRED so no caller reads a git-only lifecycle by
    omission; production readers call live_survey, and a self-test passes its own (`{}` is an explicit offline)."""
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
                   age_minutes(*stamps), last, newest_report(list(reports), b, t.path if t else None), how=how,
                   tip=git('rev-parse', '--verify', '--quiet', f'refs/heads/{b}', cwd=repo).stdout.strip())

    def measure_detached(t: Tree) -> Row:
        gitdir = pathlib.Path(git('rev-parse', '--absolute-git-dir', cwd=t.path).stdout.strip() or t.path)
        dirty = uncommitted(t.path)
        last = git('log', '-1', '--format=%h %cs', t.head, cwd=repo).stdout.strip() if t.head else '?'
        return Row(f'(detached) {pathlib.PurePath(t.path).name}', 'DETACHED', [], [], 0, t.path, t.lock, dirty,
                   age_minutes(gitdir / 'logs' / 'HEAD', pathlib.Path(t.path) / 'STATUS.md'), last)

    branches = [b for b in git('branch', '--format=%(refname:short)', cwd=repo).stdout.split() if b != 'main']
    with ThreadPoolExecutor(max_workers=16) as pool:   # subprocess-bound: ~1-3 s a branch, 35 s for 29 one by one
        rows = list(pool.map(measure, branches)) + list(pool.map(measure_detached, [t for t in trees if not t.branch]))
    import land_lane  # noqa: PLC0415  (scripts/orchestrator: it imports this module back; its attempt reader, never its text)
    lane = land_lane.view(pathlib.Path(coord_dir) if coord_dir else coord.coord_path())
    for r in rows:
        rv = r.report.review if r.report else None
        if rv and rv.verdict == 'CLEAN' and r.tip and not rv.covers(r.tip):
            r.review_holds = unreviewed_since(rv.sha, r.tip, repo=repo) == []
        if r.content not in LANDED_CONTENT and r.report and r.report.finished and r.report.head and r.tip:
            r.report_since = unreviewed_since(r.report.head, r.tip, repo=repo)
        r.state, r.why = lifecycle(r, in_flight_slugs=set(slugs), min_age=min_age, stale_hours=stale_hours, live=live,
                                   prs=prs, lane=lane)
    contained(rows, repo)
    return rows


#: The states whose branch has an owner and lands, so a branch it contains lands through it. STOPPED is one (kb/Work
#: PB3125): its landing attempt stopped, the enqueue unit's finisher owns it, and its predecessors wait with it.
CARRIER_STATES = ('IN FLIGHT', 'WAITING FOR REVIEW', 'WAITING TO LAND', 'STOPPED', 'ENQUEUED', 'BOUNCED')


def contained(rows: list[Row], repo=REPO) -> None:
    """A branch that lands THROUGH another one is IN FLIGHT, not a decision and not a second landing: its tip is an
    ancestor of the tip of a fix-lane branch (one a report names) that is IN FLIGHT, waiting or STOPPED
    (CARRIER_STATES). That is a same-file predecessor (MANDATORY-PRACTICES O2: it lands through its successor) and a
    branch a review finisher merged (kb/Work PB3027: the wave's finisher rewrites the report for its own branch; a
    later wave's finisher writes a new report and leaves the FINDINGS-stamped one behind). Once the carrier lands, the
    content test reads it LANDED and --apply deletes it. Two names for one commit carry nothing."""
    carriers = [c for c in rows if c.report and c.tip and c.state in CARRIER_STATES]
    for r in rows:
        findings = bool(r.report and r.report.review and r.report.review.verdict == 'FINDINGS')
        if not r.tip or r.dirty or r.content in LANDED_CONTENT or r.state not in ('DECISION', *CARRIER_STATES[1:]) \
                or (r.state == 'DECISION' and r.report is not None and not findings):
            continue
        c = next((c for c in carriers if c.tip != r.tip
                  and git('merge-base', '--is-ancestor', r.tip, c.tip, cwd=repo).returncode == 0), None)
        if c:
            r.state, r.carrier = 'IN FLIGHT', c.name
            r.why = f'contained in {c.name} ({c.state}): its work lands through that branch; ' + evidence(r)


def owned(rows: list[Row]) -> set[str]:
    """The branches with a live owner of their OWN: IN FLIGHT by lifecycle() (a live agent's lock, its dispatch still
    out, a touch inside --min-age, which is how a resumed branch whose tip moved past its report reads), never by
    contained(). A contained branch reads IN FLIGHT because its work lands through its carrier, and the carrier may be
    any finished branch that merged it (a same-file successor of ANOTHER note, MANDATORY-PRACTICES O2): that says
    nothing about who works on THIS branch. The planner's FINDINGS arm reads this set (plan_wave.plan `owned`, kb/Work
    PB3311): the bare state made a carried FINDINGS branch wait for an answer nobody was writing (review R1-1)."""
    return {r.name for r in rows if r.state == 'IN FLIGHT' and not r.carrier}


def verdict(rows: list[Row]) -> str:
    n = {s: sum(1 for r in rows if r.state == s) for s in STATES}
    return (f'=== BRANCH LIFECYCLE: {len(rows)} · {n["IN FLIGHT"]} in flight · {n["WAITING FOR REVIEW"]} waiting for '
            f'review · {n["WAITING TO LAND"]} waiting to land · {n["STOPPED"]} stopped · {n["ENQUEUED"]} enqueued · '
            f'{n["BOUNCED"]} bounced · '
            f'{n["LANDED"]} landed (--apply deletes) · {n["DECISION"]} need a DECISION'
            + (' (owner rule 2026-10-08, kb/Work PB2600: land it or abandon it in its kb/Work note, then delete) ===' if n['DECISION'] else ' ==='))


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


def landing_order(rows: list[Row], rank: Callable[[str], tuple[int, int]]) -> list[Row]:
    """The WAITING TO LAND rows in the order the landing lane lands them, one at a time (kb/Work PB2981's ranking, kept
    by PB3023): by their report's lead note (`rank`: the most harmful first, a compiler fix before process work), then
    the OLDEST report first, then the name."""
    return sorted((r for r in rows if r.state == 'WAITING TO LAND'),
                  key=lambda r: (*rank(r.report.lead), -r.report.age_min, r.name))


#: What may stand between a branch's name and the word ABANDONED in its record: markup and punctuation, one
#: parenthetical ("(wave 1077 group E, SPLIT)"), and the copula or the noun the record puts there ("branch `x` is
#: ABANDONED"). Any other word, above all another branch's name, means the word is about something else; so does
#: `not`, which is why "not abandoned" (the opposite decision) cannot match.
RECORD_GAP = r'(?:[\s`*_:,;"\'’]|\([^()]*\)|\b(?:is|was|are|now|branch|worktree)\b)*'


def branch_name(branch: str) -> re.Pattern[str]:
    """`branch` as a whole word: no name character (word, `.`, `-`) on either side, nor a path separator before it."""
    return re.compile(r'(?<![\w.\-/\\])' + re.escape(branch) + r'(?![\w.\-])')


def abandon_record(branch: str, work_dir: pathlib.Path) -> str | None:
    """The kb/Work note that records the decision to abandon `branch`: a line where the word ABANDONED directly
    follows the branch (as a whole word), with only `RECORD_GAP` between them. A record names a successor too ("`old`
    ABANDONED, superseded by `new`", or "`new` carries it, so `old` is ABANDONED"), and that line must not pass for a
    decision to abandon `new`. The rule reads the line alone: a version that asked the survey which other names were
    branches stopped refusing once `old` itself was deleted, because the next survey no longer knew `old` (kb/Work
    PB3161, review finding R1-1). None when no note says so (the owner's rule: the decision is recorded BEFORE the
    delete)."""
    record = re.compile(branch_name(branch).pattern + RECORD_GAP + r'\babandoned\b', re.I)
    for p in sorted(work_dir.glob('*.md')):
        if any(record.search(l) for l in p.read_text(encoding='utf-8', errors='replace').splitlines()):
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
            say(f'not abandoned: no kb/Work note records the decision to abandon {b} (a line where the word ABANDONED '
                f'directly follows its name, with the reason); record it there first')
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
    ap.add_argument('--brief', action='store_true',
                    help='the verdict line and the rows someone must act on: WAITING FOR REVIEW, WAITING TO LAND (in '
                         'landing order), STOPPED, BOUNCED and DECISION')
    ap.add_argument('--json', action='store_true', help='the survey as JSON: every row with its state and evidence')
    ap.add_argument('--no-fetch', action='store_true')
    ap.add_argument('--self-test', action='store_true')
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if not a.no_fetch:
        git('fetch', '-q', 'origin')
    rows = live_survey(reports_dir=a.reports, min_age=a.min_age, stale_hours=a.stale_hours)
    order = landing_order(rows, note_rank())
    if a.json:
        print(json.dumps({'verdict': verdict(rows), 'rows': [dataclasses.asdict(r) for r in rows],
                          'landing_order': [r.name for r in order]}, indent=1))
        return 0
    for st in STATES:
        for r in order if st == 'WAITING TO LAND' else sorted((r for r in rows if r.state == st), key=lambda r: r.name):
            if a.brief and st in ('IN FLIGHT', 'ENQUEUED', 'LANDED'):
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
            p.write_text(text, encoding='utf-8', newline="\n")
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
        # DECISION), a DONE report followed by a commit no report describes (resumed since: DECISION, whatever the
        # report's age, kb/Work PB3181), a ledger slug, a fresh one
        main_at = g('rev-parse', 'main').stdout.strip()
        for b, f in (('orphan', 'src/C.cs'), ('done-branch', 'src/D.cs'), ('split-branch', 'src/E.cs'),
                     ('w9999a-ledger', 'src/F.cs'), ('fresh', 'src/G.cs'), ('superseded', 'src/H.cs'),
                     ('stale-done', 'src/I.cs'), ('resumed', 'src/J.cs'),
                     # THE BRANCH REVIEW (kb/Work PB3027): DONE with no stamp, a FINDINGS stamp at its tip, a CLEAN
                     # stamp at a head the branch has since moved past; `pred` carries no report and lands through
                     # `succ`, which was cut from it and is DONE and reviewed CLEAN
                     ('unreviewed', 'src/N.cs'), ('review-findings', 'src/O.cs'), ('reviewed-moved', 'src/S.cs'),
                     ('pred', 'src/T.cs'), ('fnd-pred', 'src/W.cs'), ('reviewed-stamped', 'src/Y.cs'),
                     # ONE RULE FOR A FINISHED BRANCH (kb/Work PB3181, PB3125): the report's coverage, the review's,
                     # and the landing lane's newest attempt at the tip. `lane-pred` carries no report and lands
                     # through `lane-stopped`, cut from it; the other `lane-*` are each one attempt's outcome; a
                     # DISCHARGED report with branch work, one whose HEAD is its base, and a DONE report naming no HEAD
                     ('lane-pred', 'src/LP.cs'), ('lane-free', 'src/LF.cs'), ('lane-done', 'src/LD.cs'),
                     ('lane-running', 'src/LR.cs'), ('discharged', 'src/DS.cs'), ('discharged-empty', 'src/DE.cs'),
                     ('no-head', 'src/NH.cs')):
            g('switch', '-q', '-c', b, 'main')
            commit(f, f'class {b.replace("-", "_").title()}X {{}}\n', b)
        g('switch', '-q', '-c', 'lane-stopped', 'lane-pred')
        commit('src/LS.cs', 'class LaneStoppedX {}\n', 'lane-stopped')
        lane_at = {b: g('rev-parse', b).stdout.strip() for b in ('lane-stopped', 'lane-free', 'lane-done', 'lane-running',
                                                                  'discharged', 'discharged-empty', 'no-head')}
        resumed_at = g('rev-parse', 'resumed').stdout.strip()
        g('switch', '-q', 'resumed')
        commit('src/J.cs', 'class ResumedX { int AfterTheReport; }\n', 'a commit no report describes')
        reviewed_at = g('rev-parse', 'reviewed-moved').stdout.strip()
        g('switch', '-q', 'reviewed-moved')
        commit('src/S.cs', 'class ReviewedMovedX { int AfterTheReview; }\n', 'a commit after the review')
        # `reviewed-stamped`: CLEAN at a head, then only the DEVLOG stamp enqueue.py makes: still reviewed (kb/Work PB3023)
        stamped_at = g('rev-parse', 'reviewed-stamped').stdout.strip()
        g('switch', '-q', 'reviewed-stamped')
        commit('DEVLOG/0042-the-fix.md', '## Entry 42 — 2026-10-11 20:00 PDT — the fix\n\nWhy.\n', 'DEVLOG 42: stamp')
        g('switch', '-q', '-c', 'succ', 'pred')
        commit('src/V.cs', 'class SuccessorX {}\n', 'succ')
        # `fnd-pred` was reviewed FINDINGS; a later wave's finisher `fnd-fix` merged it and was reviewed CLEAN
        g('switch', '-q', '-c', 'fnd-fix', 'fnd-pred')
        commit('src/W2.cs', 'class FindingsFixedX {}\n', 'the findings answered')
        tips = {b: g('rev-parse', b).stdout.strip() for b in ('done-branch', 'review-findings', 'succ', 'fnd-pred', 'fnd-fix',
                                                                'unreviewed', 'reviewed-moved')}
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
            base = g('merge-base', branch, 'main').stdout.strip()      # where the branch was cut, as the train applier recorded
            manifest.append({'cluster': cluster, 'branch': branch, 'base': base, 'head': tip})
            # the body a lander rewrote: only the trailer survives (train 1053's 53A and 53G)
            commit(path, text, f'{cluster} PB1: the cluster as the lander edited it\n\nTrain-Cluster: {cluster}\n')
        g('switch', '-q', '-c', 'unlanded-train', 'main')
        commit('src/U.cs', 'class UniformUnlanded {}\n', 'unlanded-train')
        (landed_coord / 'scratch' / 'train7777-manifest.json').write_text(json.dumps(
            [{'cluster': 'K', 'branch': 'unlanded-train', 'base': g('rev-parse', 'main').stdout.strip(),
              'head': g('rev-parse', 'unlanded-train').stdout.strip()}]), encoding='utf-8', newline="\n")
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
        (landed_coord / 'scratch' / 'train9999-manifest.json').write_text(json.dumps(manifest), encoding='utf-8', newline="\n")
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
        # `lane-stopped` got what `enqueue.py land` commits before its gate: origin/main merged in cleanly, the DEVLOG
        # stamp; its attempt then stopped at that tip with exit 5 (GATE RED). The attempts are written by the lane's own
        # writer (land_lane.launch) and completed in its runner's record shape (run_attempt: rc, verdict, end_head).
        g('switch', '-q', 'lane-stopped')
        g('merge', '-q', '--no-edit', 'origin/main')
        commit('DEVLOG/0043-lane.md', '## Entry 43 — 2026-10-15 12:00 PDT — lane\n\nWhy.\n', 'DEVLOG 43: stamp the entry')
        g('switch', '-q', 'main')
        import land_lane  # noqa: PLC0415  (scripts/orchestrator: it imports this module back)

        def attempt(branch: str, head: str, rc: int | None, verdict: str = '', end_head: str | None = None) -> None:
            rec = land_lane.launch(landed_coord, repo, {'name': branch, 'tip': head, 'worktree': str(repo)}, 'self-test',
                                   lambda argv, log: 4242)
            if rc is not None:
                rec.update(rc=rc, verdict=verdict, ended='2026-10-15T19:00:00+00:00', end_head=end_head or head)
                land_lane.save(rec)
        attempt('lane-stopped', lane_at['lane-stopped'], 5, 'GATE RED (Linux) on x',
                g('rev-parse', 'lane-stopped').stdout.strip())
        attempt('lane-free', lane_at['lane-free'], 3, 'NOT QUEUED (exit 3, above)')
        attempt('lane-done', lane_at['lane-done'], 0, 'ENQUEUED')
        attempt('lane-running', lane_at['lane-running'], None)
        attempt('done-branch', 'an-older-head', 5, 'GATE RED (Windows) on an-older-head')   # not at this tip: ignored
        wt = tmp / 'worktrees'
        # worktrees: dirty-but-merged (DECISION), locked by a live agent, locked by a dead pid (LANDED), detached
        g('worktree', 'add', '-q', '-b', 'dirty-merged', str(wt / 'dirty-merged'), 'main')
        (wt / 'dirty-merged' / 'scratch.txt').write_text('uncommitted\n', encoding='utf-8', newline="\n")
        g('worktree', 'add', '-q', '-b', 'agent-live', str(wt / 'agent-live'), 'main')
        g('worktree', 'lock', '--reason', 'claude agent live (pid 101)', str(wt / 'agent-live'))
        g('worktree', 'add', '-q', '-b', 'agent-dead', str(wt / 'agent-dead'), 'main')
        g('worktree', 'lock', '--reason', 'claude agent dead (pid 102)', str(wt / 'agent-dead'))
        g('worktree', 'add', '-q', '--detach', str(wt / 'pin'), 'main')
        # unreadable: a landed worktree whose status git cannot produce (a corrupt index) holding an untracked file
        g('worktree', 'add', '-q', '-b', 'unreadable', str(wt / 'unreadable'), 'main')
        (wt / 'unreadable' / 'precious.txt').write_text('uncommitted\n', encoding='utf-8', newline="\n")
        (repo / '.git' / 'worktrees' / 'unreadable' / 'index').write_bytes(b'not an index')
        rdir = tmp / 'reports'
        rdir.mkdir()
        hours = lambda h: time.time() - h * 3600  # noqa: E731
        for name, text, h in (
                # the attended shape (kb/Work PB2602): Status, Worktree (a Windows path), Branch, HEAD, Base
                ('w9998a-PB1-report.md', 'Status: DONE\nWorktree: E:\\x\\.claude\\worktrees\\done-wt\n'
                                         f'Branch: `done-branch`\nHEAD: {tips["done-branch"][:9]}\nBase: 1234567\n'
                                         f'**Review:** CLEAN w9998a-review-r1 {tips["done-branch"]}\n', 10),
                ('w9998k-PB12-report.md', f'Status: DONE\nBranch: unreviewed · head {tips["unreviewed"][:12]}\n', 10),
                ('w9998l-PB13-report.md', f'Status: DONE\nBranch: review-findings\nHEAD: {tips["review-findings"]}\n'
                                          f'**Review:** FINDINGS 2 w9998l-review-r1 {tips["review-findings"]} '
                                          'E:/s/review-w9998-l.jsonl\n', 10),
                # rewritten for the commit after the review: the report covers the tip, the stamp does not
                ('w9998m-PB14-report.md', f'Status: DONE\nBranch: reviewed-moved\nHEAD: {tips["reviewed-moved"]}\n'
                                          f'**Review:** CLEAN w9998m-review-r1 {reviewed_at[:12]}\n', 10),
                ('w9998n-PB15-report.md', f'Status: DONE\nBranch: succ\nHEAD: {tips["succ"]}\n'
                                          f'**Review:** CLEAN w9998n-r1 {tips["succ"]}\n', 10),
                ('w9998q-PB17-report.md', f'Status: DONE\nBranch: reviewed-stamped\nHEAD: {stamped_at[:12]}\n'
                                          f'**Review:** CLEAN w9998q-review-r1 {stamped_at[:12]}\n', 10),
                ('w9998o-PB16-report.md', f'Status: DONE\nBranch: fnd-pred\nHEAD: {tips["fnd-pred"]}\n'
                                          f'**Review:** FINDINGS 1 w9998o-r1 {tips["fnd-pred"]} E:/s/review-w9998-o.jsonl\n', 10),
                ('w9998p-PB16-report.md', f'Status: DONE\nBranch: fnd-fix\nHEAD: {tips["fnd-fix"]}\n'
                                          f'**Review:** CLEAN w9998p-r1 {tips["fnd-fix"]}\n', 10),
                # kb/Work PB3181, PB3125: the report, the review and the landing lane's attempt at the tip
                ('w9998r-PB18-report.md', f'Status: DONE\nBranch: lane-stopped · head {lane_at["lane-stopped"][:12]}\n'
                                          f'**Review:** CLEAN w9998r-r1 {lane_at["lane-stopped"]}\n', 10),
                ('w9998s-PB19-report.md', f'Status: DONE\nBranch: lane-free\nHEAD: {lane_at["lane-free"]}\n'
                                          f'**Review:** CLEAN w9998s-r1 {lane_at["lane-free"]}\n', 10),
                ('w9998t-PB20-report.md', f'Status: DONE\nBranch: lane-done\nHEAD: {lane_at["lane-done"]}\n'
                                          f'**Review:** CLEAN w9998t-r1 {lane_at["lane-done"]}\n', 10),
                ('w9998u-PB21-report.md', f'Status: DONE\nBranch: lane-running\nHEAD: {lane_at["lane-running"]}\n'
                                          f'**Review:** CLEAN w9998u-r1 {lane_at["lane-running"]}\n', 10),
                ('w9998v-PB22-report.md', f'**Status:** DISCHARGED · **branch:** `discharged` · **base:** `{main_at[:9]}` · '
                                          f'**HEAD:** `{lane_at["discharged"][:9]}`\n'
                                          f'**Review:** CLEAN w9998v-r1 {lane_at["discharged"]}\n', 10),
                ('w9998w-PB23-report.md', f'Status: DISCHARGED\nBranch: discharged-empty · base {main_at} · head {main_at}\n'
                                          f'**Review:** CLEAN w9998w-r1 {lane_at["discharged-empty"]}\n', 10),
                ('w9998x-PB24-report.md', 'Status: DONE\nBranch: no-head\n'
                                          f'**Review:** CLEAN w9998x-r1 {lane_at["no-head"]}\n', 10),
                ('w9998b-PB2-report.md', 'SPLIT\nBranch: split-branch\n', 10),
                ('w9998c-PB3-report-v2.md', 'Status: DONE\nBranch: orphan\n', 10),        # suffixed: invisible
                ('w9998d-PB4-report.md', '**Status:** DONE · **branch:** `superseded`\n', 10),
                ('w9998e-PB5-report.md', 'DONE\n**worktree:** `E:/x/.claude/worktrees/stale-wt` · branch stale-done\n', 30),
                ('w9998f-PB6-report.md', f'Status: DONE\nBranch: resumed\nHEAD: {resumed_at}\n', 10),
                ('w9998g-PB7-report.md', 'Status: DONE\nBranch: other\n\n## Notes\nits predecessor was orphan\n', 10),
                # a header whose branch field names another branch is not about the one it names as its base
                ('w9998h-PB9-report.md', 'Status: DONE\nWorktree: E:\\x\\worktrees\\zz (branch zz)\nBase: orphan\n', 10),
                # PB2708: a carried branch's lead note is still open, so without the train record it read WAITING TO LAND
                ('w9998i-PB10-report.md', 'Status: DONE\nBranch: carried\n', 10),
                ('w9998j-PB11-report.md', 'Status: DONE\nBranch: carried-moved\n', 11)):
            (rdir / name).write_text(text, encoding='utf-8', newline="\n")
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
                                          stale_hours=24, live=live, coord_dir=landed_coord, prs={})}
        want = {'merged': 'LANDED', 'landed-content': 'LANDED', 'orphan': 'DECISION', 'done-branch': 'WAITING TO LAND',
                'split-branch': 'DECISION', 'w9999a-ledger': 'IN FLIGHT', 'fresh': 'IN FLIGHT', 'superseded': 'DECISION',
                'stale-done': 'DECISION', 'resumed': 'DECISION',
                'dirty-merged': 'DECISION', 'agent-live': 'IN FLIGHT', 'agent-dead': 'LANDED', '(detached) pin': 'DECISION',
                'modified-only': 'DECISION', 'unreadable': 'DECISION',
                'carried': 'LANDED', 'carried-moved': 'DECISION', 'add-del': 'DECISION', 'picked': 'LANDED',
                'picked-lost': 'DECISION', 'unlanded-train': 'DECISION', 'ejected': 'DECISION',
                'unreviewed': 'WAITING FOR REVIEW', 'review-findings': 'DECISION', 'reviewed-moved': 'WAITING FOR REVIEW',
                'pred': 'IN FLIGHT', 'succ': 'WAITING TO LAND', 'fnd-pred': 'IN FLIGHT', 'fnd-fix': 'WAITING TO LAND',
                'reviewed-stamped': 'WAITING TO LAND',
                'lane-stopped': 'STOPPED', 'lane-pred': 'IN FLIGHT', 'lane-free': 'WAITING TO LAND',
                'lane-done': 'ENQUEUED', 'lane-running': 'WAITING TO LAND', 'discharged': 'WAITING TO LAND',
                'discharged-empty': 'DECISION', 'no-head': 'DECISION'}
        for name, state in want.items():
            got = rows.get(name)
            check(got is not None and got.state == state, f'{name}: want {state}, got {got.state if got else "no row"} ({got.why if got else ""})')
        # ONE RULE FOR A FINISHED BRANCH (kb/Work PB3181, PB3125): coverage, never age; the lane's attempt at the tip
        check('exit 5' in rows['lane-stopped'].why and 'step 4 finisher' in rows['lane-stopped'].why
              and 'later commits mechanical' in rows['lane-stopped'].why,
              f'a stopped attempt at a tip of mechanical commits after the report is STOPPED, its owner named: '
              f'{rows["lane-stopped"].why}')
        check('contained in lane-stopped (STOPPED)' in rows['lane-pred'].why,
              f'a predecessor of a STOPPED branch lands through it: {rows["lane-pred"].why}')
        check('exit 3' in rows['lane-free'].why and 'landing_check.py passes' in rows['lane-free'].why,
              f'an exit-3 attempt waits with the lane\'s retry rule named: {rows["lane-free"].why}')
        check('exit 0' in rows['lane-done'].why, f'an exit-0 attempt at the tip reads ENQUEUED offline: {rows["lane-done"].why}')
        check('being landed' in rows['lane-running'].why, f'a running attempt is said: {rows["lane-running"].why}')
        check('the landing lane lands it' in rows['done-branch'].why and 'an-older' not in rows['done-branch'].why,
              f'an attempt at another head does not decide this tip: {rows["done-branch"].why}')
        check('DISCHARGED' in rows['discharged-empty'].why and 'Review CLEAN' not in rows['discharged-empty'].why,
              f'a DISCHARGED report whose HEAD is its base is not finished: {rows["discharged-empty"].why}')
        check('names no HEAD' in rows['no-head'].why, f'a report naming no HEAD covers nothing: {rows["no-head"].why}')
        check('1 commit(s) since the report' in rows['resumed'].why,
              f'a commit after the report that no report describes is a DECISION, counted: {rows["resumed"].why}')
        check(rows['landed-content'].content == 'LANDED', 'a branch re-applied on main classifies LANDED by content')
        check('no fix-lane report' in rows['orphan'].why,
              'a suffixed report name is invisible (plan_wave REPORT_NAME), and a name below the header binds nothing')
        check('SPLIT' in rows['split-branch'].why, 'a DECISION row names its report status')
        check('already landed' in rows['superseded'].why, 'a DONE report whose lead note landed is superseded')
        check('never landed' in rows['stale-done'].why, 'a DONE report over --stale-hours is a DECISION, said so')
        check('uncommitted' in rows['dirty-merged'].why, 'a dirty landed worktree is a DECISION, with the reason')
        check('no review stamp' in rows['unreviewed'].why, 'a DONE branch with no review stamp waits for its reviewer')
        check('but the branch is at' in rows['reviewed-moved'].why, 'a stamp at an older head is unreviewed work, said so')
        check('2 finding(s)' in rows['review-findings'].why and 'finisher' in rows['review-findings'].why,
              'a FINDINGS stamp is a finisher DECISION, with the findings file')
        check('contained in succ' in rows['pred'].why, f'a predecessor lands through its successor: {rows["pred"].why}')
        check('contained in fnd-fix' in rows['fnd-pred'].why,
              f'a FINDINGS branch its finisher merged lands through the finisher: {rows["fnd-pred"].why}')
        # owned(): IN FLIGHT by the branch's OWN lifecycle only; a contained branch names its carrier and is not owned
        # (kb/Work PB3311 review R1-1: the planner's FINDINGS arm waits only on an owned branch)
        live_set = owned(list(rows.values()))
        check((rows['fnd-pred'].carrier, rows['pred'].carrier, rows['lane-pred'].carrier, rows['fresh'].carrier)
              == ('fnd-fix', 'succ', 'lane-stopped', ''), 'contained() names the carrier; a fresh branch has none')
        check(live_set == {n for n, s in want.items() if s == 'IN FLIGHT'} - {'fnd-pred', 'pred', 'lane-pred'},
              f'owned = IN FLIGHT by its own lifecycle (lock, ledger, touch), never by containment: {sorted(live_set)}')
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
        # THE PULL-REQUEST STATES (kb/Work PB3023, PB3024): ONE GraphQL read; a PR in the queue (or with auto-merge on) is
        # ENQUEUED; one the queue removed, still at the branch's tip, is BOUNCED with GitHub's reason; a bounced PR whose
        # branch moved on (the finisher's fix) reads by its report again; offline it is a non-observation ({})
        def fake_gh(args):
            if args[:2] == ['repo', 'view']:
                return 0, 'o/r\n', ''
            removed = {'nodes': [{'reason': 'FAILED_CHECKS', 'createdAt': '2026-10-11T20:00:00Z'}]}
            return 0, json.dumps({'data': {'repository': {'pullRequests': {'nodes': [
                {'number': 17, 'url': 'u17', 'headRefName': 'done-branch', 'headRefOid': tips['done-branch'],
                 'isInMergeQueue': True, 'autoMergeRequest': None, 'timelineItems': {'nodes': []}},
                {'number': 18, 'url': 'u18', 'headRefName': 'succ', 'headRefOid': tips['succ'],
                 'isInMergeQueue': False, 'autoMergeRequest': None, 'timelineItems': removed},
                {'number': 19, 'url': 'u19', 'headRefName': 'fnd-fix', 'headRefOid': 'an-older-head',
                 'isInMergeQueue': False, 'autoMergeRequest': None, 'timelineItems': removed},
                {'number': 20, 'url': 'u20', 'headRefName': 'unreviewed', 'headRefOid': 'x',
                 'isInMergeQueue': False, 'autoMergeRequest': {'enabledAt': 'now'}, 'timelineItems': removed}]}}}}), ''
        prs = pull_requests(fake_gh)
        pr_rows = {r.name: r for r in survey(repo, reports=report_index(rdir, terminal), slugs={'w9999a'}, min_age=180,
                                             stale_hours=24, live=live, coord_dir=landed_coord, prs=prs)}
        got = {b: pr_rows[b].state for b in ('done-branch', 'succ', 'fnd-fix', 'unreviewed', 'pred', 'merged')}
        check(got == {'done-branch': 'ENQUEUED', 'succ': 'BOUNCED', 'fnd-fix': 'WAITING TO LAND',
                      'unreviewed': 'ENQUEUED', 'pred': 'IN FLIGHT', 'merged': 'LANDED'}
              and 'FAILED_CHECKS' in pr_rows['succ'].why and '#17' in pr_rows['done-branch'].why,
              f'the merge queue states: a queued or auto-merge PR is ENQUEUED, a removed one at the tip BOUNCED with '
              f'its reason, a moved tip reads by its report, a predecessor of a bounced branch lands through it: {got}')
        # THE ONE PRODUCTION SURVEY (kb/Work PB2807 review R1-2): live_survey reads the open pull requests itself, so
        # every reader (main, the landing lane, the planner's partition) sees the queue states; and no production
        # reader calls survey directly with arguments of its own (the planner once omitted `prs` and never saw BOUNCED)
        live_rows = {r.name: r.state for r in live_survey(repo, coord_dir=landed_coord, reports_dir=rdir,
                                                           terminal=terminal, live=live, gh=fake_gh)}
        check({b: live_rows.get(b) for b in ('done-branch', 'succ', 'fnd-fix')}
              == {'done-branch': 'ENQUEUED', 'succ': 'BOUNCED', 'fnd-fix': 'WAITING TO LAND'},
              f'live_survey reads the pull requests (ENQUEUED, BOUNCED): {live_rows}')
        direct = [p.name for p in (REPO / 'scripts' / 'orchestrator').glob('*.py')
                  if re.search(r'prune_worktrees\.survey\(', p.read_text(encoding='utf-8'))]
        check(not direct, f'production readers call prune_worktrees.live_survey, never survey: {direct}')

        def offline_gh(args):
            import ruleset  # noqa: PLC0415
            raise ruleset.Unobserved('no network')
        check(pull_requests(offline_gh) == {} and pull_requests(lambda a: (1, '', 'x')) == {},
              'offline, or a gh failure, the pull-request states are empty (git-only lifecycle)')
        # THE LANDING ORDER (kb/Work PB2981's ranking, kept by PB3023): the WAITING TO LAND rows, the most harmful lead
        # first, a compiler fix before process work at equal harm, the oldest report first; nothing else
        def waiting(name, lead, age):
            return Row(name, 'CHECK', [], [], 1, None, None, 0, 999.0, '',
                       Report(f'w1a-{lead}-report.md', 'DONE', lead, False, age), 'WAITING TO LAND')
        ranks = {'PB1': (-8, 0), 'PB2': (-4, 0), 'PB3': (0, 0), 'PB4': (0, 1)}
        eight = [waiting(f'b{i}', lead, age) for i, (lead, age) in enumerate(
            (('PB4', 500), ('PB3', 10), ('PB3', 90), ('PB1', 5), ('PB4', 50), ('PB2', 7), ('PB3', 30), ('PB4', 900)))]
        order = landing_order(eight + [dataclasses.replace(eight[0], name='landed-row', state='LANDED')],
                              lambda n: ranks.get(n, (0, 1)))
        check([r.name for r in order] == ['b3', 'b5', 'b2', 'b6', 'b1', 'b7', 'b0', 'b4'],
              f'the landing order is the most harmful lead first, compiler before process, oldest first: '
              f'{[r.name for r in order]}')
        check(verdict(list(rows.values())).startswith('=== BRANCH LIFECYCLE: 39 · 6 in flight · 2 waiting for review · '
                                                      '7 waiting to land · 1 stopped · 1 enqueued · 0 bounced · 5 landed'),
              f'verdict line: {verdict(list(rows.values()))}')
        # the pure rule: a lock with no pid is honoured; a ledger slug matches only at a word boundary
        r0 = dataclasses.replace(rows['orphan'], lock='')
        check(lifecycle(r0, in_flight_slugs=set(), min_age=180)[0] == 'IN FLIGHT', 'a hand lock (no pid) is live')
        r1 = dataclasses.replace(rows['orphan'], name='w99990-other')
        check(lifecycle(r1, in_flight_slugs={'w9999'}, min_age=180, live=live)[0] == 'DECISION', 'slug w9999 does not bind w99990-…')
        # the in-flight slugs are the ledger entries' recorded slugs (kb/Work PB3186): a retired hand entry has none
        sdir = pathlib.Path(tempfile.mkdtemp(prefix='slugs-'))
        (sdir / 'inflight-groups.json').write_text(json.dumps({'groups': [
            {'wave': '9998', 'letter': 'B', 'slug': 'w9998b', 'notes': ['PB1'], 'files': ['src/F.cs'], 'at': 1.0},
            {'wave': '9998', 'letter': 'C', 'notes': ['PB2'], 'files': ['src/G.cs'], 'at': 1.0},
            {'wave': 'hand', 'letter': '', 'hand': True, 'notes': ['PB3'], 'files': ['src/H.cs'], 'at': 1.0}]}),
            encoding='utf-8', newline='\n')
        check(in_flight_slugs(sdir, lambda n: False) == {'w9998b', 'w9998c'},
              f'in-flight slugs: {in_flight_slugs(sdir, lambda n: False)}')
        # a group its report answers (it returned, SPLIT included) is no longer in flight by slug (kb/Work PB2795)
        (srep := sdir / 'reports').mkdir()
        (srep / 'w9998b-PB1-report.md').write_text('# PB1\n\n**Status:** SPLIT\n', encoding='utf-8', newline='\n')
        check(in_flight_slugs(sdir, lambda n: False, srep) == {'w9998c'},
              f'an answered group is not in flight: {in_flight_slugs(sdir, lambda n: False, srep)}')
        # --apply removes exactly the LANDED rows plus the --abandon branches whose decision a kb/Work note records,
        # archives nothing (owner 2026-10-08 00:52 PDT), and loses nothing else
        notes = tmp / 'Work'
        notes.mkdir()
        # the record names a successor before and after the word: neither passes for a decision to abandon it
        (notes / 'PB8.md').write_text('---\nid: PB8\n---\nsplit-branch carries the rest, so the orphan branch: ABANDONED '
                                      '2026-10-08, the approach was replaced by PB9 on split-branch.\nsplit-branch: not '
                                      'abandoned, a finisher takes it.\n', encoding='utf-8', newline="\n")
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
        # the guard reads the line alone: with `orphan` already deleted (nothing in the survey names it), the record
        # that abandoned it still does not abandon the branch it names (review finding R1-1)
        check(abandon_record('orphan', notes) == 'PB8' and abandon_record('split-branch', notes) is None,
              'a record abandons the branch the word follows, not the successor it names, with no survey to ask')
        shapes = tmp / 'Shapes'
        shapes.mkdir()
        (shapes / 'PB1.md').write_text(
            '**2026-10-10, lander: branch `old-a` (wave 1, SPLIT) ABANDONED,** superseded by `new-a`.\n'
            '`new-b` carries it, so `old-b` is ABANDONED.\n'
            '`old-c` (successor `new-c`) ABANDONED: replaced.\n'
            'Branch `old-d` is not abandoned: `new-d` ABANDONED instead.\n', encoding='utf-8', newline='\n')
        got = {b: abandon_record(b, shapes) for b in ('old-a', 'new-a', 'old-b', 'new-b', 'old-c', 'new-c', 'old-d', 'new-d')}
        check(got == {'old-a': 'PB1', 'new-a': None, 'old-b': 'PB1', 'new-b': None, 'old-c': 'PB1', 'new-c': None,
                      'old-d': None, 'new-d': 'PB1'}, f'record shapes: {got}')
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

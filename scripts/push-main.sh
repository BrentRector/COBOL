#!/usr/bin/env bash
# push-main.sh — ⛔ THE ONLY WAY A COMMIT REACHES `main`. (Owner decision 2026-09-06, question 23; kb/Work/PB796.)
#
# `main` carries a REQUIRED status check — `ci-gate`, the terminal job of .github/workflows/build-and-test.yml —
# with `enforce_admins: true`. Every push to this repository is made with the owner's credentials, so an
# administrator exemption would be worth nothing; the rule binds the orchestrator's own pushes exactly as it binds
# a lander's. A bare `git push origin HEAD:main` of an unverified commit is therefore REFUSED BY THE SERVER, and
# this script is the flow that earns the verdict first:
#
#     1. push HEAD to the landing branch `ci/<short-sha>`   (a check run is attached to the COMMIT, not a branch,
#     2. find the run for that exact sha                     so the verdict earned there is the one the protection
#     3. `gh run watch <id> --exit-status`                   reads when the same sha reaches main)
#     4. GREEN  -> `git push origin <the VERIFIED sha>:main` (fast-forward only), then delete the ci/ branch
#        RED    -> print the failing jobs and exit non-zero, WITHOUT touching main
#
# ⚙ WHY IT EXISTS. PB796: `git push origin HEAD:main` was the LAST step of both lander briefs, so nothing in the
# loop ever read the verdict the push triggered, and `main` stayed red for 29 hours across 16 consecutive
# completed runs while eleven landings each reported a green LOCAL gate. The local battery runs on ONE host
# (Windows, Debug); the workflow's `ubuntu-26.04` jobs and its Release build are gated by CI and by nothing else.
# The brief-level fix ("wait for the run, report its conclusion") was prose in four places. This is the mechanism.
#
# ⭐ IT IS IDEMPOTENT AND SAFE TO RE-RUN. A full-matrix run is ~25–30 min, which is longer than an agent's command
# timeout — so a caller that is cut off simply RUNS IT AGAIN: a sha that already carries a green `ci-gate` skips
# straight to the main push, and a run already in flight for that sha is re-attached to, never duplicated. Run it
# in the background (the Bash tool's `run_in_background`) or re-run it; do not hand-roll the wait.
#
# Usage:  bash scripts/push-main.sh [--branch-prefix ci] [--no-delete] [--audit]
#         --audit  — land nothing: report stale landing branches and exit 1 if any survives whose tip is
#                    already an ancestor of origin/main (the drift check kb/Work/PB950 asks for).
#         PUSH_MAIN_TIMEOUT_MIN=45  — how long to wait for the landing run (default 45)
# Exit:   0 = the commit is on main and its CI was green · 1 = CI red or the push refused · 2 = usage/state error
#         3 = the landing check STOPPED the landing (scripts/orchestrator/landing_check.py: a file outside an R3
#             wave's declared set, or one shared with earlier-dispatched in-flight work): re-plan, never retry as is
#         4 = another lander holds the LANDING LEASE (scripts/orchestrator/landing_lease.py, kb/Work PB2537): main
#             NOT touched; acquire it with `landing_lease.py acquire --wait-min 9`, rebase, re-gate, then re-run
#         5 = the commit IS on main but its main-branch run's verdict could not be read (UNVERIFIED, kb/Work PB1639):
#             read it by hand; never re-land it
#         6 = the landing carries no new DEVLOG entry, or a malformed or misplaced one (scripts/orchestrator/
#             landing_devlog.py, kb/Work PB2605): main NOT touched; write the entry at the TOP, commit, re-run
#         7 = the arch-oracle baseline is behind the landing (scripts/orchestrator/landing_oracle.py, kb/Work PB2885):
#             main NOT touched; compare_oracle.py, then capture_oracle.py --record, commit the manifest, re-run

set -uo pipefail

PREFIX=ci
DELETE_BRANCH=1
AUDIT=0
TIMEOUT_MIN="${PUSH_MAIN_TIMEOUT_MIN:-45}"
while [ $# -gt 0 ]; do
  case "$1" in
    --branch-prefix) PREFIX="${2:?--branch-prefix needs a value}"; shift 2 ;;
    --no-delete)     DELETE_BRANCH=0; shift ;;
    --audit)         AUDIT=1; shift ;;
    -h|--help)       sed -n '2,/^$/p' "$0"; exit 0 ;;   # the header comment, to its first blank line
    *) echo "push-main.sh: unknown argument '$1'" >&2; exit 2 ;;
  esac
done

cd "$(dirname "$0")/.." || exit 2
say() { printf '%s\n' "$*"; }
die() { printf '⛔ push-main: %s\n' "$*" >&2; exit 2; }
. scripts/python-resolve.sh
LEASE=scripts/orchestrator/landing_lease.py

command -v gh  >/dev/null 2>&1 || die "the GitHub CLI (gh) is not on PATH — the landing verdict cannot be read"
gh auth status >/dev/null 2>&1 || die "gh is not authenticated (gh auth login)"

REPO="$(gh repo view --json nameWithOwner --jq .nameWithOwner 2>/dev/null)"
[ -n "$REPO" ] || die "could not resolve the repository from gh"
SHA="$(git rev-parse HEAD)"      || die "no HEAD"
SHORT="$(git rev-parse --short=12 HEAD)"
BRANCH="$PREFIX/$SHORT"

# ── Landing-branch hygiene (kb/Work/PB950) ───────────────────────────────────────────────────────────────────
# ⛔ TWO MECHANISMS LEFT NINE BRANCHES BEHIND, AND THE OBVIOUS ONE WAS NOT THE COMMON ONE.
#   A. $BRANCH is computed from HEAD AT INVOCATION. A caller that rebases between attempts lands sha2, deletes
#      only ci/<sha2>, and orphans ci/<sha1> FOREVER — no later invocation ever computes that name again.
#      Measured 2026-09-21: battery #82 pushed ci/410280c17f6e, rebased onto a main registrar #8 had moved,
#      landed e3902c8f3, and left the first branch behind. SIX of the nine survivors were this shape.
#   B. the delete was `git push … --delete "$BRANCH" 2>/dev/null && say deleted`, so a FAILED delete printed
#      nothing, set no exit code anyone read, and the landing printed its success marker anyway. Two were this.
# The fix is a LEDGER for A and a SWEEP for B — plus an unsilenced delete, without which B cannot be diagnosed
# at all. The sweep is what makes the NEXT orphan disappear automatically instead of accumulating.
ATTEMPTS="$(git rev-parse --git-common-dir)/push-main-attempts"   # shared by every linked worktree
TOPLEVEL="$(git rev-parse --show-toplevel)"
TAB="$(printf '\t')"

delete_remote_branch() {   # $1 = branch name. Loud on failure — never `2>/dev/null && say`.
  local out rc
  out="$(git push --quiet origin --delete "$1" 2>&1)"; rc=$?
  if [ "$rc" -eq 0 ]; then
    say "push-main: deleted the landing branch $1 (the run and its check runs live on the commit)."
    return 0
  fi
  case "$out" in
    *"remote ref does not exist"*) return 0 ;;   # already gone — not a failure, and not worth a line
  esac
  say "⚠ push-main: COULD NOT DELETE the landing branch $1 (git exit $rc):"
  printf '%s\n' "$out" | sed 's/^/      /'
  say '      it is left on the remote; bash scripts/push-main.sh --audit will keep reporting it.'
  return 1
}

record_attempt() {         # remember this branch so a LATER attempt from the same worktree deletes it — PB950 A
  [ "$DELETE_BRANCH" = 1 ] || return 0
  printf '%s%s%s\n' "$TOPLEVEL" "$TAB" "$1" >> "$ATTEMPTS" 2>/dev/null || true
}

fetch_landing_refs() {
  git fetch --quiet --prune origin \
    "+refs/heads/$PREFIX/*:refs/remotes/origin/$PREFIX/*" 2>/dev/null || true
}

sweep_landing_branches() {
  [ "$DELETE_BRANCH" = 1 ] || return 0
  fetch_landing_refs

  # A — every branch THIS worktree pushed for an earlier attempt. Keyed by worktree path, so a concurrent
  #     lander's in-flight attempt is never touched; its own landing sweeps its own rows.
  if [ -f "$ATTEMPTS" ]; then
    local wt br kept
    kept="$(mktemp 2>/dev/null || echo "$ATTEMPTS.keep")"
    : > "$kept"
    while IFS="$TAB" read -r wt br; do
      [ -n "$br" ] || continue
      if [ "$wt" = "$TOPLEVEL" ]; then
        [ "$br" = "$BRANCH" ] || delete_remote_branch "$br" || true
      else
        printf '%s%s%s\n' "$wt" "$TAB" "$br" >> "$kept"
      fi
    done < "$ATTEMPTS"
    mv -f "$kept" "$ATTEMPTS" 2>/dev/null || rm -f "$kept" 2>/dev/null
  fi

  # B — a landing branch whose tip is ALREADY an ancestor of origin/main has by definition landed; if one
  #     survives, a delete failed silently. Deleting it here is the self-heal; --audit is the check.
  local ref br
  for ref in $(git for-each-ref --format='%(refname)' "refs/remotes/origin/$PREFIX/" 2>/dev/null); do
    br="${ref#refs/remotes/origin/}"
    if git merge-base --is-ancestor "$ref" refs/remotes/origin/main 2>/dev/null; then
      delete_remote_branch "$br" || true
    fi
  done
}

audit_landing_branches() { # report, change nothing, FAIL on the provable-recurrence state
  fetch_landing_refs
  local ref br landed=0 orphan=0
  for ref in $(git for-each-ref --format='%(refname)' "refs/remotes/origin/$PREFIX/" 2>/dev/null); do
    br="${ref#refs/remotes/origin/}"
    if git merge-base --is-ancestor "$ref" refs/remotes/origin/main 2>/dev/null; then
      say "⛔ LANDED BUT NOT DELETED: $br — $(git log -1 --format=%s "$ref" | cut -c1-66)"
      landed=$((landed + 1))
    else
      say "⚠ orphan, never reached main: $br — $(git log -1 --format=%s "$ref" | cut -c1-66)"
      orphan=$((orphan + 1))
    fi
  done
  if [ "$landed" -eq 0 ] && [ "$orphan" -eq 0 ]; then
    say "push-main --audit: no $PREFIX/* landing branches on the remote — clean."
    return 0
  fi
  say ""
  say "push-main --audit: $landed landed-but-undeleted, $orphan orphaned (kb/Work/PB950)."
  [ "$landed" -eq 0 ]        # a LANDED survivor proves a delete failed silently; an orphan is reported, not fatal
}

# ⛔ THE AUDIT DISPATCH IS DELIBERATELY HERE, ABOVE EVERY LANDING PRECONDITION. Placed after them it was dead
# code in the one state you most want to run it in — a clean checkout sitting at origin/main, where the
# "nothing to land" early-exit fires first. Measured 2026-09-21 by running it.
if [ "$AUDIT" = 1 ]; then
  git fetch --quiet origin '+refs/heads/main:refs/remotes/origin/main' || die "git fetch origin main failed"
  audit_landing_branches
  exit $?
fi

# ── The tree is not what lands; HEAD is. Say so loudly rather than silently leaving work behind. ──────────────
DIRTY="$(git status --porcelain -- . ':!.claude/settings.local.json' | grep -v ' STATUS.md$')"
if [ -n "$DIRTY" ]; then
  say "⚠ the working tree is DIRTY — these paths are NOT part of the commit being landed:"
  printf '%s\n' "$DIRTY" | sed 's/^/    /'
fi

git fetch --quiet origin '+refs/heads/main:refs/remotes/origin/main' \
  || die "git fetch origin main failed"
BASE="$(git rev-parse refs/remotes/origin/main)"
if [ "$BASE" = "$SHA" ]; then
  say "push-main: origin/main is already at $SHORT — nothing to land."
  # A run that landed and was then cut off before its EXIT trap (a killed tool call) left this worktree's landing
  # lease held; this re-run is where the landing ends, so it frees it (another worktree's lease is left alone: exit 4).
  "$PY" "$LEASE" release --outcome "push-main.sh: $SHORT is already on main"
  case $? in 0|4) ;; *) say "⛔ push-main: the landing lease could NOT be released (above): run $PY $LEASE release" ;; esac
  exit 0
fi

# ── ONE LANDER ON MAIN AT A TIME (kb/Work PB2537). This script used to serialize only the push itself, so a landing
# whose rebase, gates and CI take ~50 min lost the race to every train that landed inside that window: on 2026-10-07
# the R1 lander rebased and re-gated three times and ended SPLIT with every gate and CI green and nothing on main. A
# lander takes the LANDING LEASE before its final rebase and gates (lander briefs, step 2b). Here the lease is
# re-taken (idempotent for the worktree that holds it), taken when the caller holds none, and REFUSED while another
# worktree holds it — so an unleased landing can no longer move main under a leased one. It is renewed every minute
# while this script runs (the CI wait is its longest step) and released when it exits, landed or not. ──
ON_BRANCH="$(git branch --show-current 2>/dev/null)"
take_lease() {     # 0 = this worktree holds it; exits 4 when another worktree does; dies on anything else
  "$PY" "$LEASE" acquire --holder "push-main.sh on ${ON_BRANCH:-a detached HEAD}" \
      --reason "landing $SHORT (its caller took no lease before push-main)"
  case $? in
    0) return 0 ;;
    4) echo "⛔ push-main: another lander holds the landing lease (above) — main NOT touched. Wait for it with
     $PY $LEASE acquire --holder <you> --reason <what> --wait-min 9   (re-issue until ACQUIRED),
     then rebase onto origin/main, re-gate, and re-run this script." >&2
       exit 4 ;;
    *) die "the landing lease could not be read or taken (above) — main NOT touched; fix it and re-run" ;;
  esac
}
take_lease
# The renewer outlives no one (it ends with this script) and holds no caller's pipe (its output is a file). It keeps
# renewing through a transient failure and stops only on LOST (exit 4); its log is printed if the lease is gone later.
LEASE_LOG="$(mktemp 2>/dev/null || echo "${TMPDIR:-/tmp}/push-main-lease.$$")"
renew_lease() {    # $1 = this script's pid: renew every minute while it lives
  local n=0
  while kill -0 "$1" 2>/dev/null; do
    sleep 5
    n=$((n + 1))
    if [ "$n" -ge 12 ]; then
      n=0
      "$PY" "$LEASE" renew
      [ $? -eq 4 ] && return
    fi
  done
}
renew_lease $$ > "$LEASE_LOG" 2>&1 &
RENEWER=$!
release_lease() {
  local rc=$?
  kill "$RENEWER" 2>/dev/null
  "$PY" "$LEASE" release --outcome "push-main.sh exit $rc on $SHORT"
  case $? in
    0|4) ;;
    *) printf '⛔ push-main: the landing lease was NOT released (above): every other lander waits on it until it expires.
     Release it: %s %s release\n' "$PY" "$LEASE" >&2 ;;
  esac
  rm -f "$LEASE_LOG"
  exit "$rc"
}
trap release_lease EXIT

# ⛔ FAST-FORWARD ONLY. Refused HERE, before a run is spent, and refused again by the server at the push.
if ! git merge-base --is-ancestor "$BASE" "$SHA"; then
  die "HEAD ($SHORT) is not a descendant of origin/main ($(git rev-parse --short=12 "$BASE")).
     Rebase first:  git fetch origin && git rebase origin/main
     A landing is a pure fast-forward; this script never force-pushes."
fi

# ── EVERY LANDING ON MAIN CARRIES A NEW DEVLOG ENTRY (owner 2026-10-08, kb/Work PB2605: the rule covers "Only commits
# landing on main"; an implementer's WIP checkpoints carry none, the lander writes the train's one). Checked HERE, on
# this landing's own range (DEVLOG.md at $BASE against DEVLOG.md at $SHA), because every landing passes here and a
# commit cannot know whether it will land. Refused before a run is spent. ──
"$PY" scripts/orchestrator/landing_devlog.py --rev "$SHA" --base "$BASE"
case $? in
  0) ;;
  1) echo "⛔ push-main: this landing carries no valid new DEVLOG entry (above) — main NOT touched." >&2
     exit 6 ;;
  *) die "the DEVLOG landing check could not read the range (above) — main NOT touched" ;;
esac

# ── THE ARCH-ORACLE BASELINE DESCRIBES THE LANDED TREE (kb/Work PB2885). Re-recording it was prose (lander step 3c,
# MANDATORY-PRACTICES L11), and train 1049, an operator hand-landing, skipped it: six new cases reached main behind
# a stale baseline and the next train inherited the drift. landing_oracle.py reads the recorded manifest at $SHA and
# refuses when any path under its derived `inputs` changed after the commit it names. Refused before a run is spent. ──
"$PY" scripts/orchestrator/landing_oracle.py --rev "$SHA"
case $? in
  0) ;;
  1) echo "⛔ push-main: the arch-oracle baseline is behind this landing (above) — main NOT touched." >&2
     exit 7 ;;
  *) die "the arch-oracle landing check could not read the range or the baseline (above) — main NOT touched" ;;
esac

# ── THE FILE-SET PARTITION'S GUARANTEE (kb/Work PB2118 Drafts 9-10; DESIGN-architecture-review §8.7). The planner admits
# an R3 restructuring wave on its COMPUTED file set, an estimate three refuter rounds each found a hole in; this is
# where a miss is caught instead of landed. landing_check.py compares what this landing ACTUALLY changed with its
# declared set (R3 work) and with every in-flight branch (when either side is R3 work): a file outside the set, or
# one shared with an earlier-dispatched branch, STOPS the landing before a run is spent, and the later branch
# re-plans and rebases. Every landing passes here, so it is the ONE place the check runs. ──
if ! "$PY" scripts/orchestrator/landing_check.py --rev "$SHA" --base "$BASE"; then
  echo "⛔ push-main: the landing check STOPPED this landing (above): re-plan the named work and rebase — main NOT touched." >&2
  exit 3   # its own code (the header's Exit list): a STOP is a re-plan, never a usage error to retry
fi

green_ci_gate() {   # 1 = this sha already carries a successful `ci-gate` check run
  local n
  n="$(gh api "repos/$REPO/commits/$1/check-runs?check_name=ci-gate&per_page=100" \
         --jq '[.check_runs[] | select(.conclusion == "success")] | length' 2>/dev/null)"
  case "${n:-x}" in ''|*[!0-9]*) return 1 ;; esac
  [ "$n" -gt 0 ]
}

run_for_sha() {     # prints the newest run id for sha $1 on branch $2, or nothing
  gh run list --branch "$2" --limit 20 \
     --json databaseId,headSha,status,conclusion --jq \
     "[.[] | select(.headSha == \"$1\")] | first | .databaseId" 2>/dev/null | grep -E '^[0-9]+$'
}

report_red() {      # $1 = run id, $2 = what it means for main
  say ""
  say "⛔ CI IS RED ON $SHORT — ${2:-main is UNTOUCHED}. Failing jobs:"
  gh run view "$1" --json jobs --jq \
    '.jobs[] | select(.conclusion != null and .conclusion != "success" and .conclusion != "skipped")
     | "  JOB  \(.name) = \(.conclusion)", (.steps[]? | select(.conclusion == "failure") | "       step: \(.name)")' \
    2>/dev/null || say "  (could not enumerate jobs — gh run view $1)"
  say ""
  say "  attribute it:  gh run view $1 --log-failed"
}

# ── 1. Earn the verdict on the landing branch ────────────────────────────────────────────────────────────────
RUN_ID=""
if green_ci_gate "$SHA"; then
  say "push-main: $SHORT already carries a green ci-gate check — skipping straight to the main push."
else
  say "push-main: pushing $SHORT to $BRANCH for verification …"
  git push --quiet origin "$SHA:refs/heads/$BRANCH" || die "could not push $BRANCH"
  record_attempt "$BRANCH"    # so a later attempt from this worktree deletes this one — PB950 mechanism A

  say "push-main: waiting for the run on $SHA (timeout ${TIMEOUT_MIN} min) …"
  for _ in $(seq 1 30); do                       # the run takes ~10–40 s to appear
    RUN_ID="$(run_for_sha "$SHA" "$BRANCH")"
    [ -n "$RUN_ID" ] && break
    sleep 5
  done
  # ⛔ NO RUN IS A FAILURE, NEVER A PASS. Since `paths-ignore` was removed every push to `ci/**` starts a run, so
  # "no run for my sha" no longer means "docs-only" — it means the workflow did not fire, and main would refuse
  # the push anyway for want of a check (feedback_verdict_evidence_invariant).
  [ -n "$RUN_ID" ] || die "no workflow run appeared for $SHORT on $BRANCH within 150 s — main NOT touched.
     Check: gh run list --branch $BRANCH  ·  gh workflow list"

  say "push-main: run $RUN_ID — https://github.com/$REPO/actions/runs/$RUN_ID"
  gh run watch "$RUN_ID" --exit-status --interval 20
  WATCH_RC=$?
  # ⛔ `gh run watch` exiting non-zero is not proof of a red — a dropped connection exits non-zero too. The
  # AUTHORITATIVE verdict is the run's own conclusion, so it is re-read here, and a run that is somehow still
  # in flight is polled to completion rather than guessed at.
  DEADLINE=$(( $(date +%s) + TIMEOUT_MIN * 60 ))
  while :; do
    STATUS="$(gh run view "$RUN_ID" --json status --jq .status 2>/dev/null)"
    [ "$STATUS" = "completed" ] && break
    if [ "$(date +%s)" -ge "$DEADLINE" ]; then
      die "run $RUN_ID is still '$STATUS' after ${TIMEOUT_MIN} min — main NOT touched. Re-run this script."
    fi
    sleep 20
  done
  CONCLUSION="$(gh run view "$RUN_ID" --json conclusion --jq .conclusion 2>/dev/null)"
  say "push-main: run $RUN_ID concluded '$CONCLUSION' (gh run watch rc=$WATCH_RC)"
  if [ "$CONCLUSION" != "success" ]; then
    report_red "$RUN_ID"
    say "  the landing branch $BRANCH is LEFT IN PLACE so the run can be re-run after the fix."
    exit 1
  fi
  # The run being green and the CHECK the protection reads being green are two statements; assert the second.
  green_ci_gate "$SHA" || { report_red "$RUN_ID"
    say "  (the run concluded success but no green 'ci-gate' check run is attached to $SHORT)"; exit 1; }
fi

# ── 2. Land ──────────────────────────────────────────────────────────────────────────────────────────────────
# ⛔ THE VERIFIED SHA IS PUSHED BY NAME, NEVER `HEAD:main`. The wait above is ~25–30 min on a full matrix, and
# the caller is a lander in its own worktree: a commit made in that window would move HEAD, and `HEAD:main` would
# land it UNVERIFIED on the back of another commit's green run — the precise failure this script exists to make
# impossible. `$SHA` was captured before the run was requested and is what the check run is attached to.
if [ "$(git rev-parse HEAD)" != "$SHA" ]; then
  say "⚠ HEAD moved while CI ran ($SHORT -> $(git rev-parse --short=12 HEAD)). Landing the VERIFIED sha $SHORT;"
  say "  the newer commit(s) are NOT landed — re-run this script to verify and land them."
fi
# The lease was renewed throughout the wait; a lease LOST meanwhile (taken over after an expiry) means another lander
# may be about to move main, so this landing stops rather than racing it (kb/Work PB2537).
"$PY" "$LEASE" check
case $? in
  0) ;;
  1) take_lease ;;   # it expired and no one took it: no other lander can be moving main, so take it back
  4) say "⛔ push-main: this worktree no longer holds the landing lease: another lander took it over (above) — main NOT"
     say "   touched. The renewer's log:"; sed 's/^/      /' "$LEASE_LOG"
     say "   Acquire it again, rebase onto origin/main, re-gate and re-run."
     exit 4 ;;
  *) die "the landing lease could not be checked (above) — main NOT touched; fix it and re-run" ;;
esac
say "push-main: ci-gate is green on $SHORT — fast-forwarding main …"
if ! git push origin "$SHA:main"; then
  say "⛔ the push to main was REFUSED. Either origin/main moved (rebase and re-run) or the protection"
  say "   did not see the ci-gate check. main is unchanged."
  exit 1
fi
say "push-main: ✅ $SHORT is on main."

if [ "$DELETE_BRANCH" = 1 ]; then
  # Unconditional: a re-run that skipped verification still has last time's branch to clean up. The run and its
  # check runs are attached to the COMMIT, which is now reachable from main, so nothing is lost with the branch.
  delete_remote_branch "$BRANCH" || true
  # …and every branch this worktree pushed for an EARLIER attempt, plus any landing branch that already landed.
  # Without this a rebase between attempts orphans the first branch permanently (PB950 mechanism A).
  sweep_landing_branches
fi

# ── 3. Read the verdict of the run THIS push just started — the whole point of PB796. It is the
# already-verified short-circuit in the `changes` job, so it is `changes` + `ci-gate` and takes under a minute. ─
MAIN_RUN=""
for _ in $(seq 1 30); do
  MAIN_RUN="$(run_for_sha "$SHA" main)"
  [ -n "$MAIN_RUN" ] && break
  sleep 5
done
if [ -z "$MAIN_RUN" ]; then
  say "⚠ no run appeared on main for $SHORT within 150 s — check it by hand: gh run list --branch main"
  exit 0
fi
gh run watch "$MAIN_RUN" --exit-status --interval 15 >/dev/null 2>&1
# ⛔ A MISSING OBSERVATION IS NOT A NEGATIVE ONE (kb/Work PB1639). `gh run view` can answer EMPTY on a transient
# API failure (measured 2026-09-27 on ca28783e3: conclusion '', job enumeration failed, the run itself 'success'),
# and the old test `!= success` reported that silence as "CI IS RED … ALREADY ON MAIN". Re-read until a verdict
# arrives; if none does, say UNVERIFIED — never red, never green.
MAIN_CONCLUSION=""
for _ in $(seq 1 12); do
  MAIN_CONCLUSION="$(gh run view "$MAIN_RUN" --json conclusion --jq .conclusion 2>/dev/null)"
  [ -n "$MAIN_CONCLUSION" ] && break
  sleep 10
done
say "push-main: main run $MAIN_RUN = ${MAIN_CONCLUSION:-<no verdict read>}"
if [ -z "$MAIN_CONCLUSION" ]; then
  say "  ⚠ UNVERIFIED: no conclusion could be read for main run $MAIN_RUN in 2 min — the commit IS on main; read it by hand:"
  say "    gh run view $MAIN_RUN --json conclusion"
  exit 5   # its own code (the header's Exit list): exit 3 is the landing check's STOP, which says "never retry as is"
fi
if [ "$MAIN_CONCLUSION" != "success" ]; then
  report_red "$MAIN_RUN" "AND IT IS ALREADY ON MAIN"
  say "  ⛔ THE COMMIT IS ON MAIN AND ITS MAIN-BRANCH RUN IS NOT GREEN — report this as a BLOCKING finding."
  exit 1
fi
say "push-main: ✅ landed $SHORT — ci/$SHORT run ${RUN_ID:-<reused>}, main run $MAIN_RUN, both green."

## Entry 1022 — 2026-07-26 15:26 PDT — Six project skills + four hooks: moving the rituals out of always-on context and mechanizing the rules that keep getting violated

The Entry-1020/1021 pass shrank what a session READS. This entry changes *how* it reads: the recurring rituals move
from always-on prose into on-demand skills, and four rules that have been violated repeatedly become hooks the
harness enforces rather than instructions the model is trusted to remember.

**Why skills.** `CLAUDE.md`, plan §0 and the memory set all carried procedure — how to start a session, how to
derive from the spec, which gate to run, how to land a fix. Procedure is the worst possible use of always-on
context: it is long, it is only relevant during the minute you need it, and it dilutes the standing rules it sits
next to. `.claude/skills/` did not exist. Six now do, each loading only when its trigger fires:

- `session-start` — plan §0, then `session-probe.ps1`, then the gate baseline; how to READ the probe (what an
  unexpected branch means, why catalog-above-src is the real diagnostic anomaly, what "invent: not built yet" means)
- `spec-lookup` — the order-of-operations rule as a procedure: find the §, check the repaired figure notes, render
  the page only to settle a doubt, write the expected value + citation, and only THEN read code
- `gate` — wave-local vs comprehensive, plus the four ways a false green has actually been produced here
- `land-a-fix` — the fix-queue loop end to end, including the manifest registration that has bitten twice
- `new-construct` — grammar fragment placement, the version predicate, the `constructs.json` row, and the
  edition-gate test sweep that keeps getting missed because `VersionMatrixTests` passing does not clear it
- `kb-sync` — the four remaining-work registers, which is authoritative for what, and the phase-close sweep

**Why hooks.** Four rules in `CLAUDE.md` are violated not through disagreement but through forgetting, and every one
of them is mechanically checkable:

1. **SessionStart** runs `session-probe.ps1` and injects the result. Plan §0 bootstrap step ③ was a manual ritual,
   and manual rituals get skipped — that is precisely how §0 came to disagree with the probe about the next free
   diagnostic code (Entry 1020).
2. **PreToolUse on `git commit`** asks for confirmation when `DEVLOG.md` is not staged. Commits have shipped without
   an entry and been corrected. Decision is `ask`, not `deny` — amends and doc fixups are legitimate exceptions.
3. **PostToolUse on Write/Edit** checks a touched conformance golden against its manifest and says so if it is
   unlisted. This is the highest-value one: an unregistered golden never runs AND fails the integrity test, but only
   at the COMPREHENSIVE gate — never at the per-commit wave-local run. That delay is the whole reason it has bitten
   twice in one session. The hook collapses the feedback loop from "next batch" to "immediately."
4. **Stop** reports unpushed commits, against the commit-AND-push-every-checkpoint rule.

**Implementation note.** `jq` is not on PATH on this box, so the usual inline-shell hook idiom was unavailable. The
hooks are four small Python scripts under `scripts/hooks/` instead, which is better anyway — they are readable,
testable in isolation, and cross-platform. All four are read-only and fail silent by construction: a hook that
breaks a session is worse than a hook that misses a case.

Each was pipe-tested against its real stdin payload before being wired up, and both branches were exercised where
there are two (`devlog_staged` correctly warns with a dirty index and goes silent once `DEVLOG.md` is staged — the
first attempt at that test was invalid, because `git add DEVLOG.md` stages nothing when the file is unchanged). The
`golden_registered` hook was then proven end to end by writing an unregistered `.cob` into
`tests/conformance/2002/` and confirming the context came back; the probe file was deleted immediately.

One real bug found and fixed during testing: `session-probe.ps1` emits `·`, `⚠` and `→`, and running it through
`pwsh -File` returned mojibake. Fixed by invoking via `-Command` with `[Console]::OutputEncoding` set to UTF-8
first.

**Also cleaned:** `.claude/settings.json`'s permission allowlist was twelve rules pointing at
`src/CobolSharp.CLI/bin/Debug/net9.0/…` — paths that have not existed since the .NET 10 retarget and the greenfield
rename. Replaced with the commands actually run now.

**Lesson.** The rules that keep getting violated are not the subtle ones — they are the boring mechanical ones, and
they get violated because they depend on memory at exactly the moment attention is elsewhere. Anything checkable by
a five-line script should not be an instruction at all.


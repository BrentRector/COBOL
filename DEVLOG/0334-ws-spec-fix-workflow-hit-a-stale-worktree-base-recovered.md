## Entry 334 — WS-SPEC fix workflow hit a STALE worktree base; recovered + first 3 fixes re-implemented on main

Launched a 6-agent **worktree-isolated** fix workflow (Intrinsics / Initialize / Currency / CopyReplace /
CompFloat / EditedDisplay). It completed with all 18 bugs reported fixed + CLI-verified — **but the worktrees
were branched from a STALE commit `e577e32`** ("session-end sync to DEVLOG 310"), ~2 sessions behind main
(`a14507b`): the worktree guard ran against **268 NIST** baselines, `SPEC_FIX_BACKLOG.md` didn't exist there, and
an agent appended a colliding "Entry 311". The diffs are therefore **unsafe to apply** to current main (divergent
grammar `.g4` + this session's binder/IR/emit changes; they also excluded the regenerated ANTLR artifacts).

**Recovery:** discarded the diffs; removed the 6 stale worktrees + branches (`git worktree remove`/`branch -D`);
salvaged the agents' detailed, CLI-verified fix **recipes** (file/method/cause each) as the implementation guide.
Re-implemented the first 3 (quick, independent) directly on main, each CLI-verified + regression-tested
(`SpecFixTests.cs`):
- **CONCAT** — added to `BindingContext.AlphanumericFunctions` (was typed numeric → InvalidCastException).
  `FUNCTION CONCAT("AB","CD")` → `ABCD`.
- **CURRENCY PICTURE SYMBOL** — CBL3124 rejected *all* letters; ISO §13.18.3 r27 forbids only
  A,B,C,D,E,N,P,R,S,V,X,Z — relaxed to that set. `CURRENCY SIGN IS "$" WITH PICTURE SYMBOL "U"`, `PIC U99` → `$42`.
- **BLANK WHEN ZERO** — `GetDisplayString` `TrimEnd` collapsed the all-blank field to `""`; preserve the
  PICTURE-width blank field. `MOVE 0` to `PIC ZZ,ZZ9 BLANK WHEN ZERO` → 6 spaces.

**Lesson:** worktree-isolated workflows in this repo branch from a stale base (a tooling quirk) — do compiler
fixes **directly on main**, or via read-only patch-design agents, not worktree isolation. The remaining 15
backlog fixes continue via the recipes.


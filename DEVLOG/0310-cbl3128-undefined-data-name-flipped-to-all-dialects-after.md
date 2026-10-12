## Entry 310 — CBL3128 (undefined data-name) flipped to ALL dialects, after fixing the IC228A ordering AND 6 more false-positive sources the corpus dry-run missed

Goal: make undefined-data-name (CBL3128, from DEVLOG 305) fire by default, closing the assessment's #1 gap
for ordinary users — not just named-strict modes. The DEVLOG-305 dry-run said the lone blocker was IC228A.

**IC228A fix.** A contained program's reference to a containing program's `IS GLOBAL` data is invisible to
`ReferenceResolver` because `Compilation.InheritGlobalItems` runs AFTER `BuildSemanticModel` (which contains
the pass). Fix: compute inherited-GLOBAL names from the already-built ancestor models (containing programs
are processed first) and feed them into the undefined-name whitelist via `CollectInheritedGlobalNames` —
same category as the SPECIAL-NAMES whitelist. Verified: IC228A 4 CBL3128 → 0, compiles.

**The dry-run was insufficient — adversarial verification was essential.** Before flipping, a 5-agent
read-only workstream tried to find VALID COBOL that CBL3128 would now wrongly reject *outside* the 349-program
corpus. It found a far larger surface than "just IC228A" (the corpus runs Default where CBL3128 was gated
off, so it could not have caught these). Run as a **loop-until-dry** (4 rounds: verify → fix → re-verify),
the following were fixed:
1. **Option-2 SPECIAL-NAMES switch** — `SWITCH-1 ON STATUS IS cond` (no mnemonic) was dropped by an
   `ids.Length >= 2` guard, so the condition-names never registered (also a latent bind bug). Capture
   `ids.Length == 1` + status clauses.
2. **Inherited-GLOBAL condition-names** (ISO §8.4.5) — level-88s under a global group are `ConditionSymbol`s
   (not in `DataItemsInOrder`); enumerate the ancestor scope and yield those rooted at a global item.
3. **Inherited-GLOBAL index-names** (ISO §8.4.5) — `INDEXED BY` names of a global table; yielded top-down via
   `member.Occurs.IndexNames`. (First tried marking the index `IsGlobal`, but that tripped CBL3116
   "GLOBAL only on level-01" since the index is a synthetic 77 — reverted to the whitelist approach.)
4. **SCREEN SECTION screen-names** — referenced by `DISPLAY`/`ACCEPT`, live in no scope; whitelisted from
   `ScreenItems`.
5. **Special registers** that lex as identifiers — `RETURN-CODE`, `SORT-RETURN`/`-CONTROL`/`-CORE-SIZE`/
   `-FILE-SIZE`/`-MODE-SIZE`, `TALLY`, and the COBOL-85 debugging registers `DEBUG-ITEM`/`-LINE`/`-NAME`/
   `-CONTENTS`/`-SUB-1..3`. Added a static `SpecialRegisters` whitelist in `ReferenceResolver`: these are
   *recognized* COBOL names, not "undefined" (whether a register is *implemented* is a separate concern —
   whitelisting restores the pre-flip permissive behavior and stops a spurious error on the ubiquitous
   RETURN-CODE). LINAGE/LINE/PAGE-COUNTER and figurative constants are distinct tokens (never reach the check).
6. **GLOBAL FD file** (ISO §8.4.6.2) — `FD … IS GLOBAL` had NO SemanticBuilder handler, so the record/fields/
   88s weren't global. Added `FileSymbol.IsGlobal` (set from the FD clause) and inherit the record's names
   via `OwningFile.IsGlobal` (no `DataSymbol.IsGlobal` change → no validator side-effect).
7. **CHANNEL mnemonic** — `CHANNEL n IS mnemonic` (e.g. C01) used in `WRITE … ADVANCING` had no handler;
   captured into the mnemonic carrier so it is whitelisted.

**The flip itself:** removed the `>= StrictCobol85` gate in `ReferenceResolver.VisitDataReference` (and the
now-unused `_options`); CBL3128 fires in all dialects. The item-5 `Default_…_NoCBL3128` test was inverted to
`Default_…_ReportsCBL3128`. Net: `MOVE 5 TO NONEXISTENT-ITEM.` is now a hard error in the default mode every
ordinary `cobolsharp foo.cob` user gets.

**Deferred (documented), NOT flip regressions:**
- **OPEN of a GLOBAL file-name in a contained program** = the unimplemented **GLOBAL-FILE-inheritance feature
  (IC233A/234A, Phase 4)**; such programs already `COMPILE_FAIL` (the OPEN: "not a declared file"), so the
  flip adds a redundant error to an already-failing compile, not a regression on a *compiling* program.
- **Duplicate same-named level-88 where the GLOBAL one is the *rejected* duplicate** — `GetAllSymbols` skips
  the rejections side-list; a very narrow edge.
- Whitelisting special registers makes them *recognized* but they remain *unimplemented* (silent today) —
  implementing them is a separate follow-up.
- Cosmetic: CBL0702 ("I/O on unopened file") still emits with the `"<source>"` placeholder (a site item-7
  missed, in the file-state checker) — harmless, tracked.

**Verify:** build 0 warnings; +6 unit tests (Option-2 switch, screen-name, inherited-GLOBAL cond/index,
inherited-GLOBAL FD record, special registers, CHANNEL mnemonic — all valid COBOL, asserted clean in Default);
the item-5 Default test inverted. **Four adversarial rounds converged** (special-registers / special-names /
non-data-positions / subscript-refmod all "safe"; only the deferred feature + the LOW duplicate-88 edge
remain). Full guard **ALL GREEN — 1040 unit (1034 +6) / 347 integration / 349 NIST baselines, 0 FAIL\***.


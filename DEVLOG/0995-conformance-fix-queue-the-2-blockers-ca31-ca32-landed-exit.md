## Entry 995 — 2026-07-22 22:16 PDT — Conformance fix-queue: the 2 BLOCKERS (CA31 + CA32) landed — EXIT PERFORM / EXIT PERFORM CYCLE in a multi-level inline PERFORM VARYING

Started the spec-first CONFORMANCE-FIX-QUEUE top-down. The two blockers share ONE root cause and ONE fix.

**The bug (§14.9.14.4 GR5a/GR5b).** A multi-level inline `PERFORM VARYING … AFTER …` lowers to *nested* C# `while`
loops (one per level), and the innermost VARYING augment is emitted as a trailing loop-body statement. `EXIT PERFORM`
emitted a bare C# `break` — which leaves only the innermost loop, so control resumed in the enclosing AFTER-level,
ran its augment + re-test, and executed extra iterations (CA31: the whole PERFORM was not left). `EXIT PERFORM CYCLE`
emitted a bare `continue` — which jumps to the `while` condition and *skips* the trailing augment, so the induction
variable never advanced ⇒ **infinite loop** (CA32). Both are shared with the legacy oracle, which is exactly why the
NIST/GnuCOBOL/corpus differentials were blind to them.

**The fix (one shared change).** Added `F3Region.Inline` to the emitter's EXIT-PERFORM region model. `EmitPerform`
now, for an inline PERFORM, brackets the body with a fresh `Inline(pid)` region + two labels: `__pcont{pid}` at the
loop-control boundary (immediately after the body, *before* the VARYING augment) and `__pexit{pid}` just past the
outermost loop. `EXIT PERFORM` → `goto __pexit` (leaves EVERY nested level, GR5a); `EXIT PERFORM CYCLE` →
`goto __pcont` (falls through to the augment + re-test, GR5b/§14.9.28.4 GR13). This reuses the existing F3 label
machinery (singular pattern — no second dispatch mechanism); every inline PERFORM now sets its OWN `Inline` id, so a
nested inline PERFORM's EXIT PERFORM correctly targets the innermost loop ("most closely preceding", GR5a), which
retired the old manual `SetF3Region(None)` reset in `EmitInlinePerform`. The `EmitPerform` switch body was renamed
`EmitPerformLoop`; the new `EmitPerform` is the thin bracketing wrapper (out-of-line PERFORM takes the bare loop —
SR8 means it never contains its own EXIT PERFORM). Unreferenced labels are safe under the file's `#pragma warning
disable CS0164`.

**Verified end-to-end.** Direct CLI runs now match the spec-derived output: CA31 `PERFORM VARYING A…AFTER B…` with
`EXIT PERFORM` at A=2,B=2 → `11 12 13 21 DONE` (was `11 12 13 21 31 32 33 DONE`); CA32 `EXIT PERFORM CYCLE` at I=2 →
`1 3 DONE` (was a hang). Both shipped as spec-derived goldens `tests/conformance/2023/exit_perform_multilevel` +
`exit_perform_cycle` (2023 manifest; `GreenfieldOnly` in the legacy suite — the frozen oracle shares the bug / would
infinite-loop, so greenfield owns the byte-compare).

**Gate (wave-local + full greenfield).** Characterization 33/33 — 2 snapshots re-baselined (`char_perform`,
`char_occurs`), the diff being exactly the `__pcont`/`__pexit` labels + a loop-id shift (the out-of-line `__Dispatch`
correctly got no labels). Unit 575/575. **Greenfield Conformance 3869/3869, 0 regression** (+2 new goldens). The
comprehensive legacy-guard / GnuCOBOL / NIST differential rides the per-batch pre-merge gate. Queue now: 2 blockers
DONE; next = the accept-display-misc majors (CA1 INITIALIZE dynamic-length→0, CA2 INITIALIZE pointer/object→NULL).



Owner adjudicated the 2 reclassified items. **CA14 → APPROVED the uniform introduction-error policy** (option a):
route SYNC-on-group through the canonical `ConstructRegistry.Check` + activate the dormant `sync-on-group-2023` row so
it is a hard error on both axes like every other 2023 introduction, removing the sole `--permissive` accept-inert
exception (effort S). **V59 → APPROVED building the Tier-C `byte[]` canonical** (option B): a REDEFINES/RENAMES class
mixing a BINARY/PACKED leaf with a differently-represented view routes to a `byte[]` canonical (RedefCodec radix-2 +
BCD) so a character pun reads the leaf's true bytes — matches GnuCOBOL + the §13.18.60.4 GR4/GR11 letter + the
byte-pun-fidelity mission; the current value-faithful Tier-B zoned image is ACCEPTABLE INTERIM (effort L, NOT a
blocker). `CONFORMANCE-FIX-QUEUE.md` + §24 V59 + plan §0 updated: the queue is now **46 fix-ready, 0 pending owner
input** — the new session can work it end to end.


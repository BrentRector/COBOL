## Entry 489 — Deep-dive doc sync: reconcile the SSOT §14 supersessions (owner directive — keep deep-dives current)

Before resuming G5/G6, the owner restated two non-negotiables: (1) implement each feature **from its subsystem
deep-dive design doc** + the ISO spec, never improvising; (2) whenever the SSOT (`COBOLNET_DESIGN.md`) supersedes a
deep-dive's original design, **update that deep-dive with the current design AND why the original wasn't followed** —
the docs are only trustworthy if current. Reading the deep-dives against the SSOT §14 "cross-cutting consistency"
(which names each loser) surfaced four stale spots where a reader following the deep-dive would implement the
*rejected* approach. Reconciled each (memory `feedback_follow_design_docs_and_spec`):

- **`COBOLNET_DATA_MODEL_DESIGN.md` §8 / D5 / hard-problem — REDEFINES.** The original "redefining and redefined items
  are SEPARATE typed fields; a write to one is not visible in the other" is the loser (SSOT §14.3): separate fields
  reproduce the exact silent-stale-read that triggered the DEVLOG-457 pivot, and a loud cross-type-read guard only
  *detects* it, it never makes the program correct. Replaced with the canonical **4-tier one-canonical-backing** model
  (A Alias / B StringCanonical / C class-scoped ByteCanonical / D reject-loud; every non-canonical view is a computed
  `Place` accessor over the single backing). Tiers A/B stay 100% typed and cover the entire near-term NIST path; bytes
  are confined to genuine mixed-USAGE puns (Tier C). Pointed at `COBOLNET_REDEFINES_DESIGN.md` as canonical. (This is
  the doc that governs the upcoming Slice B.)
- **`COBOLNET_PIPELINE_DESIGN.md` D2 + the worked example — dispatcher SHAPE.** The original `while(true) switch { …
  goto case N+1; … if(pc==exitPc) return; }` and "GOBACK returns the current Dispatch level" are both superseded
  (SSOT §14.6 / §14.5). `goto case` cannot express PERFORM-THRU exit detection (the `atExit`-captured-before-the-body
  check is needed because the body overwrites `pc`), and C# forbids `goto` *into* another switch section; and a C#
  `return` for GOBACK exits only the innermost recursive `__Dispatch`, so a GOBACK nested in a PERFORM would resume the
  PERFORM caller — GOBACK throws a distinct `ProgramReturn` caught at the program `Entry`. Rewrote the shape + example
  to the **pc-variable + bounded `while` + pre-body `atExit`** form already implemented in `CSharpEmitter.EmitDispatcher`
  (G4 ✅).
- **`COBOLNET_STRING_OPS_DESIGN.md` D7 / ref-mod / D6.** `StringLvalue` IS a `Place` (SSOT §14.1 — one lvalue model,
  not a per-verb abstraction); the ref-mod helpers are `CobolString.RefMod`/`SpliceInto`, not `CobolStrings.RefModStore`
  (SSOT §14.8 — `CobolStrings` is the multi-operand INSPECT/STRING/UNSTRING home); national `HIGH-VALUE`=U+FFFF /
  `LOW-VALUE`=U+0000 is now SETTLED (SSOT §18 #14), no longer an open question.
- **`COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` — `StoreChecked` ≡ `CobolNum.TryStore`** (SSOT §14.7): one checked-store
  method, settled on `TryStore` (returns `bool`, `false` = ON SIZE ERROR); updated the prose + code example.

`COBOLNET_INTERPROGRAM_DESIGN.md` (`ManagedRef<T>.OverField` is the `Place`'s Read/Write closures) and
`COBOLNET_INTRINSICS_DESIGN.md` D5 (national HIGH-VALUE = U+FFFF) were already consistent — left as-is. No code change;
guard unaffected. New memory `feedback_follow_design_docs_and_spec` records both standing rules.


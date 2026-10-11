## Entry 778 — 2026-07-11 12:51 PDT — P5.8+9: RecordLayout is THE single width/offset authority — all 6 geometry copies deleted; the "wider-redefiner divergence" DISSOLVED by §13.18.44.3 SR8 (which the compiler now ENFORCES); the multi-record area-class width bug found and fixed

**The spec dissolved the step's premise (feedback_use_the_spec, working as designed):** the mandated failing-first
probe — a key after a REDEFINES whose redefiner is WIDER than its target — turned out to be ILLEGAL COBOL.
§13.18.44.3 SR8: the redefining storage area shall not be larger than the redefined unless the redefined item is
level 1; and SR3 bans level-1 REDEFINES in the FILE SECTION entirely. On every LEGAL record the image and physical
offset bases coincide, so the Sort/Keyed fold is a PURE PORT — the doc's and audit's "divergent basis" concern was
conditioned on a shape no edition permits. What the probe DID expose, in order:
1. **The compiler silently ACCEPTED the SR8-illegal shape** and emitted an overlay with byte-position semantics no
   edition defines (my probe program compiled and ran with garbage key windows). NOW ENFORCED: COBOLNET1539 in
   `ResolveRedefines` (level-1 exception honored; the EXTERNAL nuance noted as unmodeled residue).
2. **`FileModel.RecordWidth`'s flip to `PhysicalWidth` exposed the AREA-CLASS width bug:** both `PhysicalWidth`
   twins skipped ANY child carrying a `.Class` link — but a SUBORDINATE of a class member carries an INHERITED
   link while occupying its own positions inside the member's window, so a multi-record FD's record width
   collapsed to 0 (a latent flaw shared by `OdoModel.PhysicalWidth`, consistently wrong on both sides of the #5
   identity, invisible until a consumer depended on the real value). FIX: the class-backing substitution is
   MEMBERS-gated (`cls.Members.Contains(c)`), and the CANONICAL may itself be a REDEFINER (the classifier picks
   the storage owner, not the target) — a general redefiner pre-skip loses its width, which identity #5 caught
   across 445 NIST programs (16 divergences → 0 after mirroring the exact Members-fixed semantics).
3. A first `OffsetInRecord` draft recursed forever on the class-anchor lookup (the canonical carries the class
   link) — the conformance run ABORTED with a stack overflow at program 732; the anchor resolves through the
   plain walk, as the Sort original did structurally.

**The fold (Steps 8+9 landed together):** `RecordLayout` gains `OffsetOf` (the ONE physical area-offset walk,
OCCURS-bails per §12.4.5.12 SR1/§14.9.40.3 SR6b), `OffsetInRecord` (the Sort class-anchor semantics),
`AreaWidth` (the record-frame rule incl. the multi-record shared area), and `KeyIndexByPosition` (§14.9.41.3
SR6/§14.9.30 SR11/§12.4.5.12.4 GR4 — byte-position key matching); its leaf widths are now DECLARED-shape
(`DataItem.ElementaryImageWidth`, internal) so the whole authority is PHASE-FREE. DELETED: `SortPhysicalWidth`/
`SortOffsetInRecord`/`SortPlainOffset` (binder), `KeyedKeyIndex`/`KeyedAreaOffset` (binder), `KeyedImageOffset`
(the emitter twin), `OdoModel.PhysicalWidth` — grep-zero; identity #5 retired with the last copy. Exit criterion
#4 HOLDS. New goldens (KeyedOffsetSpecTests ×4): SR8 rejection + level-1 acceptance + the §12.4.5.12.4 GR4
sibling-record START operand retrieving by byte position + retrieval past a legal redefinition. Battery:
conformance 3163 (+4) · unit 281 · characterization 32 BYTE-IDENTICAL · Release leg 281 · guard green.


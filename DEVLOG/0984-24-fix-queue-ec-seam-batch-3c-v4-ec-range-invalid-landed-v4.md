## Entry 984 — 2026-07-22 13:36 PDT — §24 fix-queue: EC-seam batch #3c — V4 EC-RANGE-INVALID LANDED (V4 + the whole EC-seam batch COMPLETE)

Increment C of the V4 EC-RANGE family — the last of the four, and with it the whole EC-seam batch (F10 · V3 · V4). §14.7.8
THROUGH-phrase rule 2 (:24863): an alphanumeric/national THRU range whose starting value collates AFTER its ending value
(an inverted range) — in a level-88 VALUE clause or an EVALUATE WHEN range — sets the nonfatal EC-RANGE-INVALID and the
range is treated as EMPTY. A numeric descending range (rule 1) sets NO EC.

**Key simplification (from the design-validation Workflow): "treat as empty" is ALREADY EMERGENT** — both existing
renderers emit the inclusive-bound test `read>=lo && read<=hi`, which is vacuously false when lo>hi, so there was no
behaviour gap, only the missing EC-set. Realized by a new runtime `CobolString.ThruMember(read, lo, hi, collate)` (3
overloads mirroring `Compare` — char-pad / `ushort[]` weights / `NationalCollation`): it sets EC-RANGE-INVALID when
`Compare(lo, hi, collate) > 0` and returns false, else the inclusive bound test. A RUNTIME collate compare (not a
compile-time lo>hi fold) — a native/locale ALPHABET can reorder lo/hi. Emitted via `RuntimeApi.ThruMember` (the ratchet
façade). TWO sites: (a) **level-88** — `ConditionRenderer.RenderMembershipTest` routes the range through ThruMember when
`BoundCondition88.CheckRangeInvalid` (bind-time `Turn.Enabled` capture at BOTH construction sites) AND the category is
Alphanumeric/National; (b) **EVALUATE WHEN range** — a new `BoundRangeMembership` bound node (the source-gen visitor
auto-adapted the `Accept`; `ConditionRenderer.Visit` renders it via ThruMember), produced by `EvaluateBinder` only for a
LITERAL alphanumeric/national range under checking (rule 2 is literal-scoped; a numeric range keeps the plain relation
pair). Both sites are byte-identical when the directive is absent (the inline form stands).

Gate: golden `2023/ec_range_invalid` (valid range no-EC · numeric inverted no-EC [rule 1] · level-88 inverted range EC +
empty · EVALUATE inverted range EC + WHEN-OTHER taken) · characterization **33/33 byte-identical** (the OFF path keeps the
inline `CobolString.Compare` form) · evaluate/condition/ec_range Conformance **147/147** · greenfield Unit **571/571**.

**⭐ THE EC-SEAM BATCH IS COMPLETE (F10 · V3 · V4).** All the once-catalogued-but-never-raised ECs in the batch now raise
spec-correctly, each checking-gated + default-OFF byte-identical, each with a design adversarially validated before code.
⏭ Next: the comprehensive pre-merge gate (full Conformance + legacy guard + GnuCOBOL/NIST differential — the differential
is the required confirmation that the SEARCH `<1` latent-bug fix moves no other program's output) before the batch merges;
then the §24 residue (V45 ref-mod-externality, misc-semantics, minors, doc-slices, owner-decision items).


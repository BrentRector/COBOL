## Entry 562 — 2026-06-10 19:54 PDT — I-O-CONTROL SAME RECORD AREA (§12.4.6.4 GR2) → ST131A + IX205A/IX206A locked (803 conformance)

**The grammar parsed `I-O-CONTROL. SAME RECORD AREA …` and NOTHING consumed it** (both scout briefs found this
independently — st-chain §4.7, rlix C4). Implemented per ISO §12.4.6.4 GR2 (spec read in full): the record-area
format's files "share a memory area for processing the current logical record … equivalent to an implicit
redefinition of the area with records aligned on the leftmost byte position." New `DataBinder.BindIoControl`
(after `BindFileSection`, before classification): chain each listed file's FIRST record as a synthesized
REDEFINES of the first LISTED file's first record — EXACTLY the multi-01-under-one-FD mechanism (singular-
pattern: no second storage-sharing path). The tier machinery aliases every record of every listed file over ONE
backing, and the existing READ/RELEASE image distribution yields the record-of-the-most-recently-read-file
semantics with zero verb changes. A sort/merge file participates per SR6 — ST131A's NOTE says its `READ FILE3`
then `RELEASE S3` (no FROM) are sufficient BECAUSE of the shared area, and that is now literally true here.
Formats 1 (SAME AREA) and 3 (SAME SORT[-MERGE] AREA) are storage-economy permissions (GR1/GR4 — allocation
reuse + program-side open-mode constraints; nothing a typed-native runtime must alias) — bound as conformant
no-ops with the rationale in the doc comment. The SR2–SR11 static legality checks are staged with the Phase-2
EditionValidator (the diagnose-correctly track), noted in the doc comment, not silently absent.

**Locked: ST131A, IX205A, IX206A** (all swept GREEN first try). **803 conformance + 15 unit green.**


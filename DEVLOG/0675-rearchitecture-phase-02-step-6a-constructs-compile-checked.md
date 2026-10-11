## Entry 675 — 2026-07-07 21:33 PDT — Rearchitecture PHASE 02 step 6a — `Constructs.*` compile-checked id-consts at every call site

Turned the magic-string construct ids into the generated `Constructs.<PascalId>` consts (P2.5's `Constructs.g.cs`) at all
54 sites that name a registered id: the 39 direct `ConstructRegistry.Check(…, "id", …)` 3rd-args (EditionValidator ×26,
PicInfo ×13, six StatementBinder partials ×13) plus the 2 `NotImplementedSkeleton(edition, "id", …)` funnel callers
(`pic-external-float-2002`, `national-edited-2002`). Now an unregistered / mistyped id is a COMPILE error, not a runtime
`ArgumentException`. The one genuinely-dynamic site — `PicInfo.NotImplementedSkeleton`'s `Check(edition.Edition, edition,
rowId, where)` — keeps its runtime `rowId` (a string; a `Constructs.*` const flows in fine from the two literal callers).
Applied by a scoped script that only rewrites a literal that exactly matches a registered id in a `Check`/`Skeleton` call
position; the build then proves every `Constructs.X` name resolves.

Pure ergonomics, byte-stable: conformance **2036** · unit **224** (both unchanged — the const value equals the old
literal). Build 0/0. (Step 6b — folding the five inline gates into registry rows — follows.)


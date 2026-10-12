## Entry 902 — 2026-07-19 08:30 PDT — PHASE-13 Wave G — numeric-edited VALUE literal size check (VCR 34) — CLASS A COMPLETE

Landed the previously-deferred VCR 34 (ISO §13.18.63 SR4/SR5; Annex E.2 item 27): at COBOL-2023 an alphanumeric
edited-image literal in the VALUE clause of a numeric-edited item is checked against the PICTURE size — a literal
LONGER than the edited width is rejected with **COBOLNET1570** (before 2023 it was stored truncated — the "unclear
value"). This is the LAST Wave G CLASS-A item; **Wave G CLASS A is now complete with no deferred residue.**

**Spec-first (the deferral resolved).** The remaining-waves scout specified `length == pic.Length`; direct spec reading
gives SR4/SR5 = "shall not **exceed**" (`<=`), so the correct check is `> pic.Length` (an exact-or-shorter literal is
valid, space-padded). The scout's class-mismatch leg (a national literal on a DISPLAY numeric-edited item) is ALREADY
rejected at all editions by COBOLNET0898 (`DataBinder.ValidateValueCategory:1006`), so VCR 34's genuine new surface is
only the ≥2023 SIZE check for a plain alphanumeric literal (leading `"`).

**Design.** A ≥2023 check in the item-VALUE path (`DataBinder`, beside the VCR 86 gate; scoped to the item VALUE, not
level-88): a plain-string VALUE whose decoded length > `pic.Length` on a numeric-edited item →
`Edition.Sink.Report(COBOLNET1570, EditionSeverityPolicy.For(Removed, edition), …)` — Error under strict, Warning
(compile succeeds) under `--permissive` migration mode (the removed-capability posture, matching the matrix's
RemovedConstruct_CompilesPermissive contract, same as VCR 27). Matrix row `value-numeric-edited-oversize-removed-2023`
(introducedIn 85, removedIn 2023). COBOLNET1570 registered in DiagnosticCatalog (DIAGNOSTICS.md regenerated).

**Verified** CLI-probed: `PIC ZZ9 VALUE "ABCDEF"` (6>3) → 1570 @2023 strict, WARNING+compiles @2023 --permissive,
clean @2014; an exact-size `PIC ZZ9 VALUE "  1"` (3=3) is NOT flagged. Gated with the FULL Conformance project.


## Entry 568 — 2026-06-10 21:22 PDT — FILE-NAME qualification (§8.4.2.2) + ADVANCING mnemonic-name (§14.9.46) — SQ207M triaged to its legacy quirk (818 conformance)

**Two SQ-M findings, one fix each + one legacy-quirk classification:**

**1. FILE-NAME qualification:** SQ207M's `WRITE PRINT-REC IN PRINT-FILE` failed loud — `ResolveQualified` only
knew DATA-item qualifiers, and the FD's file-name is the HIGHEST permissible qualifier of its record names and
their subordinates (§8.4.2.2). The resolver now scopes a reference whose outermost qualifier names a file to
that file's record descriptions (the named item may BE a record, not only a descendant). One resolver — every
verb gets it.

**2. ADVANCING mnemonic-name (§14.9.46):** `SPECIAL-NAMES. XXXXX073 IS MNEMONIC-NAME` + `WRITE … {BEFORE|AFTER}
ADVANCING MNEMONIC-NAME`. The positioning for a feature-name is IMPLEMENTOR-DEFINED; the golden encodes the
legacy's rule for the BEFORE form = a ZERO-line advance (the next write welds onto the line). `BindAdvancing`
now binds a SPECIAL-NAMES mnemonic operand (recognized via the ONE mnemonic registry, `AcceptMnemonics`) as 0
lines. **The AFTER form exposed a legacy hole:** the golden bytes show the AFTER-mnemonic write contributed
NOTHING — no text, no newline — i.e. the legacy DROPPED the write entirely, though §14.9.46 GR1 transfers the
record regardless of positioning (the legacy runtime's own WriteAdvancing would have welded the text — the drop
happens upstream in its binder's unresolvable-expression path). COBOL.NET writes the record (conforming).
**SQ207M is DIFF 4 — exactly the two dropped-write rows — and joins the exceeds-golden sign-off queue.**
New SPEC-PINNED test `WriteAdvancingMnemonic_ZeroLineAdvance_RecordAlwaysReleased` pins the weld + the
always-released record + the `WRITE record IN file-name` qualification shape.

SQ101M (DIFF 11, extra blank lines — a different print-control corner) stays on the triage list. **818
conformance + 15 unit green.**


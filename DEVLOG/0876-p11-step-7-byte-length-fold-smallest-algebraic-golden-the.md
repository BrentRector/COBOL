## Entry 876 — 2026-07-17 12:41 PDT — P11 Step 7 — BYTE-LENGTH fold + SMALLEST-ALGEBRAIC golden + the CONCATENATE spec-faithfulness DELETION; ZERO Deferred rows (exit criterion 1 MET)

The last intrinsic wave drives the IntrinsicBind.Deferred backlog to ZERO. The scout notes forced a
spec-faithfulness SCOPE CHANGE on CONCATENATE.

**BYTE-LENGTH (§15.14)** — a compile-time FOLD (BindByteLengthFold beside BindLengthFold; row Deferred→Fold),
the byte-counting twin of LENGTH (the D7 distinction). NEW DataItem.ByteWidth mirrors ImageWidth's group
recursion and PINS COBOL.NET's implementor-defined per-usage byte widths (the spec leaves them to the
implementor, §13.18.60): DISPLAY/BIT 1 byte per position, NATIONAL 2 bytes per position (UTF-16, D-N1),
BINARY/COMP-5/PACKED/BINARY-CHAR..DOUBLE the StorageWidth, COMP-1/FLOAT-SHORT 4, COMP-2/FLOAT-LONG/-EXTENDED
8, index/pointer/object-reference 8. §15.14.3 r1 permits only alphanumeric/national LITERALS (the LENGTH
asymmetry — a boolean/numeric literal is rejected). Ref-mod / ODO / ANY LENGTH stay loud by name (the LENGTH
discipline). The national probe proves BYTE-LENGTH(N(3)) = 6 ≠ LENGTH(N(3)) = 3.

**SMALLEST-ALGEBRAIC (§15.83, new-in-2023)** — the fold pre-existed (BindAlgebraicFold = 10^(-scale)); Step 7
adds the value golden (S999→1, 99V9(3)→0.001, S9PP→100, BINARY-CHAR→1, the §15.83.4 RVR2 NOTE table) + the
2014-window negative row.

**CONCATENATE — DELETED (spec-faithfulness).** The P11 anchor re-scout (PHASE-11-scout-notes.md,
spec:concat-smallest) established that "CONCATENATE" has ZERO occurrences anywhere in ISO/IEC 1989:2023 — not
§15, not the §8.9 name list, not the Annex E incompatibility/substitution lists, not the archaic/obsolete
lists. §15.18 CONCAT is documented as ADDED in 2023 (Annex E.3 item 23). So the phase doc's premise
("CONCATENATE = the 2002/2014 ISO name removed in 2023" → implement with window [2002,2023)) was AUDIT DRIFT
— a 2023 removal of a real 2002 function would appear in the E.2 incompatibility list, and it does not. This
is the exact P10 lesson (the audit drifts; re-scout spec-first) recurring. The greenfield IntrinsicCatalog
row is DELETED (replaced by an explanatory NOTE); a reference to FUNCTION CONCATENATE now correctly draws
COBOLNET1501 at EVERY edition. CONCATENATE is a vendor extension (Micro Focus / GnuCOBOL / ACUCOBOL); adding
it as a dialect-gated extension is a SEPARATE future decision, not an ISO edition window. (The frozen legacy
CobolSharp engine keeps its own CONCATENATE untouched — it is the differential oracle only.)

**AI misstep logged (feedback_transparency):** the initial full-battery run went RED (conformance 3501/3502).
The failing fact was the pre-existing DeferredFunction_InWindow_FailsLoud, which had used BYTE-LENGTH as its
"still-Deferred" example — now a working fold. With zero Deferred rows the test could no longer demonstrate
the §1.4 doctrine on a fully-deferred function, so it was retargeted (renamed StagedLoudResidue_FailsLoud) to
a genuine current loud path: a reference-modified BYTE-LENGTH argument (runtime length, §15.14.4 r5) that
compiles then fails loud by name. The targeted-suite filter had not caught it (it ran CorpusRunner/
VersionMatrix/drift, not the full IntrinsicFunctionDifferentialTests class) — a reminder that the full
conformance suite is the gate, not a targeted subset.

Tests SAME COMMIT: goldens 2002/intrinsics_byte_length + 2023/intrinsics_smallest_algebraic; negatives
byte_length_below_2002 @85 + smallest_algebraic_below_2023 @2014 (COBOLNET1502); matrix rows byte-length-2002
+ smallest-algebraic-2023 (149 rows); 3 spec-pinned facts (BYTE-LENGTH≠LENGTH, the SMALLEST increments,
CONCATENATE→1501); GreenfieldOnly exclusions.

Battery: conformance **3502/3502** (+15 net over Step 6) · unit **301/301** · legacy 1196+636 · guard-fast
**353 MATCH, 0 regressions, ALL GREEN — first pass, no flake**. Deferred set: **2 → 0**. Exit criterion 1
(zero Deferred) MET.


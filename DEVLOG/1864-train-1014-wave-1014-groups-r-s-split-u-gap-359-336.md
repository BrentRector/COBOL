## Entry 1864 — 2026-10-04 11:08 PDT — Train 1014: wave 1014 groups R, S (split), U; GAP 359 → 336

**Group R — PB1402 (relanded) and PB1944.** PB1402's first landing (`6ef69c00c..0753cca0e`) was never merged; group R
cherry-picked it onto main and resolved it against trains 1008-1012. The mechanism: the lexer's word classes were
`[a-z0-9_-]` alone, so `01 CAFÉ PIC X(3)` died COBOL0001 at the É although §8.3.2.1 and §8.1.3.2 GR4 make extended
letters word characters (Annex A.4.6, claimed), and every name table compared words by `OrdinalIgnoreCase`, the
host's invariant upper-casing, which disagrees with Annex C on the extended letters (U+0130, U+212A, final sigma,
Cherokee). The fix: `EXT_CHAR` in `NAME_BODY`/`NAME_TAIL`; `CobolWordRule` screens the finished word against the
edition's Annex B.3 (COBOLNET2773 from 2002; the 2023 E.2 item 4 removals are COBOLNET0902; below 2002 the
construct gate); word length in code points; and ONE Annex C fold, `Runtime/CobolNames`, keying every COBOL-word
table and comparison in Compiler, Frontend and Runtime, with `CobolNameFoldDriftTests` refusing `OrdinalIgnoreCase`
over a word. PB1944 is the CI break the first landing drew: ANTLR read the grammar in the JVM default (Cp1252 on
Windows CI), so the raw noncharacters in `EXT_CHAR` became an empty set; `Invoke-Antlr4CSharp.ps1` now passes
`-encoding UTF-8`, `EXT_CHAR` uses escapes, and `GrammarEncodingDriftTests` pins both. After the report, the owner
dropped the ASCII-only-grammar-code rule (`e93e673c6`, `2d6699a3c` on the branch, carried here): the encoding is
declared, and a character that does not display is written as an escape. Re-probe: `CAFÉ`/`café` displays `abc` at
2002 and 2023, COBOLNET0900 at 85. Goldens 2002/pb1402_extended_letters and 2023/pb1402_extended_letters_2023, four
negatives. Rows GR-8.1.3.2-3, GR-8.1.3.2-4, GR-8.3.2.2-L2.2, SR-8.3.2.2-3, DOC-A.1-219 and DOC-A.1-20 CONFORMS. Its
four leads (Editions word tables, C# identifier keys and default externalized names still on the host mapping;
fixed-form columns counted in UTF-16 units) are kb/Work PB1965 and PB1966.

**Group S — PB1455, PB1426, PB1416, PB1425 (half), PB611 (retired); SPLIT.** PB1455/PB1426: a subscript or reference
modifier written BETWEEN qualifiers (`E (1) OF T`) bound as if written `E OF T (1)`, silently, at every edition,
because every binder flattens `dataReferenceSuffix*`. The grammar stays a superset parse so the refusal can name the
rule; `ExpressionFormationPass` refuses it as COBOLNET2776 in both arms (the `dataReference` arm and the
SUBSCRIPT-mode capture `X (E (1) OF T)`). The self-review found the sibling: the subscript splitter asked §8.4.2.3.3
SR2 of the LAST qualifier only, so legal `X (E OF T OF G (2))` was split and refused COBOLNET2270; it now reads the
whole qualified name. PB1416: `ADDRESS OF FUNCTION WS-NAME(2)` died COBOL0001 because the lexer retyped the `(` as a
function-argument paren; a FUNCTION after `ADDRESS [OF]` no longer heads a function-identifier. PB1425 half:
`object-class-name-1 OF SUPER` through the new `selfAndSuper` grammar rule with SR4 (COBOLNET2777) and GR4's search
root; the identifier-tier remainder stays in PB1425. PB611 did not reproduce (landed by PB974) and is retired. Five
goldens; rows FMT-8.4.2.2.2, SR-8.4.3.1.3-3, FMT-8.4.3.12.2, SR-8.4.3.8.3-4, SR-8.4.3.8.3-6 and GR-8.4.3.8.4-4
CONFORMS. Its two leads are kb/Work PB1967 (a spaced nested subscript in `ADDRESS OF FUNCTION FUNCTION
UPPER-CASE(...)` derails the next statement) and PB1968 (Table 3's unary-unary cell is not screened inside a
subscript capture).

**Group U — PB1429, PB1136 (selector half), PB1165, PB1275, PB1932.** PB1429: a chained inline invocation through a
universal temp ended in the COBOLNET2362 internal error; one §8.4.3.4.3 SR2 predicate now screens every segment
(COBOLNET2138). PB1136: one literal-1 decoder for INVOKE and the inline form through `CobolLiteral.Decode` (the
private copy read `NX"0047…"` as alphanumeric hex), a national or group identifier-2 (SR8; a group selector emitted
CS1503), and SR1/SR3/SR4 a)–i) messages derived from the receiver; INVOKE NULL went from a refusal under a
nonexistent rule to an honest `BoundUnsupported` deferral, and the receiver half (SR-14.9.23.3-1, SR-8.4.3.7.3-2)
stays open in PB1136. PB1165 did not reproduce (fixed by PB1112): verdict-only golden. PB1275: `BoundStores.StoreKindOf`
is total and non-nullable, so an object or pointer property works in every SET format and in ALLOCATE and FREE;
STRING INTO is receiving-only (§14.9.43.3 SR10, §8.4.3.9.4 GR2). PB1932: a property GET in a function argument runs
at its argument's place (`SUM(CNT, P OF A)` gave 2, now 1). Four positives, eight negatives; 12 rows closed,
GR-13.18.42.4-1/-2 PARTIAL under PB1274; semgrep raw-code-literal 283 → 282.

**The train.** Applied R, S, U onto f02e71350 (train 1013 had landed): conflicts only in `DIAGNOSTICS.md`,
`DiagnosticCatalog.cs` and the 2002 manifest (whole rows, both sides), `DRIFT_RULES.md` (regenerated, 266) and one
`ReferenceResolver.cs` line (R's `CobolNames.Same("FUNCTION")` with S's qualified-name split). The inventory hunks
applied cleanly; every S and U `record_verdicts` batch dry-runs as a no-op on the merged tree. Gate 1 (run
20261004T175608Z-e13471) was RED on one Unit case, `CobolNameFoldDriftTests.NoOrdinalIgnoreCase_OverACobolWord_…`:
train 1013 (`008babef7`) had added three `OrdinalIgnoreCase` method- and interface-name tables after R's base. R's
report names this rebase fold as its own step, so the lander folded the three through `CobolNames.Comparer` inside
R's commit and corrected the `Invoke-Antlr4CSharp.ps1` header, which still described the dropped ASCII rule. Gate 2
(run 20261004T180157Z-a4bfd4): `=== BUILD-LOCAL GATE: GREEN — Conformance 10,384/10,384 · Unit 30,559/30,559 ·
Characterization 35/35`; legacy Integration 503 passed, 1 skipped; semgrep counts did not rise. Review over the
train diff: no correctness finding, no cluster dropped. GAP 359 → 336. Codes used: COBOLNET2773, 2776, 2777;
returned 2774, 2775, 2778, 2779-2781. Notes filed: PB1965-PB1968.


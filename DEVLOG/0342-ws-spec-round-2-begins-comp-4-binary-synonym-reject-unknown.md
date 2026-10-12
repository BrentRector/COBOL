## Entry 342 — WS-SPEC round 2 begins: COMP-4 ≡ BINARY synonym + reject unknown COMP-n (CBL0816)

Resuming the M1 spec-conformance drive autonomously (the /loop). A 10-agent read-only diagnosis workflow
root-caused + ISO-version-classified the remaining WS-SPEC backlog (digest in `docs/SPEC_FIX_RECIPES_M1.md`):
the genuinely-'85 M1 set is COMP-4, variable-length SORT/MERGE per-record length, cross-program GLOBAL FD I/O,
table-(ALL)-over-ODO, and Report Writer control breaks; SORT Format-2 self-key, multi-char CURRENCY, CALL
RETURNING/BY VALUE, and READ PREVIOUS are confirmed 2002+ → deferred to M2.

First fix (backlog #1), two parts on one commit:

1. **COMP-4 / COMPUTATIONAL-4 ≡ BINARY (feature).** There was no `COMP_4` lexer token, so `COMP-4` (and the
   invalid `COMP-9`) lexed as a hyphenated IDENTIFIER, out-munching the 4-char `COMP` keyword, and were
   absorbed by the last-resort `genericDataClause` (vendor-extension hook) → `usageClause()` null → the item
   silently became USAGE DISPLAY. Added `COMP_4`/`COMPUTATIONAL_4` lexer tokens, the two parser alternatives
   (usageClause bare form + usageKeyword), and the `UsageMapper` mapping to `UsageKind.Binary`. No runtime
   work — Binary storage/codec is already complete (PicDescriptorFactory/PicRuntime treat Comp≡Binary), so
   COMP-4 inherits exact BINARY layout. COMP-4 is the universal IBM/MicroFocus vendor synonym; it is not an
   ISO keyword in any edition (§13.18.60.2 lists only BINARY/COMP/COMPUTATIONAL/DISPLAY/INDEX/PACKED-DECIMAL),
   but the BINARY semantics it aliases are '85, so this closes an '85-era leniency hole, not a post-'85 feature.

2. **Reject unknown COMP-n (CBL0816).** The flip side of the same hole: `COMP-9` was silently accepted as
   DISPLAY. Added `IsUnsupportedCompUsage` (matches `COMP-<digits>`/`COMPUTATIONAL-<digits>` whose suffix is
   not 1–5, leaving genuine vendor clauses and data-names like `F-WHENCOMP-01` untouched) and a check on the
   `genericDataClause` first IDENTIFIER in `VisitDataDescriptionEntry` → hard error CBL0816. Unconditional
   (default-on) like CBL0815, since an unknown COMP-n is invalid in every dialect. Adversarial corpus
   pre-check first (the CBL3128 lesson — a 0-FAIL dry-run alone is insufficient): the only `COMP-[6-9]`
   matches in tests/nist/programs are the data-names `F-WHENCOMP-01/02`, not usages, so CBL0816 fires on
   zero baselines.

Tests (`SpecFixTests.cs`): `Comp4_IsBinarySynonym_StoresAndComputes` (both bare + USAGE IS forms),
`CompNine_IsRejected_HardDiagnostic`. Guard ALL GREEN: **1047 unit / 418 integration (+2) / 364 NIST**,
0 regressions. Next M1: variable-length MERGE per-record length (runtime-only, `SortRuntime.MergeRecordsInternal`).


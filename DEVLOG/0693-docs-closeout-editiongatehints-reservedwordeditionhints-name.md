## Entry 693 — 2026-07-08 11:37 PDT — Docs closeout — `EditionGateHints` → `ReservedWordEditionHints` (name reflects the permanent reservation-word residue)

**PHASE 02 docs closeout (the bind-time migration's final cleanup).** After the bind-time gating migration
(DEVLOG 680–690) emptied the parse-layer recognizer of all 24 hard-reserved constructs, what remains is the
irreducible **reservation-word residue** — XOR/EXCLUSIVE-OR, the boolean operators (B-AND/OR/XOR/NOT + the
boolean-COMPUTE lookahead), SHARING, RETRY, UNLOCK, PROPERTY — plus the JSON/XML vendor branch. Every ISO arm
here is a §8.9 context-sensitive keyword that is a legal user-defined word below its edition, so its
`{isYYYY()}?` predicate is LOAD-BEARING for tokenization and CANNOT move to bind time (ungating would let the
word bind as a user name and miscompile). The class is therefore a PERMANENT fixture, not a transitional
heuristic — but its name `EditionGateHints` still described the pre-migration "reverse-engineer any edition
gate" role. Renamed to **`ReservedWordEditionHints`** to name what it now is.

- `git mv EditionGateHints.cs → ReservedWordEditionHints.cs`; class renamed; the doc-comment rewritten to
  lead with the reservation-word/load-bearing-predicate rationale and the "hard-reserved constructs gate at
  bind time now" boundary (the DEVLOG-679 R1 forward-stamp warning kept).
- The ONE call site (`CobolErrorStrategy.cs`) + its comment updated; all prose references in code comments
  swept (`GateId.cs`, `StatementBinder.FileLock.cs`, frontend `DiagnosticDescriptors.cs`, the frontend
  `.csproj` comment, `EditionGateDiagnosticTests.cs`, `UdfInvocationTests.cs`). Historical DEVLOG/design-doc
  mentions of the old name are left as-is (they record the state as-of-then).
- `DESIGN-edition-framework.md`: R1 flipped from "*resolution anticipated*" to "**NOW FULLY REALIZED**"
  (the bind-time migration executed; the recognizer is the permanent residue), and Q2 marked ✅ RESOLVED
  by implementation.

Zero behavior change (identifier + comments only). **Battery:** greenfield conformance 2055 · unit 224 ·
characterization 32 GREEN; FULL legacy guard NIST **353 MATCH** (0 regressions), legacy unit 1196,
integration 607 GREEN. RESUME AT step 10.


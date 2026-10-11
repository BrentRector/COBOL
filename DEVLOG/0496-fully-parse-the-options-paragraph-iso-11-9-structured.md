## Entry 496 — Fully parse the OPTIONS paragraph (ISO §11.9), structured; DEFAULT ROUNDED applied (ROUNDED complete)

Owner directive: "divert and fully parse the OPTIONS paragraph so we can use its contents for all remaining
implementations — ROUNDED is just the first." The OPTIONS paragraph (a COBOL-2002 identification paragraph) was an
opaque token blob (`optionsContent : ~DOT+`), accepted-but-not-applied. Replaced it with a **structured parse of all
seven §11.9 clauses** and made DEFAULT ROUNDED the first applied consumer — which also completes the ROUNDED feature
(Entry 495): a **bare** `ROUNDED` now uses the program's DEFAULT ROUNDED mode (§14.7.4.3 r1), not a hardcoded
NEAREST-AWAY-FROM-ZERO.

This touches the SHARED front-end (one ANTLR grammar feeds both the greenfield compiler and the legacy oracle), so it
was done with an ANTLR4-expert design review and the full guard.

**Grammar (idiomatic ANTLR4 — the expert's guidance).** ISO classifies the OPTIONS clause words as *context-sensitive*
(reserved only inside OPTIONS), so the right pattern is a global lexer token **plus** `cobolWord` membership (not a
semantic predicate, not leave-as-IDENTIFIER): 13 new tokens — single-word `ARITHMETIC`/`DEFAULT`/`INTERMEDIATE`/
`ROUNDING` and hyphenated `STANDARD-BINARY`/`STANDARD-DECIMAL`/`ENTRY-CONVENTION`/`FLOAT-BINARY`/`FLOAT-DECIMAL`/
`HIGH-ORDER-LEFT`/`HIGH-ORDER-RIGHT`/`BINARY-ENCODING`/`DECIMAL-ENCODING` — placed before `IDENTIFIER` (maximal munch:
`STANDARD-BINARY` wins over IDENTIFIER on declaration-order tie; bare `STANDARD` and `WC-COBOL` are unaffected). Each
is added to `cobolWord` (so `01 ARITHMETIC PIC X.` stays legal everywhere except inside OPTIONS — no over-reserving)
AND mirrored in the lexer's `_dataNameTokens` set (so a subscripted use of such a name still triggers SUBSCRIPT mode —
a hand-maintained C#-target coupling the lexer comment mandates). Corpus-checked: zero of the 13 appear as
user-defined words in the 494-program corpus. `optionsContent ~DOT+` → `optionsClause+` over seven rule-per-clause
productions (LL(1)-clean — each starts with a distinct token; order-independent, a superset of the spec's fixed
order); `intermediateRoundingMode` is a *separate* 4-mode rule (not the 8-mode `roundingModeName`) so the grammar
enforces §11.9.11's restricted set; ENTRY-CONVENTION's value is `cobolWord` (so `COBOL` need not be reserved). The
INITIALIZE clause (§11.9.10) is fully structured, not a loose capture. Co-change: `DEFAULT` becoming a token let
`initializeDefaultPhrase : THEN? TO IDENTIFIER` (with an aspirational, never-implemented "must be DEFAULT" comment)
tighten to `THEN? TO DEFAULT` (its only consumer null-checks the sub-context). Regen clean — no ANTLR conflicts.

**Consumers.** Legacy `SemanticBuilder.VisitOptionsParagraph` rewired from the ~20-line token-blob scan to the typed
`clause.defaultRoundedClause().roundingModeName()` (the structuring made the only hand-written consumer *simpler* —
the strongest argument for the change). Greenfield: a reusable `OptionsModel` record (all 7 clauses → typed fields,
ISO-implied defaults) built by `OptionsBinder` and exposed program-wide as `DataBinder.Options` (reachable by every
later pass — ARITHMETIC will feed the numeric engine, FLOAT-BINARY/DECIMAL the standard-float USAGE, INITIALIZE the
initial-state fill). The keyword→`CobolRounding` mapping is now a single shared `RoundingModes` helper used by both
the ROUNDED phrase and the DEFAULT/INTERMEDIATE ROUNDING clauses (one canonical mapping). `StatementBinder`'s bare
ROUNDED resolves to `data.Options.DefaultRounding`.

**Verification.** Full guard ALL GREEN — 364 NIST MATCH (0 regressions), 1204 unit, 535 integration (the shared-grammar
change did not perturb the legacy; the `cobolWord` + `_dataNameTokens` membership prevented any token collision).
Greenfield 195 → 200 conformance + 13 unit. New `OptionsDifferentialTests` (5): bare ROUNDED honoring DEFAULT ROUNDED
NEAREST-EVEN (2.5→2) and TRUNCATION (2.5→2) — pinned to spec AND cross-checked against the legacy (which also applies
DEFAULT ROUNDED); the no-DEFAULT-ROUNDED bare-default (2.5→3); a multi-clause `ARITHMETIC IS STANDARD` + DEFAULT
ROUNDED paragraph; and a bare `OPTIONS.` header. The pre-existing legacy OPTIONS acceptance tests stay green.

**Not yet applied (parsed + captured for their owning features):** ARITHMETIC mode, ENTRY-CONVENTION, FLOAT-BINARY/
DECIMAL endianness/encoding, INTERMEDIATE ROUNDING, INITIALIZE fill. They live in `OptionsModel` awaiting the features
that consume them — which is exactly the owner's "use its contents for all remaining implementations."


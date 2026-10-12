## Entry 746 — 2026-07-10 17:09 PDT — PHASE 04 Group D reconciliation + the D10 design note (D10.0 is NOT byte-neutral — a legacy-entanglement finding)

Group D is a RECONCILIATION, not code; and the D10 SUBSCRIPT-mode removal is now a decision-complete DESIGN with two
hard findings that re-shape the owner's "fully remove" ruling.

**Group D — reconciled (no grammar/side-table work):** the design's **construct-id annotation side-table is
SUPERSEDED**. P3 built the two-arm `VersionConformancePass` whose ParseArm walks the RAW parse tree directly, so it
never consults a keyed side-table; installing grammar actions + storage would be dead scaffolding. The superset grammar
is already complete (P3 dropped every edition-REJECTION predicate). **⚠ Reconciled the inaccurate "only two
forward-detects survive" claim (exit criterion 5 corrected):** the grammar retains a small set of load-bearing
CROSS-EDITION DISAMBIGUATION predicates — the 2 forward-detects PLUS `{is2023()}? inlineMethodInvocationStatement`
(ambiguous with a subscripted `x(args)`), `{is2002()}? linkageProcedureParameter`, and the `{!(is2002() &&
LA(1)==PROPERTY)}?` VALUE-list negative lookahead. Each disambiguates a genuine cross-edition syntactic ambiguity
(permitted by the design's own forward-lookahead allowance); NONE rejects a below-edition construct. No new predicate
introduced this phase.

**D10.0 is NOT byte-neutral (the recon's "delete the 4 dead rules, land immediately" was wrong).** The structured
`subscriptList/subscriptEntry/subscriptQualification/relativeOffset` rules are dead in the GRAMMAR, but the generated
`CobolParserCore.SubscriptEntryContext` is still consumed by the **frozen legacy** compiler
(`CobolSharp.Compiler/…/ExpressionBinder.cs:1306 BindSubscriptEntry`); deleting the rules breaks the legacy build
(`CS0426`, caught + reverted). The `SUB_*` tokens are likewise legacy-shared. So the SUBSCRIPT machinery cannot be
removed from the SHARED grammar until the legacy oracle is retired (PHASE 15 / G8), short of modifying the frozen
oracle or forking the grammar. (This is the SECOND recon over-claim caught at implementation this session — the first
was `UsageKeyword`/`ExtractValue` single-caller in Group C. Lesson re-logged: a "dead grammar rule" grep MUST also
sweep the C# consumers of the generated context types, not just the `.g4` self-references.)

**D10 design note authored — `DESIGN-frontend-grammar.md §9.** It surfaces a genuine spec-vs-directive tension:
removing the SUBSCRIPT mode collides with **ISO §8.3.5 space-separated subscript/argument lists** (`X(I J)`,
`MAX(A B)`) and **sign-adjacency** (`+1` vs `+ 1`, the `-15.6` fraction) because DEFAULT mode `->skip`s WS — the mode
exists precisely to keep `SUB_WS` a real token for `SplitSubscriptTokens`. So "full removal" likely reduces to
"replace the flat uninterpreted `SUB_*` stream + the ~250-line hand-rolled C# re-parsers with INTERPRETED grammar
rules, retaining a minimal WS/sign-adjacency lexer assist the spec compels" (§9.4 Option A, recommended). The removal
is a MAJOR multi-stage sub-track (D10.1 corpus → D10.2 ref-mod converge → D10.3 subscript grammar → D10.4 the
Intrinsics.cs recursive-descent rewrite → D10.5 mode delete, the last gated on the G8 legacy retirement per the
entanglement finding) — NOT completable in the byte-neutral Group-A/B/C window, and **gated on ONE owner decision**
(§9.4: preserve ISO space-separators [Option A, needs a scoped WS mechanism] vs require commas [Option B, a spec
narrowing]).

**Phase-04 status:** Groups A + B + C DONE (byte-neutral, all pushed, legacy guard 353 MATCH each). Group D reconciled
(doc). D10 designed + blocked on the §9.4 owner decision + the G8 legacy entanglement. Battery unchanged: 3157
conformance · 227 unit · 32 characterization · legacy guard 353 MATCH.


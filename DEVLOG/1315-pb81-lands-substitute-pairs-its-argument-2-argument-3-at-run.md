## Entry 1315 — 2026-08-18 09:27 PDT — PB81 lands: SUBSTITUTE pairs its argument-2/argument-3 at run time when a table(ALL) is among them

**PB81.** `SUBSTITUTE("aXbXc" SBF(ALL))` over SBF = ("X","Y") was the staged COBOLNET0899. §15.3: an ALL may be
written where "the definition of a function permits an argument to be repeated a variable number of times", and
§15.87.2's `{ [ANYCASE] [FIRST | LAST] argument-2 argument-3 } …` repeats the PAIR — so the enumerated elements
form the pairs by position, "as if each table element … were specified", and a keyword before the enumeration
attaches to the pair its first element opens.

- **The flat form.** An enumeration among the pairs makes the pairing a run-time fact, so `BindSubstitute`
  switches: `Args` = [argument-1, part₁, part₂, …] (a part is a written operand or a `TableAllPlace`),
  `SubstituteModes` carries ONE flag per part (the keywords that preceded it), `SubstituteFlat = true`;
  `RenderSubstitute` emits `CobolIntrinsics.SubstituteFlat(source, string[][] parts, int[] partFlags)` — each part
  its singleton or its `CobolTable.AllArgs` enumeration. The runtime pairs the elements in order, takes each pair's
  mode from its argument-2 element, decides the §15.87.2 shapes only a count can decide — an odd element count, a
  keyword on an argument-3 element, FIRST with LAST — as EC-ARGUMENT-FUNCTION with the zero-length default (rule
  1's disposition), and runs the ONE `Substitute` kernel. Written-only calls keep the bind-time pairing
  byte-for-byte; the staged `substitute-all-subscript-argument` descriptor is retired.
- **The first cut regressed the plain path** — it zeroed the pending flags after argument-2, and the `substitute`
  golden's FIRST/LAST/ANYCASE rows went red at the CLI probe (a pair's mode is the flags recorded on its argument-2
  UNION anything pending before argument-3 — the union the old pairing always applied). Caught before the gate.
- **Measured** (golden `pb81_substitute_table_all`, every value the §15.87.4 result of the written-out call): P2
  `aYbYc`; P3 `aYbYc` (X→Y, Z→!); FIR `aYbXc` (FIRST on the pair the enumeration opens); TWO `bbdbbd` (two
  enumerations, four elements, two pairs); MIX / ODD blank (five / three elements — odd — EC-ARGUMENT-FUNCTION and
  the zero-length default); AC `AYBYc`. Verdicts FMT-15.87.2, RV-15.87.4-1…-5 → CONFORMS (GAP 3980 → 3974).

**Gates (wave-local).** Solution build · Characterization 33/33 · Conformance
`Corpus|Intrinsic|Substitute|Function|All|Nist|Argument` **1554/1554** · Unit
`Intrinsic|Registry|Manifest|Guard|Diagnostic|SpecTraceabilityInventory|Catalog|Loud` **151/151** · CLI probes (the
`substitute` golden byte-equal). Docs: COBOLNET_INTRINSICS_DESIGN.md (the ALL-argument section), DIAGNOSTICS.md
regenerated, kb/Work PB81 → landed, plan §0. Battery owed for the PB92 + PB81 batch with the next landing.


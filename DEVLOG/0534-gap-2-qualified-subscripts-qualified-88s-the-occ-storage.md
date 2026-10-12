## Entry 534 — 2026-06-10 08:53 PDT — GAP-2 qualified subscripts + qualified 88s + the Occ storage-form bridge + alphanumeric figurative comparisons → TWO new NC greens (44 total)

**The reference-resolver completion continues (diagnosis Wave 4 GAP-2 + Wave 9):**
- **Qualified data-name subscripts (ISO §8.4.2.3.2):** `TBL (SUB1 OF SUBSCRIPTS OF PART1)` — the subscript
  splitter now treats a trailing OF/IN as segment-continuation (it split `SUB1 OF GRP` into two subscripts), and
  `RenderSegment` gathers `name (OF|IN qualifier)*` runs through `ResolveQualified`.
- **The `CobolTable.Occ` storage-form bridge:** a subscript data-item read is bind-time TEXT, but whether that
  leaf stores as a native `long` or its character image is decided by the POST-bind whole-group analysis
  (NC206A's `CX-SUB` under a whole-referenced group emitted a string into an indexer). One emitted form —
  `CobolTable.Occ(path)` with `long`/`string` overloads — lets C# overload resolution pick the conversion at
  backend-compile time, whatever the field became. (A bind-vs-emit phase seam the dual-backend discipline
  flagged; the bridge keeps the bound text storage-form-neutral.)
- **Qualifier-aware condition-name selection (§8.4.2.2 Format 2):** duplicate 88 names across tables
  (NC246A declares `EQUALS-M` three times) now select by walking each candidate's conditional-variable ancestor
  chain against the OF/IN qualifiers, innermost-first; subscripted 88 references resolve the SUBSCRIPTS against
  the conditional variable (`ResolveForItem`, §8.4.2.3 Format 2).
- **B3 fixed (diagnosis Wave 0):** `RenderCondition88` read the conditional variable RAW — a `StoreAsImage`
  numeric leaf (whole-group-aliased / Tier-B view) compared its character image to an unscaled long (CS0019,
  NC250A). The numeric branch now renders via THE numeric read path (`NumericRenderer.FieldNum` → ParseDisplay).
- **Alphanumeric figurative-vs-numeric comparisons (§8.8.4.2.1):** a NON-numeric figurative (SPACE/QUOTE/
  HIGH/LOW-VALUE) or `ALL "literal"` against a numeric item is an ALPHANUMERIC comparison — the numeric operand
  participates via its character image at its own width (was a loud guard; NC126A/215A/250A now RUN with diffs).

**Result: NC206A + NC246A byte-match → 44 NC locked in. 426 conformance + 15 unit green.** Now-running-with-
diffs to chase next: NC126A (level-number elementary-item edge, VI-21 5.3.3), NC215A, NC250A (diff 50);
NC104A advanced to computed-expression-in-string-context.


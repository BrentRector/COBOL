## Entry 1377 — 2026-08-29 06:47 PDT — PB136 LANDED: the subscript splitter learns the parenthesized-expression subscript (declaration-informed, the NOTE 2 form) and quotient subscripts take the exact evaluator

Two defects in the D10 transitional machinery. (1) SplitSubscriptTokens never treated a depth-0 '(' as a
segment starter, so the spec's own NOTE 2 form — `DOG (XCOUNTER (- YCOUNTER))`, a parenthesized expression
as the second subscript — collapsed into ONE segment and drew a wrong-subscript-count rejection on the
standard's own example. The repair is the rule-5 restructuring the batch-8 note predicted: after ')' or a
literal a depth-0 '(' always opens a new segment; after an IDENTIFIER the answer is DECLARATION-INFORMED —
`DOG (BAKER (I) 3)` keeps BAKER's paren as its own subscript because BAKER carries OCCURS, while
XCOUNTER's cannot — threaded as an optional isTableName predicate (the resolver passes its unqualified
lookup; the intrinsic-argument caller passes nothing and keeps its exact prior behavior). Both the spaced
and unspaced spellings ride the same helper.

(2) RenderSegment's '/' case spliced C# INTEGER DIVISION over long reads, so `E ((W-A + W-B) / 2)` with
the sum 7 silently selected occurrence 3 where §8.4.2.3.4 GR1b evaluates the exact result (3.5) and
requires EC-BOUND-SUBSCRIPT. A quotient-bearing segment now routes to the D18 exact evaluator
unconditionally — checking ON raises the fatal (probed: "subscript value 3.500000000 is not an integer");
checking OFF takes the documented lenient truncation. PB41's scaled-operand routing caught only segments
whose OPERANDS are scaled — the all-integer quotient was exactly the case it could not see.
THE GATE FOUND THE FIX'S OWN EDGE: the first predicate split a FUNCTION reference from its own argument
list (six goldens red on "0 given") — an UNRESOLVED name may be a function name, so the split now fires
only for a name that RESOLVES to a non-table data item; unknown names keep their paren and fail loud with
the operand named. GR-8.8.1.2-5 DIVERGES → CONFORMS.


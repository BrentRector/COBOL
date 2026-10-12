## Entry 938 — 2026-07-20 17:31 PDT — VALUE clause Format 2 (table, §13.18.63.2, COBOL-2002) — per-occurrence init + the glued-multi-literal reject

Grammar batch position 6. The Format 2 (table) VALUE clause keys a literal list to occurrence subscripts by a
MANDATORY `FROM (subscript-1)` phrase with an optional `TO (subscript-2)` — initializing SELECTED OCCURS-table
elements/ranges, distinct from Format 1 (one literal, every occurrence the same). Before this, the clause did not
parse at all: `valueClause` had no FROM/TO/subscript surface, so even the spec's own Annex-D.3.7 example was a raw
COBOL0001 — a reject-valid-input failure.

Grammar: `valueClause` becomes two arms with the Format-2 arm FIRST (`valueClauseTablePhrase+`) — the mandatory FROM
terminates the operand loop so ALL(*) selects it whenever FROM is present; a bare-list-first ordering would consume
the literals then die on FROM. FROM/TO are not subscript-trigger words, so `FROM (1)` lexes as DEFAULT LPAREN/RPAREN.
No new reserved word. 2002 introduction gate is recognition-fired by `VisitValueClause` (construct
`value-table-format-2002`, COBOLNET0900 below 2002 — the edition is derived: the 2023 Annex E VALUE rows are
numeric-edited only, the A1 authority gap D18).

Runtime (ISO §13.18.63.4): `ValueInitializer.TableValueInit` resolves occurrence→literal (GR12 sequential fill, GR13
cyclic reuse under TO, GR14 no-TO = fill to the maximum, GR15 later-FROM-wins), then emits a fixed table as
`new T[]{ ElemInit(1) … ElemInit(n) }` and a dynamic table as a `CobolDynTable` opened at the GR16 initial capacity
with a per-occurrence seed. `InitializerFor` gained a `rawOverride` so ONE initializer path serves both the item's
own VALUE and each occurrence's literal (singular pattern). `CobolDynTable` gained a `Func<int,T>` per-occurrence-seed
ctor overload; the existing `Func<T>` ctor delegates as `_ => seed()` so every current dynamic-table golden emits
byte-identically. `OccursSpec.InitialCap` stays the FROM minimum (unchanged) — the GR16 capacity is computed at emit
and passed as a new ctor arg, so no model field flips to a second-writer. Annex D.3.7 lands verbatim:
`OCCURS DYNAMIC FROM 1 TO 20 VALUES ARE "Leeds","Bordeaux","Pisa" FROM (1) TO (3)` opens at capacity 3 with the three
names.

⛔ SPEC PRECISION (the verdict's one refutation): occurrences OUTSIDE every FROM..TO range are NOT asserted to default
to spaces/zero — §13.18.63.4 leaves them UNDEFINED; the golden `FixedTable_ExplicitRange` deliberately asserts only
the in-range elements. They take the element's implementation default (not a spec guarantee).

THE GLUED-MULTI-LITERAL REJECT (the paired defect): `ExtractValue` GLUES a bare multi-operand Format-1 VALUE via
`GetText` over the collapsed `valueItem` (the ledger's "binds the first literal" was wrong — it glues). A data-item
VALUE with >1 operand and no FROM is now rejected loud (COBOLNET1585); 88s are untouched (BindCondition has its own
per-operand loop, never ExtractValue). Ran the FULL battery + a corpus grep for the blast radius (a program depending
on a glued value would break).

LANDABLE scope = a single-dimension table on its OWN OCCURS entry (fixed or dynamic). A multi-dimension odometer
(subscript per nested OCCURS) or a table VALUE on an item SUBORDINATE to the OCCURS is recognized, bound, then staged
loud (COBOLNET0899, P14 GAP — the per-occurrence path threading through GroupValueSlicer is the model-change boundary).
SRs: SR20 subscript range (1586), SR21 TO range / TO<FROM (1587), SR22 dynamic-no-TO (1588); SR19 is grammar-enforced
(integerLiteral). Diag 1585-1588 used; 1589-1590 released. Conformance `ValueFormat2Tests` (12): cyclic fill, explicit
range, last-wins, numeric element, the D.3.7 dynamic golden, the glued reject (+ the single-literal non-regression),
SR20/SR21, multi-dim staging, the below-2002 gate. Design docs corrected (COBOLNET_DATA_MODEL_DESIGN §10 + the §"2002
introductions" row; COBOLNET_DESIGN §"VALUE init") — the old "positional one-literal-per-element list" model was a
spec inversion. Shared `.g4` ⇒ full legacy guard.


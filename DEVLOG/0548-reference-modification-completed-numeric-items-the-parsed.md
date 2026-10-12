## Entry 548 — 2026-06-10 14:45 PDT — Reference modification completed: numeric items + the parsed refModSpec form → NC224A byte-matches (85/95)

NC224A (the ref-mod feature program) needed the two missing ref-mod shapes. (1) **Ref-mod over a NUMERIC
USAGE-DISPLAY item** (`TEST-1-DATA(3:)` on PIC 9(6)): §8.4.2.4 — reference modification views the item's STANDARD
DATA FORMAT and the unique result is an elementary ALPHANUMERIC item. New `NumericImagePlace` wraps the typed
place: Read formats the display image (sign-aware), Write decodes the spliced image back via ParseDisplay; the
resolver inserts it for a numeric-DISPLAY inner (StoreAsImage/Tier-B windows are already strings; binary/packed
usage stays loud — usage DISPLAY is the §8.4.2.4 precondition). Two render rules pin the alphanumeric-result
semantics: `OperandText.FieldAsString` returns a RefModPlace's slice directly (never the numeric format path) and
`NumericRenderer.FieldNum` decodes a ref-mod slice as an unsigned integer (Table 16). (2) **The parsed
`refModSpec` form** (`(arithmetic-expression : [arithmetic-expression])` lexed in DEFAULT mode): the resolver now
renders its expression leaf-tokens through the SAME segment renderer the SUBSCRIPT-mode form uses — RenderSegment
accepts both token vocabularies (SUB_* and default INTEGERLIT/IDENTIFIER/operators incl. OF/IN qualifiers).
**85/95 NC GREEN; 566 conformance + 15 unit.** Wave-2 workflow (CALL, RL/IX, ODO implementers + SORT/MERGE and
DECIMAL-POINT scouts) is running in parallel as this lands.


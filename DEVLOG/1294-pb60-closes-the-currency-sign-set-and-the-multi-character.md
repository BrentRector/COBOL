## Entry 1294 — 2026-08-17 22:53 PDT — PB60 closes: the CURRENCY SIGN SET and the multi-character currency string

**The row.** AR-15.68.3-3, PB60's last: §15.68.3 r3 says NUMVAL-C without argument-2 needs "only one currency
string for the compilation unit, either the default currency sign or a currency string specified in the
SPECIAL-NAMES paragraph", and the refuter's three reproductions were all about the binder's model of that
string: `CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "U"` — legal per §12.3.7.3 r23 ("Literal-7 may have any
length") — was COBOLNET0896 "not yet supported"; `CURRENCY SIGN IS "#"` plus `01 WS-P PIC $$,$$9.99` was
COBOLNET0808 "invalid PICTURE symbol '$'" although r25 IMPLIES the `'$'` clause whenever no clause names '$';
and two clauses ('#' then '@') compiled clean while `NUMVAL-C("#1,234.56")` silently returned 0 — the scalar
`CurrencyPicSymbol`/`CurrencyString` pair kept whichever clause bound last.

**The shape.** The scalar pair is gone. `DataBinder.CurrencySigns` is the SET — every currency PICTURE SYMBOL
(uppercase-keyed, r20's letter-case equivalence) → its currency string: the bare form binds one character as
both (r22, the forbidden set through `ValidateCurrencyChar`), the PICTURE SYMBOL form binds a one-character
symbol (r26/r27) to a string of any length (r23a at least one non-space, r23b none of `0-9 + - , . *`), a hex
literal-7 requires the PICTURE SYMBOL form (r19), r21 refuses two clauses binding equivalent symbols to
different strings, and `FinalizeCurrencySigns` adds the implied `'$' → "$"` after the walk unless a clause named
'$' as literal-7 or literal-8 (r25). A PICTURE uses ONE symbol kind (§13.18.40.3 r24/r28): `PictureAnalyzer`
finds which set member a mask uses, CANONICALIZES it to `$` in `PicInfo.EditMask` (position-preserving, so the
EDITING rules' indexes still hold), records the string on the new `PicInfo.CurrencyString` (null when it is
"$"), and adds the string's extra length to `PicInfo.Length` for the first occurrence — §13.18.40.4 GR14 ("The
first occurrence of the currency symbol adds the number of characters in the currency string to the size of
the item. Each subsequent occurrence of the currency symbol adds one"). Its SR2 whitelist admits '$' iff it is a
set member. `CobolEdit.Format/TryFormat/DeEdit` take `currencyString:` per item — `EmitContext.EditCfg(pic)`
replaced the per-unit `EditCfgArgs` and its `currency:` symbol argument at the twelve call sites, the
compile-time `MaskScale`/`MaskCapacity`/`EditCompose` helpers run on the canonical mask — and the multi-character
string touches NO editing rule: the core edits the LOGICAL one-position-per-symbol image, `ExpandCurrency` turns
the one rendered currency position into the string (fixed insertion where the symbol is; floating insertion
where it landed before the first nonzero digit; a floating zero under r6b or a BLANK WHEN ZERO image is all
spaces at the physical width), and `CollapseCurrency` inverts it for the de-editing MOVE (§12.3.7.4 GR13). The
NUMVAL-C / TEST-NUMVAL-C r3 default is `SoleCurrencyString`: "$" with no clause, the ONE explicitly specified
string, and NULL past that — the reference is then COBOLNET1644 (a catalogued descriptor). Reading recorded on
the row: r25's implied '$' is the picture-symbol default, not a competing r3 string, else r3's "a currency string
specified in the SPECIAL-NAMES paragraph" alternative could never be the only one. Grammar untouched.

**Measured after.** `PIC U9.99` with "USD" → `USD5.25` (7 wide); `PIC UUU9` → ` USD12` / `USD123` / `  USD0`
(6 wide); `PIC UUUU` zero → six spaces, 7 → `  USD7`; `PIC $$9.99` beside two declared symbols → ` $1.50`;
`PIC G9` with "GBP" → `GBP7`; the de-editing MOVEs 525 / 000 / 300; a group image `USD5.25X`; BLANK WHEN ZERO
five spaces; `-UUU9` with −12 → `- USD12`; `NUMVAL-C("USD1,234.56", "USD")` = 1234.56; and the bare-'#' unit:
`##,##9.99` → `#1,234.50`, `$$,$$9.99` → `$1,234.50`, `NUMVAL-C("#1,234.56")` = 1234.56, `NUMVAL-C("$1,234.56")`
= 0 (the unit's one string is "#"). Goldens `pb60_currency_sign_set` (16 rows) and
`pb60_currency_sign_bare_implied_dollar` (4) — every leg derived from GR14/r5/r6/r25/GR13/r3 before the run;
negatives `numval-c-ambiguous-currency` (r3, COBOLNET1644), `currency-sign-r21-symbol-rebound` (0891),
`currency-sign-r22-bare-multichar` and `currency-sign-r23-string-digit` (0890); unit
`CobolEditCurrencyStringTests` (18 cases — the seven expand shapes, their collapses, the default-'$' identity,
comma mode expanding after the separator swap, TryFormat's capacity on the logical mask). Inventory: AR-15.68.3-3
→ CONFORMS plus SR-12.3.7.3-21/22/23/25/26/27, GR-12.3.7.4-13 and GR-13.18.40.4-14 adjudicated CONFORMS on the
same evidence (the rules this landing implements). **PB60 is CLOSED — 15 of 15 rows.** The frozen apply plan is
deleted (applied whole); `COBOLNET_NUMERIC_DESIGN.md` D4's edge list and `COBOLNET_DESIGN.md` carry the model.

**Honest edges.** My first cut expanded only multi-character strings — the bare `CURRENCY SIGN "#"` golden printed
`$1,234.50` because a one-character non-'$' string never replaced the canonical symbol; now anything but the
symbol itself expands. My probe used `FD` as a data-name (a reserved word) and I chased two parse errors before
seeing it. The gates: unit 117/117 across the Edit|Picture|Diagnostic|SpecTraceability filters, characterization
33/33; the broad Conformance filter (Corpus|Edit|Move|Special|Currency|Picture|Oo|Report|Value|Group|
SpecTraceability) 2985/2985 — every golden and negative in the corpus, `oo_class_env`'s class-level currency
included; SpecTraceability 10/10 (GAP 4138 → 4129). **The battery on `080733bf`:** Conformance 4381/4381 · Unit
4182/4182 · characterization 33/33 · NIST 353/0 audit-clean · GnuCOBOL differential ONE per-case flip —
`run_fundamental:9066` ("SPECIAL-NAMES CLASS") WE_REJECT_THEY_ACCEPT → AGREE_ACCEPT, GnuCOBOL's own suite
confirming entry 1293's `classValueSet` fix (`CLASS HEXA IS '0' THRU '9' 'A' THRU 'F'`); the baseline row is
regenerated with that attribution. Every leg is green; the flip is a fix.


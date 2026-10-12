## Entry 558 — 2026-06-10 18:30 PDT — DECIMAL-POINT IS COMMA + CURRENCY SIGN end-to-end → NC107A/NC108M byte-match (90/95 NC; 692 conformance)

**The wave-11 SPECIAL-NAMES pair, implemented from the saved scout brief** (`wave2result-decimalPointBrief.txt`)
**+ the four adjacent gaps NC107A/NC108M exposed.** Surface per §12.3.7: `SwitchBindDecimalPoint` /
`SwitchBindCurrency` bind the two clauses into DataBinder config (GR13/GR14; SR21/SR22/SR23/SR26/SR27 validation —
the forbidden-character sets cited verbatim; diagnostics 0890–0896, with `WITH PICTURE SYMBOL` edition-gated 2002+
as 0893 → VERSION_CHANGE_REFERENCE Table 7 row 7.3 + matrix construct `currency-picture-symbol-2002`, and
multi-char currency STRINGS rejected as the M2-deferred size-changing surface 0896). **Literals (GR14a):** ONE
bind-time normalization (`NormalizeNumericLiteral`) canonicalizes comma-decimals to dot form at the established
chokepoint (`CheckLiteral` — `BindPrimary` routed through it; VALUE + level-88 capture call it directly), so the
ENTIRE emit-side decode pipeline is untouched; the wrong-separator cases diagnose as 0895 in BOTH directions (the
legacy accepts both unconditionally — a version-invariant non-conformance, NOT ported). **Editing (SR13/GR14b):**
`CobolEdit` public entries take `(currency, commaMode)`; comma-mode CANONICALIZES the mask (swap `.`↔`,`), the
core logic stays dot-canonical, and `Format` swaps the rendered output back — which realizes GR14b exactly,
INCLUDING zero suppression absorbing grouping PERIODS and stopping at the COMMA decimal (§13.18.40.5 — the
legacy's edit pass reads its comma flag and never uses it; spec-pinned). The hard-coded `'$'` became the currency
parameter throughout (Format/TryFormat/MaskScale/MaskCapacity/DeEdit/FractionDigits); `PicInfo.Analyze` takes the
program symbol so `PIC WWWWW` classifies numeric-edited; ONE `EmissionContext.EditCfgArgs` producer threads the
named arguments into every generated CobolEdit call (empty under the default config — ordinary generated code
byte-unchanged, proven by the census).

**The four adjacent gaps (each spec-cited):** (1) **§13.18.8 GR2** — BLANK WHEN ZERO on a category-numeric
picture DEFINES the item numeric-edited (NC108M's `PIC 9(9) BLANK ZERO` holds SPACES after a zero store and
compares alphanumeric); `PicInfo.Analyze(blankWhenZero)` reclassifies. (2) **§13.18.63 GR6** — a NUMERIC literal
VALUE on a numeric-edited item converts per the MOVE editing rules; the edited image is baked at COMPILE time
(FieldEmitter calls the runtime CobolEdit.Format on the parsed constant). The CCVS-leniency sibling — an
ALPHANUMERIC literal VALUE on a numeric item (`PIC 999 VALUE "000"`) — stores its characters (SR2 wants numeric;
strict rejection queued for EditionValidator). (3) **§13.18.60 GR1** — group-level USAGE now INHERITS to
subordinate elementary items (`InheritUsageClauses`, before the SIGN pass; `DataItem.OwnUsage` captured at entry
bind) — NC107A's `01 U9 USAGE COMPUTATIONAL` children are binary. (4) **Mixed-usage groups in character
contexts** — the typed-native backend DEFINES the binary leaf's character representation as its decimal DIGIT
image (§8.8.4.1.1 leaves it to the implementor): `OperandText.MixedGroupImage` makes whole-group compare/DISPLAY
total over plain COMP-leaf shapes (`IF U22 > U12`), and `CSharpEmitter.AlignedLeafPairs` emits a whole-group MOVE
between POSITIONALLY-IDENTICAL layouts as a memberwise leaf copy (`MOVE U5 TO U9` — for identical layouts the
GR4 representation copy IS the memberwise copy). Non-aligned mixed receivers stay the loud Tier-C island.
COBOLNET_DESIGN §14.4 updated with the settled interim. Also: `NumericRenderer.FieldNum` static→instance (it
now reads the program edit config for DeEdit).

**One more golden-vs-runtime find:** NC107A's figurative information lines print HIGH-VALUE — the golden holds
`?` (the legacy print writer's ASCII fallback) where we wrote raw 0xFF. `SequentialFile.PrintSafe` maps >0x7F to
`?` on PRINT-control writes only (record/data writes keep raw bytes — round-trip integrity); no golden in the
corpus contains raw high bytes (verified). ⚠ The sweep's `diff` counted ZERO lines on the binary-content file —
a FALSE-GREEN hole (the suite harness caught it); prefer the locked suite over the sweep for byte-acceptance.

**Verification:** 692 conformance (677 + 9 DecimalPoint facts + NC107A/NC108M locks + the 4 matrix cells) + 15
unit ALL GREEN; the full 276-program census shows ZERO programs lost and the two targets gained — the
chokepoint normalization provably did not disturb the default literal path. **90/95 NC byte-match; only NC105A
remains actionable (NC214M/303M no-golden, NC235A/236A spec-pinned-exceeding).** Frontend/legacy untouched.


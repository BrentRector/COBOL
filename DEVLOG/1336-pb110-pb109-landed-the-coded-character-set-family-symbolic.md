## Entry 1336 — 2026-08-19 18:08 PDT — PB110 + PB109 LANDED: the CODED CHARACTER SET family — SYMBOLIC CHARACTERS binds (a figurative constant at last), CLASS … IN resolves ordinals in THE NAMED alphabet's set (a silent wrong answer flipped), the FD CODE-SET clause parses both formats with its SRs checked and the identity sets CLAIMED, and the alphabet-name class condition is live — one `CodedCharacterSet` model, one resolver, COBOLNET1670–1672; 15 inventory rows (GAP 3857 → 3843)

**The family** (ISO §12.3.7.4 GR7 NOTE 2 + Table 6): an alphabet-name references a COLLATING SEQUENCE in the PCS /
SORT / MERGE positions and a CODED CHARACTER SET in exactly four others — the class condition (§8.8.4.4.4 GR3 a),
`SYMBOLIC CHARACTERS … IN` (GR11 b/c), `CLASS … IN` (GR12 a), and the FD `CODE-SET` clause (§13.18.13). kb/Work
PB110 measured all four dark: SYMBOLIC CHARACTERS was accepted-INERT (bound by nothing — every reference then died
as an undefined name), `CLASS BYORD IS 66 THRU 68 IN REV` built the class from the NATIVE ordinals (the IN phrase
silently ignored — a WRONG ANSWER a class test then acted on), CODE-SET carried the '85 one-name grammar (the 2002
`FOR ALPHANUMERIC IS …` format was a raw parse error — rejects legal source) and no binder read it, and the
§12.3.7.3 SR16g / SR17d / §13.18.13.3 SR1–SR3 rules could not fire. PB109: `IF X IS STD` (an alphabet-name class)
was a loud staged value.

**One model, one resolver.** `CodedCharacterSet` (`Binding/CollatingModel.cs`) is the set an alphabet defines —
`CharAt(ordinal)` / `OrdinalCount` / the phrase — with the ordinal↔character DETERMINATIONS documented in
CONFORMANCE.md: the native sets are the UTF-16 repertoire in code-unit order (ordinal n = code unit n−1 — the SAME
correspondence FUNCTION CHAR / ORD use); STANDARD-1/2 are the 128 ISO/IEC 646 IRV characters (GR7 c's implementor
correspondence is the identity — the native set's first 128 ARE ISO 646); UCS-4/UTF-8 are the 10646 scalar values
(ordinals skip the surrogate block; a supplementary character is ONE ordinal, a surrogate PAIR of code units); a
literal-phrase alphabet's ordinals ARE its collating positions + 1 (GR7 k4's latitude — the unspecified characters
follow the highest specified position in native relative order, so the set is TOTAL; the sparse national table's
inverse is computed arithmetically). `DataBinder.CodedCharacterSetOf(name, site, rule)` is THE resolver every site
calls: a LOCALE alphabet (Table 6's one blank coded-set row) is COBOLNET1669 citing the site's own rule, an
undeclared name COBOLNET0898.

**SYMBOLIC CHARACTERS** (§12.3.7.4 GR11; SR16 → COBOLNET1670, sub-rule named): each name defines a FIGURATIVE
CONSTANT whose value is the character at ordinal integer-1 in the native set of the clause's class or the IN
alphabet's set; names pair with integers positionally per entry, one-to-one (SR16 b/c), once per paragraph (a).
The figurative substitutes at the SAME seams a §13.10 constant-name does — `FieldOperand` (operands everywhere),
the VALUE raw-text capture (`ALL"c"` — the GR2 fill), INSPECT's operand arm — plus the new grammar arm
`figurativeConstant : … | ALL cobolWord` (Format 7's explicit ALL; LAST, so keywords and literals win; the sites
carrying their OWN ALL adjective — INSPECT's `(ALL|LEADING…)`, UNSTRING's `DELIMITED BY (ALL)?` — consume it
greedily first, which is the correct reading). The node is `BoundAllLiteral` (ONE carrier for the fill semantics)
with a new `BeginsWithAll` flag: FALSE for the bare form, so the three screens that bar "a figurative constant
that begins with the word ALL" (STRING/UNSTRING SR2, INSPECT SR3) fire on the WORD, not the semantics — the first
cut tripped STRING's screen on a bare symbolic sender, which §8.3.3.6.4 GR3 b makes ONE legal character
(golden line STRING-SC).

**CLASS … IN** (GR12 a; SR17 b2 → COBOLNET1671): a numeric literal is the ordinal within alphabet-name-4's set —
`CLASS BYORD IS 1 THRU 3 IN REV` (REV = "Z" THRU "A") is {Z, Y, X} now, and the golden pins 'ZYX' in / 'ABC' out
where the old code made BOTH false at these ordinals and 'ABC' TRUE at 66–68. Out-of-range ordinals (which used to
DEGRADE TO RAW TEXT silently, even without IN — 70000 fell through the int filter into the member string) are 1671.
Both CLASS and SYMBOLIC CHARACTERS bind AFTER the SPECIAL-NAMES walk (the clauses are order-free — an ALPHABET
declared later in the paragraph resolves; the `ResolveProgramCollating` shape).

**CODE-SET** (§13.18.13; COBOLNET1672): both formats parse — `IS alphabet-name-1 [alphabet-name-2]` and the FOR
phrases (one or both, any order, each at most once; the national half gated below 2002, `code-set-national-2002`);
SR1/SR2 class checks through the resolver, SR3's one-class / usage-display / SIGN-SEPARATE rules over the record
items. **The GR2/GR6 determination, documented (CONFORMANCE.md §2 row 27):** the sets whose correspondence to the
native set is the IDENTITY — NATIVE, STANDARD-1/2, UTF-16 — are CLAIMED, their conversion being the identity
(golden `pb110_code_set_identity`, a byte-exact round trip); an alphabet whose on-medium representation would
DIFFER (a literal alphabet's remapped ordinals; UTF-8/UCS-4 as variable-width medium encodings) is the DOCUMENTED
Annex A §A.3 item 27 non-support ("dependent upon a device capable of supporting the specified code") — refused
with 1672, NEVER a silent identity, which for an EBCDIC-shaped alphabet would be a wrong answer.

**PB109 — the alphabet-name class condition** (§8.8.4.4.4 GR3 a): `BoundCodedSetClassCondition` → the binder maps
the phrase to a membership KIND — Ascii (STANDARD-1/2), ScalarValues (UCS-4/UTF-8 — an unpaired surrogate is not a
character of these sets), AllNative (NATIVE / UTF-16 / a literal phrase — the total set, deliberately) — rendered
as `CobolClass.IsInCodedSet(s, kind)`; GR1 zero-length false, GR2 NOT; golden `pb109_class_condition_alphabet`
(ASCII yes/accent no/NOT/space-is-a-member/literal-total). The `GR-8.8.4.4.4-3` row stays PARTIAL on the float
items g)–m) alone (FARTHEST-FROM-ZERO … NEAREST-TO-ZERO have no binder arm — loud).

**Tests.** Goldens `2023/pb110_symbolic_and_class_in`, `2023/pb110_code_set_identity`,
`2023/pb109_class_condition_alphabet`; negatives `pb110-symbolic-locale-alphabet` (1669), `pb110-symbolic-sr16`
(1670), `pb110-class-ordinal-range` (1671), `pb110-code-set-nonidentity` (1672), `pb110-code-set-locale-alphabet`
(1669); `CodedCharacterSetTests` (the per-phrase correspondences incl. the literal-table and sparse-national
inverses; the three membership kinds). Inventory: 15 rows — SR-12.3.7.3-16/-17, GR-12.3.7.4-11/-12,
SR-8.3.3.6.3-4, GR-8.3.3.6.4-2/-3, FMT-13.18.13.2, SR-13.18.13.3-1/2/3, GR-13.18.13.4-1/2/7 CONFORMS;
GR-8.8.4.4.4-3 re-adjudicated (item a landed; PARTIAL on g–m) — GAP 3857 → 3843.

**Docs.** CONFORMANCE.md §2 row 27; COBOLNET_DESIGN §18 item 14a (the coded-character-set model — written as part
of registering PB110, corrected to as-built); DESIGN-locale-facility §7 (h) ✅; DIAGNOSTICS.md 1670–1672; kb/Work
PB110 + PB109 landed; plan §0.

**Gates.** Wave-local RED then attributed and re-run GREEN per case: the first full run (Conformance 3003/3008 · Unit 4474/4474 · Char 32/33) carried exactly six reds — the five `code-set-national-2002` matrix cases (the constructs.json probe program was malformed: a stray second ENVIRONMENT DIVISION and an undeclared STD — source fixed, ALL VersionMatrixTests re-run 2102/2102 GREEN) and `RuntimeApiGuardTests.Bare_runtime_accesses_do_not_increase` (the new coded-set render was a BARE `CobolClass.` access — routed through the nameof-anchored `RuntimeApi.ClassInCodedSet` façade per the P7 Step 4b ratchet, never a baseline bump; the emitted text is byte-identical and the golden re-probed exact). Full Unit 4474/4474 and Characterization 33/33 re-run GREEN after the fixes.. ⚠ Battery #26 is OWED for the accumulated batch (PB111 + PB110/PB109) —
plan §0 says so; it is the NEXT SESSION'S FIRST ACT before any code change.

**Misstep, honestly.** The first `CodedCharacterSetOf` used `a?.CodedSet ?? n!.CodedSet` — for an alphanumeric
LOCALE alphabet (CodedSet null) the `??` fell through to the NATIONAL dictionary's null entry and threw NRE; the
probe caught it before any commit. And the STRING screen over-fired on the bare symbolic form until the
`BeginsWithAll` flag separated the WORD (what SR2 bars) from the fill semantics (what the node carries) — the
golden's STRING-SC line now pins that separation.


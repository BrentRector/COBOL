## Entry 2147 — 2026-10-17 04:14 PDT — PB780: a constant-name stands at every literal slot of the grammar, and a generated check holds it

kb/Work PB780 is the whole-language admission rule §13.10.3 SR2 ("constant-name-1 may be used anywhere that a format
specifies a literal of the class and category of constant-name-1"; `cite.py --check` OK), with §13.10.4 GR1's
substitution ("as if literal-1 … were written"). Its 2026-10-07 adjudication listed fourteen positions (V1-V14) that
refused a constant-name while the written literal passed.

**Re-probe on this branch's build.** The w1033 probe sources (run with `--source-format free`, since fixed form is now
the default) show V6, V7, V8 and V14 fixed by PB1942 and V9 (PROGRAM-ID / FUNCTION-ID `AS KX`) fixed too. Still refused:
V1 `CALL K` (COBOLNET1639), V2 `CALL K AS NESTED` (1676), V3 `CANCEL K` (1760), V4 `ADDRESS OF PROGRAM K` (1639 + 2157),
V5 `RD … CODE IS KC` (2713), V12 `INVOKE C K` (0866), V10 `VALUE … FROM (K)` (COBOL0309), V11 `OPTIONS. INITIALIZE …
TO KF` (COBOL0001) and V13 `O :: K (Y X)` (COBOL0001).

**Mechanism 1, the binder arms.** V1-V5 and V12 are spelled `literal | dataReference` by the grammar, and each binder's
data-reference arm went to the identifier resolver without asking the one literal-alias resolution
(`ExpressionBinder.LiteralAliasOf`, PB1544). The program-name positions now share ONE reader,
`Binding/Procedure/Verbs/ProgramNameLiteral.cs`: CALL literal-1 (§14.9.4.3 SR2) and its AS NESTED form (SR15), CANCEL
(§14.9.5.3 SR2), the program-address-identifier (§8.4.3.13.3 SR2) and the vendor `SET … TO ENTRY`. It takes the written
literal or the alias, requires an alphanumeric or national literal that is not zero-length, and forms the name with
`ExternalizedNames.FormInEdition`. Sweeping the siblings found a second copy: SetBinder decoded the ADDRESS OF PROGRAM
literal itself, without trimming spaces (PB1539) or using the edition spelling (PB2840), and SET ENTRY read only
STRINGLIT. Both now go through the one reader. CallBinder reads literal-1 once (`Literal1Of`, alias first), so SR15's
presence test, the NESTED scope lookup and the static target agree. INVOKE treats the alias as literal-1 before the
identifier-2 arm (`OoMethodNameOf(LiteralAlias)`, §14.9.23.3 SR2/SR3). The CODE clause reads a constant as literal-1
under §13.18.12.3 SR1.

**Mechanism 2, the grammar positions.** V10: §13.18.63.3 SR19 makes subscript-1 and subscript-2 integer numeric literals,
so the grammar's new `valueTableSubscript : signedIntegerLiteral | cobolWord` admits the word, and
`DataBinder.TableValueSubscript` substitutes an integer constant or refuses the word by SR19.
`SignedIntegerSlotDriftTests` now names `valueTableSubscript` as the one signed slot. V13: `inlineInvocationSegment :
COLONCOLON (literal | cobolWord) …`, with a bare-word overload of the alias resolution
(`LiteralAliasOf(CobolWordContext)`; `LiteralAliasResolutionDriftTests` now also bars `FindConstant` / `NamesSymbolic`
outside it). The lexer needed one more rule: `OnDefaultLParen` had typed the '(' after `:: K` as a reference paren,
because K could be a data-name. A word after `::` is literal-1, so its '(' is now the plain argument-list LPAREN, as it
is after a written literal.

**Goldens** (expected values derived in each header from §13.18.63.4 GR13/GR14, §8.4.3.13.4 GR2 and §13.18.12.4 GR1):
`conformance:2002/pb780_constant_names_name_programs` (IN SUB ×3, IN NESTED, IN GREET, `[QHELLO]`),
`conformance:2002/pb780_constant_names_literal_operands` (`ZABA`, `PQQS`, `BBBAAA`). Negatives:
`pb780-call-numeric-constant` (§14.9.4.3 SR2), `pb780-code-national-constant` (§13.18.12.3 SR1) and
`pb780-value-subscript-not-integer-constant` (§13.18.63.3 SR19).

**V11, the OPTIONS fill (wave 1100 B).** V11 was a phase-order problem, not a missing arm: `OptionsBinder` bound four OO
units' OPTIONS paragraphs (`OoDriver`) and every method's before the unit's constant entries were declared. The OPTIONS
paragraph now binds at one point, `DataBinder.BindDeclarations`, after the constants, for every unit kind, so
`INITIALIZE … TO KF` reads `KF CONSTANT AS X'2A'` as literal-1 (§11.9.10.3 SR1). Golden
`conformance:2023/pb780_constant_name_options_fill`, negative `pb780-options-fill-constant-not-hex`.

**The generated check (wave 1101 B).** `ConstantNameLiteralPositionDriftTests` derives every literal reference of the
parser grammar from the `.g4` text (rules as nested alternative blocks; one-line rules included). A literal with no word
alternative beside it must be argued as a position no constant-name stands in: a level-number (a user-defined word,
§8.3.2.2.13), the 1985 STOP literal and the non-ISO ENTRY. A slot that admits a word must be WITNESSED: an enabled 2002+
positive golden whose parse writes a declared constant-name in that rule's word arm, so the strict compile shows the
binder admitted it. The alternative is an argued row: a compiler directive's operand (SR2's own "Except in a compiler
directive"), the declined SCREEN, VALIDATE and message-server facilities, the deleted VALUE OF and PADDING CHARACTER
clauses, and ASSIGN TO / LOCALE, whose reading is kb/Work PB2500's open latitude. The integer-n positions stay with
`IntegerOperandSlotDriftTests`. Over main's grammar from before PB780 the check is red at `inlineInvocationSegment`,
`optionsInitializeFill` and `valueClauseTablePhrase` (V13, V11, V10). A witness alone cannot see a binder arm that
admits the word and then reads it wrong, so each witness is an output golden, not a compile-only one.

**V15, found by the check.** Its first run left eleven word-admitting slots unwitnessed, and writing their witnesses
found one more refusal: the constant entry's own literal-1. `01 K2 CONSTANT AS K1.`, K1 alphanumeric, was COBOLNET1547,
because `constantValue` parses a lone word as `arithmeticExpression` and the arithmetic path admits only numeric operands.
§13.10.2 specifies literal-1 there, so `DataBinder.ConstantLiteralOperand` makes the entry a copy of the named constant's
class, category and text (§13.10.4 GR1/GR2). A numeric constant is then a single numeric literal (§13.10.3 SR1): `AS KF`
keeps KF's 0.25, where the expression form would truncate it (GR4). A word that closes a cycle stays on the arithmetic
path, which reports §13.10.3 SR5 once. The pre-bind reader `PreBindConstants.ValueOf` follows the same chain, so a
PROGRAM-ID AS phrase naming such a constant reads it as the data bind does.

**Goldens (wave 1101 B).** `conformance:2002/pb780_constant_names_slot_witnesses` (`K2=AAA`, `NX=NAT`, `Q=1` from KG*4
with KG AS KF = 0.25, `M=14` from MULTIPLY 2 BY KN, `R=AAAAAA` from an inline invocation's constant arguments, `LV=0007`
from CALL AS NESTED BY VALUE KN, `IN ENTRY` from SET PP TO ENTRY KE, then STOP RUN WITH NORMAL STATUS KZ, under
PROGRAM-ID AS KP2, which is AS KP1). Negative `pb780-constant-as-constant-cycle` (§13.10.3 SR5).

**Row.** `SR-13.10.3-2` PARTIAL → CONFORMS, editions 2002, 2014, 2023 (GAP 60 → 59 on the merged tree). kb/Work PB780 landed.

**Self-review** found two leads outside the mechanism, filed through the report: the SELECT PADDING CHARACTER operand is
never bound, so `PADDING CHARACTER IS NOSUCH` compiles clean at 2002; and two private sole-data-reference descents
(`OoBinder.OoExtractBareReference`, `SearchAllFormat2Rules.SoleDataReference`) duplicate `SoleOperand.DataRef`.

**Branch review, round 1.** R1-1: V15's copy restated `Specification` (§13.10.3 SR9's subject, the AS operand as
written) as the WORD KX, and the two rules that ask literal-1's FORM read `Specification`. So `KY AS KX AS X'2A'` drew
COBOLNET1727 at `INITIALIZE … TO KY`, and `PICTURE SYMBOL KY` over `KX AS X'23'` compiled (§12.3.7.3 SR26). A FROM
constant already failed both ways, because its specification is `FROM name`. The fix splits the field:
`ConstantDef.WrittenLiteral` is literal-1 as written when it is one nonnumeric literal token. `AS KX` inherits it, and a
FROM entry takes it from the DEFINE operand (`CtValue.WrittenLiteral`; §13.10.4 GR1, "the text represented by
compilation-variable-name-1"). The OPTIONS fill and the CURRENCY screen read it; SR9 keeps `Specification`. The spec
has no third form rule. Golden `2023/pb780_constant_name_options_fill` gains `U=[JJ]` and `V=[HHHH]`. New negatives:
`pb780-currency-symbol-constant-chain-hex` and `-from-hex`. R1-2: a grammar comment named COBOLNET3980, which does not
exist; it now names COBOLNET1547.

**Gate** (run `20261016T224025Z-bc695d`): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — leg 2 left
to CI's whole-population run: Conformance 5,795 · Unit 497 · Characterization 31 …`; every added golden, negative and
`ConstantNameLiteralPositionDriftTests` case Passed in leg 1. semgrep verify: PASS, counts unchanged.

**Gate after the review fixes** (run `20261016T231310Z-bc0b58`): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating,
PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 5,740 · Unit 442 · Characterization 31 …`; the
extended golden and both new negatives Passed in leg 1. semgrep verify: PASS, counts unchanged.

**Finisher (landing stop).** The landing's Linux gate went RED on the merge `26b822385`:
`SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree` found three rows (GR-11.9.4-1,
GR-11.9.5.2-4, FMT-11.9.11.1) still citing `OptionsBinder.cs#Bind`, the member V11 deleted. The cause was the branch,
not the host: the Windows gate's leg 1 never ran the sweep, because it costs more than the cheap-sweep cut
(`SWEEP_CHEAP_SECONDS` 2 s; it ran 6 s), and the Linux gate runs the whole unit population. The fix retires the
witness through `record_verdicts.py` with its reason and names the code that replaced it:
`OptionsBinder.BindParagraph` and `DataBinder.BindDeclarations` for GR-11.9.4-1 (the container's model as the
baseline), and `OptionsBinder.Apply` for FMT-11.9.11.1 (the INTERMEDIATE ROUNDING clause). The merge of the newer
origin/main conflicted on `SR-13.10.3-2`, which main's PB3173 had re-noted for its editions. The inventory was taken
from main and the branch's adjudication applied over it as a batch, keeping main's editions sentence. Every
code-location in the merged inventory now resolves (4,195 rows).

**Finisher gates** at `61162c799`: Windows (run `20261017T102755Z-5d2c3b`) `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched
gating, PB2515): GREEN — leg 2 left to CI's whole-population run: Conformance 5,787 · Unit 409 · Characterization 31 …`;
Linux `=== LINUX GATE: GREEN (legs selftests unit characterization conformance guard selftests-built; HEAD 61162c799) ===`,
with `EveryCodeLocation_ResolvesInTheTree` Passed in its unit leg.


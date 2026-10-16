## PB780 (part 1): a constant-name stands at nine more literal positions; the OPTIONS fill and the generated check remain

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

**Not done in this part.** V11 is a phase-order problem, not a missing arm. `OptionsBinder` binds four OO units' OPTIONS
paragraphs (`OoDriver`) and every method's (`DataBinder.OoBindMethodData`) before the unit's constant entries are
declared. The note records what the fix needs. `SR-13.10.3-2` stays PARTIAL until V11 lands and the generated check the
note describes exists.

## Entry 839 — 2026-07-12 15:17 PDT — P7 Step 12 COMPLETE (PHASE-07 CLOSED) — FUNCTION args are real expressions; the IntrinsicRenderer static channel is DELETED

**The grammar route, in full (the phase doc's preferred option — decision logged here as required).** FUNCTION
arguments now PARSE as real trees: `functionCall : FUNCTION functionName (LPAREN functionArgList? RPAREN)?`, each
`functionArgument` one of the §8.4.3.2 SR8 shapes (`fnArgPhraseWord | OMITTED | nonNumericLiteral |
arithmeticExpression`), and every arithmetic argument binds through the ONE `ExpressionBinder.BindExpr` — nested
`FUNCTION` calls recurse naturally through `BindPrimary`. The ~170-line hand-rolled recursive-descent argument
parser (`ParseAdditive…ParseArgPrimary` + `ArgExprVisitor` + the token utilities) is DELETED OUTRIGHT — no reduced
fallback survives anywhere: the D2 keyword-omitted form re-parses its captured argument text through the SAME
`functionArgList` rule (`Frontend.Parsing.FunctionArgFragment`, the fragment lexer primed via
`CobolLexer.PrimeFunctionArgs`), and `UdfBinder.UdfBindCall` binds through the same `IntrinsicBinder.BindArgOperand`.

**The two lexer blockers, resolved spec-first.** (1) The SUBSCRIPT push: `(` after `FUNCTION functionName` is ALWAYS
the argument list (§8.4.3.2 SR6), so a two-token lookbehind (`_prevNonWsTokenType == FUNCTION`) suppresses the push —
the keyword-omitted `name(args)` form (no FUNCTION token) still captures SUB tokens, and a nested `TBL(I)` inside an
argument still enters SUBSCRIPT mode (the D10/PHASE-15 subscript-string deferral is untouched). (2) The
space-separated-argument hazard: §8.7.1 requires every arithmetic operator to be preceded AND followed by a space,
and §8.3.3.3.2 r2 makes a literal's sign its leftmost CHARACTER — so `MAX(A -4)` is TWO arguments and `MAX(A - 4)`
one subtraction. DEFAULT mode skips WS, so argument-region-gated `SIGNED_INTEGERLIT`/`SIGNED_DECIMALLIT` twins
(`{InFunctionArgs() && SignedLiteralCanStart()}?` — a paren-region stack + a char-level LA(-1) separator check)
preserve the distinction, re-typed to the SUBSCRIPT-mode token types so the parser sees one vocabulary.

**Two rounds of corpus-driven fixes (7 failures → 0).** Round 1: (a) phrase words (FIND-STRING ANYCASE etc.) now
surface in `cobolWord` name slots and tripped the §8.9 funnel's 0901 — a BARE function-argument word is a §15
phrase-word position, NOT a provable user-word slot, so `IsBareFunctionArgumentWord` (sole-child expression-spine
walk) skips it; a declaration collision still fires at its provable declaration slot. (b) `MIN(A * B, (3 + 1) / 2,
…)` (IF119A/IF123A): with the §8.3.5 comma separator SKIPPED in DEFAULT mode, the `(` after `B` mis-lexed as B's
subscript — the `FNARG_SEPARATOR` token (`{InFunctionArgs()}? [,;] [ \t\r\n]+`, argument regions only) keeps the
argument boundary alive for both the whitelist action and the `functionArgList` rule. Round 2 (differential): the
legacy shim dropped a NESTED call's inner separator — mapped to `SUB_COMMA`/`SUB_SEMICOLON`.

**The frozen oracle adapts, thinly.** Legacy `BindFunctionCall` consumes the reshaped CST via `MapFunctionArgTokens`
(per-argument leaf tokens → SUB-token twins → its proven `BindSubscriptSegment`; one argument = one segment, the
grammar already did the separator split) — behavior-identical, proven by the FULL legacy guard: **NIST 353 MATCH, 0
regressions**, legacy unit 1196, legacy integration 610 (one NEW GreenfieldOnly row, below).

**The emit side: ONE channel.** `IntrinsicRenderer`'s static string channel
(`NumStatic/NumStaticExpr/StaticAdditive/StaticMul/IntStatic` + the static visitors) is DELETED; `RenderString` is
INSTANCE and renders numeric arguments through the ONE `NumericRenderer` under `ReceiverContext.None` — division,
float items, nested numeric intrinsics, and numeric-edited de-edits now render where H3 stayed loud.
`OperandText.AsString` takes the per-unit renderer (the intrinsic-operand case intercepted at entry; a new
`FieldImage(Place)` entry serves the Place-holding callers); `NumericRenderer.Render/AsNum` are save/restore
RE-ENTRANT so the mid-render default-receiver call cannot stale the ambient receiver (the H1 class stays closed by
construction). The whole file routes through `RuntimeApi` (new `Intrinsic`/`DateFn`/`EcFn`/`ModuleNameFn` anchors +
the `DateFormat21`/`DateFormatFractionDigits` compile-time passthroughs) — the ratchet's `IntrinsicRenderer` entry
(47) is DELETED. Characterization stayed 33/33 with snapshots BYTE-EXACT (the render text is fragment-identical).

**A latent spec bug died with the hand parser.** `ParsePower` folded `**` RIGHT-associatively, mis-citing §8.8.1;
§8.8.1.2 r3 says consecutive same-level operations execute LEFT to right — `BindExpr`'s left fold is correct, so
`MAX(2 ** 3 ** 2, 1)` = 64 (was 512 on the deleted path; the corpus had ZERO `**` in arguments). The frozen legacy
still folds right — `func_expr_arg` is GreenfieldOnly with the citation. Also spec-aligned: `MAX(4,7)` (comma NOT
followed by space is no separator, §8.3.5 r2) now diagnoses via the comma-decimal literal path (COBOLNET0895)
instead of silently splitting, and under DECIMAL-POINT IS COMMA it is the literal 4.7 (§12.3.7 GR14a) — the old
splitter split BOTH unconditionally; no corpus case exercised either. New diagnostics: COBOLNET1543 (malformed
keyword-omitted argument list), COBOLNET1544 (OMITTED as an intrinsic argument, §8.4.3.2 SR7).

**New nets.** `IntrinsicFunctionDifferentialTests` Step-12 section: compound/division args differential (incl. the
IF119A/IF123A shapes + a nested compound), sign-adjacency spec pins, `**` associativity, string-channel division +
numeric-edited de-edit (ORD∘CHAR round-trips), ref-mod argument live (§8.4.2.4), `FUNCTION PI ()` empty parens (the
SR6 NOTE shape), keyword-omitted expression args, OMITTED→1544. Corpus: `tests/conformance/2014/func_expr_arg.cob`
(the phase-doc probe — division/compound/paren/sign/table(ALL)/string-channel cases) + negative
`func-omitted-arg`. Battery at the boundary: conformance 3166→3177 green · unit 282 · characterization 33
(byte-exact + ratchet) · NIST 353 MATCH.

**AI missteps (transparency).** (1) The first grammar cut forgot that the skipped `COMMA_SEP` also erased argument
boundaries the old SUB-token capture preserved — two NIST programs caught it; the FNARG_SEPARATOR fix followed the
evidence, not the plan. (2) The legacy shim initially passed a nested call's FNARG_SEPARATOR through unmapped,
silently dropping arguments — the differential bake caught it (the golden differed from the greenfield BEFORE the
legacy was consulted properly). (3) An early `RenderSubstitute` draft used a `TrimEnd(')')` hack to fit the
RuntimeApi wrapper — replaced with the correct argument-string composition before it ever built.


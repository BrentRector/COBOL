## PB2621: one reader of a literal's delimiters and the comment indicator

Wave 1096 E, an R3 unify wave of the PB1754 architecture review (kb/Work PB2621, R2 finding
editions-diagnostics-1--duplication--f1#2).

**Re-probe.** The duplication was still there on the base (1fc8daf67): the literal-aware scan was written five times
in three types — `CompilerDirectiveLine.StripInlineComment` and `.SeparatorsAsSpaces` (two quote-tracking loops in one
type), `TextWordScanner.LiteralEnd` and `.DecomposeLiteral`, and `LogicalLineBuilder.ScanProgramText` — the `*>` test
was spelled in four places, and the §8.3.5 5) opening-delimiter prefix was parsed twice (`TextWordScanner.LiteralPrefixLength`
forward, `LogicalLineBuilder.LiteralKindAt` backward, only the second refusing a prefix glued to a word).

**Fix shape.** `CobolSourceText` in Cobol.Net.Editions, beside `CobolSpace`, is the one reader: `CommentIndicator` /
`CommentIndicatorAt` (§6.2.3.1), `IsQuotationSymbol`, `TryOpeningDelimiterAt` → `OpeningDelimiter` (§8.3.5 5)),
`ClosingDelimiterAt` (the one doubled-quotation test, §8.3.3.2.3 3)), `LiteralEnd`, `LiteralContent`, `InlineCommentAt`.
Every member reads a span and looks no further than its end, so the caller that carries a literal across a line end
(the §6.5 conversion) keeps that state and the caller that bounds a literal at its line (the text-word scanner) passes
the line. Every caller changed in this change: `StripInlineComment`, the unused `CompilerDirectiveLine.InlineComment`,
`LiteralPrefixLength`, `LiteralKindAt` and the private `LiteralKind` enum are deleted; `ScanProgramText` keeps only its
cross-line `PendingClose` state, the continuation indicator and the directive-after-text arm; the debugging-line
carrier and the multiple-character floating indicators name the reader's constant. `DESIGN-frontend-grammar.md`
describes the reader. A stale cref (a three-argument `CompilerDirectiveLine.TryParse` that does not exist) in
`TextWordKind.DirectiveLine` is corrected.

**Neutrality.** `python scripts/arch/compare_oracle.py --base 1fc8daf67`: `=== ARCH-ORACLE: IDENTICAL — 8011 cases`.
The prefix-glued-to-a-word refusal now holds for every caller; for the text-word scanner it changes nothing, because a
text-word never starts right after a word character (a character-string runs until a separator, a quotation symbol or
`&`).

**Tests.** `CobolSourceTextTests` pins each member against the rules (expected values read from §8.3.5 5),
§8.3.3.2.3 3), §6.2.3.1); `LiteralScanDriftTests` refuses a stage that reads program text before the lexer (all of
`Preprocessor/`, the directive-line parse, the END-PERFORM locator in `Frontend.cs`) and spells the comment indicator,
the quotation-symbol pair or a doubled-quotation test. Its first gate was RED on `HostWitnessTraitDriftTests`: it
carried `[Trait("Sweep", "tree")]` while scanning no tree, so the trait came off and the test names its own scope,
which `drift_rules.py` now lists as a specific rule of each stage file. Three stages outside this wave's declared file set still carry their
own spelling (`CobolWordsDirectiveProcessor.Tokenize` / `ReadQuoted`, `ConditionalCompilationProcessor.FindRangeWord`,
the END-PERFORM locator in `Frontend.cs`); a `PendingStages` list that may only shrink holds them, and the follow-up is
reported as a lead.

**Rows.** No verdict changed. A witness batch re-sited three rows' code locations to the reader (GR-7.2.2.5-2:
`LiteralPrefixLength` → `TryOpeningDelimiterAt`, `LiteralEnd`; SR-7.3.3-3 and SR-7.3.3-4: `StripInlineComment` →
`InlineCommentAt`), GAP 78 → 78.

**Gate.** See the report for the verdict line of `pwsh scripts/build-local.ps1 -Priority BelowNormal`.

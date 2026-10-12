## Entry 1340 — 2026-08-28 22:22 PDT — PB64 T6 waves 2+3: PICTURE format 2 and the NUMVAL-C/TEST-NUMVAL-C LOCALE keyword END TO END — the two COBOLNET1518 arms flip, COBOLNET1673/1674 allocated, PB113 (report-writer silent accept) and PB114 (the required IS) fixed, six goldens + three negatives

The compiler half over wave 1's runtime (Entry 1339), from the 7-agent hardening workflow's 55-step edit plan.

FRONT END: `pictureLocaleAhead()` loses its edition gate (the ORDER TABLE precedent — no data description
clause begins with a user word, so LOCALE after a PIC_STRING has no '85 reading; without this the
picture-locale-format2-2002 row's 85 cell died on a raw ANTLR error at SIZE instead of COBOLNET0900);
`pictureLocalePhrase` becomes `cobolWord (IS? cobolWord)? SIZE IS? integerLiteral` — the required IS rejected
legal source (PB114; the IS is a non-underlined optional word, §13.18.40.2 figure note / §5.2.3).

MODEL + BIND: `PicInfo.LocaleEdit` (`LocaleEditSpec` — LocaleRef + SIZE + the canonical picture; Length =
integer-1 per §13.18.40.4 GR17, EditMask NULL — and EditMask's doc REWRITTEN in the same commit: that "null for
every other category" sentence was what licensed every `pic.EditMask!` deref, reachable-null after T6);
`PictureAnalyzer.AnalyzeLocaleEdited` (the five-symbol alphabet with the currency symbol classified against the
unit's CURRENCY SIGN set — never a hard-coded '$'; Table 11's closed form `[+] [cs] Z… 9… [. Z…|9…]` — row Z's
9-column is BLANK so no 9 precedes any Z; SR33/34/35 + the EDITING-beside-LOCALE pairing → COBOLNET1673 with
the sub-rule named; SR36 subsumed by the position checks; ⛔ NO compile-time relation between integer-1 and the
digit count — the currency string's length is a runtime locale quantity; DECIMAL-POINT IS COMMA INERT per
§12.3.7.4 GR14 — the draft had it backwards, the hardening critic corrected it against SR13's FORMAT 1 heading);
DataBinder: the 1518 arm → the format-2 capture (SR37 through the ONE COBOLNET1664; SIZE ≥ 1), §13.16.3 SR19 →
COBOLNET1674 beside the float-edited SIGN check, SR32's TWO halves (same-entry beside the CONSTANT RECORD
ladder; subordinate in BindEntries where the parent chain exists), ValidateNumericValue's locale legs (the empty
mask made signBearing false — a legal negative VALUE on a '+' picture was falsely rejected — and storedScale 0);
the REPORT-WRITER arm (PB113 — `DataBinder.Reports.cs` read only PIC_STRING and analyzed format 2 as format 1,
a LIVE silent wrong answer; one analyzer, three callers; the SIGN non-arm asserted — §13.15.3 has no SR19 twin);
the SCREEN twin (§13.17.3 SR9) rides A.4.13's recognized-and-warned posture — decided and recorded, not silent.

EMIT — the dispatch stays SINGULAR (the IsFloatEdited doctrine): `RuntimeApi.EditFormatFor` gets the locale arm
FIRST (before the mask deref); `EditTryFormatLocale` (the §14.7.5 capacity twin — EC-SIZE-TRUNCATION is digit
CAPACITY, EC-LOCALE-SIZE is r14 b's CHARACTER truncation, distinct conditions on one store);
`ReceiverScaleOf` — the ONE receiver-scale rule replacing the SAME RULE WRITTEN TWICE (MoveEmitter.SenderContext
+ ArithmeticEmitter.ScaleOf, both silently falling to Scale 0 for a locale item — G16's two-copy defect; ⚠ the
first cut special-cased float-edited to 0 and flipped a DIVIDE golden to 0.00000E+00 — the OLD MaskScale-on-the-
float-mask behavior restored and commented); MoveEmitter's fixed lane routes through EditFormatFor (two
`EditMask!` NREs gone); ArithmeticEmitter's locale arm before the mask arm (a COMPUTE into a legal format-2
receiver LoudStmt'd as "non-fixed-point target"); NumericRenderer's de-edit arm; the THREE VALUE-image
producers (`LocaleEditCompose` — RUNTIME images at the field initializer, the group-image codec and the level-88
membership: no compile-time image exists, §13.18.40.5 r11); `EmitCore.EditCfg` → "" for locale items (no
currencyString:/commaMode: — a leaked named arg is a generated CS1739); HIGHEST-/LOWEST-ALGEBRAIC's locale arm
(the MaskCapacity deref was a reachable NRE); OoConformance's locale-identity axis (null==null masks passed two
DIFFERENT locales; `LocaleSymbol.SameLocaleAs` — never a second identity test); MoveTable16.IsEdited.

NUMVAL-C: `BindNumvalCFamily`'s r5 arm — LOCALE [locale-name] recognized in the positional walk, the
numval-c-locale-phrase-2002 gate, argument-2 × LOCALE mutual exclusion (§5.2.6.2), the r3 sole-currency
injection SKIPPED under LOCALE, and `BoundIntrinsicCall.LocaleWritten` — Locale defaults to Current on every
node, so a bare LOCALE (the r5a current-locale form, a DIFFERENT accepted language) and no phrase at all were
indistinguishable at the renderer (C11); THREE renderer arms — native, TEST-, and the RenderDec lane the
alwaysDec list routes EVERY standard-arithmetic NUMVAL-C through (fixing :248 alone leaves --arithmetic
standard indexing the absent injected argument-2). `LocaleUnsupported` + the 1518 raise DELETED;
`IntrinsicBind.Unsupported` (zero rows since T7) deleted with its three stale comment sites; the 'l'
argument-kind row names BindNumvalCFamily (the drift test demands the reason name a real method).

TESTS: goldens `2002/pb64t6_picture_locale_smoke` (en-US ASCII + the current-locale ORD witness) /
`_locale_edit_rules` (the r14/r15 battery — every byte hand-derived) / `_locale_edit_dpc` (byte-identity under
DECIMAL-POINT IS COMMA) / `_locale_size_ec` (EC-LOCALE-SIZE BOTH ways — the silent suppressed-space and
literal-zero arms pinned, the raising digit/separator arms observed by declarative) / `_numvalc_locale` (the
parens-negative DEFAULT path, int_curr_symbol, ANYCASE scope, r1b.1's "0 1"→3, EC-LOCALE-MISSING at use) /
`_locale_value_88` (runtime VALUE image, level-88, INITIALIZE, REDEFINES width = SIZE, the ref-mod garbage →
EC-DATA-INCOMPATIBLE de-edit); negatives pb64t6-picture-locale-{syntax,sign,constant-record}; INVERSIONS —
pb100-picture-locale-format-2 SPLIT (its P1 was LEGAL format 2 all along — moved into the smoke golden; P2
stays the SR34 negative at 1673), locale_keyword_a49 → COBOLNET1664 (its LOC1 is undeclared — the design's
"invert to accept" was wrong, the critic caught it), LocaleDispositionTests → NumvalCLocalePhrase_IsLive +
BareKeyword + the alternatives-exclusion fact (AssertA49 deleted, zero callers), LocaleModuleNonSupportTests →
TheNumvalCLocaleKeyword_IsEditionGated (the class summary's forcing-function paragraph ANSWERED).

Process note, logged per the transparency rule: one wave-local gate run was VOIDED — launched before the wave-3
edits, then the tree was edited and rebuilt while its --no-build legs ran, exactly what §0's freeze rule
forbids; the run wedged and was killed, and the clean gate re-ran whole on the final tree. The 5 matrix reds its
interim output showed (the numval-c row's cells before wave 3 landed) were the sequencing mistake of adding a
construct row one wave before its bind.


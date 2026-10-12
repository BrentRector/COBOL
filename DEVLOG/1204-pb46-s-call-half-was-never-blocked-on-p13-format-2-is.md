## Entry 1204 — 2026-08-07 14:40 PDT — PB46's CALL half was never blocked on P13: Format 2 is selected by a SYNTACTIC `AS` phrase whose NESTED arm needs no registry

**PB46 is CLOSED, and its own blocker was a misdiagnosis.** The note said, twice and confidently:

> "⛔ AND THE FORMATS ARE NOT DISTINGUISHABLE AT PARSE TIME. Both are `CALL {identifier|literal} USING …`; what
> selects Format 2 is whether the called name resolves to a program-prototype declared in the REPOSITORY — a
> SEMANTIC question, exactly the D2 shape."

Both halves of that are false, and the rendered §14.9.4.2 general format says so:

```
Format 2:  CALL { identifier-1 | literal-1 } AS { NESTED | program-prototype-name-1 }
Format 1:  CALL { identifier-1 | literal-1 }                        ← no AS phrase at all
```

The discriminator is SYNTACTIC. And the AS brace has **two arms with different dependencies**, only one of which
needs the registry: `AS NESTED` (§14.9.4.3 SR13/SR15 — a COMMON or directly-contained program named by
literal-1) needs nothing that does not already exist, while `AS program-prototype-name` (SR16) genuinely needs
the §12.3.8.2 program-specifier that `repositoryEntry` lacks. **The whole half was filed as blocked because the
note reasoned about the formats instead of reading the figure** — the THIRD time this item's premises did not
survive contact with its own general format (`feedback_validate_the_premise_not_only_the_rule`).

**LANDED:** `callAsPhrase : AS cobolWord`; the binder splits the arms, applies SR15 to NESTED and names P13 for
the other; `callByContent` carries the Format-2 operand set, narrowed at bind so Format 1 keeps
`{ identifier-2 } …` only. `CALL "PB46IN" AS NESTED USING BY CONTENT N + 1` now passes 6.

⚠ **NESTED IS NOT LEXED AS A TOKEN, DELIBERATELY.** §8.9 reserves it from 2002, but this repo enforces
reservation through the `VisitCobolWord` funnel on the word's SPELLING — which is how its sibling MODULE-NAME
phrase words (CURRENT / ACTIVATING / STACK / TOP-LEVEL) already work. Tokenizing it would break
`FUNCTION MODULE-NAME(NESTED)`, force an `fnArgPhraseWord` entry, and make it asymmetric with those siblings, all
to answer a question the binder answers in one comparison. The funnel gained the same position exemption PB27
gave the LOCALE clause, scoped to exactly the word NESTED — because the OTHER arm's word IS a user-defined word
and must keep being funneled.

⛔ **§0's SHARED-GRAMMAR CAUTION FIRED, EXACTLY AS WRITTEN.** I removed `dataReference` from `callByContent` the
way PB46's INVOKE half had removed it from `invokeArgument` — and the build broke: `callByContent` lives in the
SHARED `CobolParserCore.g4` and the LEGACY binder reads `callByContent.dataReference()`. "A shared-grammar change
must be ADDITIVE until P15" is not a style note; the accessor is generated and its removal is a compile error in
a compiler that shares the file. Restored, and the rule's comment now records why it differs from
`invokeArgument`'s.

⭐ **AND THE BATTERY WENT NOT-GREEN ON TWO GUARDS THAT WERE BOTH RIGHT ABOUT MY OWN WORK.** Conformance
4247/4247, NIST 353 MATCH, differential 0 flips — everything about BEHAVIOUR passed; what failed was
CONSTRUCTION:
1. **`BooleanExpressionGateSiteDriftTests` — the guard written two items earlier for exactly this — caught the
   fourth grammar site the moment it appeared:** "grammar rule(s) admit a booleanExpression with no introduction
   gate: callByContent". It derives its site list from the `.g4` files, so a hand-maintained list would have said
   nothing. `VisitCallByContent` is now Site 4.
2. **`COBOLNET0899` is a SPLIT CODE** and my four new emits used bare string literals. `DiagnosticRegistryDrift`
   named all four lines; they are now three descriptors carrying their clauses, plus the regenerated
   `DIAGNOSTICS.md` that the sibling guard would have demanded next.

**REMAINING, both loud and cited rather than silent:** `AS program-prototype-name` (P13 — the REPOSITORY
program-specifier), and `boolean-expression-1` under CALL's BY CONTENT (Format 2 admits it; `BoundCallArg` has no
counterpart to the INVOKE side's `ContentBool`).

**ALSO IN THIS COMMIT — PB27 §③ RESOLVED BY REMOVING A CLAIM RATHER THAN GUESSING A YEAR.** The
`LOCALE-TIME-FROM-SECONDS` introduction edition is not derivable from anything this repo holds, and the search is
now recorded so nobody repeats it: §15.54 has no note, §8.11 lists names without edition data, Annex E covers
only 2014→2023, the reserved-word tables exclude intrinsic names (§8.11, not §8.9), `specs-private/` holds the
2023 PDF alone, GnuCOBOL's testsuite pins no `-std=` on these functions, and there is no `cobc` to query — the
differential harness reads their EXPECTATIONS, never runs them. So `IntrinsicBinder` now suppresses COBOLNET1502
when `Bind = Unsupported`: the diagnostic asserted a FACT about the standard we cannot substantiate, and it was
redundant anyway because §A.4.9 non-support rejects at EVERY edition. The suppression keys on `Bind`, so
implementing the locale module restores the gate — forcing the year to be verified then, which is the right
forcing function.

**GATE.** `=== BATTERY: ALL GREEN ===` on ONE whole run — Conformance **4247/4247** zero skipped · Unit
**4040/4040** · characterization **33/33** · guard-fast ALL GREEN with NIST **353 MATCH / 0 REGRESSION**, audit
CLEAN, legacy Unit 1203/1203 and Integration 503/504 (1 skipped) · GnuCOBOL differential 1323 cases, **0
PER-CASE FLIPS**. The FULL battery because `CobolParserCore.g4` is shared with the legacy compiler. Golden
`pb46_call_format2_as_nested` (6 assertions incl. the Format-1 controls) · negatives
`pb46-call-format1-content-expression` and `pb46-call-as-prototype-name` · `LocaleModuleNonSupportTests`
(4 editions + the failing direction: TRIM keeps its 1502 gate).


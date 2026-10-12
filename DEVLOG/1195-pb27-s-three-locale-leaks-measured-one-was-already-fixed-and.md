## Entry 1195 — 2026-08-07 03:40 PDT — PB27's three locale leaks, measured: one was already fixed and hiding a FALSE diagnostic, one had an unnamed sibling, and the third's premise is refuted by the spec

**Not one of the three bullets was what the entry said it was**, and the entry was six days old. This is the
standing §0 caution — a queue entry's own root cause is a CLAIM — applied to an entry whose claims were all
plausible and all stale in different ways.

**① "The SPECIAL-NAMES LOCALE clause is SILENTLY ACCEPTED."** It is not, and has not been since PB25 landed
`localeClause`. But asking only "is it silent?" stops one line too early:

```
SPECIAL-NAMES. LOCALE FRENCH IS "fr_FR".
  ⛔ COBOLNET1518: the SPECIAL-NAMES LOCALE clause is in the optional locale module …   ✅ true
  ⛔ COBOLNET0901: 'LOCALE' is a reserved word in COBOL-2023 and cannot be used as a
                  user-defined word (ISO §8.9)                                          ⛔ FALSE
```

`LOCALE` is not a lexer token, so §12.3.7's `localeClause` matches the clause's own KEYWORD through `cobolWord`
and the §8.9 funnel read it as a user-defined word. **A true diagnostic and a false one, printed together** — and
the false one tells the reader to rename something they never named. The funnel already carries exactly this
exemption for `ENTRY-CONVENTION`'s value slot and for `EXCEPTION-OBJECT` in an object reference, so LOCALE joins
them — **position-exact**, because the clause's three `cobolWord` slots are three different kinds of word:
[0] the keyword (exempt), [1] locale-name-1, a genuine user-defined word (**stays funneled**), [2]
external-locale-name-1, an implementor name whose values §12.3.7 GR5 leaves to the implementor (exempt).
Exempting the clause wholesale would have hidden a real §8.9 violation in slot [1]; both directions are pinned.

⛔ **NO GATE COULD HAVE SEEN THIS, AND THE REASON IS STRUCTURAL: a negative fixture's `.err` names ONE code.**
`pb25-special-names-locale-a49` asserted COBOLNET1518 was PRESENT and has no way to ask whether anything ELSE
was, so it stayed green through the whole defect (`feedback_green_test_can_hold_a_gap_open`). The new assertion
is a `DoesNotContain` — a shape the fixture format cannot express, which is why it went in the unit suite.
⚠ The first probe of the FAILING direction was itself wrong: I tested slot [1] with `MOVE`, which is a lexer
TOKEN, so it died in the parser and never reached the funnel at all. `LOCALE` itself is the probe that works —
reserved at 2002+ AND lexed as an identifier.

**② The `ArgKinds` row was wrong — and so was the line above it, which the entry did not name.** `LOCALE-TIME`
read `"is"` (argument-1 as an integer); `LOCALE-DATE` is the preceding catalog line with the identical wrong
value. §15.52.3 r1 and §15.53.3 r1 both say "class alphanumeric or national … 8 [resp. 6] character positions",
and the normative Table 21 agrees for both (`Anum1 or Nat1, Loc2`). ⚠ **Annex D disagrees and does not govern** —
D.31.4.2 calls argument-1 "a date in standard date form (YYYYMMDD)", which reads as an integer and is very
likely where the `'i'` came from, but Annex D is marked `(informative)`. A concepts annex is not a rule.
⚙ The column is DEAD for these rows (`Bind = Unsupported` ⇒ 1518 fires first at every `--std`, measured with both
an integer and a spec-correct alphanumeric argument) — which is precisely why it was never contradicted
(`feedback_a_dead_lookup_is_also_unverified`). PB1 is the standing proof of the opposite mistake's cost.

**③ The `IntroducedIn 2002` claim is UNCHANGED, because its own premise is refuted by the standard.** The entry
argued LOCALE-TIME-FROM-SECONDS belongs to the 2014 package because argument-1 is in standard numeric time form
(§15.5.5) — "a form no 2002 function produced". §15.80.1: "The SECONDS-PAST-MIDNIGHT function returns a value in
**standard numeric time form**", and that function is catalogued 2002. Real citation, real rule, false premise —
`feedback_validate_the_premise_not_only_the_rule`, twice in one note.
⚠ And there is no authority either way in the 2023 standard: §8.11 lists the intrinsic NAMES with no edition
data, Annex E covers only 2014→2023, and `IntroducedIn` is hand-assigned. The field is LIVE — at `--std 85` it
prints `COBOLNET1502: … was introduced by ISO/IEC 1989:2002`, a factual claim to the user — so it is not free to
guess at. **Disposition: no change, with the refuted argument recorded so it is not re-run.** No owner question
was raised, because the argument FOR changing it collapsed; there is nothing to decide until someone holds a
copy of ISO/IEC 1989:2002 or :2014.

**GATE.** Unit **4023/4023** zero skipped (4021 + the two new locale assertions) · Conformance
`~Corpus|~Negative|~Intrinsic|~Locale|~ReservedWord|~VersionMatrix` **2668/2668, zero skipped**.


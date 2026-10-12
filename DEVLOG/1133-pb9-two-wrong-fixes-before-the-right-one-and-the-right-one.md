## Entry 1133 - 2026-07-30 16:58 PDT - PB9: two wrong fixes before the right one, and the right one came from the functions' own general formats

PB9 as I wrote it yesterday evening said: `RANDOM` is the one zero-argument intrinsic unreachable without the
FUNCTION keyword, scope "measured at exactly one word", root cause "it lexes as a reserved word". Every clause of
that was either wrong or imprecise, and finding out how was most of the work.

**THE SCOPE WAS WRONG BECAUSE I HAD SWEPT THE WRONG SET.** I had swept the nine ZERO-ARGUMENT intrinsics, which is
why only RANDOM showed up. The set that matters is **§8.9 reserved words ∩ §8.11 intrinsic function names** —
a reserved word cannot be a data name, so it cannot arrive through the ordinary keyword-omitted route. That
intersection is **four**: LENGTH, RANDOM, SIGN, SUM. LENGTH already worked; SIGN and SUM take arguments, which is
exactly why a zero-argument sweep could not see them.

**AND THE MEASUREMENT FOUND A TRANSCRIPTION DEFECT.** My first intersection returned 88 intrinsic names when §15
defines more. §8.11's list had six names RUN TOGETHER on one line —
`SUM TAN TEST-DATE-YYYYMMDD TEST-DAY-YYYYDDD TEST-FORMATTED-DATETIME TEST-NUMVAL` — where every other entry is
one per line. Rendering printed folios 213 and 214 shows why: `SUM` ends one page and `TAN…` opens the next, so
the join is a page boundary the transcription kept. Repaired with a word-conservation assert (409,851 = 409,851);
`lint_rendering.py` clean; the list now reads 94. `fix_transcription_errors_immediately` — the defect was found
while doing something else and repaired then, not listed.

**THE FIRST FIX WAS WRONG AND NIST CAUGHT IT.** `cobol-words.json` has a `nameSlot` flag that admits a word to the
parser's `cobolWord` funnel; LENGTH carries it, so copying that for RANDOM/SIGN/SUM looked obvious. Four
Conformance failures, all NC116A, one root cause:

```
01  WRK-DS-LS-5 PICTURE S99999   VALUE ZERO
        SIGN LEADING SEPARATE.        ->  COBOLNET1585 "takes exactly one literal"
```

A name-slot word is admissible as a VALUE-clause literal (a constant-name), and the operand loop is greedy — so
it swallowed the SIGN CLAUSE of the following line as a second VALUE literal. **LENGTH is safe in that slot only
because LENGTH does not BEGIN a data-description clause.** SIGN does (§13.18.53) and SUM does (§13.14). The
generator's pinned `subscriptTrigger-only` invariant had already refused to emit until I updated it and said
exactly which constant to touch — a guard doing its job before the tests even ran.

**THE SECOND FIX WAS WRONG THE SAME WAY, WHICH IS THE PART I SHOULD HAVE PREDICTED.** I moved the words out of
`cobolWord` into a dedicated `functionCall` alternative and called it "confined to expression positions, so it
cannot reach a data description". It reaches one in a single hop: `valueClauseOperand : unaryExpression` →
`primaryExpression` → `functionCall`. Identical failure. I asserted confinement instead of tracing the rule, and
the second attempt cost what the first one had already paid for.

**THE FIX THAT WORKS IS DERIVED, NOT GUARDED.** §15.81.2 writes `FUNCTION SIGN ( argument-1 )` and §15.88.2
`FUNCTION SUM ( { argument-1 } … )`: neither has a no-argument form, so **a bare `SIGN` or `SUM` is never a
function reference at all**. Requiring the argument group in the grammar makes `SIGN LEADING` unmatchable as a
functionCall, so the VALUE-clause collision is structurally impossible rather than guarded against — and the
same applies to the report-writer `SUM OF` clause. RANDOM keeps its bare form because §15.75.2 brackets the whole
parenthesised part, and RANDOM begins no data-description clause. The alternative I would have written as an
ad-hoc "don't continue the loop into SIGN or SUM" predicate is unnecessary: the arity rule already says it.

**THE ARGUMENT CARRIER CAME FROM A DUMP, NOT FROM REASONING.** These words carry `subscriptTrigger=true`, so with
no FUNCTION token before them the lexer pushes SUBSCRIPT mode at the '(':

```
COMPUTE N = SUM(1 2 3)  ==>  COMPUTE IDENTIFIER EQUALS SUM LPAREN SUB_INTEGERLIT×3 SUB_RPAREN
```

So the alternative takes a `subscriptPart` and re-parses through the SAME D2 `ReparseArgs` path every other
keyword-omitted reference uses — one argument grammar, not a second. My first attempt at this alternative used
`LPAREN functionArgList RPAREN` and simply did not match; the dump explained it in one line where reading the
lexer had not. `CobolLexerModeDriftTests` now pins the carrier for all three.

**Ledgered, not fixed here:** `LENGTH(A)` with NO REPOSITORY paragraph compiles clean and throws at run time.
§8.4.3.2.3 SR2 requires the declaration, so the correct verdict is a compile-time rejection; delivering it at run
time is the DA7 wrong-stage family §0 already names. It rejects nothing legal, so it is not urgent — but it is a
real hole and it is now written down rather than noticed and forgotten.

**Verification.** The golden exercises both reference forms side by side for all four words and asserts they
agree, and carries the NC116A construct itself (`VALUE ZERO` + `SIGN LEADING SEPARATE`, printing -00123) so the
regression that cost two attempts is pinned inside the test that would have caught it.


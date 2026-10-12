## Entry 1191 — 2026-08-06 03:10 PDT — PB40: a CLASS screen cannot enforce a TYPE rule, and the fix caught a golden that asserted illegal source

**`FUNCTION CHAR(FUNCTION ABS(W-F))` over a `PIC 9V9` item compiled clean.** §15.15.3 r1 is "Argument-1 shall be
an integer"; §15.2 item 5 says an integer function has "no digits to the right of the decimal point", and
§15.7.1's table makes ABS over a scaled item a NUMERIC function. So this is illegal source, silently accepted.

**WHY THE SCREEN COULD NOT SEE IT, structurally.** `IntrinsicArgumentRules` resolves through §8.5.2.1 Table 2's
CLASS column and says so in its own doc comment — which is right for the `'n'` code and *wrong* for `'i'`.
**§15.3 type 6 is INTEGER, and integer is not a class.** §15.2 items 5 and 6 put an integer function and a
numeric function both in class numeric, so the `'i'` arm had no vocabulary to separate them. This is the same
shape as PB12's: not a gap in the table's contents but in what the table can express.

**THE RULE THE FIX RESTS ON IS THE ONE THAT MAKES REJECTING SAFE.** §8.4.3.2.3 SR11: "A numeric function shall
not be specified where an integer operand is required, **even though a particular reference of the numeric
function might yield an integer value**." The standard settles it on the function's TYPE, so there is no "but
this one is integral" to weigh — which is the opposite of the fail-open reasoning every class arm needs, and it
is the standard's choice rather than mine.

**⚠ AND IT IS DELIBERATELY NARROW, BECAUSE THE OPPOSITE ERROR WAS ONE LINE AWAY.** Type 6 admits "an arithmetic
expression that will always result in an integer value" in as many words. A screen written the obvious way —
"reject unless PROVABLY an integer" — refuses `FUNCTION CHAR(W-I + 1)`, which is the PB1 disaster arrived at from
the other direction. Only two shapes are provably outside type 6 and only those two are rejected: a NUMERIC
function (SR11) and a numeric data item with digits right of the decimal point. Everything else fails open. The
golden asserts the four accepted shapes precisely because that half is the one worth pinning.

**THE NOTE NAMED ONE FUNCTION; THE SWEEP FOUND EVERY `'i'` POSITION.** `FACTORIAL(FUNCTION ABS(6.5))` silently
returned 720 and `INTEGER-OF-DAY(FUNCTION ABS(6.5))` returned 0. The note also asserted "CHAR is additionally not
in `Verified` at all" — stale since the batch-2 wave. The screen did fire; it just could not see the distinction.

⭐ **AND IT CAUGHT A GOLDEN ASSERTING ILLEGAL SOURCE — the third time this session a spec-derived change found a
wrong artifact rather than a wrong compiler.** `pb32_dec_carrier_intrinsic_argument` ran
`FUNCTION MOD(A + B, B)` over `PIC S9(9)V99` items, and its header cited §15.3 **type 10** ("an arithmetic
expression or a numeric data item") for the whole family it exercises. But MOD is **type 6** — §15.64.3 r1 is
"Argument-1 and argument-2 shall be integers" — so that one line was illegal source the golden asserted COMPILES,
accepted only because a scaled numeric item is class numeric. I fixed the SOURCE, not the screen: `MOD(J + 10, 7)`
keeps the dec-carrier point (an arithmetic-expression argument through the AlignedArgs route under
ARITHMETIC IS STANDARD-DECIMAL) and conforms to r1; §15.64.4 makes the value 6, and no other line of the golden
moved. **A golden is evidence of what we decided, not of what is legal**, and the discipline that catches this is
deriving the expected result from the clause every time rather than trusting the artifact that already agrees.

⚙ **CI, ATTRIBUTED RATHER THAN ASSUMED.** The run for the previous commit came back failed, and the failure was
GitHub's, in GitHub's own words: `The job was not acquired by Runner of type hosted even after multiple attempts`
on the Greenfield job (zero steps executed, no runner name — dropped at the queue stage), plus an
`Internal server error` on the run itself, which is also why the original push produced no run at all. The same
three greenfield suites passed in the Windows job, which runs them in Release. **A cancelled job is not a failing
one and is not a passing one either** — it is a missing observation, and the fix was to re-run it rather than to
read the four green jobs as a verdict.

⛔ **AND THE BATTERY WENT RED ON THIS — the external oracle, disagreeing correctly.**
`=== BATTERY: NOT GREEN (rc=1) ===` on one row: the GnuCOBOL differential reported **1 per-case flip**,
`run_functions:221`, `AGREE_ACCEPT → WE_REJECT_THEY_ACCEPT`, carrying this fix's own `COBOLNET1627`. §0 classes
an AGREE→divergence flip a REGRESSION, which is the right default and the wrong answer here — so I attributed it
from the CASE rather than from the label. The test declares its argument `PIC S9(4)V9(4)` and passes it to
`FUNCTION CHAR`; §15.15.3 r1 requires an integer and §15.3 type 6 admits an integer data item or an
always-integral expression, so the program is illegal COBOL and GnuCOBOL is lenient. We now diverge in the
STRICT direction on source the standard rejects.

⚙ **THAT IS WHY THE DIFFERENTIAL IS WORTH ITS RUNTIME, AND ALSO WHY IT CANNOT BE READ MECHANICALLY.** It is the
only net that puts a real-world corpus against a new rejection, and the one flip it produced was a fact I could
not have got from the internal suites — but its own classification called a conformance improvement a
regression. `--permissive` downgrades the same message to a warning and compiles, verified rather than asserted,
so the leniency has its proper home and a migrating user is not blocked. Baseline regenerated; the diff is
exactly one row and a fresh run prints `0 PER-CASE FLIP(S)`.


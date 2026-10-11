## Entry 1172 — 2026-08-04 14:25 PDT — PB15's own closing claim was a deduction, and probing it opened PB40

**PB15's note ended with a tidy sentence: the new INTEGER-following result rules are "declared but not yet
observable", because every consumer reads `ResultCategory` and that folds Integer into Numeric. It read like a
fact. It was a deduction, and deductions about reachability are exactly the thing this project keeps getting
wrong.**

Probing it took one program:

    01 W-F PIC 9V9 VALUE 6.5.
    01 W-R PIC X.
        MOVE FUNCTION CHAR(FUNCTION ABS(W-F)) TO W-R      *> compiles clean, no diagnostic

§15.15.3 r1 is "Argument-1 shall be an integer." §15.2 item 5: an integer function "has an operational sign and
no digits to the right of the decimal point". `FUNCTION ABS(W-F)` over a scaled item is a NUMERIC function, so
this violates r1 and is accepted silently. The control is correct in the same program —
`FUNCTION CHAR(FUNCTION ABS(W-I))` with `W-I PIC 9(3) VALUE 65` prints `@`, ordinal position 65 read 1-based.
Filed as **PB40**, all citations `cite.py --check`ed.

**THE ROOT CAUSE IS A CLASS/TYPE CONFUSION THE SCREEN DOCUMENTS AS A FEATURE.** `IntrinsicArgumentRules` says so
outright: "§15's rules say 'shall be of CLASS numeric' … so they resolve through Table 2's CLASS column." Right
for the `'n'` code, wrong for `'i'` — **§15.3 type 6 is INTEGER, and §15.2 items 5 and 6 put integer AND numeric
functions both in class numeric**, so the `'i'` arm cannot separate them and admits both. The information is gone
before the screen sees it, because `ClassOf` maps a nested call through `ResultCategory`.

That is also why PB15's shape matters more than its eight fixes: **`Resolve` returns the §15.2 TYPE precisely so
INTEGER survives the fold**, and `IsIntegerOperand` already answers the question for literals, PICTURE items and
nested calls. PB40 is wiring over a correct primitive rather than new machinery. ⚠ And it must not be answered by
screening `ArgKinds` wholesale — PB1 established that column is UNAUDITED and enforcing it as written rejected 12
legal corpus programs.

**A SECOND SCREEN FELL OUT OF THE SAME SEARCH, FROM THE STANDARD ITSELF.** §8.4.3.2.3 SR12: "An integer function
other than the integer form of the ABS function shall not be specified where an unsigned integer is required."
The standard legislates about "the integer form of the ABS function" **by name** — independent confirmation that
the integer/numeric split is load-bearing and not bookkeeping — and it names an unsigned-integer screen that does
not exist either. Recorded in PB40 for the same wave.

**AND A DEFECT IN MY OWN FIX, FOUND BY REVIEWING IT RATHER THAN BY A TEST.** `IsIntegerOperand` asked a nested
call for `ic.Sig.Type` — its DECLARED type. ABS's row declares Numeric and resolves to Integer only against its
own argument, so `FUNCTION SUM(FUNCTION ABS(I) J)` would never have selected the "all arguments integer" row
through a nested integer function. It now re-resolves (`Resolve(ic.Sig, ic.Args)`), bounded by expression nesting
depth. No test caught this because no consumer reads the integer type yet — which is PB40 restated from the
other side, and a reminder that a rule with no reader cannot be validated by running the suite.

**ALL FOUR OF THE NEW GUARDS ARE NOW PROVEN IN THE FAILING DIRECTION**, not merely green: removing TRIM's
`Result:` column fires the population assertion BY NAME; injecting a real name list into CODE fires the
anti-pattern one (and the comment alone fires neither, which is the false positive that had to be fixed);
reverting `BindTrim` to a hardcoded category fires `NoBespokeBindPath_HardcodesAStringResultCategory`, which
names `BindTrim` and `FUNCTION TRIM` exactly. A guard nobody has watched fail is a guess about a guess.

**GATE:** greenfield Unit **3646/3646** · Conformance corpus **431/431**, both 0 failed 0 skipped.


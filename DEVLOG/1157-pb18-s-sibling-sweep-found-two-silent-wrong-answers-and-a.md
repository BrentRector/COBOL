## Entry 1157 — 2026-08-03 18:31 PDT — PB18's sibling sweep found two silent wrong answers and a THIRD denominator gap: §8.8.1 is not in the catalog at all

PB18 was re-verified and deliberately not implemented (entry 1156) because its recipe omits the scale question.
Before leaving it, the sweep its own entry did not ask for — **rule 4, every bug is a pattern** — over the rest
of §8.8.1.2 rule 6. That rule has three parts; PB18 concerns none of them.

**⛔ TWO OF THE THREE ARE VIOLATED ON THE NATIVE PATH, and both return a plausible answer with no diagnostic.**
Measured at `--std 2023` into `PIC S9(9)V9(4)`, each with an `ON SIZE ERROR` arm:

    COMPUTE R = Z ** Z   with Z = 0            ->  1.0000     ON SIZE ERROR did NOT fire
    COMPUTE R = N ** H   with N = -2, H = 0.5  ->  0.0000     ON SIZE ERROR did NOT fire

§8.8.1.2 r6a says a zero base *shall* have an exponent greater than zero, "otherwise the EC-SIZE-EXPONENTIATION
exception condition is set to exist and the size error condition is raised"; r6c says the same of a negative
base with a non-integer exponent. `EC-SIZE-EXPONENTIATION` is **Fatal** in Table 14. The second case is the
uglier one: `System.Math.Pow(-2, 0.5)` is NaN and `FromDouble(NaN, ws)` quietly yields 0.

**THE ROOT CAUSE IS A HALF-FIXED DISPATCH — AND THE CORRECT HALF IS ALREADY WRITTEN.** `CobolDec.Pow` raises
`CobolSizeError(…, "EC-SIZE-EXPONENTIATION")` for exactly these two legs, citing §8.8.1.2 r6a and r6c by name
(`CobolDec.cs:87`, `:93`). `NumericRenderer.Power` sends STANDARD/STANDARD-DECIMAL there and sends **every
NATIVE arm** — the float/receiverless arm and the fixed-point arm — straight to `System.Math.Pow` with no r6
check at all. That is the PB2/PB14 shape for a third time: one arm of a dispatch corrected, its sibling left
behind. Filed as **PB28**, to be fixed with PB18 since they are the same two lines.
⚠ r6b ("if the evaluation yields both a positive and a negative real number, the value returned is the positive
number") is a third leg and is unexamined — named in the entry so it is not discovered fourth.

**⛔ AND THEN THE LARGER ONE: THOSE RULES ARE NOT IN THE DENOMINATOR.** Checking whether PB28 was already
adjudicated returned zero §8.8.1.2 rows — and then zero rows under **§8.8.1 entirely**. Measured, not inferred:
every §8 section the catalog holds has the shape `8.x.y.2` / `.3` / `.4`, because `extract_rule_catalog.py` keys
on the *General format* / *Syntax rules* / *General rules* sub-clause headings. §8.8.1's rules are numbered
`1)`…`n)` directly beneath ordinary prose headings, so the map does not see them. The count:

    §8.8.1.2   7 top-level + 3 lettered      §8.8.1.4.2  3      §8.8.1.4.4  4 + 2
    §8.8.1.5.2 3                             §8.8.1.5.4  4      = 21 top-level + 5 sub-rules, all missing

**This is the THIRD instance of one defect class**, and the plan records the first two: the denominator went
3,790 → 3,846 (ten rules under headings the literal-spelling map did not know) → 3,861. Same shape every time —
a rule-bearing clause whose heading is not one of the three recognised ones. Filed as **PB29**, and the fix is
not "add §8.8.1 by hand": the extractor must REPORT any clause carrying `N)`-shaped rules it did not harvest,
the way it already reports an unresolvable rule-shaped heading. Otherwise the fourth instance is found the same
way — by tripping over a violated rule.

**WHY THIS MATTERS TO THE DEFINITION OF DONE.** D13 makes v1.0 the inventory at zero GAP. A rule absent from the
denominator can never BE a GAP — so **zero GAP is currently reachable while arithmetic-expression evaluation has
never been adjudicated**, including the two rules PB28 proves are violated today. §11 **A3** is the numeric-
semantics depth audit and its stated subject is "the intermediate-results model vs §8.8.1 end-to-end"; it is now
marked BLOCKED, because running it against a denominator that omits its own subject would produce a burn-down
that cannot reach what it audits.

⚠ **My own counter was wrong before the data was, again** — the fourth time today. It reported 0 rules under
§8.8.1, which I nearly filed as "the clause has no numbered rules". The bash heredoc had eaten one backslash, so
the regex `\\?` became `\?` and matched a literal question mark. The lesson is small and repeatable: **when a
script needs backslashes, write it to a FILE rather than through a heredoc** — the shell is a second parser
between the intent and the code.


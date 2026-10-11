## Entry 1190 — 2026-08-06 01:52 PDT — PB12 · PB30 · PB31: the argument screen's SHAPE was the gap, and its own comment claimed a control-flow property that was false for eight functions

**Three register items, one piece of work.** PB30 said eight functions had argument rules no code consulted.
PB31 said the cross-argument rule was enforced nowhere. PB12 said six more functions could not be screened *at
all* and named the reason: the table carried ONE kind per FUNCTION, and a function whose positions differ in
class cannot be described that way without rejecting legal source. PB12 was right, and it was the whole story —
the other two are what that shape had been costing.

**THE RESTRUCTURING.** `Verified` went from `name → (kind, clause)` to
`ArgSchema(Positions, Tail, Cross, CrossClause)`. Per-position rules, so FIND-STRING declares `['s','s','i']`
and §15.37.3 r3's integer argument-3 is enforced *beside* r1's string argument-1. A variadic tail, which is what
every pre-existing row became — position for position identical to the old behaviour, which is what made the
migration provable rather than merely tested. And `At(i)` returns null for a position the clause does not
describe: UNSCREENED, never screened by whatever the previous position happened to declare.

⛔ **AND THE OTHER HALF WAS NOT A TABLE PROBLEM AT ALL.** `CheckArgumentClasses`'s own comment read: "It sits
HERE — after arity, before every per-function arm — so a new catalog row is screened the day it is added rather
than the day someone remembers to write its arm." **That was false for eight functions.** TRIM, FIND-STRING,
SUBSTITUTE, CONVERT, MODULE-NAME, LENGTH, NUMVAL-C and EXCEPTION-FILE bind through a bespoke arm that `return`s
*above* that line, so no row could ever have screened them — the review had recorded it as "structurally
unreachable" three batches ago and the sentence claiming otherwise sat there the whole time. Each binder now
calls the screen on its own operand list, and a drift test asserts it, **proven in the failing direction**. A
claim about control flow is exactly the kind a comment cannot keep true.

⭐ **PB31 COST THREE LINES, BECAUSE PB48 HAD ALREADY BUILT THE MODEL.** Two days ago I replaced "the ONE class
this operand has" with "the SET of classes it can present", to admit a figurative constant whose class §8.3.3.6.4
GR4 makes context-dependent. §15.59.3 r2 — "All arguments shall be of the same class" — is then *exactly* "some
class is available to every argument", i.e. an intersection: `MAX(ZERO 5)` → {numeric}, `MAX(ZERO "A")` →
{alphanumeric}, `MAX(1 "A")` → ∅ → rejected. A model built for a different question answered this one for free.
That is CLAUDE.md rule 5's "prefer the shape that makes the NEXT case automatic" as an observation rather than an
aspiration, and it is the first time I can point at the payoff instead of predicting it.

⚙ **ONE FINDING WAS RIGHT AND ITS REASON WAS WRONG.** PB12 recorded HIGHEST-ALGEBRAIC and LOWEST-ALGEBRAIC as
"left unscreened until a kind can express category numeric or numeric-edited" — a missing capability, and I was
one edit from adding that kind. Measured first: `BindAlgebraicFold` already enforces §15.43.3 r1 / §15.58.3 r1
in FULL, including the half a class screen structurally cannot express — "shall be a data item … and shall not
be an integer function or numeric function", so a literal, expression, group, ref-mod or nested function is
refused. Adding the kind would have been a second mechanism for a rule the first enforces *more* completely.
They move to `DeliberatelyUnscreened` with that reason. The finding said "unscreened"; the measurement said
"screened by a better mechanism" (`validate_the_premise_not_only_the_rule`).

**MEASURED BOTH WAYS, because a screen is two claims.** A 37-case probe classifies every row as legal-accepted
or illegal-rejected: **zero bad rows** — 15 new bind-time rejections firing with their cited clause, every legal
form still accepted, and the ALGEBRAIC pair correctly landing on their own `COBOLNET1516` arm. The corpus (599
programs) stayed green throughout, which is the assertion that matters most: **PB1's disaster was a screen that
rejected 12 legal corpus programs**, so "it rejects the right things" is only half the gate and the cheaper half.

**Every one of the fifteen new citations was `cite.py --check`ed before it was written down**, not after. The
two that were easy to get wrong and were not: standard numeric time form is `'n'` and not `'i'` (§15.3.3 admits
a fractional-seconds representation, so screening it as an integer would reject a legal fractional time), and
MEAN/MEDIAN/MIDRANGE's rule names argument-1 only — which for a VARIADIC function governs every occurrence of
the repeated argument, so the uniform row is the rule's own shape rather than a widening.

⚠ **Residue, recorded rather than dropped:** §15.59.3 r1's "nor shall it be a strongly-typed group item" and r3's
"argument-1 shall not be a zero-length literal" are not class constraints and remain unenforced. Half a rule
enforced is PARTIAL, never CONFORMS.


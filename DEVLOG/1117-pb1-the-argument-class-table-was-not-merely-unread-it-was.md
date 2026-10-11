## Entry 1117 - 2026-07-29 20:40 PDT - PB1: the argument-class table was not merely unread, it was unverified — and wiring it in as-written rejected 12 legal programs

**THE DEFECT.** `IntrinsicCatalog` declares an `ArgKinds` per-argument class code on all **79** rows and exposes
`IntrinsicSig.ArgKind(int)` to read it. `ArgKind` had **zero callers** — the only read of `ArgKinds` anywhere in
`src/` was one `== "p"` MAX/MIN polymorphism test. So no §15 argument rule was enforced from the table built for
it, and class checking survived only as hand-written arms for a handful of functions. Found by the Phase-B
traceability review as 11 separate DIVERGES rows over 11 functions; it is ONE defect.

**THE SPEC SPLIT, derived before reading any code.** §15.3 draws a line the fix has to respect. Argument **TYPE**
is static — "Numeric. An arithmetic expression or a numeric data item **shall be specified**" — so a violation is
a compile-time matter. Argument **VALUE** is not: "The rules for a function may place constraints on the
permissible values … if the **evaluation** of an argument results in an incorrect value … the
**EC-ARGUMENT-FUNCTION** exception condition is set to exist." This change implements the first half only.
Conflating them would have been DA7's wrong-stage defect in reverse — a run-time condition raised at compile time.

**§4.2.2 paragraph 3 is the hook** (validated; the phrase the first agent implied, "shall be indicated", is not in
that clause): "There are rules in standard COBOL that are not identified as general formats or syntax rules, but
nevertheless specify elements that are syntactically distinguishable. This warning mechanism shall indicate
violations of such rules." An argument rule is exactly that. Disposition follows **DA6**, which settled the
sibling §8.8.1.1 question for arithmetic operands hours earlier: reject under strict conformance, leniency
dialect-gated behind `--permissive`. One question, one mechanism.

**⛔ CLASS, NOT CATEGORY — the row that decides the whole thing.** §8.5.2.1 Table 2 puts **numeric-edited (usage
display) under class ALPHANUMERIC**, and §8.5.2.1 closes with "Use of the name of a data class or data category in
the rules of COBOL refers to the category unless class is specifically indicated". §15.7.3 r1 says *class*. So
`PIC ZZ9.99` is not a legal numeric argument however numeric it looks. Reading that row the other way would have
silently admitted the exact operand the rule excludes, so it has its own negative fixture.

**⛔ AND THEN THE ESTIMATE BROKE, WHICH IS THE ENTRY'S REAL CONTENT.**

The queue entry said "consume `sig.ArgKind(i)` in `BindIntrinsicCore`". I did. The comprehensive gate returned
**12 failing corpus programs, every one of them legal COBOL**:

    FUNCTION BYTE-LENGTH(<numeric item>)   rejected — but §15.14.3 admits an argument of ANY class

`BYTE-LENGTH` is declared `"s"`. `LENGTH` and eight others declare `""`, and `ArgKind`'s own body reads
`ArgKinds.Length == 0 ? 'n' : …` — so the empty rows would have been screened as NUMERIC-ONLY.

**The table is not merely UNREAD. It is UNVERIFIED — and it was unverified *because* it was unread.** Those codes
were written as dispatch hints and drifted freely for years for precisely the reason PB1 exists: nothing consulted
them, so nothing could contradict them. Wiring 79 unaudited declarations into a rejection path converts one silent
defect into 79 chances to reject valid source, which is strictly worse than what it replaced and is the one
outcome CLAUDE.md rule 4 forbids outright.

So the screen is driven from a **spec-verified table**, not from `ArgKinds`: an entry names the function, the
admissible classes, and **the ISO clause it was read from**. Eleven entries — §15.7 and §15.70–15.79, exactly the
functions the Phase-B batch adjudicated. A function absent from it is screened exactly as before, which is why
this cannot regress anything, and the table grows as the review adjudicates each clause.

⚠ **That is not a deferral, and the distinction matters.** The FEATURE — enforce §15.3 argument classes — is
implemented completely and generally. What I decline to do is assert 68 argument rules I have not read. Claiming
them would not be completeness; it would be fabrication, with every wrong entry rejecting legal COBOL.

**THE DRIFT TEST CAUGHT ITSELF GOING BLIND.** `IntrinsicArgumentClassDriftTests` asserts the screen is wired, that
every verified rule cites a §, and that every verified function is really in the catalog. That last one failed
immediately: it reported **ORD-MAX as absent from the catalog**. It is not — my row-parsing regex required digits
for the arity bounds, and a variadic row writes `inf`, so every variadic function was silently skipped. A guard
against a dead table had itself gone half-dead on its first run. Fixed, with the reason in a comment, because it
is the same failure mode one level up.

**Evidence the central guard bites:** at the pre-fix commit the binder contains `ArgKind(` **zero** times and the
guard asserts more than zero — so it fails at `HEAD` and passes now.

**WHAT LANDED, AND WHAT HONESTLY DID NOT.** Of the 12 rows PB1 owned, **5 close** (ABS, PRESENT-VALUE, RANDOM,
RANGE, REM — bare class constraints, fully enforced) and **5 go PARTIAL**, because their rules have a second half
this screen does not reach: REVERSE and ORD also require a LENGTH ("at least one character position", "one
character position"), ORD-MAX/ORD-MIN also bar a STRONGLY-TYPED GROUP, and SECONDS-FROM-FORMATTED-TIME's r3 is a
CROSS-ARGUMENT rule ("argument-2 shall have the same type as argument-1") that a per-position screen cannot
express. Half a rule enforced is PARTIAL, not CONFORMS — recording those as closed is how a burn-down starts
lying.

**GOLDENS.** Four negative fixtures, registered in `tests/conformance/negative/manifest.json` in the same commit:
`pb1-numeric-arg-alphanumeric` · `pb1-string-arg-numeric` (the two hand reproductions) ·
`pb1-numeric-arg-numeric-edited` (the Table-2 row) · `pb1-numeric-arg2-alphanumeric` (REM's argument-2 — a screen
that stopped at the first argument would pass every other fixture and still miss that rule's second half).

**GATES.** Solution build clean. Unit **960/960**, characterization **33/33**, `docs/DIAGNOSTICS.md` regenerated
for `COBOLNET1627` (the drift test caught that omission — rule 6 working). False-positive sweep by hand over 18
legal forms including group items, ref-mod, nested expressions, national, MAX polymorphism and zero-argument
functions: all compile. `--permissive` downgrades all four fixtures to warnings and compiles.


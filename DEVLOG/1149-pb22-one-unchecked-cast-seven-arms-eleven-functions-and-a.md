## Entry 1149 — 2026-08-03 10:01 PDT — PB22: one unchecked cast, seven arms, eleven functions — and a wrap that impersonated a valid date

`FUNCTION INTEGER-OF-DAY(P * 100 + 62)` with `P PIC 9(18) VALUE 184467440737115466` returned **143951** — a
perfectly ordinary integer date — from an argument nineteen orders of magnitude out of range, with no
EC-ARGUMENT-FUNCTION even under enabled checking.

### Why it was invisible

`IntrinsicRenderer.AsInt` emitted a bare `(long)(…)` over an `Int128`-typed expression, and `RoslynBackend` sets
no `checkOverflow`, so the cast wrapped modulo 2⁶⁴ — **before** the function's own range guard ran. The guard
inside `CobolDate.IntegerOfDay` (§15.5.2 — 1601..9999 / 1..366) is correct and was simply unreachable.

The arithmetic is what makes it nasty: `P * 100 + 62` is 18446744073711546662 = **2⁶⁴ + 1995046**, so the wrapped
low bits are a genuinely valid 1995-046. The function did not return garbage; it returned a plausible answer to a
question nobody asked. A reviewer sampling outputs sees a date and moves on — which is the third time this session
that a defect survived because its symptom looked reasonable (PB5's 9223372036.85, PB13's 0170141…, this).

### One landing, not eleven repairs

Both entries — `IntArg` for the numeric channel and `ArgInt` for the string one — funnel through that single
`AsInt`, so it covers seven renderer arms over eleven functions. That is the right shape rather than a convenient
one: **a value the receiving body cannot represent is an incorrect argument (§15.3), and the place to say so is
where the narrowing happens.** Repairing eleven call sites would have left the twelfth to be written wrong.

`CobolIntrinsics.IntegerArg(Int128)` raises EC-ARGUMENT-FUNCTION instead of wrapping. Its double-typed twin is
`IntegerArgReal`, a **distinct name rather than an overload** — an integer literal converts implicitly to both
`Int128` and `double`, so an overload pair would make `FUNCTION FACTORIAL(5)` a CS0121 ambiguity. That exact
collision is documented in `RealArgs.cs` and broke six corpus programs when the `…Real` bodies were first written;
paying attention to it cost one line here and would have cost an afternoon later.

I had already applied this lesson inside PB21's `TryIntegerArg` earlier the same day, which is why that helper
carries its own bound rather than a bare cast. Landing PB22 makes the two consistent by construction rather than
by coincidence.

### What the golden pins

`WRAPPED=NO`, not a particular value: §15.3 leaves the result implementor-defined while checking is disabled, so
pinning 0 would be pinning our latitude rather than the rule. It also checks that the in-range value the wrap
impersonated still computes (143951) and that the sibling arm shares the landing — because a fix at a shared site
should be demonstrated at more than one of the sites it shares.


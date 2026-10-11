## Entry 1148 — 2026-08-02 23:31 PDT — PB21: the guard that existed to prevent this had exempted the group it happened in

`FUNCTION INTEGER-OF-DAY(<COMP-2>)` bound clean and then failed Roslyn with **CS0117** — a raw backend error on
conforming source, because `CobolIntrinsics.IntegerOfDayReal` did not exist. That is PB2's shape, and PB2 shipped
a drift guard specifically to stop it recurring. The guard was green throughout.

### Two independent blind spots, and I only found the second by fixing the first

**One — the scope filter.** `IntrinsicRealArgDriftTests` scoped on catalog `ArgKinds` `'n'` alone, and said why:
"an integer-argument function ('i' — DATE-OF-INTEGER, FACTORIAL) … cannot receive a float without violating their
own argument rule first". **That premise is false.** §15.3's INTEGER type resolves through CLASS NUMERIC, and a
COMP-2 item is category numeric (§8.5.2.12 item 2) hence class numeric (§8.5.2.1 Table 2) — so
`Admissible('i')` is `[Numeric]` and ADMITS a float. Integer-ness is a VALUE property, checked at run time, not a
class the screen can reject on. The exemption was reasoning about the rule's INTENT where the code screens on its
CLASS.

**Two — the extraction.** With `'i'` included the guard reported four missing bodies. The renderer writes
or-chained case labels, and the regex `case "(?<m>[A-Za-z]+)"` anchors on the word `case`, so

```
case "DateOfInteger" or "DayOfInteger" or "IntegerOfDate" or "IntegerOfDay":
```

contributed ONE name and hid three — systematically, since the renderer uses or-chains throughout. With both
fixed the count went **3 → 10**: the whole integer date/windowing family plus FACTORIAL (which also closes the
half PB14 had named).

The batch reported three because it looked at three FUNCTIONS. The guard, once it could see, reported the class.

### One landing helper, because the alternative is shape-dependence

Nine bodies delegate through a single `TryIntegerArg`, so a float operand gets the IDENTICAL disposition a
fixed-point one gets from `IntrinsicRenderer.AsInt` — truncation toward zero, the §15.3 latitude for a value that
is not the integer the rule demands. Two conversions would let the same function answer differently depending on
how its argument happened to be stored, which is exactly the receiver-shape defect PB13 closed; one helper makes
that impossible rather than unlikely. The helper carries the range guard PB22 is about, applied early: a `(long)`
cast on an out-of-range double is UNDEFINED in C#, so a huge operand would wrap into a plausible date instead of
raising.

### FACTORIAL is the exception, and it earns it

`RenderFloat` wraps every result in `FromDouble(double, ws)`, so a `…Real` body must return something a double can
carry. §15.36's result cannot be: 33! is ~8.7×10³⁶, which is why the exact body returns `Int128`. Writing
`FactorialReal` to satisfy the pattern would have meant returning a double and silently losing exactness past
2⁵³ — the very shape-dependence the paragraph above exists to prevent. So `RenderNum` skips the float dispatch
for it by name and its exact arm handles the float through `IntArg`'s `(long)(double)`.

The guard carries the matching exemption, and it is COUPLED rather than free: the exemption applies only while the
renderer actually contains the skip, so removing the skip makes the missing body reachable again AND makes the
test fail. An exemption that cannot notice its own precondition disappearing is how the first blind spot survived.

`IntegerOfBooleanReal` is likewise absent, for the opposite reason: PB19's §15.45.3 r1 screen rejects a float
argument at bind time, so it would be unreachable code that reads as coverage.

### Verification

The golden cross-checks rather than merely records: 1995-046 and 1995-02-15 must produce the SAME integer date
(143951), DAY-OF-INTEGER and DATE-OF-INTEGER must invert it, and the float path must agree with the fixed-point
path for the same value — which is the invariant the single landing helper exists to guarantee.


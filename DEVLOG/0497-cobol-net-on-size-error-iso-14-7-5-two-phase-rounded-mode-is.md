## Entry 497 — COBOL.NET: ON SIZE ERROR (ISO §14.7.5), two-phase; ROUNDED MODE IS PROHIBITED completed

The ON SIZE ERROR phrase on the arithmetic statements (the grammar already had `arithmeticOnSizeError`/
`computeOnSizeError`). Implemented the two-phase size-error model (§14.7.5) and, with it, finished ROUNDED MODE IS
PROHIBITED (§14.7.4.3 r7) — both turn on the new checked store. Greenfield-only (no grammar/legacy change).

**Runtime.** `CobolNum.TryStore(value, scale, profile, mode, out stored) → bool` — the checked sibling of `Store`:
returns false (receiver left unchanged) on a high-order capacity overflow (`|rescaled| ≥ 10^Digits`, compared
without `Math.Abs` to dodge `long.MinValue`) or a PROHIBITED-inexact rescale. `CobolNum.DivideOrThrow` raises a new
`CobolSizeError` on a zero divisor (case 2) AND on a PROHIBITED-inexact quotient (detected from the exact remainder,
since the division rounds at the receiver scale and would otherwise consume the inexactness before the store sees
it). `CobolNum.MulChecked(a,b) = checked(a*b)` — a runtime checked multiply (a method, so a constant product is
checked at run time, never folded to a CS0220 compile error) that raises `OverflowException` on an intermediate that
exceeds the long engine (§14.7.5 case 5).

**Bound tree / binder.** One `SizeErrorPhrase(OnError?, NotOnError?)` field on the 9 arithmetic nodes; `BindSizeError`
fills it (the NOT-only alternative detected by its leading `NOT` token).

**Emitter — two-phase (§14.7.5 rules 1/2/4).** `EmitArith` wraps a statement that has the phrase: `bool __sizeErrN
= false; try { per-receiver stores } catch (CobolSizeError) {…} catch (OverflowException) {…}; if (__sizeErrN) {on}
[else {not}]`. The per-receiver store uses the checked `TryStore`: a receiver that overflows sets the flag and is
left unchanged, while the OTHER receivers still store (rule 2); a /0 or intermediate overflow during evaluation
(phase a) is caught with no receiver changed (rule 4). Isolation (advisor): the checked helpers
(`DivideOrThrow`/`MulChecked`) are emitted ONLY when `EmissionContext.InSizeErrorContext` is set, so a statement
WITHOUT the phrase is byte-for-byte unchanged (it keeps `Divide`/bare-`*`/`Store`); the no-phrase
EC-SIZE-fatal model is left to the EC subsystem.

**Verification.** 10 differential tests (`OnSizeErrorDifferentialTests`): capacity overflow fires + receiver
unchanged; NOT path; both-phrases on each branch; PROHIBITED-inexact fires vs PROHIBITED-exact does not; divide-by-
zero; multi-receiver PARTIAL store (rule 2 — only the overflowing receiver unchanged); intermediate multiply
overflow fires; and a no-phrase control. Receiver-unchanged cases assert the receiver's post-value (an explicit
VALUE makes it deterministic across engines — an unvalued numeric displays differently in the legacy), not just the
imperative output. Conformance 200 → 210; 13 unit; legacy oracle untouched. **A 3-lens adversarial panel** (the
"pattern that works") reviewed it: the receiver-unchanged/partial-store and imperative-routing classes came back
CLEAN; its one real finding — intermediate long-overflow silently wrapping — is now fixed by `MulChecked` for the
long range (its own second finding correctly noted this is the documented Int128 deferral).

**Known-latent (Int128 / G3):** an intermediate that overflows BEYOND the long range, or an additive/`Pow10`-scaling
overflow, is not yet detected — the complete fix is the Int128 carrier (it computes wide then capacity-checks at the
store, subsuming `MulChecked`). PROHIBITED-inexact / EC-SIZE WITHOUT an ON SIZE ERROR phrase (the fatal path) awaits
the EC model. COMP-5 (BinaryCapacity) overflow bounds by width — also a later slice.


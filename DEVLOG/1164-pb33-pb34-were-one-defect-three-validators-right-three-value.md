## Entry 1164 — 2026-08-04 00:58 PDT — PB33 + PB34 were one defect: three validators right, three value producers wrong

**Filed as two queue items, closed as one.** The sweep was the whole content of the work — the fix itself is a
parameter and a comparison.

**MEASURED FIRST**, 34-digit argument at `--std 2023` under `>>TURN EC-ARGUMENT-FUNCTION CHECKING ON`:

| | result | verdict |
|---|---|---|
| `TEST-NUMVAL(34)` | 32 | correct |
| `TEST-NUMVAL-C(34)` | 32 | correct |
| `NUMVAL(34)` | `0141183460469231731687303715884` | wrong |
| `NUMVAL-C(34)` | `0141183460469231731687303715884` | wrong |
| `NUMVAL-F(34)` | `7014118346046923173168730371588` | wrong |

Those are `Int128.MaxValue` **saturation artifacts** — a plausible-looking 31-digit number, not a broken one —
and **execution continued past all three**, so a FATAL exception condition was never set. This is the PB5/PB13
saturation family meeting PB32's validating-twin-fixed asymmetry, in one place. ⛔ **The queue entry described it
as NUMVAL-C's cap alone; it was three functions, and only asking all six told me that.**

**THE RULES, cited and mechanically validated.** §15.67.3 r3/r4 · §15.68.3 r6/r7 · §15.69.3 r2/r3 — 31 digits
native, 34 standard-decimal, 35 standard-binary (unreachable; the mode is loudly refused). ⚠ **§15.93.3 and
§15.95.3 carry NO cap sentence at all** — the validators' caps live in their RETURNED-VALUE rules, which is why
they were already right, and why moving them would have traded one asymmetry for another. They are deliberately
untouched.

**THE FIX IS ONE RULE IN ONE PLACE, and the existing delegation paid for a second function.** `Numval` takes the
`digitCap` the TEST- twins already received and checks it BEFORE accumulating, so an over-long argument never
reaches `Rescaled`'s saturation at all. **`NumvalC` delegates to `Numval`**, so threading the parameter covered it
with no second implementation — the one place in this family where the existing structure was already right.
`NumvalF` counts the **significand** only, because §15.69.3 r2/r3 says so explicitly, so it keeps its own count.
One raise site: `DigitCapExceeded`.

⭐ **BOTH DISPOSITIONS VERIFIED, NOT JUST THE LOUD ONE.** With checking ON the fatal EC-ARGUMENT-FUNCTION is
raised and the run unit terminates. With checking OFF the result is the §15.3 implementor default **0** — and
that is the observable that discriminates, because 0 is a DEFAULT whereas `0141183460469231731687303715884` was a
value manufactured by saturation. A golden that only checked "it no longer returns the artifact" would pass on a
body that returned any other wrong number.
And the cap is mode-dependent **in fact**, not merely in code: 34 digits raises under native and is ACCEPTED
under `ARITHMETIC IS STANDARD-DECIMAL`.

⚠ **THE MANIFEST-INTEGRITY TEST EARNED ITSELF.** A `cd` earlier in the same command left the shell in the
scratchpad, so the golden's registration wrote to a path that did not exist and reported success;
`Manifest_CoversEveryProgram_NoOverlap` failed within the minute. The Write hook had warned about exactly this
class ("an unregistered golden never runs AND fails the manifest-integrity test at the comprehensive gate") — the
guard caught it at the wave gate instead, which is the cheaper of the two.

**REMAINING:** PB33's other half — §15.68.3 r1's two general formats are still unenforced — so PB33 is half
landed and PB34 is closed.
**GATE:** Corpus 422/422 (new golden included) · Conformance Intrinsic+Numval+Arithmetic 183/183 ·
characterization 33/33.


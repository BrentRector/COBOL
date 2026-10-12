## Entry 1161 — 2026-08-03 22:34 PDT — PB18 + PB28 land on one owner decision, and closing them closed PB32's blocked half as predicted

**Three owner decisions were taken today and this entry is the first of them reaching code.** The decision:
**native `**` computes EXACTLY in `Int128` while the result fits the carrier, and falls back to the documented
double approximation past it — never a size error merely for outgrowing the carrier.** Recorded in full as
`COBOLNET_NUMERIC_DESIGN.md` **D19**, which is where it belonged before any emitter changed.

⭐ **THE SURVEY DECIDED IT, AND THE OWNER ASKED FOR THE SURVEY BEFORE DECIDING — TWICE, ON TWO DIFFERENT
QUESTIONS.** IBM Enterprise COBOL and Micro Focus both fall back to floating point past the fixed capacity;
GnuCOBOL has no boundary at all, because `cob_decimal` is GMP arbitrary-precision. **No shipping COBOL raises a
size error merely because an exact power outgrew its carrier** — which ruled out the EC-SIZE-EXPONENTIATION
option that `CobolDec.Pow`'s own precedent would otherwise have made the obvious one. The owner's rule, stated
plainly: *"We must obey the spec and should follow the other compilers if practical."*
⚠ **AND I ANSWERED THE FIRST SURVEY FROM MEMORY, WHICH IS WHY IT HAD TO BE ASKED TWICE.** The measured oracle was
in the tree the whole time — `tests/external/gnucobol/tests` and the 3.2 tarball, both git-ignored. Reading
`cobc/reserved.c` observationally settled the *other* decision (PB37) in minutes. `survey_compilers_on_latitude`
is updated: lead with the survey, and MEASURE it; a documentation-derived table is a hypothesis.

**WHAT LANDED.**
- **PB18** — `CobolIntrinsics.PowNativeInt`: exact repeated multiplication, overflow falling back through
  `FromDouble` so an out-of-range magnitude SATURATES above the receiver's capacity check instead of wrapping
  (the D18/PB13 mechanism reused). `COMPUTE B31 = 10 ** 30` now returns 10³⁰ exactly, where it returned
  `1000000000000000071935427891953`.
- **PB28** — `CheckPowRule6` on EVERY native arm. §8.8.1.2 rule 6's own title is "Native, standard-binary, and
  standard-decimal arithmetic", so it binds native `**` exactly as it binds the SDIDI one. `CobolDec.Pow` had
  enforced r6a and r6c since it was written and every native arm ignored them, so the same program answered
  differently depending only on whether an ARITHMETIC clause was present: `0 ** 0` returned **1** (IEEE's
  convention, not COBOL's) and `-2 ** 0.5` returned **0** (`Math.Pow` gives NaN, the quantizer turned it into
  zero). Both now raise EC-SIZE-EXPONENTIATION, Fatal in Table 14. **r6b was checked in the same pass** rather
  than left to be discovered as a fourth leg — it is a selection rule, not a screen, and cannot arise here.
- **PB32's blocked half — closed, exactly as its entry predicted.** `Power` returned `Real: true` in a
  receiver-less context, so `A ** 2` was exact under `COMPUTE` and binary64 under `DISPLAY`/an `IF` subject,
  which routed `FUNCTION MOD` to a DIFFERENT BODY. Testing the OPERANDS before the receiver restores §15.4.
  MEASURED after: `DISPLAY` prints **930000007** (was 930000008) and
  `IF FUNCTION MOD(A ** 2, B) = 930000007` evaluates **TRUE** (was FALSE).

**⛔ TWO SELF-INFLICTED DEFECTS, BOTH CAUGHT BY PROBING RATHER THAN BY REASONING.** Recorded because they are the
two shortcuts the next person will reach for:
1. **The first cut returned the exact integer at scale 0 unconditionally**, which turned `COMPUTE R = 2 ** -2`
   into **0.0000** instead of 0.2500. A negative exponent is §8.8.1.2's RECIPROCAL and is not an integer;
   forcing an integer carrier onto it truncates the entire value away. The landing scale is now a parameter, and
   the receiver-less arm — where that scale is 0 — takes the exact path only for a non-negative literal exponent.
2. **Spotting that literal with `long.TryParse(e.Expr)` on the RENDERED expression text silently never matched**,
   so every literal exponent went back to the approximation arm and PB32's defect re-opened while the build was
   green. It reads `BoundNumLiteral` from the BOUND TREE now. ⭐ Both were found by re-running the probes after
   each edit rather than after the last one — the same discipline that found the ABS/SIGN/INTEGER/FRACTION-PART/
   FACTORIAL half of PB14 earlier today.

**GATE.** Build green; characterization 33/33 · Conformance corpus 419/419 · Conformance
Arithmetic+Intrinsic+Compute 213/213 · **full Unit 3639/3639**. Every power probe re-run after every edit, and the
seven cases that had to stay correct — `2 ** -2`, `(-2) ** 3`, `1.5 ** 2`, `1.5 ** 4`, `2 ** 10`, and both MOD
shapes — verified by VALUE, not by exit code. Golden `2023/pb18_native_power_exact_and_rule6` pins all of it,
including the two receivers that must be UNCHANGED after a size error (§14.7.5).


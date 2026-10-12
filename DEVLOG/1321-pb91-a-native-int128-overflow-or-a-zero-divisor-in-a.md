## Entry 1321 — 2026-08-18 13:22 PDT — PB91: a native Int128 overflow or a zero divisor in a condition / argument / subscript under EC-SIZE checking is the size error condition — the checked kernels follow the statement's checking state, and raise CobolSizeError

**What landed.** kb/Work **PB91** (MINOR, wrong answer, silent, rare): under `>>TURN EC-SIZE-OVERFLOW CHECKING ON`,
`IF A * A > 5` with `A = 10^20 − 1` compared a WRAPPED Int128 product and `IF 5 / Z > 1` with `Z = 0` compared 0 —
`NumericRenderer` chose the checked kernels (`MulChecked` / `AddChecked` / `SubChecked` / `DivideOrThrow`, the
checked NUMVAL landing) only from `_rcv.InSizeError`, the flag the ARITHMETIC statement threads for its ON SIZE
ERROR / EC-checked stores; a receiver-less render (`ReceiverContext.None` — a relation operand, a function argument,
a subscript, a SET amount) had it false whatever the statement's checking state, although §14.7.5 no-phrase rule 3
makes such an intermediate overflow EC-SIZE-OVERFLOW ("if an arithmetic operation on an intermediate data item …
would cause the new nonzero value to be farther from zero … than is allowed for the intermediate data item") and
§14.6.13.1.3 its disposition fatal — the same dispositions PB75 gave the decimal carrier in those positions. And the
checked kernels threw a bare `OverflowException`, which the arithmetic statement's try catches but PB75's ambient
EC-SIZE guard (a `catch (CobolFatalException …)`) does not — the native twin would have escaped as a crash.

Two changes, one mechanism each: **`NumericRenderer.Checked => _rcv.InSizeError || (_rcv.Receiverless &&
ecState.SizeChecking)`** — THE checked-kernel decision, where `EcState.SizeChecking` reads the per-statement
`EcStatementInfo` the `BoundEcChecked` emitter already sets (any EC-SIZE-* name enabled); `NumericRenderer` now
takes the `EcState`. And **the kernels raise the CONDITION**: `MulChecked` / `AddChecked` / `SubChecked` catch the
`OverflowException` and throw `CobolSizeError` ("EC-SIZE-OVERFLOW"), which the arithmetic statement's try already
catches first (reading `EcName`) and the ambient guard dispatches as the fatal EC; `DivideOrThrow` already raised
`CobolSizeError("EC-SIZE-ZERO-DIVIDE")`. Checking OFF is unchanged: the wrap item 179 (1) documents, and a zero
divisor's 0 (probed: `scratchpad/pb91/off2.cob` prints GT / LE2, exit 0).

**Evidence.** `2023/pb91_native_overflow_outside_arithmetic`: `IF A * A > 5` and `FUNCTION ABS(A * A)` run the USE
declarative (CAUGHT=EC-SIZE-OVERFLOW) and RESUME AT NEXT STATEMENT continues after the statement; `IF 5 / Z > 1` →
CAUGHT=EC-SIZE-ZERO-DIVIDE; an exception-checking PERFORM's WHEN handler runs (WHEN=EC-SIZE-OVERFLOW); a COMPUTE's
own ON SIZE ERROR still takes precedence (PHRASE=EC-SIZE-OVERFLOW; R stays 7); `IF A - 1 > 5` still evaluates.
The DEFECT backlog is now EMPTY — `work.py next` lists only the two features (PB64 locale, PB66 data-division).

**Gate.** Characterization 33/33 · Conformance (filtered: Ec/Exception/Size/Arith/Nist/Corpus/Numeric/Intrinsic/
Turn) 2820/2820 · Unit (filtered) 3787/3787. Battery #17 owed for the PB96 + PB91 batch.


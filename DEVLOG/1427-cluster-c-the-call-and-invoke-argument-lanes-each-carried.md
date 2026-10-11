## Entry 1427 — 2026-09-02 16:41 PDT — Cluster C: the CALL and INVOKE argument lanes each carried their OWN literal rendering, and neither could carry a floating-point literal — the row that said "exact in EVERY position" had five goldens and all five were the same position

`kb/Work/PB263` and `PB264` were opened by the SIBLING SWEEP of owner decision D-B (the arithmetic wave,
2026-08-31), which asked the one question a landing of that shape has to ask: *where else is a floating-point
literal rendered?* This entry lands the answer.

**Three lanes, one contract, one liar.** `EmitText.UnscaledLit`'s contract is "render a numeric literal as a
scaled integer". For the fixed-point spelling it did exactly that. For the E-form it silently returned the
literal's own source text as a binary64 `Real` `NumX` — and both `CallEmitter` (the `USING BY CONTENT` /
`BY VALUE` argument arms) and `OoEmitter`'s `INVOKE` arm believed it. The generated C# then handed a `double`
to `ManagedPointer<long>.Cell(...)` / `ManagedPointer<Int128>.Cell(...)`, and did not compile: **conforming
source rejected by a raw Roslyn `CS1503`, with no COBOL diagnostic at all**, measured on both carrier arms.

Reproduced before it was fixed, and re-measured after — six spellings, three of which nothing had ever looked at:

| spelling | before | after |
| --- | --- | --- |
| `CALL … BY CONTENT 1.5E+3` | raw `CS1503` double→long | `1500` |
| `CALL … BY CONTENT 1.23…E+3` (wide carrier) | raw `CS1503` double→`Int128` | exact |
| `CALL … BY VALUE 1234.5678901234567890123` | `0.0000048071159228778590190` (modular low-order bits) | exact |
| `CALL … BY VALUE 1.23456789E+3` | `1234.56789` (truncated) | exact |
| `INVOKE … BY CONTENT 1.5E+3` | `CS1503` — the THIRD arm, previously unmeasured | `1500` |
| `CALL … BY VALUE -1.234E-5` | `-0.000012` — found only by probing the sign | `-0.00001234` |

**The fix is at the helper, not at its callers.** `TryUnscaledParts` decomposes BOTH notations to the exact
`(unscaled, scale)` pair taken straight from `TryParseExact`'s `(significand, power of ten)` — never expanded to
text, so a large exponent simply moves the scale negative. That is ISO §8.3.3.3.3 rule 5 read literally ("the
value of a floating-point numeric literal is the algebraic product of the value of its significand and the
quantity derived by raising ten to the power of the exponent") sitting alongside §8.3.3.3.2 rule 4 for the
fixed-point form. `IntLiteralCore` now decides the rendered text, the unsigned-wide flag AND the carrier type
*together*; the cell type used to be re-derived from `Text.Count(char.IsAsciiDigit)`, which counts exponent
digits as if they were significand digits. The expression arm's `ManagedPointer<long>.Cell((long)…)` became
`Int128` with a widening conversion.

**What the arithmetic lane did NOT get.** `NumericRenderer.LiteralNum` diverts canonical float literals into the
SDIDI lane before they can reach `UnscaledLit`, so D-B's §8.8.1.3 intermediate-lane determination is untouched by
this change. The sweep that proves the fix is complete: `grep -rn "UnscaledLit" src/` → 5 live sites, of which 3
are the constant `"0"` or `LiteralNum`'s guarded fixed arm; **exactly 2 render operand text, and both are fixed**.

**The verdict row is the more uncomfortable half.** `GR-8.3.3.3.3-5` was ALREADY `CONFORMS` — flipped
2026-08-31 by D-B on the claim that the literal's value is carried EXACTLY in EVERY position through ONE
rendering. That claim was false of two positions on the day it was written (three, counting `INVOKE`), and the
row's five goldens were *all* VALUE-clause or arithmetic-operand positions — so no test it named could ever have
contradicted it. The row's evidence and notes are amended and the two new goldens added; **this is a correction,
not a GAP closure, and the GAP is unchanged at 3636.** `GR-8.3.3.3.2-4` was deliberately LEFT at a GAP: the
four-edition survey behind it has not been done, and a row is not closed by the half of it that was measured.

**Goldens.** `tests/conformance/2023/pb263_call_literal_arg_exact_value` pins the four-cell table (two notations
× `BY CONTENT` / `BY VALUE`, all four displaying the same value); `pb263_invoke_literal_arg_exact_value` pins the
`INVOKE` arm. Both are registered in the 2023 manifest and both were confirmed to execute BY NAME in the corpus
runner (`CorpusRunnerTests_P0` and `_P1`), not merely to be present on disk.

**Registered, not fixed here.** Three new notes, and a note-id collision that had to be resolved at landing time
(the implementer allocated `PB278`/`PB279`/`PB280` in an isolated worktree while `PB278` and `PB279` were being
landed on main by other lanes, and `PB280` was claimed by the Annex A.1 lane) — renumbered on the way in, with
every occurrence enumerated first rather than replaced hopefully:

- **PB288** (MAJOR, silent wrong answer, OPEN) — `CobolArgAdapt.Num`/`NumValue` rescale a caller cell to the
  formal's scale with the UNCHECKED `CobolNum.Rescale`, so `BY CONTENT 1.0E+30` into a `PIC S9(9)V9(9)` formal
  arrives as `87399551400673280`. Both notations wrap identically, so it is pre-existing and not caused by this
  landing; the value is destroyed inside the ABI, before the §14.2.3 GR9/GR10 "COMPUTE statement without the
  ROUNDED phrase" fill rule could apply. Five sites to sweep together.
- **PB289** (MAJOR, blocked by PB165) — the EXPRESSION half of PB264's landing-scale fault: a runtime expression
  passed `BY VALUE`/`BY CONTENT` still lands at the receiver-less working scale (6 fraction digits) instead of
  the formal's. The literal half is fixed here; the expression half needs PB165's formal on `BoundCallArg`.
- **PB290** (process) — about 50 code, doc and test sites cite ISO **§8.3.1.2, a clause that does not exist**,
  for three DIFFERENT subjects (fixed-point literal digit caps → §8.3.3.3.2, alphanumeric literal delimiters →
  §8.3.3.2, figurative constants → §8.3.3.6), and two of those sites are SHIPPED diagnostics
  (`COBOLNET0801`/`COBOLNET0802`). It was ledgered as reconciliation item 54 and never swept. The three sites
  inside this diff are corrected; the citation-repair sweep (PB159) absorbs the rest.

**Self-review, and what the report claimed that did not hold.** The implementer's own duplication pass found
`NumericArgText` decomposing the literal twice (`UnscaledLit` internally calls `TryUnscaledParts`) and collapsed
it to one decomposition plus one carrier decision. "Which arm did I fix?" was answered by a 10-row probe: plain,
signed, parenthesized, E-form, parenthesized-E and parenthesized-negated spellings all agree. Two contract claims
did NOT survive re-derivation: PB264's `spec_refs` carried `8.3.1.2` (a dead clause — `cite.py --check` FAIL,
corrected to §8.3.3.3.2), and its "truncated after five digits" is six (the sixth digit is a zero).

**Gate (this worktree, at the landing tree).** `dotnet build CobolSharp.sln -c Debug` → Build succeeded,
0 Warning(s), 0 Error(s). Conformance filtered `~Call|~Invoke|~Literal|~Corpus|~Sdidi|~Numeric`:
`Passed! - Failed: 0, Passed: 1283`. Full Unit: `Failed! - Failed: 2, Passed: 5149, Total: 5151` — both failures
are `ExternalCorpusPopulationDriftTests` (the GPL corpus is absent in a fresh worktree; environmental, and the
known shape). Characterization: `Passed! - Failed: 0, Passed: 33`. `scripts/semgrep/verify.py` unchanged against
the PB175 baseline (`cobolnet-no-biginteger` 36, `cobolnet-raw-diagnostic-code-literal` 475 — no increase).
`work.py check` ✓ 340. The Conformance count is 1283 where the implementer measured 1500 on its older base; the
difference is A13's test-partition landing in between, and it is a change in what the FILTER matches, not in what
passes — the corpus leg is 1007/1007 either way.


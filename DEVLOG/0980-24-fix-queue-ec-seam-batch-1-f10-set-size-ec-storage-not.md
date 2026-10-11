## Entry 980 — 2026-07-22 11:39 PDT — §24 fix-queue: EC-seam batch #1 — F10 SET SIZE EC-STORAGE-NOT-AVAIL (GR37/GR38) LANDED

First wave of the EC-seam batch (spec-first, one finding at a time). **F10:** `SET [SIZE OF] dynamic-length-item TO
arithmetic-expression-5` (ISO §14.9.39.4 Format 16) floored a negative to length 0 (GR37) and clamped an over-maximum
to the LIMIT (GR38) but never set the nonfatal EC-STORAGE-NOT-AVAIL either way — the in-code rationale ("a checking-off
no-op, stored value identical") was false: a nonfatal EC is observable under `>>TURN … CHECKING ON` via FUNCTION
EXCEPTION-STATUS, exactly like the CONTINUE AFTER EC-CONTINUE-LESS-THAN-ZERO the same wave wired. Fix mirrors the
CONTINUE template end-to-end: bind-time `Turn.Enabled("EC-STORAGE-NOT-AVAIL", …)` capture onto a new
`BoundSetSize.CheckStorage` field (BOTH construction sites — the explicit `SIZE OF` form and the bare re-routed form) →
`RuntimeApi.DynSetSize` grows a check arg → `CobolDynString.SetSize` sets `ExceptionState.Set("EC-STORAGE-NOT-AVAIL",
fatal:false)` on the GR37 negative and GR38 clamp legs.

**A pre-commit spec-anchor Workflow (6 agents, anchor→adversarial-verify) caught a load-bearing bug in the first cut:**
GR37 sign-tests the EVALUATED value, but the emitter truncated the amount to `long` BEFORE the runtime call, so a
fractional negative in (−1,0) (e.g. `SET SIZE OF X TO -0.5`) silently truncated to 0 and never raised. Corrected to the
CONTINUE discipline exactly — render the amount at FULL precision (`NumericRenderer.Real`, a `double`) and do the sign
test + toward-zero truncation in the runtime (`SetSize(string?, double, int, bool)`). The golden's `FRAC` line pins this
(it fails without the full-precision path). The same review confirmed the integer-2 literal form is compile-time bounded
by SR34 (so the runtime EC pertains to the arithmetic-expression-5 form; the golden uses data-item operands throughout),
and that GR38's third leg — storage physically unavailable — is unreachable under the .NET managed heap (pinned in
`CONFORMANCE.md §3`). Corrected the false `CobolDynString` doc comment.

Gate (wave-local): golden `2023/ec_storage_not_avail` (valid control no-EC · GR37 fractional-negative · GR37
integer-negative · GR38 clamp — all three raise legs + the negative control) · characterization **33/33 byte-identical**
(the OFF path — every existing SET SIZE — is unchanged; the emit render moved long→double but the stored length and
runtime output are identical) · Conformance SET/dynamic-length/ec_storage 5/5 · Set/DynString/DynLength unit 55/55. The
full greenfield Conformance + guard/GnuCOBOL comprehensive gate runs before the EC-seam batch merges (V3/V4 to follow).
⚠ correction folded: the review ledger's F10 anchor line :31737-41 pointed at the START-statement diagram; the real
GR37/GR38 text is §14.9.39.4 :31403/:31405.


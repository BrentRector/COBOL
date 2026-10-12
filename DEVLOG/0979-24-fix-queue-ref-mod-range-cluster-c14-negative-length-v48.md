## Entry 979 — 2026-07-22 10:58 PDT — §24 fix-queue: ref-mod range cluster — C14 (negative length) + V48 (ODO zero-extent) LANDED

Second §24 batch — the ref-mod correctness cluster (ISO §8.4.3.3.4 item 5c). **C14:** a specified ref-mod length that
evaluated NEGATIVE at runtime was silently treated as the length-omitted "to the end" form, so EC-BOUND-REF-MOD could
never raise for a negative even under checking — because `PlaceRenderer.RmLen` rendered the OMITTED length as the
sentinel `-1` and `CobolString.RefMod`/`SpliceInto` treated any negative as to-end. Fix: a DISTINCT omitted sentinel
`CobolString.OmittedRefModLength` (= `int.MinValue`, emitted via the new `RuntimeApi.OmittedRefModLength` façade so the
Step-4b ratchet catches a rename) — the two to-end emitters (`RmLen` + the INVOKE §14.8.2.2 rule-1 prefix in `OoEmitter`)
switch to it; the guards raise EC-BOUND-REF-MOD for a specified `length < 0` regardless of REF-MOD-ZERO-LENGTH (which
relaxes ONLY zero, item 5c). **V48:** `PlaceRenderer.ReceiveInto` now passes `allowZeroLength: true` — a zero-extent
OCCURS-DEPENDING group MOVE (count 0) is a no-op (§13.18.38 GR8a), not a ref-mod violation. **C14-cite/V50 (part):**
`ExceptionState.RefModError` §8.4.2.3 c → §8.4.3.3.4 item 5b/5c; `PlaceRenderer` ref-mod line §8.4.2.4 → §8.4.3.3
(the nonexistent §8.4.2.4; the numeric-image line 32 left for the dedicated V50 pass — different concept).

CLI-proven: `W(2:)`→BCDEF, `W(2:3)`→BCD, `W(2:L=-1)` under checking → fatal `EC-BOUND-REF-MOD (2:-1)`. Gate:
`RefModRangeTests` 4/4 (specified-negative→fatal · negative-under-REF-MOD-ZERO-LENGTH→still-fatal · omitted→to-end ·
V48 zero-extent no-raise) · characterization 33/33 byte-identical (no corpus program used the omitted form; the new
const routes through `RuntimeApi`, so the runtime-API ratchet stays satisfied) · string/refmod/ODO unit 55/55 · full
greenfield **Conformance 3842/3842, 0 regressions** (the shared `CobolString.RefMod`/`SpliceInto` runtime change is
behaviour-neutral for every existing program — only a specified negative length under checking is now caught). Design
SSOT `COBOLNET_STRING_OPS_DESIGN.md` updated to the sentinel
encoding (was an as-designed separate `RefModToEnd`). §24 tier 6 remaining: V45-sentinel + V45-externality (EXTERNAL
file consistency, a different subsystem).


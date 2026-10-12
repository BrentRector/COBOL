## Entry 1286 — 2026-08-09 21:10 PDT — PB60's channel threading: one value per call, whichever statement asks

The fleet's p11 probe showed four channels giving three answers for one legal NUMVAL-C call — COMPUTE
exact, MOVE truncated at the receiver-less 6-floor, the relation agreeing with the truncated value,
DISPLAY at the floor. §15.68.4 r1 carries no channel qualification, so the fix threads the context the
channel actually has: a numeric MOVE's sender now renders under the RECEIVER's scale and headroom
(`MoveEmitter.SenderContext` — Receiverless deliberately stays true, so the float family's binary64
sending path and its ToScaled landing are byte-identical), and each relation side renders knowing the
OTHER side's static scale (`ConditionRenderer.StaticScaleOf` — over-asking is capped by the PB13
headroom invariant, under-asking was the defect). The receiver-less DISPLAY floor becomes a DOCUMENTED
§15.4.1 native determination on CONFORMANCE.md item 92's row rather than an accident: one
determination, every channel. Verified: CM/MV/EQ golden legs all read 0.123456789; the broad gates —
Conformance ~Corpus|~Move|~Condition|~Intrinsic|~Numval|~Arithmetic 1112/1112, Unit 2713/2713
(the float-quantize drift battery untouched) — prove the two shared seams moved without a ripple.
RV-15.68.4-1 → CONFORMS (GAP 4148). Still open on PB60: the STANDARD-DECIMAL exactness half
(RV-15.67.4-1 — the NumvalDec/SDIDI branch the one-scan now makes trivial), the binder walk + the
category/argument-2 screens, the currency-SET model, and the DECIMAL-POINT IS COMMA scoping row.


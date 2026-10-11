## Entry 1009 — 2026-07-23 03:23 PDT — Conformance fix-queue: CA5 (arithmetic) — ROUNDED/PROHIBITED bind to the FINAL transfer only; a nested division no longer inherits the receiver's mode (an explicit _outermost flag replaces the ds==_rcv.Scale proxy)

**CA5 (CONFORMANCE-FIX-QUEUE, MAJOR/M) landed** — the careful, two-directional arithmetic defect. Root cause:
`NumericRenderer.Divide` used `mode = ds == _rcv.Scale ? _rcv.Rounding : Truncation` as a proxy for "this division's
quotient IS the value transferred to the resultant identifier." The proxy is wrong in both directions:
- **(a) spurious size error** — a SINGLE-receiver COMPUTE renders the whole RHS with the receiver's real mode
  (PROHIBITED). For a NESTED division whose operands' scales ≤ the receiver scale (e.g. integer operands), the old
  guard-digit gate was false so `ds == _rcv.Scale` and the nested quotient INHERITED PROHIBITED; under ON SIZE ERROR
  the emitted `DivideOrThrow(...,Prohibited)` threw on the inexact INTERMEDIATE quotient — a spurious size error on a
  statement whose FINAL value is exactly representable.
- **(b) missed PROHIBITED** — a MULTI-receiver COMPUTE pre-truncated the shared RHS to the widest receiver scale, so a
  genuine PROHIBITED violation on the root division was missed (the per-store TryStore then rescaled scale-N→scale-N,
  an identity, saw no inexactness, and silently stored a truncated value).

**The fix** (per the finding): an explicit `_outermost` flag threaded from the emit sites that OWN the final transfer.
`NumericRenderer` — save/restore `_outermost` in `Render`/`AsNum` (new param), clear it for a `BoundBinary` node's
children and restore for the node's own combine, clear at `Fold` entry, add the param to `Combine`, and rewrite
`Divide`: **outermost** → compute at the resultant's scale + ROUNDED mode in one exact step (DivideOrThrow detects
PROHIBITED via the exact remainder, §14.7.4.3 GR7); **nested** → ALWAYS the D2 guard scale + TRUNCATION (never the
receiver's mode; the gate that skipped guard digits at receiver scale is dropped — §14.7.7 rule 3 NOTE 1: ROUNDED
applies only to the final transfer; §14.7.5 has no native intermediate-inexactness size-error case). `ArithmeticEmitter`
— pass `outermost: true` at the single-receiver COMPUTE render and the DIVIDE top-level division; the multi-receiver
COMPUTE render and the DIVIDE operand renders keep the default false.

**Golden re-derivation (the finding's own Program B was mis-specified — the "re-derive, don't copy" discipline caught
it).** The finding's Program B wrote `COMPUTE X Y ROUNDED MODE IS PROHIBITED` and DISPLAYed X, expecting 0000. But
per §14.9.8 ROUNDED attaches PER-RESULTANT to the immediately-preceding identifier — so that syntax gives X=Truncation,
Y=Prohibited (the generated C# confirmed `TryStore(…, _P_0, Truncation)` for X, `_P_1, Prohibited` for Y). X (truncation)
legitimately becomes 3.33; only Y (PROHIBITED) size-errors. The corrected golden makes BOTH resultants carry their own
PROHIBITED phrase, so both stay unchanged: `SIZE-ERROR / 0000 / 0000` (before the fix: `OK / 0333 / 0333`). Program A
(`(1/3)*0` into a PROHIBITED receiver) → `OK / 0000` (before: `SIZE-ERROR / 0700` — the spurious intermediate throw).

**Blast radius.** The change alters codegen only where a division is either an outermost transfer with a high-scale
operand OR a nested division at/below the receiver scale — and NONE of the 33 characterization snapshots or any
positive corpus golden across 85/2002/2014/2023 shifted (characterization 33/33 byte-identical; Corpus + arithmetic
family 430/430). Both spec-derived goldens verified end-to-end via CLI `--run`.

**Gate.** characterization 33/33 · greenfield unit 575/575 · Conformance Corpus+Arithmetic+Compute+Divide+Rounded+
Multiply+StandardDecimal+OnSizeError 430/430 · full Conformance (numeric-render gate — running). Goldens
`conformance/2014/ca5a_prohibited_nested_no_size_error` + `ca5b_prohibited_multi_receiver_size_error` (2014+ —
ROUNDED MODE IS PROHIBITED). **19 landed / 27 fix-ready remain.** NEXT = CA6 (§14.7.7 rule 2b — the four fixed-width
binary-N operands EXCLUDED from the composite), completing the arithmetic batch → then the FULL batch gate before merge.


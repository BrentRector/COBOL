## Entry 544 — 2026-06-10 12:55 PDT — THE ARITHMETIC WAVES (merged from the parallel worktree): 74→80 NC green — single evaluation, edited SIZE ERROR, P-scale images, sign-condition binding, figurative 88s

**The other half of the parallelism experiment:** while the six verb agents ran, I implemented the five fix waves
from the earlier 10-program DIFF diagnosis MYSELF in a manual worktree (`arith-waves` from the EVALUATE commit) —
my builds/tests never collided with agents writing files into the main tree. Merged back with ONE conflict
(CobolEdit.cs — both sides inserted before MaskScale; kept both).

**Wave 1 — single evaluation for multi-receiver arithmetic (ISO §14.7.7 GR4 + NOTE 3; §14.9.12.4 GR5).** New
`CSharpEmitter.Snapshot` materializes rendered senders into `Int128`/`CobolDec` temps; applied in EmitInPlace (the
operand sum), EmitGiving, EmitDivide (both operands; division still renders per receiver at its scale+mode),
EmitCompute (the RHS renders ONCE at the widest receiver scale when >1 receiver), and EmitDivideRemainder (both
senders ALWAYS — they appear in several emitted expressions and the quotient stores before the remainder forms).
The bug class: `DIVIDE b INTO a GIVING x a y` re-read the dividend FIELD per receiver, so the receivers after the
aliasing one divided the stored quotient (NC172A/NC173A: 0.7/1/0.7/0 instead of 2.5/3/2.5/2). Swept ALL multi-
receiver arithmetic per feedback_scan_all_similar — ADD/SUBTRACT in-place and GIVING and COMPUTE had the same
latent re-evaluation.

**Wave 3 — ON SIZE ERROR for numeric-edited resultants (§14.7.5 case 3 + storing rule 2).** `CobolEdit.TryFormat`
(+ `MaskCapacity`, the exact mirror of Format's digit-capacity prologue; `CobolNum.Pow10Wide` → internal):
under `_sizeErrVar` an edited receiver whose ALIGNED |value| exceeds the mask's digit positions sets the flag and
stays UNCHANGED; `Format`'s silent high-order truncation remains MOVE behavior (§14.9.25). → NC170A, NC175A.

**Wave 4 — P-scale sending images (§13.18.40.3 'P' operations item b; §14.9.25.4 GR6a).** `OperandText.PExpand`
appends/prepends the P-position ZEROS to the de-signed digit image in all three deSign paths — `PIC S9P(17)` now
moves 18 characters, not 1. → NC114M.

**Wave 5 — the NC250A binder pair.** (a) `BindOperandExpr` walks BREADTH-FIRST to the shallowest
arithmeticExpression — the `comparisonOperand → valueOperand → arithmeticExpression` chain nests one level deeper
than the old direct-child scan, whose `FindLeaf` collapsed `IF NINE ** 2 + (90 - 180) POSITIVE` to just `NINE`
(§8.8.4.3 — the sign condition's operand is the WHOLE expression). FindLeaf deleted. (b) bare figurative words in
string level-88 VALUEs materialize to the conditional variable's width (`FigurativeFillChar` — QUOTE/SPACE/
HIGH-VALUE/LOW-VALUE/ZERO, §8.3.1.2 + §8.3.3.6.4 GR2). → NC250A 115/115.

**Census 74 → 80 GREEN of 95 (+6: NC114M 170A 172A 173A 175A 250A), zero lost; 556+6 conformance locks; 15 unit.**
Remaining 15: collating NC215A/219A (step ③), ODO NC235A, NC104A/124A (unmapped DIFFs), NC105A/224A/401M runtime
loud/NRE, NC107A/108M/247A/252A compile-or-flake (NC252A = a REDEFINES-tier `REDEF11` emission gap), NC236A
(spec-pinned legacy divergence), NC214M/303M no-golden (by design).


## Entry 314 — Bare relative START + systemic NOT-INVALID-KEY-only mis-binding → RL205A baselined (NIST 351→352)

**RL205A** (9 FAIL*) had two independent compiler defects, both from the parallel diagnosis.

**FIX A — bare relative START ignored the implicit KEY (1 failure).** `START RL-FS2.` with no KEY phrase must
behave as `KEY IS EQUAL TO` the RELATIVE KEY data item (ISO §14.9.41.4 GR8). `FileIoLowerer.LowerStart`
resolved the implicit key only via `ResolveKeyData(file, -1)`, which returns the prime RECORD KEY — null for a
relative file (whose key is in `FileSymbol.RelativeKey`, not `RecordKey`). So `operandSym` stayed null, no
`IrStartFile` was emitted, and the bare START was a silent no-op; the file was never positioned. Fix: fall back
to `RelativeKey` when the prime key is null, mirroring the READ chain's `RecordKey ?? RelativeKey`. `condition`
already defaults to 0 (Equal), satisfying GR8.

**FIX B — systemic `NOT INVALID KEY`-only phrase mis-bound (8 failures + the class).** The grammar for all five
key phrases is `INVALID KEY? block (NOT INVALID KEY? block)? | NOT INVALID KEY? block`. All five binders
(`FileIoBinder.BindWrite/BindRead/BindRewrite/BindDelete/BindStart`) unconditionally bound `statementBlock()[0]`
as the INVALID-KEY block and `[1]` as NOT-INVALID. For the standalone `NOT INVALID KEY block` form there is only
ONE block but it is the NOT-INVALID block — yet it was bound as INVALID-KEY, so on a *successful* I/O the
NOT-INVALID imperative never ran (and on a failure it ran wrongly). RL205A's NEW-COBOL-8X START tests used this
form (`MOVE "A"` in `NOT INVALID KEY`). Fix: one extracted helper `BindInvalidKeyBlocks(blocks, NOT(), …)` used
by all five binders — the discriminator is **`blocks.Length == 1 && NOT() != null`** ⇒ the NOT-only form (a NOT
in the first grammar alternative always brings a second block, so one-block-plus-NOT uniquely identifies it).
ISO §9.1.14. (`NOT()` is a single `ITerminalNode` because the rule has at most one NOT per parse.)

RL205A → 066 OF 067, 0 FAIL* (the 1 "deleted" is the program's own DE-LETE bookkeeping), baselined; self-
contained, placed after RL204A. Guard ALL GREEN: **1040 unit / 347 integration / 352 NIST** (RL 29→30), 0
regressions — confirming the shared invalid-key binder change is behavior-preserving for the INVALID-only and
both-phrase forms every other RL/SQ/IX/ST writer uses.

**Transparency — the diagnosis over-reached on IX108A.** The RL205A diagnosis predicted FIX B would also flip
the known-failing IX108A (`WRITE … NOT INVALID`) green. It did not: IX108A still reports `000 OF 001 … 001
TEST(S) FAILED` with NO detail line — a *test that never executes/records a result*, a different defect (it
exercises the `END-WRITE`/`END-DELETE`/`END-REWRITE` explicit scope terminators, not just the NOT-INVALID
binding). IX108A stays a documented remaining-work item; not chased here (it was not one of the nine diagnosed
backlog targets).


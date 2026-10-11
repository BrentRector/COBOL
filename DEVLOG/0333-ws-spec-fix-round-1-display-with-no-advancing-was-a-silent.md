## Entry 333 — WS-SPEC fix round #1: `DISPLAY … WITH NO ADVANCING` (was a silent no-op)

First fix from `docs/SPEC_FIX_BACKLOG.md` (a P0). The grammar parsed `displayNoAdvancing` (CobolParserCore.g4)
but `DataStatementBinder.BindDisplay` **dropped** it; `BoundDisplayStatement`/`IrPicDisplay` never carried it; and
`CilDataEmitter.EmitPicDisplay` always called `Console.WriteLine` — so `DISPLAY "AB" WITH NO ADVANCING` still
emitted a trailing newline. Threaded a `NoAdvancing` flag through all four layers (bound → IR → emit;
`Console.Write` when set). CLI-verified: `DISPLAY "AB" WITH NO ADVANCING` then `DISPLAY "CD"` → `ABCD` on one
line. Added `NucleusSpecTests.Display_WithNoAdvancing_SuppressesNewline`. Guard ALL GREEN.

Running the fix round **directly and sequentially** (guard-gated) rather than as a parallel workflow — the
WS-SPEC test *authoring* parallelized well (additive, low-risk) but compiler-correctness *fixes* warrant
controlled, one-at-a-time integration.


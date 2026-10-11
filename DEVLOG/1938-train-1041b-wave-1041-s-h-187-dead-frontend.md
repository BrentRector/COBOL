## Entry 1938 — 2026-10-08 07:26 PDT — Train 1041b: wave 1041's H — 187 dead frontend DiagnosticDescriptors fields deleted under the Delete program; A and G dropped at the landing check (PB2194 landed; PB2683 filed)

**H — PB2194 (landed; census R0-0157).** The note named 46 caller-free fields of the frontend
`DiagnosticDescriptors`; the re-probe found 187 of 217. Census a02b165da counted 141 of them as used only because the
legacy CobolSharp compiler referenced them, and PB2110 deleted that compiler; the 187 match census 8be23006851a's
R0-0152. The file goes from 738 lines to 146; the 30 survivors (the generic parse error, the parser hints, the eight
COBOLNET parse-layer twins, the COPY errors) each have a caller, and the class is no longer partial.
VERSION_TEST_MATRIX_DESIGN §6.2, DESIGN-architecture-review (twinOf) and DESIGN-frontend-grammar §3.8 no longer name
deleted members. The census defect behind the undersizing is filed as **PB2683**: the scope rule's "excluded" arm kept
product members alive through legacy-only uses, so the open Delete notes pinned to a02b165da are re-measured against
8be23006851a before their waves.

**A — PB2230, PB2231, PB2232, PB2233, PB2234 (DROPPED at the landing check).** A deleted three census-measured dead
artifacts (`db101a.txt`, the applied PB59 family-5a plan, the `push-main.ps1` forwarder). It refused three findings on
evidence. `prepend-devlog.py` is in use, so every lander brief now names it as the DEVLOG inserter. The spike record and
`consolidation-1-candidates.md` are frozen evidence, so `census.py` stopped judging `docs/rearchitecture/evidence/`.
Its sweep found that `census.py`'s `GENERATED_READERS` prefix was a backslashed `str(RECORD_DIR)` on Windows. A census
record therefore counted as a live reader of every artifact it had found dead. A fixed it with `as_posix()` and two
self-test arms.

**G — PB2193 (DROPPED at the landing check).** G deleted `DataReferenceCst.Context`, `.HasNoSuffix` and `.Span`, which
had no readers. It also made the PB457 drift test refuse a raw `dataReferenceSuffix(` read in `CapacityRegisterFor`.

**The drop.** The three-cluster train was GREEN on every gate: whole population Conformance 11,234/11,234 · Unit
32,678/32,678 · Characterization 36/36 (run 20261008T140112Z-c9a130), Linux gate GREEN, oracle IDENTICAL, semgrep
unchanged, review no findings. `push-main.sh` then stopped at the landing check (exit 3, main untouched). R3 work had
changed four files outside its declared set: `scripts/arch/census.py`, `scripts/push-main.ps1` and
`ArchCensusDriftTests.cs` (A), and `CapacityRegisterReferenceDriftTests.cs` (G). No in-flight work shared any of them.
The cause is two planner holes that are already filed. For A, the declared set was `compare_oracle.py` alone, because
the planner never reads a note's Sites block, so even the deleted artifact was outside it (PB2678). For G, a drift test
names the dead member only as text (PB2679). Both notes now record this occurrence, the preserved branch and the
re-land condition: A is `train1041b-dropped-a-pb2230` (`61b4a00fc`) and G is `train1041b-dropped-g-pb2193`
(`419b480c0`), both on `9910c9b60`. PB2230-PB2234 and PB2193 stay open. G's lead goes with G: once
`DataReferenceCst.Span` is gone, PB2192 takes `SourceSpan.cs` with `DataDescriptionCst.Span`. The Windows census defect
stays on main until A lands.

**The train.** Three clusters dispatched, one landed, two dropped (at the landing check); none ejected and none fixed
in the train. Gating was batched: each implementer gated leg 1 only. All three branches applied cleanly, and
origin/main stayed `9910c9b60`, so no rebase was needed. Two whole-population runs were made. The first ran on A+G+H
and the second on H alone (run 20261008T141559Z-a0f374, the same counts). Both were GREEN first time, and the Linux gate (GREEN) and the oracle (IDENTICAL over 7614 cases against 23f3eb1efbe5, re-recorded as
39a6ad782bfe) were re-run on H alone. Before landing, the H-only head passed the landing check (0 stops). Semgrep unchanged (biginteger
46, decimal 2, raw-diagnostic-code-literal 268). No diagnostic codes claimed (COBOLNET3170-3172 and 3188-3193
returned). No inventory rows changed: GAP 136 → 136.


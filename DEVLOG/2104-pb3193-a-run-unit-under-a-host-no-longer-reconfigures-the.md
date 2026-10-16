## Entry 2104 — 2026-10-16 12:55 PDT — PB3193: a run unit under a host no longer reconfigures the host's console; a redirected stdin decodes as UTF-8

**Mechanism.** `StandardStreams.EnsureUtf8` (the PB59/PB899 rule that the standard streams carry UTF-8) was called
from `ProgramTable.RunOnThisThread`, which runs on every `RunMain`, the host entry. A host that embeds a run unit
therefore had its process console reconfigured: `Console.OutputEncoding` was switched to UTF-8 or, on a process with no
console, both of its writers were replaced by the fallback's. This is the console twin of PB3163, where the run unit
wrote the host's exit code.

**Re-probe** (fresh Debug build, a Roslyn-compiled host in its own process): the host set `Console.OutputEncoding` to
Latin-1 (28591), installed its own `Console.Out` and `Console.Error`, ran one program through `ProgramRegistry.RunMain`,
and read code page 65001 afterwards.

**Design call.** The process console belongs to the process, so only the entries that ARE the process configure it:
the `cobol` CLI's `Main` and `ProgramTable.RunModule`, the generated `Main` (DOC-A.1-167: a standalone executable is
its own host). Under a host, the standard display device is the host's `Console.Out` / `Console.Error` as the host
configured them (DOC-A.1-59). I considered a run-unit-owned display stream and did not adopt it: it would either
bypass a host's `Console.SetOut` redirection or just wrap the host's writer. Per-run-unit streams belong to the host
surface (PB2099). The decision is recorded in DESIGN-runtime-library.md and CONFORMANCE.md DOC-A.1-59 and DOC-A.1-167.

**Fix.** The call moved to `RunModule`, before the run-unit thread starts. **Sibling arm (I9), same function:** the
device-boundary rule covered output only. On Windows a redirected standard input was decoded in the console's OEM code
page, so piping the bytes C3 A9 5A into `ACCEPT X` gave `FUNCTION ORD` values 9501 8977 (cp437's U+251C U+2310)
instead of 234 91. `EnsureUtf8` now gives a redirected stdin a UTF-8 reader (no BOM detection, the same shape as
Console's own reader), documented in DOC-A.1-1. An interactive console is left to decode its own input.
`ProcessObserver` encodes what it pipes as UTF-8 to match.

**Witnesses.** `RunUnitHostConsoleTests` (Conformance) checks that the host's encoding and both of its writers
survive and that each DISPLAY reaches the host's writer intact. `AcceptDifferentialTests.Device_RedirectedInput_DecodesAsUtf8`
checks the input arm. The new sweep `ProcessStateOwnerDriftTests` (Unit) holds every process-owned write (the exit
code, `Environment.Exit`, the console setters, the `EnsureUtf8` calls) to its process entry, so the next host entry
cannot reach one. All three went red against a mutation that restored the old call site and disabled the input arm.
No inventory row is claimed or closed.

**Self-review.** Architecture: one rule, one helper, two process-entry callers, pinned by the sweep. Code: the input
arm runs before anything reads stdin, so no byte is buffered under another decoding. Performance: once per process.
Duplication: the exit-code and console ownership rules now share one owner table.

**Gate** (run 20261016T193237Z-2d03cd): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance
6,130/11,841 · Unit 33,381/33,777 · Characterization 5/36 in leg 1; leg 2 left to CI). The three added tests passed in
leg 1. Linux gate: the lander's (batched gating).

**Leads, not probed here:** an interactive Windows console's ACCEPT input is still decoded in the console's input code
page; and the `Console.OutputEncoding` setter at a process entry may leave the parent shell's shared console on code
page 65001 after the process exits.


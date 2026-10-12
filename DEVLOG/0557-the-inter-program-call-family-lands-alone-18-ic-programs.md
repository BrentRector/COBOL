## Entry 557 — 2026-06-10 17:39 PDT — The INTER-PROGRAM (CALL) family lands ALONE → 18 IC programs byte-match (677 conformance)

**The fourth and final wave-2 family — the STRUCTURAL one.** The parked CALL agent delivered
`Runtime/Control/ProgramRegistry.cs` (ManagedPointer/ICobolProgram/CobolArg/ExternalStore/ProgramRegistry — the
§14.6.2.3 state model: cached singleton = last-used, INITIAL fresh + implicit-CANCEL-on-return §14.9.18 GR2,
RECURSIVE per-activation, CANCEL §14.9.5), `DataBinder.Linkage.cs` (LINKAGE roots into the ordinary forest §13.7;
USING/RETURNING formals §14.2.2 SR1 — carrier-resident elementary formals alias per access via
`ManagedPointer<T>` §14.2.3 GR8, group/redefined formals round-trip their image at the activation boundary;
EXTERNAL 01s re-based onto ExternalStore §8.6.7; GLOBAL 01s collected §13.18.27), `StatementBinder.Call.cs`
(BoundCallProgram/BoundCancel/BoundExitProgram/BoundGoback + per-edition gates), `CSharpEmitter.Call.cs`
(multi-unit run-unit emission D3/SSOT §18 #8 — EVERY program unit and nested program compiles, one instantiable
class per program, nested programs as nested classes with ref-bridge GLOBAL inheritance §13.18.27 GR2, the entry
wrapper Program.Main registering all units + §14.6.11 CloseAll), and 11 spec-derived facts. All 11 anchored
edits applied CLEAN; the build compiled FIRST TRY. The static→instance flip: the per-program `Main` became
`public void __Activate()` (catches ProgramReturn — GOBACK/EXIT-PROGRAM return to the activator §14.9.18
GR2/GR3), root + index + ALTER-target fields became INSTANCE fields (a fresh instance IS the §14.6.2.3.2 initial
state), and file connectors register per-instance at first activation (the IC114A lesson) under run-unit-global
keys QUALIFIED per program ("PROG::FILE" — a connector is internal to its program, §8.6.3).

**Integration fixes (mine):** (1) the agent's allocated diagnostic band 0860–0869 COLLIDED with KeyedIO's
0860–0865 (the manifest's own flagged risk) — renumbered to 0880–0889 before the first build; (2) the
`procedure-returning-2002` matrix construct declared its RETURNING item in WORKING-STORAGE — ISO §14.2.2 SR5
says "data-name-2 shall be defined as a level 01 entry or level 77 entry in the LINKAGE SECTION", so the new
binder's COBOLNET0888 was CORRECT and the construct source was invalid COBOL (fixed: RC → LINKAGE SECTION) —
the version matrix catching a test bug, not a compiler bug.

**Verification (the verify-HARD protocol):** 11/11 CALL facts; 677 conformance + 15 unit ALL GREEN; the full
276-program census (NC/ST/RL/IX/IC/SM) shows ZERO programs lost — the census's NC174A/254A "regressions" were
an env artifact (the switch programs need COBOL_SWITCH_1=ON, which the prior census shell had exported; the
frozen pre-CALL compiler produces the IDENTICAL diff without it, and both byte-match WITH it); frontend + legacy
untouched (git diff clean over src/Cobol.Net.Frontend + CobolSharp.*). **18 IC programs byte-match from ZERO and
are locked: IC101A 103A 106A 108A 112A 114A 201A 203A 209A 213A 216A 223A 224A 225A 226A 228A 235A 237A** —
146 locked NIST programs total. IC residuals queued: IC207A/227A DIFF (227A = EXTERNAL FD shared connectors,
the known file-subsystem joint edge), IC109A/110A/117M/205A/210A/233A/234A RUNERR (233A/234A = GLOBAL FDs +
cross-program GLOBAL USE — the queued USE-declaratives subsystem), IC113A TIMEOUT, IC222A/401M CMPL_FAIL.
Known follow-ups from the manifest: GR3a eval-once for subscripted BY REFERENCE args (capture subscripts into
locals), ContainsNextSentence arm for CALL ON-phrase bodies, OMITTED arguments + header BY REFERENCE/VALUE
phrases (grammar gaps), cross-assembly dynamic CALL (open question, escalate before G8).


## Entry 1370 — 2026-08-29 04:44 PDT — PB133 wave A + PB163 LANDED: the activation boundary's three mechanical defects — §11.10.4 GR4 recursive inheritance, the instance-slot restore, the GR3e mask consumed on failure — and the GLOBAL LOCAL-STORAGE registration gap the golden flushed out

Wave A of the PB133 activation-boundary campaign, the three mechanisms that needed no new conformance
machinery. (4) §11.10.4 GR4 — "the RECURSIVE clause specifies that the program AND ANY PROGRAMS CONTAINED
WITHIN IT are recursive" — is one parent read in BinderDriver.MakeUnit (parents build before children, so
it cascades); the legal R→C→C cycle that used to draw EC-PROGRAM-RECURSIVE-CALL on conforming source now
runs, pinned by 2023/pb133_recursive_containee (R C1 R C2, the depth counter in the leaf containee's
shared-STATIC WS — the recursive-container-with-WS composition stays its own staged posture and the
fixture respects it). (5) ProgramTable.CallProgram now RESTORES the instance the fresh-activation branch
displaces — the registry slot is how containees reach their container's frame, and at depth 2 the old code
left the dead frame in it; pinned end-to-end by 2023/pb133_instance_restore (C-SEES=0002 then 0001, where
the old runtime printed 0002 twice), with the INITIAL implicit CANCEL additionally guarded on Active == 0
(an INITIAL containee of a recursive container can return under a live outer activation of itself).
(6) The §14.9.4.4 GR3e pending-mask handshake is consumed BEFORE resolution, so an EC-PROGRAM-NOT-FOUND /
EC-PROGRAM-RECURSIVE-CALL throw no longer leaks the site's mask into the NEXT statement's activator latch
— pinned by ProgramActivationMaskTests (the failure half is unreachable from a CLI golden; the success
half asserts the latch/restore pair).

PB163, found by the instance-restore golden and immediately measured on a PLAIN container: a GLOBAL
LOCAL-STORAGE item was UNDEFINED in every containee — CallBindExternalAndGlobal scanned the
working-storage section only, where §13.18.27.3 SR1b (cite-checked) names file, working-storage,
local-storage, AND linkage. The scan now covers WS + LS + LINKAGE for GLOBAL (EXTERNAL stays WS-only per
§13.18.22.3 SR1); the emit-side §13.18.27 GR2 bridge was already section-agnostic, so registration was
the whole defect. Golden 2023/pb163_global_local_storage; the clause-site screens (level-77 looseness,
the FILE-section and constant/screen-entry legs) are the note's NAMED residue.

THE GATE FOUND THE FIX'S OWN SIBLING: recursive_ws went red because CancelNode ran the emitted
__ResetStatics ONLY when a cached instance existed — and the activation-slot restore leaves a recursive
unit's slot correctly NULL once every activation returns, so §14.9.5 GR3's static-WS reset silently
stopped happening (the old DANGLING depth-1 instance had been masking the gate). The reset now runs
outside the instance check — idempotent on a never-called unit, so GR7's no-op posture holds — and
recursive_ws's CANCEL-then-recall derivation (D1 WS=01) is again exact.

PB133 remains OPEN — waves B (StoreReturn's pointer/object lanes, GR3a once-only identification) and C
(§14.8.2 conformance + EC-PROGRAM-ARG-MISMATCH, GR12's omitted-argument gate, the image-incapable group
arguments) follow.


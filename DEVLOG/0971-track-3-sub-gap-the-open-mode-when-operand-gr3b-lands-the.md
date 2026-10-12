## Entry 971 — 2026-07-22 02:30 PDT — Track ③ sub-GAP: the open-mode WHEN operand (GR3b) LANDS + the full GR3a→g tier order fixed

Continuing Track ③'s staged sub-GAPs. **The open-mode WHEN operand form (`WHEN EXCEPTION INPUT|OUTPUT|I-O|EXTEND`)
now works** (the 0899 staging is lifted for it), and — forced by the same tier logic — the WHEN matcher's operand
priority is corrected to the **full §14.9.49.4 GR3a→g order**.

**The tier fix (spec re-derivation, verified in the spec):** GR17 selects across the WHEN operands by GR3's a→g
priority. Increment 4's matcher only ranked the EC-name tiers (GR3c-g) and put a bare file-name at tier 1 — but per
GR3, a bare file-name (GR3a) and open-mode (GR3b) OUTRANK every exception-name form. Corrected to the 7-tier scheme:
**bare-file (GR3a, 0) > open-mode (GR3b, 1) > file+L3 (GR3c, 2) > file+L2 (GR3d, 3) > L3 (GR3e, 4) > L2 (GR3f, 5) >
L1/EC-ALL (GR3g, 6)**; source order only within a tier. (The EC-name tiers keep their relative order, so the Incr-4
tier tests still pass; only bare-file moved up.)

**The open-mode matcher:** a `WHEN EXCEPTION <mode>` arm (tier 1) emits
`ExceptionCatalog.IsIoName(__ec) && __f is not null && CobolFile.OpenModeOf(__f) == <FileOpenMode ordinal>` — it
matches an EC-I-O whose file is CURRENTLY OPEN in that mode (`CobolFile.OpenModeOf` — the same query `__IoCheckEc`'s
F1 mode switch uses). The binder's blanket open-mode 0899 is deleted; `ModeOrdinal` maps INPUT/OUTPUT/I-O/EXTEND →
the `FileOpenMode` enum. **Documented limitation:** an OPEN-failure's mode is best-effort (the connector reports its
mode only once open) — noted in §9.7 / CONFORMANCE.

**Behavior tests (`PerformFormat3BehaviorTests`, now 42 F3 tests total):** `OpenModeWhen_MatchesByCurrentOpenMode`
(a READ past EOF on an INPUT-mode file → `WHEN EXCEPTION INPUT` fires) · `BareFileWhen_MatchesAnyIoConditionForTheFile`
(GR3a) · **`OpenMode_OutranksExceptionName`** (a `WHEN EC-I-O-AT-END` written BEFORE `WHEN EXCEPTION INPUT` → the
open-mode wins — GR3b before GR3e) · the obsolete `OpenModeWhen_Staged0899` → `OpenModeWhen_CompilesClean`.
CLI-probed the file-I/O path end-to-end on `cobol.exe`.

**Gate (wave-local — F3-matcher-only, all gated `UnitHasF3Perform`): characterization 33/33 byte-identical + F3 tests
42/42 + GnuCOBOL differential UNCHANGED from the Incr-4 baseline (478/171/106/568 — 0 regressions) + build 0/0.**
Docs swept: §9.3.2 tier list + §9.7 (open-mode LANDED) + §9.9 (bare-file/open-mode tier RESOLVED) + D12 (the GR3a→g
priority). **⏭ Remaining Track ③ staged sub-GAPs: F3-in-a-method (OO pc-slice wiring), cross-CALL "in range",
EC-FLOW-USE/`>>PROPAGATE`, exception-object raise in imp-1.** Then ④ §24 fix-queue → Wave I merge → P14.


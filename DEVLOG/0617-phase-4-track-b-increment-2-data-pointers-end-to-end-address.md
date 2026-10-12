## Entry 617 — 2026-07-05 14:18 PDT — Phase 4 track (b) increment 2: data pointers end-to-end (ADDRESS OF / BASED / SET ADDRESS OF / ALLOCATE-FREE / F10 arithmetic)

Track (b) closes its primary rows. Implemented FROM the recon-workflow design captured in
`docs/PHASE4_RECONCILIATION.md` §"M2-DATA-5 / M2-PROC-5 — increment 2" (wf_28c3930a-592 — four parallel
readers over the goldens / the spec / the increment-1 seams / the storage-model options, then an xhigh
synthesis; every file:line anchor spot-verified before implementation). All three pending goldens
(`based_pointer` B=HELLO/X=WORLD, `pointer_alloc` B=HELLO/B2=WORLD/FREED=YES, `pointer_arith` C0=A/C4=E/C2=C)
byte-exact; **zero grammar change** — every surface already parsed.

**The storage decision (D-P2): ONE cell type + a window pointer, generalizing what EXTERNAL already shipped.**
`ExternalStore.Holder` → the general `StorageCell` (character image + Allocated/Freed — never a byte
substrate); `CellPointer(cell, offset)` joins Null and `ManagedPointer<T>` as the third carrier —
`SameTarget` gains the STRUCTURAL branch its own doc comment promised (§8.8.4.2 :9772 "equal if they
reference the same address": same cell, same offset — two independent `ADDRESS OF X` now compare EQUAL,
which closures never could). `ADDRESS OF x` forces x's record onto a per-instance cell through the factored
`ForceStringCanonical` (the ONE cell-backing mechanism — EXTERNAL, addressable, and BASED are three callers
of the proven Tier-B re-basing); a BASED 01/77 emits NO storage — an implicit `__addr_X` pointer (GR2 NULL)
plus a deref bridge `ref CobolPtr.Deref(__addr_X, width).Ref` that makes §13.18.5 GR3/GR4 loud at every
reference, with the window offset riding `CobolPtr.OffsetOf` inside the UNCHANGED RedefViewPlace machinery
(zero new verb code; no new Place subtype). ALLOCATE/FREE/SET-F7/SET-F10 bind in the new
StatementBinder.Ptr.cs (F10 = the D-U7 category re-route on the shared 85 index grammar shape). The
IC-series EXTERNAL baselines rode the Holder→StorageCell rename byte-identical (the design's OWNER-1
guard-proof).

First-run lesson: the addressable cell's seed must honor VALUE — `CallInitialImage` (the EXTERNAL
default-image helper the design cited) lost `X VALUE "HELLO"` (B= read five spaces); the cell now seeds via
the SAME `FieldEmitter.ImageInitOf` expression the Tier-B stored backing uses.

**THE ADVERSARIAL REVIEW WAVE (same change set; wf_4c49e522-ec0, retried once over a session-limit window:
24 raw → 23 confirmed / 1 refuted — ~10 distinct issues, all fixed/staged/documented** (full disposition in
the reconciliation's review subsection). The heavy catches: ① ADDRESS OF a SUBORDINATE of a BASED record
silently returned the BASE address (ClassOffset dropped — wrong-bytes aliasing; now UpBy-displaced,
CLI-probed C=PQR); ② `SET P UP BY 2.5` silently truncated to 2 AND the new registry row falsely claimed a
bind-time reject — GR19 is a VALUE rule, realized exactly now via `CobolPtr.UpByScaled` (divisibility test →
EC-SIZE-ADDRESS fatal; 2.0 moves by 2; registry/matrix text corrected); ③ the 0881 **VALUE-on-BASED arm
rejected CONFORMING programs under a nonexistent spec rule** — removed (VALUE seeds ALLOCATE INITIALIZED
GR7); ④ BASED+EXTERNAL (§13.16.3 SR5) and BASED-USING-formal (§14.2 SR1 :23658) both undetected → CS-level
failures; now 0881/0889; ⑤ class-unit BASED → CS0103 (no OO bridge loops) → staged 0899; ⑥ FREE's nonfatal
EC never ran the F3 declarative → the §14.6.13.1.3 #5 sequence now emits. Documented-not-fixed: the
>>TURN'd-fatal-pointer-EC USE-F3 walk and the gap-#10 image-group pointer-leaf guard (both named residue).
The review also proved the two NEW 0900 gates were deletable without any test failing (masked by co-resident
gates at 85) — PointerAddressingTests now asserts their gate-specific where-texts.

Battery: conformance 1688/1688 (+35 over 616: 3 goldens · 7 negative cases · 16 matrix cells for the two new
+ two flipped rows · PointerAddressingTests ×9) · unit 130/130 (CobolPtrTests ×7) · zero regressions. Docs: reconciliation rows M2-DATA-5/M2-PROC-5 → LANDED + as-built + review subsections; SSOT
§9.1 gains the AS-BUILT CellPointer note (supersedes Cell(T)-as-ALLOCATE-backing); wave-sizing (b) → 0
primary. Track (b) residue = the CALL-boundary pointer legs + the M3-4 typed-pointer leg.


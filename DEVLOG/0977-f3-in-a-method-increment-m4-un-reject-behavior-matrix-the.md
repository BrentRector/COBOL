## Entry 977 — 2026-07-22 10:22 PDT — F3-in-a-method increment M4: UN-REJECT + behavior matrix — the feature LANDS

Final increment of the §9.10 design — **an exception-checking (Format-3) PERFORM inside an OO method compiles clean
and RUNS.** Deleted the `F3StagedInMethodStub` early return in `EcBindExceptionPerform` (retired the stub method): the
method path now binds handler pc-ranges EXACTLY like a program's (`AddF3Handler` records `ctx.CurrentMethodScope`, so
M1's `StampMethodHandlerSlices` sub-slices per method, and M3's `EmitMethod` emits the method-local machinery + the
two-range `__MDispatch` + the entry frame FLOOR).

**CLI-probed first, then the suite.** A driver INVOKEing an F3 method printed `WHEN N=5 / COMMON / AFTER N=6 /
FINALLY N=6 / METHOD-DONE` — the handler and the resumed imp-1 share the method's LOCAL-STORAGE N (5→6), PROVING the
per-activation capture (a class member could reach neither `__MDispatch` nor N). The cross-INVOKE probe printed
`INNER-CONTINUED / OUTER-AFTER / OUTER-DONE` with NO `OUTER-CAUGHT-LEAK` — the §9.10.1-C2 frame floor correctly hides
the outer method's WHEN from the inner method's unmatched raise.

**NEW `PerformFormat3MethodBehaviorTests` 12/12** (spec-pinned, mirrors the program-path matrix in a method):
GR17+GR20 nonfatal resume-in-place · GR20 fatal terminate (the method's `CobolFatalException` unwinds past
`catch(MethodReturn)`/finally through the INVOKE) · RESUME NEXT suppression · GR3 tier precedence · WHEN OTHER · WHEN
COMMON · RESUME-skips-COMMON · FINALLY · EXIT PERFORM→FINALLY · GR21 re-raise-not-re-caught → fatal · the explicit
method-local mutation-visible-to-imp-1 capture proof · the C2 cross-INVOKE isolation.

**COMPREHENSIVE GATE ALL GREEN:** `PerformFormat3MethodBehaviorTests` 12/12 · full greenfield **Conformance 3834/3834**
(was 3822 + the 12 new) · characterization **33/33 byte-identical** · greenfield **Unit 571/571** · legacy guard **NIST
353 MATCH / 0 regression** (legacy Unit 1203 / Integration 678) · **GnuCOBOL +2 fixes / 0 regressions** (2 corpus
F3-in-method programs now compile — count-verified: the AGREE_REJECT + WE_ACCEPT_THEY_REJECT buckets are unchanged, and
the un-reject can only remove a reject, so the only movement is WE_REJECT_THEY_ACCEPT→AGREE_ACCEPT). Doc sweep: design
SSOT §9.10 STATUS→IMPLEMENTED + §9.7 staged-list boundary + `PerformFormat3Tests` header; plan §0 (F3-in-method LANDED,
NEXT = the §24 fix-queue → P14 Step 0). **Track ③ (the exception-checking PERFORM) is now FUNCTIONALLY COMPLETE
end-to-end — program AND method.**


## Entry 1390 — 2026-08-29 11:38 PDT — PB154 LANDED: CANCEL whole — a function-name is not a program-name, GR7 is a modeled predicate, the seven initial-state actions have their evidence, GR9 closes only what is open and only what exists, and the operand class screen gets its own diagnostic

Six mechanisms (kb/Work PB154), then the two catches that reshaped the landing. (1) §8.4.6.3:
FUNCTION-ID units registered with ParentPath null + Recursive true, so ResolveVisible's rule-4 arm matched
a UDF and `CANCEL "FCTR"` re-initialized a function's statics — `Node.IsFunction` now discriminates in
BOTH directions (Register/registrar carry `isFunction`, every ResolveVisible arm filters `wantFunction`,
CallProgram asks for a function exactly on the EC-FUNCTION-NOT-FOUND path, EntryOf's two rule-4 loops skip
functions; pb154_cancel_scope's F3=0003). (2) GR7: the not-located arm ran ProbeSiblingModule — loading
assemblies, registering foreign modules, firing staticReset, CACHING the miss so a later CALL's legitimate
probe was suppressed — Cancel resolves `probe:false`. (3) Register's silent containee-drop when the
container was unregistered is a LOUD invariant. (4) §14.6.2.3.2: actions 3/5/6/7 join action 2's evidence
(REOPEN=00 / ADDR=NULL / CAP / LEN) — action 5 via the STATIC BASED BRIDGE: RouteStaticUnitStorage splits
the old RecursiveWsPointerBacked reject, a BASED root's BasedPointerField becomes a static bridge addr
nulled by `__ResetStatics`, only the ADDRESS-OF-taken case keeps a (narrowed, re-described) reject — legal
`01 X BASED` in a RECURSIVE program compiles. (5) GR9: the emitted CloseFiles body calls the new
`RuntimeApi.FileCloseIfOpen` per internal non-SD connector, and CancelNode reaches connectors through a
transient factory instance when the slot is null — the old instance-gated close LEAKED the OS handle
(post-CANCEL reopen '30'). (6) §14.9.5.3 SR1: BindCancel's dataReference arm gets the operand class screen
— `CANCEL WS-NUM` was a compile-clean silent no-op.

CATCH ONE — the wave gate's NIST leg (IC203A "CANCEL UNCALLED PROG") crashed the first landing attempt:
`file connector 'IC205A::PRINT-FILE' was never registered`. The transient close's comment claimed "the
guarded CloseIfOpen entries make this a no-op for a never-called unit" — but the guard tested OPEN-NESS
through Require(), which throws on a name the registry never held. Reachability had been deduced, not
measured (the goldens' never-called legs had no files). The fix is the rule's own shape: GR7 became a
MODELED predicate — `Node.CalledSinceCancel`, set at both activation sites, cleared by explicit CANCEL and
by §14.9.18 GR2's implicit cancel, guarding CancelNode's whole body — and CloseIfOpen treats a
never-registered name as a fortiori not open.

CATCH TWO — the pre-landing review fleet (73 agents, 4 lenses, 23 raw findings, 18 adversarially
confirmed) found what the gate could not: (a) the §14.9.18 GR2 implicit cancel runs AFTER the finally
restored an INITIAL container's slot to null, so the transient close's ParentInstance lookup THREW on
every INITIAL-program return with a null-slot containee — the cancel path now resolves the container
TOLERANTLY, and the new golden pb154_cancel_cascade pins that exact return plus the never-called-containee
no-op and the already-canceled no-op; (b) the emitted CloseFiles loop was the ONE file loop without the
`IsSortMerge` skip (an SD never registers — CANCEL of any SD-bearing unit would have thrown); (c) the
BASED arm's `continue` skipped the INDEXED-BY-cell routing — a per-activation index cell in static WS,
a silent wrong answer where the old code at least rejected loudly — and `__ResetStatics` now emits every
line from the ONE source-ordered WorkingStorageRoots walk (the bridge set was iterated as a HashSet into
generated source); (d) COBOLNET1681's CALL text promised program-pointer is admissible — CANCEL's
narrower §14.9.5.3 SR1 now has its OWN descriptor, COBOLNET1696 (the negative golden re-pinned); (e) the
new A.1 item-19 row cited §7.3.4 for the CALL-CONVENTION directive — it is §7.3.9 (the inventory row's
"7.3.4 GR2" locus was the same inherited miscitation; §7.3.4 contains no CANCEL text). The fleet also
surfaced the CALL-side twin of the connector-lifetime mismatch (a fresh RECURSIVE activation re-registers
and silently replaces a still-open connector) — registered as kb/Work PB168, not crammed into this fix.

Verdicts: SR-14.9.5.3-1 NOT-IMPLEMENTED → CONFORMS · GR-14.9.5.4-2 DIVERGES → CONFORMS ·
GR-14.9.5.4-5/-7/-9/-10/-12 PARTIAL → CONFORMS · GR-3/-4 stay PARTIAL naming their residue (the
ADDRESS-OF-taken static cell; the unobserved reverse ORDER). A.1 items 18/19 discharged and CONFORMANCE.md
§7's stale measured header re-measured (33 of 199, 166 remaining — audit_annex_a1.py owns the count).
Goldens 2023/pb154_cancel_scope / pb154_cancel_initial / pb154_cancel_active / pb154_cancel_cascade;
negative pb154-cancel-numeric (COBOLNET1696); char_seq_file snapshot re-baselined (the one intended line:
Close → CloseIfOpen). Wave-local gate GREEN on this tree (Conformance 2426/2426 · Unit 4636/4636 ·
Characterization 33/33), inventory drift gate 10/10, GAP 3679 → 3672. Battery #34 is owed for the
PB151+PB154 batch.


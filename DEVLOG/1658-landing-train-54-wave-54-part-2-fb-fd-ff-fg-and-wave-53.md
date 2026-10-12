## Entry 1658 — 2026-09-23 01:37 PDT — Landing train 54: wave 54 part 2 (FB, FD, FF, FG) and wave-53 finishers (EA, EB, EC), eight notes landed, GAP −6 to 2095

Train 54 was pipelined behind train 53 (workstream SKILL.md "Lander throughput"), but train 53 was already on
origin/main (`3f535e9dd`) when this lander started, so every cluster was merged, gated and landed on that one base
and no rebase was needed. Seven clusters, one over the 4–6 band. Each was brought in as a diff excluding the
traceability inventory, and each had a checkpoint commit. FB, FD, FF and FG came from their merge-base `57d36e4c6`.
EA came from `37f19e028`. EB came in as its arm-2 commits only (`60c05846d..af42b0a70`), because arm 1 is already
on main as `b7870a022`. EC came in as its own commit only (`ca8f16d06..4e9ca6f64`), because its merged PB202/PB189
predecessor is on main as `e9e36c609`. Taking EB and EC from the merge-base would have re-applied work that main
already carries.

**FB — contained-program GLOBAL LINKAGE carriers (PB1009 landed; PB1019 program arm, PB1019 stays open).** A
contained program that referenced a container's GLOBAL item (§13.18.27.4 GR2, "may reference that name without
describing it again") got C# compile errors in three cases: a GLOBAL LINKAGE formal, a GLOBAL BASED item's address
pointer, and `ADDRESS OF` a GLOBAL item (that one drew COBOLNET0869). Carriers were named by position (`__lnkp{N}`),
so a contained program's own first formal and the container's first GLOBAL formal were the same carrier. Carriers
are now named `__lnk{Uid}`. `DataBinder.GlobalBridgesOf` is now the one list of bridges for a GLOBAL root, and the
separate per-residence arms in `BinderDriver` are deleted. GLOBAL constants are inherited from the nearest
container first, and a local name shadows them. PB1019: an elementary formal named by an `ADDRESS OF` sender
(§8.4.3.11.3 SR1 / §8.4.3.11.4 GR1) becomes non-resident and is boundary-copied. Composition: main's
PB956/PB239 had rebuilt `PtrScanAddressOfTargets` around `(name, quals, method)` and the one
`dataAddressIdentifier` walk. FB's contained-program arm is re-expressed over that shape: the nested scan runs
through `PtrScanAddressOfSenders`, which is made internal and shared with `CallBindLinkage`, and the global-root
guard comes before main's BASED and method-formal guards. As a side effect, the scan now covers the CALL-argument
surface as well as the SET sender. The PB1009 batch (a PARTIAL for GR-13.18.27.4-2, the GLOBAL report-name
residue) was **NOT applied**, because no live note claims that row. The implementer's lead 1 (`RD … GLOBAL` →
COBOLNET0899) needs a registrar note first.

**FD — qualified OCCURS KEY (PB1018 landed; COBOLNET2353).** `KEY IS K OF B` was captured as the bare word, and
when the table had a second K the first match won. So SEARCH ALL on `K OF B` was refused (COBOLNET1965), and the
table SORT keyed on the wrong K. One post-build pass, `DataBinder.OccursKeyResolve`, now resolves each key over the
table's own subtree with §8.4.2.2 qualification (§13.18.38.3 SR3). An unknown key is now COBOLNET2353. Rows:
SR-13.18.38.3-3 and SR-14.9.37.3-8 are CONFORMS.

**FF — propagated conditions (PB606 + PB987 landed; PB993 stays open).** `CALL … NOT ON EXCEPTION` ran before the
condition the callee propagated was picked up (§14.9.4.4 GR3 i)). The pickup now runs first and returns
`__praised`, and the one NOT ON emitter is guarded on it on both the bare path and the try/catch path. A property GET
inside a condition was evaluated once per statement instead of per evaluation (§8.8.4.13 GR2). `UdfBinder.Mark` is
now a `PendingMark` over both pending lists. GR-8.8.4.13-2 is CONFORMS. GR-14.9.4.4-3 stays PARTIAL, with more
evidence.

**FG — refusals carry their diagnostic (PB1029 landed; COBOLNET2362; PB1030 stays open).** About 134 binder sites
reported an error and then returned a plain `BoundNop`, and about 99 more assumed a callee had reported one. Six
measured shapes compiled clean and then aborted the run unit. Every refusal now goes on an `EditionContext`
ledger, and the one statement funnel checks it: COBOLNET2362 means a refusal with no error, COBOLNET1756 means an
unbuilt operand. The operand error types are closed behind factories. **Composition was the largest merge in the
train.** Trains 51–53 had added report-then-no-op sites after FG's base: PB881's receiving-chokepoint refusals,
PB239's CALL `AddressArg` arms, PB210–PB212's operand-class screens, the SET program-address and
restricted-receiver arms, and PB1021's relation-operand address-identifier (a `new BoundOperandError(` that no
longer compiled). Each was rewritten in the PB1029 shape by the implementer's keep-both rule: fifteen of them were
found by FG's own `RefusalNodeDriftTests`, and two more (a ternary and a SEARCH `else`) by sweeping every remaining
`new BoundNop()`. Main's `ResolveSending`/`ResolveReceiving` roles are kept at every conflicted site. semgrep
`raw-diagnostic-code-literal` fell from 375 to 309, and the baseline is locked there. The PB1029 note's §8.8.2
quotation was misattributed by the doc-citation audit to §14.9.22.3. It is re-anchored in the note.

**EA — one §8.9 reservation gate for both parse paths (PB655 + PB764 landed).** Train 49 dropped this cluster
because of the Integration leg, and it is green here (503 passed, 1 skipped). `ReservationGateRewriter.ParseToFixpoint`
is now the one loop. The greenfield and legacy front ends each supply only their SLL→LL pass. PB764: the CALL AS
NESTED and SET LOCALE keyword arms are chosen by the grammar (`formatWord`, `setLocaleSource`), no longer by
spelling. Re-running `gen-reserved-words.ps1` and `gen-cobol-words.ps1` on the composed tree changed nothing. The
two batches were applied in separate invocations: PB764 restates SR-8.3.2.1-1 after PB655, and the writer refuses
the same row twice in one run.

**EB — POINTER storage image BY CONTENT (PB970 arm 2; PB970 landed).** One codec (`PointerImage`), and
`CallAbi.WithPointerContent` delivers it only for BY CONTENT with a same-length formal and no group layout. The
rule-2 arm refuses the same pairing at bind. DOC-A.1-216 is CONFORMS. Composition: main's PB971 `Omitted<T>()` is
kept, and a duplicate 2002 manifest entry (`pb970_image_carried_packed_returning_invoke`, already on main from arm 1)
was removed. The corpus loader now throws on duplicates.

**EC — pointer/object-leafed groups transfer their image (PB244 shape (a); PB244 stays open for shape (b)).** DISPLAY
and the MOVE sending group of a strongly-typed group with a class pointer/object leaf now transfer the storage
image, through the one-way `TransferImageCapable` capability. GR-14.9.11.4-4 and GR-14.9.11.4-6 are CONFORMS.

**The train.** Gate: `build-local.ps1 -Filter "FullyQualifiedName~CobolNet.Tests"`, which is the WHOLE Conformance
assembly. Results: Conformance `Passed! 8271/8271` (12m02s), Unit `Passed! 28980/28980`, Characterization
`Passed! 33/33`, and legacy Integration `Passed! 503, Skipped 1`. The external corpus fetched. The verdict line was
RED on one leg only, the doc-citation audit (the PB1029 note's §8.8.2 quote, attributed to FG). That was fixed, and
`audit_doc_citations` / `audit_code_citations` / `audit_evidence_supersession` were re-run green. The re-run of the
test legs was skipped because the change was one kb note line. `work.py check` ✓ 1065. **GAP 2101 → 2095** (−6).
Codes claimed: COBOLNET2353 (FD) and 2362 (FG); they do not collide. Dropped: none.


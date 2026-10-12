## Entry 1393 — 2026-08-29 14:33 PDT — PB157 LANDED: COMPUTE's boolean family on the ONE reader — a bit group is the elementary boolean it is defined to be, ZERO is not an ALL literal anywhere, Probe is finally pure, and Format 2's printed elements pay their way

One conversion closes the note's scope (kb/Work PB157): the raw-Pic readers in the boolean channel moved to
OperandPic — §13.18.29.4 GR1b's as-if PICTURE 1(m) reader six COMPUTE sites never received.
BuildComputeBoolean REJECTED a GROUP-USAGE BIT receiver as "not an elementary boolean item" while the
sending side of the same statement accepted it; BindCompute's F1→F2 reroute probes read raw Pic AND probed
only computeStore(0) (mixed `COMPUTE N B = 1` missed the boolean receiver — the probe spans the list now);
Gr3Width counted a bit group as 0 (CobolBool.Resize truncated the value); EmitComputeBoolean stored it at
width 0; BoolExprAllLengthOne/RefModLen/IsBooleanValueOperand/BoolValued and the §8.8.4.4.3 class-condition
screen read the same wrong reader. The reroute's failure arm — a BoundBoolError with NO diagnostic
(`COMPUTE B = N + 1` compiled and threw; the PB68 sweep's sixth site) — errors 1511 at bind.

THE REVIEW FLEET (100 agents, 21 confirmed findings) then earned its run three times over. (1) The widened
receiver probe exposed that **Refs.Probe was never the pure predicate its own R30 doc claims**: the OO
property hook registered a temp + pending op and the D18 subscript materializer BOUND fragments even under
a probe — an orphan op classified StoreKind.None and made OoWrapPropertyOps prepend a GET that §8.4.3.9.4
GR2 forbids for a write-only occurrence (or reject a WITH NO GET property outright). The fix is the
INVARIANT, not the caller: a `_probing` flag threads through ResolveImpl, the property hook answers the
MODEL item with no temp/op/diagnostics, the materializer returns a dummy, and the ref-mod/ambiguity
diagnostics gate on commit — every Probe caller in the compiler is pure now. (2) The SR3 fix was
one-arm-of-five: `COMPUTE B = ALL ZERO` (Format 1's OPTIONAL-ALL figurative, §8.3.3.6.2 — never the Format-6
ALL literal) and `= B-NOT ZERO` still rejected, and §8.8.2 rules 4/5 rejected `ZERO B-AND ALL B"1"` /
`ZERO B-SHIFT-L 2` on the same conflation — my parse-tree side-channel also mis-cited §8.3.3.6.3 SR1a, a
rule scoped to NUMERIC-literal positions. The structural fix: BoundBoolAll carries **IsAllLiteral**, set at
the ONE Format-6 construction site and cleared by the B-NOT fold; SR3 and rules 4/5 test the flag, the
side-channel is gone, and a sole positionless RHS REPLICATES to the receiver width at the store
(§8.3.3.6.4 GR2 — B-NOT ZERO answers all ones, not 10…0). (3) My golden could not fail: every boolean item
was 6 wide, so a Gr3Width returning 0 passed every leg — the W8 leg now mixes a 6-position group with a
3-position elementary (§8.8.2 rule 9 right-zero-extends; the wrong width answers 11000000, the pinned
value is 11011000). Also from the fleet: the rm.Category receiver read NEWLY admitted a ref-mod slice OF a
bit group onto a byte-image substrate the boolean channel sizes in BITS — staged 0899 (kb/Work PB173 owns
the bit-position slice, plus the PlaceRenderer raw-Pic pad and the pre-existing MOVE surface the refuters
measured); my LENGTH r1 arm was a NO-OP with a FALSE justification (LengthPositions already answers a bit
group's positions — pb79's golden proves it) and is reverted; and the THIRD copy of the stale
"{is2002()}?" gate claim (CobolParserCore.g4) is corrected.

Goldens: 2023/pb157_compute_bit_group (ten legs — F2 direct, bare/ALL ZERO, B-NOT ZERO, ZERO-as-one-operand,
the F1→F2 reroute, the corpus's FIRST multi-receiver Format-2 + Format-2 END-COMPUTE, truncate/zero-fill,
and the discriminating mixed-width W8), 2023/pb157_compute_multi_sizeerr (§14.7.5's partial commit, mixed
direction), 2014/pb157_compute_sdidi_sole (GR1a's exact-value arms under STANDARD-DECIMAL); negatives
pb157-compute-all-literal (SR3's actual ban, never pinned), pb157-compute-bool-arith-rhs,
pb157-compute-mixed-receivers, pb157-compute-group-receiver. Registered: PB173.

Verdicts: SR-14.9.8.3-2/-3 DIVERGES → CONFORMS · GR-14.9.8.4-3 DIVERGES → PARTIAL (the width model
observed discriminatingly; residue: the ref-mod length fallback + PB173's slice + unwired 14.6.13.2 r1) ·
GR-14.9.8.4-1 PARTIAL (sole-operand arms discharged; residue 14.6.13.2 r2) · GR-14.9.8.4-2 PARTIAL →
CONFORMS · FMT-14.9.8.2 PARTIAL → CONFORMS. Wave-local gate GREEN (Conformance 1902/1902 in-filter with
NIST · Unit 4650/4650 incl. the DiagnosticRegistry drift guard that caught the bare-0899 literal and
earned the RefModBitGroupSlice descriptor · Characterization 33/33), inventory drift gate 10/10, GAP
3667 → 3663. Battery #35 accrues from PB157.


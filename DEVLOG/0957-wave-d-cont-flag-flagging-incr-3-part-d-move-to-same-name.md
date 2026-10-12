## Entry 957 — 2026-07-21 18:26 PDT — Wave D (cont.): FLAG flagging Incr 3 (part) — d MOVE-TO-SAME-NAME (per-unit name resolution + same-DDE identity)

**Detector 12 of 19.** FLAG-02 **d MOVE-TO-SAME-NAME** (§7.3.14.4 GR4 d): a MOVE whose sending and a
receiving operand are described by the **same data description entry**, when that DDE is either **(1) category
alphanumeric-edited** or **(2) has a subordinate OCCURS…DEPENDING whose DEPENDING item is subordinate to it**.

**Approach (scouted + adversarially verified via an 8-agent Workflow, then CLI-probed).** The scout's central
recommendation — implement as a bind-time detector in `SetBinder`/`MoveBinder` — was **rejected** (it would fork a
second flagging mechanism; `feedback_singular_pattern`). Kept in the ONE `FlagConformancePass` parse-tree visitor,
extended with **per-unit name resolution**: a new `VisitProgramUnit` override (save/restore) selects the current
program unit's `DataBinder`/`ReferenceResolver` from a `ProgramUnitContext→BoundUnit` map, so an operand resolves in
ITS OWN COBOL name scope (duplicate data-names across programs never cross-resolve). "Same DDE" is then **exact
`ReferenceEquals`** of the two resolved `DataItem`s (via `ReferenceResolver.FindItem`, which resolves the
DESCRIPTION ENTRY and ignores subscripts/ref-mod — so `MOVE A(I) TO A(J)` is the same DDE, spec-correct). Condition
(1) = the established `{ Category: PicCategory.Alphanumeric, EditMask: not null }` test (there is **no**
`PicCategory.AlphanumericEdited` enum member — the scout's proposed test was impossible; corrected by the verify
agent). Condition (2) = the existing `OdoModel.TableUnder`/`IsWithin` pair (no hand-rolled tree walk).

**Both MOVE forms** handled (`MOVE CORRESPONDING a TO b` — CORR on the statement — and `MOVE a TO b…` multi-receiver);
a literal/function sender resolves to null and is skipped (can't be the same DDE). **OO method bodies** have no unit
map entry ⇒ `_current*` null ⇒ not resolved: a documented advisory **false-NEGATIVE**, never a false-positive.

**CLI-probed (verify-by-running) all cases**: ✅ `MOVE AE TO AE` (alphanumeric-edited) flagged · ✅ `MOVE G TO G`
(subordinate ODO inside G) flagged · ✅ `MOVE PL TO PL` (plain alnum) NOT · ✅ `MOVE AE TO AF` (distinct DDEs) NOT ·
✅ ODO DEPENDING item declared OUTSIDE the group NOT (IsWithin false, §13.18.38 SR20) · ✅ directive OFF NOT · ✅ no
directive NOT. 6 unit tests added (`FlagDirectiveTests` 41→47). Wave-local gate green: fresh build + FLAG 47/47 +
characterization **33/33 byte-exact** (zero-overhead invariant holds — no `>>FLAG` line = no walk change).


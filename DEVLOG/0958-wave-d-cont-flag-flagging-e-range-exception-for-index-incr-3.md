## Entry 958 — 2026-07-21 18:40 PDT — Wave D (cont.): FLAG flagging — e RANGE-EXCEPTION-FOR-INDEX; **INCR 3 COMPLETE**

**Detector 13 of 19.** FLAG-02 **e RANGE-EXCEPTION-FOR-INDEX** (§7.3.14.4 GR4 e): a Format-1 index-assignment
(`SET … TO`) or Format-2 index-arithmetic (`SET … UP/DOWN BY`) whose receiving field is an index, flagged when
EC-RANGE-INDEX checking is enabled. Reuses d's per-unit `_currentData`: two overrides (`VisitSetToValueStatement`,
`VisitSetIndexStatement`) — the two SET sub-rules that share their grammar with the pointer/object/dynamic-length
formats — test each receiver's base name against the current unit's INDEXED BY registry
(`DataBinder.IndexFields.ContainsKey`), gated by `_turn.Enabled("EC-RANGE-INDEX", null, line)` (the same TurnState
read i uses; the fold honours the EC hierarchy, so `>>TURN EC-ALL`/`EC-RANGE` also enable it).

**⛔ SPEC-PRECISE (a deliberate divergence from the scout AND the verify agent, both of which listed a USAGE INDEX
data-item receiver as a positive):** only an **index-NAME** receiver range-checks. §14.9.39.4 Format-1 **GR2b**
states a class-index DATA item (USAGE INDEX) receiver has its value copied **UNCHANGED** — no EC-RANGE-INDEX — and
Format-2 GR4a checks only `index-name-3`. Since GR4 e's own gate is "flagged when checking for EC-RANGE-INDEX is
enabled," flagging a receiver that can never raise EC-RANGE-INDEX would contradict the gate. So a USAGE INDEX
data-item receiver is **NOT** flagged. Matching only `IndexFields` names realizes exactly this: pointer / capacity /
dynamic-length / object-ref / USAGE-INDEX receivers of the shared SET grammar are intrinsically excluded (never an
index-name). (`feedback_spec_fidelity_discipline` — implement the NAMED EC, cross-check the SET GRs.)

**CLI-probed all cases**: ✅ `SET IDX TO 3` / ✅ `SET IDX UP BY 1` (index-name, EC on) flagged · ✅ `>>TURN EC-ALL`
(hierarchy) flags · ✅ EC-RANGE-INDEX off NOT · ✅ `SET IXD TO IDX` (IXD = USAGE INDEX data item) NOT · ✅ `SET N TO 3`
(plain numeric) NOT · ✅ directive OFF NOT. 7 unit tests added (`FlagDirectiveTests` 47→54). Wave-local gate green:
fresh build + FLAG 54/54 + characterization **33/33 byte-exact**.

**INCR 3 COMPLETE** (i REF-MOD-ZERO-LENGTH + d MOVE-TO-SAME-NAME + e RANGE-EXCEPTION-FOR-INDEX all landed). Next =
Incr 4, the new-analysis options (I-O-DECLARATIVE · I-O-STATUS-04/07 · EC-PROGRAM-EXCEPTIONS).


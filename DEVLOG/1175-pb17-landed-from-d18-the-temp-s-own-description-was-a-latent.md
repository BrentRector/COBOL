## Entry 1175 — 2026-08-04 19:13 PDT — PB17 landed from D18; the temp's own description was a latent wrong answer (PB41), and the gate that admitted it was written as a list (PB42)

**PB17 IS FIXED, IN FULL, PER THE OWNER RULING — no stage-loud stopgap.** `MOVE W-E(FUNCTION INTEGER(3)) TO W-R`
and `MOVE W-A(FUNCTION INTEGER(3):2) TO W-R` are legal source that compiled clean and threw
`NotImplementedCobolFeatureException` at run time; both now answer. So do the nested form, a USER-DEFINED function
in a subscript, and — decisively — a subscript inside a `PERFORM UNTIL` condition whose own operand changes in the
loop, which terminates at `W-I = 3` instead of spinning forever on a once-hoisted temp.

Implemented from `COBOLNET_DATA_MODEL_DESIGN.md` **D18**, which is what rule 2 is for: the segment's verbatim text
re-parses through an isolated `subscriptExpressionFragment : arithmeticExpression EOF` rule and binds through the
ONE `ExpressionBinder.BindExpr`, so the user-function case and the nested case fell out of the same change instead
of needing arms of their own. The value lands in the §15.4 temporary via `DataBinder.CreateCompilerTemp` and its
store rides the statement-pending list at the `BindStatement` chokepoint.

**⚠ THIS SESSION BEGAN MID-IMPLEMENTATION — THE MACHINE REBOOTED DURING THE PREVIOUS ONE.** The tree carried 12
modified files and 2 untracked ones, uncommitted, with no note saying how far along it was. Recovering the state
was reading the diff against D18's numbered execution order, not guessing: steps 1–6 were done, step 7 (goldens)
was not, and two of the six had been done DIFFERENTLY from the way the order specified — for good reasons that had
not been written down anywhere. Both corrections are now in D18.

## ⛔ THE §15.4 TEMPORARY'S DESCRIPTION WAS A CORRECTNESS DECISION, AND THE DESIGN'S FIRST ANSWER WAS WRONG

D18's execution order said to synthesize the temp with `Scale: 0` — a subscript is an occurrence number, so an
integer temp is the obvious shape. **It is exactly wrong, and wrong in the one direction that does not announce
itself.** §8.4.2.3.4 GR1b makes a subscript whose expression "does not result in an integer" SET
EC-BOUND-SUBSCRIPT, and §8.4.3.3.4 item 5)c says the same for a ref-mod position. A scale-0 temp *truncates on the
way in*, so `W-E(FUNCTION SQRT(2))` would have silently indexed occurrence 1 rather than raising — legal source
turned into a wrong answer **by the temp's own description**, inside the very change written to stop legal source
from throwing. The temp is `PIC S9(21)V9(9)`: 30 digits, the `Int128` wide tier, chosen so that no expressible
subscript overflows it (high-order truncation could WRAP an out-of-range subscript into an in-range one, which
converts a detectable error into a silent one).

## ⛔ AND ASKING THAT QUESTION EXPOSED PB41 — A PRE-EXISTING SILENT WRONG ANSWER, WITH NO FUNCTION IN SIGHT

If a scale-0 temp truncates, what does the compiler do with a scaled subscript **today**? It reads the raw
storage. COBOL.NET stores numerics unscaled — `PIC 9V9 VALUE 2.0` is the field `20L` at scale 1 — and
`ResolveSubscriptName` never consulted `Pic.Scale`, so:

```
01 W-S PIC 9V9 VALUE 2.0.
    MOVE W-E (W-S) TO W-R      ->  SCALED=00      (expected 22)
```

occurrence **20**, outside `1..5`, reading the benign scratch slot. Compiles clean, runs to completion, prints the
wrong answer. The fix is ONE ordinal-position read, `ReferenceResolver.PositionRead`, over shared
`CobolNum.HasFraction`/`PositionOf` arithmetic; the two wrappers (`CobolTable.Occ(…, scale)` and
`CobolString.RefModPosition(…, scale)`) exist ONLY because the positions name different Table 13 conditions. **The
position kind travels as a parameter, never as renderer state** — `RenderSegment` is re-entrant through
`ReadRefMod`, and ambient state goes stale across a re-entrant descent.

⚠ **The case a naive fix would have broken.** GR1b tests the integrality of THE RESULT, not of each operand:
`W-E(W-P + W-Q)` with `W-P = W-Q = 1.5` has the integral result 3.0 and is a legal subscript, while de-scaling
operand-by-operand gives `1 + 1 = 2` **and** raises the condition twice on source that never violated it. A
compound segment carrying a scaled operand therefore goes through the temp, where the rule applies once.

⚙ **PB41's own "not yet measured" list — probed rather than assumed, and all three came back clean.** `SET` an
index FROM a scaled item and a `PERFORM VARYING` bound compared against one already read the VALUE. And an
`OCCURS … DEPENDING ON` a scaled item is not a runtime question at all: **§13.18.38.3 SR17** — "Data-name-1 shall
describe an integer" — makes it illegal SOURCE, which `COBOLNET0852` already rejects. A defect that looks like it
must generalize is still a measurement.

## ⛔ PB42 — THE SWEEP FOUND THE SAME BUG ONE TOKEN OVER, BECAUSE THE GATE WAS WRITTEN AS A LIST

Rule 4 says every bug is a pattern. PB17 fixed ONE token the segment renderer could not render, and the gate it
shipped with asked **"is this segment FUNCTION-bearing?"** — a list — instead of **"can the renderer render it?"**
— the actual question. Two shapes of plain arithmetic were still dying:

```
MOVE W-E (W-I ** 2) TO W-R      ->  NotImplementedCobolFeatureException: reference 'W-E(W-I ** 2)'
MOVE W-E (2.0)      TO W-R      ->  NotImplementedCobolFeatureException: reference 'W-E(2.0)'
```

**§8.8.1.1** admits "a numeric literal … separated by arithmetic operators" and **§8.3.2.4.2** lists `**` as
"Arithmetic operator - exponentiation", so both are `arithmetic-expression-1` under §8.4.2.3.2. `W-E(2.0)` is
occurrence **2** — 2.0 IS an integer value — and only `W-E(2.5)` raises.

⚠ **`**` LOOKED HANDLED AND WAS NOT, and the reason is worth keeping.** `SUB_POWER` appears in the new
compound-segment predicate but in **no `case` arm**. It therefore worked only by accident: when an operand
happened to be SCALED, PB41's compound rule diverted the whole segment to the materializer before the renderer
reached the `**`. **The probe I would naturally have written — the scaled one, since PB41 was the subject —
reports this as working.** It took an unscaled probe to see it.

⛔ **The fix is not another arm; it is deleting the list.** Everything the renderer cannot render now routes to the
fragment, and **the `arithmeticExpression` grammar is the adjudicator** — a shape §8.8.1.1 does not admit (an
alphanumeric literal, an `ALL` figurative constant) cannot parse, the hook returns null, and the caller keeps its
exact loud posture. Admission is by parsing, not by assertion, so the next arithmetic form needs no edit at all.
That is the difference between the PB1 unaudited-table mistake and this: PB1 enforced a hand-written table nothing
had verified; here the verification IS the mechanism. And `**` could not have been fixed the other way — there is
no C# infix `**`, so a `case` arm would have to hand-write `Math.Pow` plus §8.8.1.2's arithmetic-mode semantics
into a `StringBuilder`, which is the third expression compiler D18 exists to prevent.

## ⚖ WHAT IS STILL DEBT, STATED PLAINLY

- **The inherited per-evaluation residue.** Three windows `UdfStagePerEvaluationResidue` already staged loud for
  user functions — `PERFORM VARYING BY`, a `VARYING AFTER`-level `FROM`, an `EVALUATE` selection subject — now
  stage loud for function subscripts too (`COBOLNET1509`, two negative fixtures). That is the precedent D18
  required be followed *including its loud residue*, and it rejects legal source in those three positions. A
  pre-existing GAP surface newly reachable by a second spelling — not a new decision, but not nothing either.
- **PB42's own residue: right position, wrong stage.** An alphanumeric-literal or `ALL` subscript is correctly
  refused, but at RUN TIME rather than as a bind diagnostic.

## 🔬 PROCESS — TWO MISSTEPS WORTH THE INK

1. **I edited compiler sources while the comprehensive battery was running**, having checked that phase 1 uses
   `--no-build` and NOT that **phase 2 rebuilds**. Phase 1's numbers were valid for the tree it measured
   (Conformance **4199/4199**, Unit **3646/3646**, characterization **33/33**, zero skipped); everything after it
   would have measured a half-finished PB42. I stopped the run rather than keep a result I could not attribute,
   and re-ran the battery clean on the final tree. This is the same family as the fleet-rebuild incident the
   `fleet_active_build.py` guard was written for — the guard covers subagent fleets, not batteries.
2. **One citation of mine was wrong in the way rule 1 names.** I wrote §14.9.38 for the SET statement; §14.9.38 is
   SEND, and SET is **§14.9.39**. `cite.py --check` caught it before it reached a golden's comment. The clause
   number I did not re-derive was the one that was wrong.

⚙ **§0 lost its fix-queue enumeration this session.** That line listed landed items as live — exactly the rot rule
8 predicts of a worklist living anywhere but `kb/Work/`. It now points at the register instead of restating it.


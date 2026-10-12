## Entry 1191d — 2026-08-06 15:22 PDT — PB49: hold the two string-image channels per-leaf, and the NUMERIC half needed an ANSWER rather than a guard

> ⚙ **RECONSTRUCTED 2026-08-07 from the primary sources — the commit message (`4808178a`), its `kb/Work/` note and its diff — not written at the time.** The commit landed without a DEVLOG entry; the owner's call was to reconstruct where it can be done accurately. Every fact below is carried from those sources; nothing is recalled. **Lettered rather than renumbered** because entries 1192–1198 are already referenced by pushed commit messages, which cannot be edited.

**PB25 was one rule written in two places with only one copy right.** `IntrinsicRenderer.StrArgVisitor` delegates
SOME `BoundOperand` leaves to `OperandText.AsString` and answers the rest itself, so it inherited neither the
figurative arm nor the ALL-literal arm — and `FUNCTION LOWER-CASE(SPACE)`, legal source, compiled clean and
aborted at RUN TIME. PB25 routed the two offending leaves to `AsString`; the guard its own evidence file
prescribed was never built. This entry is that guard.

**BUILT PER-LEAF, AS THE NOTE REQUIRED.** A blanket "these two functions are equal" would be WRONG, because the
channels are MEANT to differ. Three buckets:

- **DELEGATED** — `BoundFieldOperand` / `BoundFigurative` / `BoundAllLiteral` must route to `OperandText.AsString`.
- **DIVERGENT BY DESIGN** — `BoundNumericLiteral` / `BoundBoolOperand` must stay LOUD in the argument channel.
  ⭐ This direction matters as much as the first: "fixing" them into agreement would silently ADMIT an operand
  §15.3 excludes — the inverse of PB25, and just as quiet.
- **UNCLASSIFIED IS A FAILURE** — every leaf both channels implement must sit in one bucket, so a NEW leaf forces
  a decision rather than being quietly added to one side, which is how PB25's arm went missing.

**PROVEN IN THE FAILING DIRECTION:** re-introducing PB25 reds the guard on that exact row.

⭐ **AND THE NUMERIC HALF HAD AN ANSWER RATHER THAN A GUARD.** The note said to check the same question for the
numeric channels first. Measured: the compiler has FOUR `IBoundOperandVisitor` implementations — `StrArgVisitor`
and `AsStringVisitor` (string), `IsStringVisitor` (a bool classifier), and `NumericRenderer`. `NumericRenderer` is
the ONLY numeric one; `OperandText.NumericIntrinsicText` is a helper it is called from, not a parallel visitor.
There is no numeric twin, so there is nothing to guard. A test asserts the count is exactly one, so that answer
expires loudly if a second numeric channel ever appears.

**GATE.** Test-only — no compiler source altered — so the wave is the unit suite (**3992/3992**, up 6) plus
characterization (**33/33**). The tree is otherwise identical to PB33's, whose full battery was ALL GREEN.


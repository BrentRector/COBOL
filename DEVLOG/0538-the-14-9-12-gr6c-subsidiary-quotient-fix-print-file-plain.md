## Entry 538 — 2026-06-10 09:48 PDT — The §14.9.12 GR6c subsidiary-quotient fix + print-file plain WRITE → 54 NC programs byte-match

**The 10-agent DIFF-diagnosis workflow landed and immediately paid out:**
- **DIVIDE REMAINDER scale bug (the workflow's precise find):** `EmitDivideRemainder` rendered the quotient via
  the renderer's working-scale promotion (max of receiver and operand scales) but TAGGED it at the receiver
  scale — for `DIVIDE 22 INTO S9(6)V9(6) GIVING S9(5) REMAINDER S99` the "quotient" was 5050.500000 mislabeled
  scale-0, so the GR7 back-multiply poisoned the remainder (−89 instead of 11) and the store truncated the
  quotient to 0. Per ISO §14.9.12 GR6c the SUBSIDIARY quotient is truncated to the GIVING receiver's
  digits/scale: now a DIRECT kernel call `CobolNum.Divide(…, receiverScale, Truncation)` produces it at exactly
  that scale; the stored quotient recomputes at the receiver's own ROUNDED mode; the remainder follows GR7 with
  true scale tags. (DivideOrThrow selected in ON SIZE ERROR contexts.)
- **Print-file plain WRITE (§14.9.46):** a `WRITE` with no ADVANCING on a print file still line-advances — our
  record-sequential plain write emitted a raw fixed-width block that WELDED onto the previous print line
  (NC135A's NOTE block became one 360-char line). Once a connector has seen print-control advancing, a plain
  write now routes through the advance machinery in the write-then-advance shape, which reproduces the golden's
  pending-advance stream exactly (blank-line placement included).
**NC203A, NC251A, NC135A byte-match (NC133A also locked from the sweep) → 54 NC programs. 437 conformance + 15
unit green.** Workflow synthesis still in hand for the rest: NC172A/173A/175A/170A = expression-pipeline products
beyond long (the FULL CobolInt carrier), NC114M edited corners, NC219A collating, NC215A/250A/235A per-row maps.


## Entry 332 — WS-SPEC round 1: 57 verified spec-conformance tests + a 30-item fix backlog (9-agent workflow)

Ran a 9-agent WS-SPEC authoring workflow (one agent per live module). Each read its `docs/SPEC_GAP_INVENTORY.md`
section, authored conformance tests for the under-tested features, and **CLI-verified each** (compile + run,
assert the observed output) before returning it. Integrated the returned `.cs` files (HTML-decoded; the
`EndToEndTestBase.CompileAndRun` pattern): **build clean, 57/57 pass, guard ALL GREEN (integration 349→406).**

The **30 features that did NOT pass are genuine compiler bugs/gaps** — each diagnosed with a spec citation +
code location — captured in `docs/SPEC_FIX_BACKLOG.md`. Notable **P0** correctness bugs the audit surfaced
(hidden because NIST never exercised them on a passing path):
- `DISPLAY … WITH NO ADVANCING` is a silent no-op (a CRLF is still written after the operand).
- `USAGE COMP-1`/`COMP-2` **arithmetic** truncates the fraction (`COMPUTE WS-F * 2` with 3.14159 → 6.0000; MOVE is
  fine, so the bug is in arithmetic lowering not promoting to float).
- A ref-modded alphanumeric function argument (`FUNCTION UPPER-CASE(X(1:4))`) returns numeric 0.
- Table-ref `ALL` over an ODO table ranges over OCCURS MAX, not the DEPENDING-ON current value.
- Multi-char `CURRENCY SIGN` truncated to one char; `CALL … RETURNING` into WORKING-STORAGE wrongly rejected.

Next: the **fix round** — the 30 items are well-located, ideal for a parallel pass, P0 first. The Report Writer
subset (RH/RF, CONTROL/SUM, detail VALUE/SOURCE/LINE-COUNTER) feeds WS-SPEC-RW (#7).

**Lesson:** the spec-completeness audit pays off exactly as designed — authoring a passing test *per spec
feature* flushes out correctness bugs that the NIST suite's happy-path baselines mask.


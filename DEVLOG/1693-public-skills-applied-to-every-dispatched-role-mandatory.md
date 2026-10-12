## Entry 1693 — 2026-09-24 21:59 PDT — Public skills applied to every dispatched role (MANDATORY-PRACTICES P10); wave 59 dispatched

Owner, starting Friday's quota early: "apply all of our new relevant skills". The generalized skills published at
BrentRector/claude-skills (local clone `E:\claude-skills`) were referenced by four project skills as a "Generic base"
link, but no dispatched agent was ever told to READ them — the bar existed and nothing applied it.

- `MANDATORY-PRACTICES.md` gains **P10** and a "Public skills by role" table: implementers read
  `engineering-standards` + `dotnet-engineering` + `spec-oracle` before work, apply `variant-analysis` +
  `roslyn-analysis` to the sibling sweep, and SELF-REVIEW their diff against the `pr-test-analyzer`,
  `silent-failure-hunter`, `comment-analyzer` (+ `type-design-analyzer`) agent files before the report; registrars
  cluster with `variant-analysis`; adjudicators/refuters apply `spec-compliance-audit` + `spec-oracle`; landers
  `test-gate` + `silent-failure-hunter`; the orchestrator `agent-fleet` + `claude-cloud-sessions`. Project rules win
  on conflict.
- Every role brief carries the P10 line, both lane-3 workflow prompts carry it, the implementer dispatch-spec template
  carries it, and `check_practices.py` now FAILS a brief or rendered spec that drops `claude-skills` — so the next
  brief applies it automatically (rule 5).
- `land-a-fix` and `new-construct` gain "Generic base" pointers (engineering-standards, dotnet-engineering,
  variant-analysis).
- Friction fixed on the way: `fix-lane-implementer-brief.md` still hard-coded two dead sessions' scratchpad paths for
  the verdict batch and the report; both now defer to the dispatch spec's Scratch/Report paths.

Wave 59 (six implementers, one group per subsystem, codes COBOLNET2424–2441): A R43 derived selectors for clause rules
(PB1198 lead; + the PB579 Q2 VALIDATE leak) · B integer operands narrowed to C# int (PB1033, PB1058 crash, PB1178) ·
C sort-merge EC raise sites (PB1036) · D exact intrinsic arguments narrowed to binary64 (PB1041) · E dynamic-length
record round-trip (PB1053) · F implicit POP ALL vs >>DEFINE (PB1066).


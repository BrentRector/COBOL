## Entry 1762 — 2026-09-28 16:44 PDT — The four vetted learnings missing from the skills are encoded (claude-skills v1.13.0); every vetted learning now lives in a skill

The owner: "encode all four as v1.13.0". He pushed claude-skills v1.13.0 (89ac9d4). Each learning is marked validated
and uses the validator's corrected figures:
- **C6, re-probe before fixing:** agent-fleet §2, the implementer's first step, and the brief template. An item that
  no longer reproduces is discharged with evidence, and the discharge goes to a refuter. The evidence: 114 of 331
  re-probed rows were already fixed.
- **C9, the registrar re-checks leads, narrowed form:** a new registrar role in agent-fleet §2 and the brief
  template. It runs the given repro once and probes fresh only when the lead has no repro or code site.
  **Correction:** the orchestrator had told the owner "three of seven leads died in one pass"; neither the validator
  record nor the log supports that. The skill uses the recorded figures: 3 of 52 in one pass, and three findings plus
  a paraphrased citation in another.
- **C25, battery in its own worktree:** test-gate's tiers and agent-fleet §6. About 45 min is measured; "about 7
  clusters" is labelled modelled. It adds the validator's caveat that the battery still shares the cores, so a freeze
  becomes a slowdown.
- **C52, conflict markers between staging and committing:** a new test-gate section and agent-fleet §4. It gives the
  two exact commands, reads their output rather than their exit code (because of CRLF), and adds a repo-wide marker
  test that must be seen to fail.

Evals, with and without the skill: registrar re-run 1.00 vs 0.00, staged conflict markers 1.00 vs 0.17, and
re-probe backlog 1.00 vs 0.50, all kept. The battery-worktree case scored 1.00 vs 1.00, so the baseline already
does it; it was dropped and recorded.

`learnings-ledger/encoded.jsonl` gains nine location lines. All 46 vetted learnings now have a home in a public skill.


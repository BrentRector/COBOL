## Entry 918 — 2026-07-19 17:51 PDT — Context-doc refresh sweep: every session-bootstrap doc current; zero dangling references to the consolidated-away files

The owner asked for all context docs a new session needs to resume toward completing the compiler. Swept and
fixed (12 docs):
- **The plan §0 gains an explicit SESSION BOOTSTRAP checklist** (①CLAUDE.md rules → ②§0 → ③checkout+battery →
  ④top worklist item, spec-first → ⑤update §0 + DEVLOG, commit+push) and its §4 P13 row's stale carried tail
  (audit/resume-prompt pointers, old battery numbers) is corrected; four carried-text residuals inside the
  absorbed §4.1/P15 sections now point at §0/Part III.
- **CLAUDE.md's drift-prone status-snapshot paragraph SHRUNK to a lean pointer** (it had drifted twice before;
  it now states only the D13 mission, the plan-is-the-only-plan rule, and "the §0 banner is the ONLY live
  resume point"). PROMPT.md + CONSTRAINTS.md kickoff pointers retargeted at the plan §0.
- **Pointer retargets** in COBOLNET_DATA_MODEL_DESIGN, COBOLNET_OO_SLICE_BRIEFS (×3 slice checklists),
  DESIGN-test-build-ci, COBOLNET_DESIGN (earlier).
- **FROZEN HISTORICAL banners** added to the three legacy planning ledgers a session could mistake for live
  tracking: COMPLETION_ROADMAP_COUNCIL.md, ISO2023_CONFORMANCE_PLAN.md, PHASE4_RECONCILIATION.md (the review
  proved rows in the latter two stale — each banner now says so and points at the plan §0/§8 + the review
  ledger §24).
- **Supersession notes** on the two P13 scouts (they cite the deleted audit as provenance; the note points at
  the verified review ledger + the §0 worklist; scouts still the working designs, deleted at P13 close).
Verification: repo-wide grep — zero live-doc references to resume-prompt/the phase docs/the audit remain
(DEVLOG + the review ledger's evidence citations are history, intentionally untouched). README checked, current.


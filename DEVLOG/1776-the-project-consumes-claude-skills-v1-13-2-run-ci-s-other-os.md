## Entry 1776 — 2026-09-29 09:50 PDT — The project consumes claude-skills v1.13.2 (run CI's other-OS legs locally)

The owner pushed the public claude-skills v1.13.2 (`64d153a`). Its agent-fleet skill now carries PB1732's general
practice:
- run CI's other-OS test legs locally before pushing, all of them when they are cheap (measured here at ~4 min);
- build on the target OS for tests that embed build paths;
- make git readable across the OS boundary;
- keep a drift test holding the local legs equal to CI's.

The `tools/claude-skills` pin moves 1.13.1 → 1.13.2. The four project skills (review, spec-lookup, workstream, gate)
and PB1699 name the new pin, and `check_practices.py` is GREEN.


## Entry 1712 — 2026-09-25 13:39 PDT — Guard hooks write UTF-8 stderr (first live block arrived mangled)

R49's role agents were proven live after the registry reload: a `cobol-refuter` smoke agent's Write into
`E:\CobolSharp\docs` was BLOCKED by `readonly_repo.py` and its Write into the scratchpad succeeded. The block
message's em dash reached the agent mangled: Python on Windows encodes stderr in the console code page, while the
harness reads hook stderr as UTF-8. Both guard hooks (`readonly_repo.py`, `forbidden_commands.py` — the sibling with
the same em-dash messages) now `sys.stderr.reconfigure(encoding="utf-8")`; the bytes were checked (E2 80 94) and
both self-tests stay green (21/21, 4/4). Also: push-main's second invocation for Entry 1711 reported REFUSED only
because the first, backgrounded with `&` and believed dead, had already landed e105e0833 on a green ci-gate.


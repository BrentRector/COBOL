## Entry 1887 — 2026-10-05 04:55 PDT — A wave unit hung two hours at start: the supervisor deadlocked writing its prompt against the CLI's large init event (PB2022)

The wave unit started at 23:53 produced a 0-byte stream log, no checkpoint and no work for two hours (`claude.exe` at 0.66 s of CPU, no session transcript, no
children); I killed it at 01:50 and the supervisor recorded it as failed and synthesized its handoff, which worked. The owner asked whether it was doing useful
work and said to fix it so it does not recur. The first hypothesis was a machine sleep; the evidence refuted it (the AC sleep timeout is 0, the power log holds no
suspend event, `checkpoint.py` runs in 0.4 s). The cause, reproduced: `Invoke-Unit` wrote the prompt with a synchronous `WriteLine` before it read stdout, the CLI
writes a large `init` event before it reads stdin, and once the wave prompt outgrew the stdin pipe buffer (about 4 KB; PB2015 and PB2016 had added paragraphs to
it that day, while `meter` stayed near 3 KB) the two blocked on each other. A fake that writes 300 KB of `init` before reading stdin hangs the old supervisor and
fails both new checks; the fix makes the prompt write asynchronous so the read loop starts at once, and adds a startup watchdog that counts loop ticks (not wall time) and
kills a unit that never emits an event. A mistake of mine along the way: my first `silent` fake emitted `init` before its mode switch, which disarmed the very watchdog it
tested; a hung unit emits nothing, and the fake now does too. `test_orchestrate.ps1` 78/78 in 54 s.

**Files:** `scripts/orchestrator/orchestrate.ps1`, `test_orchestrate.ps1`, `testdata/fake-claude.ps1`, `kb/Work/PB2022.md`. No diagnostic code used.

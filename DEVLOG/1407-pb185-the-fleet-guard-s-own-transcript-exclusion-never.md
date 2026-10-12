## Entry 1407 — 2026-08-31 01:05 PDT — PB185: the fleet guard's own-transcript exclusion never matched a subagent — and the payload said so, once someone looked

The guard that freezes the tree while a subagent fleet is live has now self-blocked three different ways, and
each fix was a string comparison bolted next to the last one. PB101 was an agent inside its own worktree; PB103
was a main-tree agent counting its own transcript, fixed by excluding the hook payload's `transcript_path` from
the live set. PB185 is that exclusion not working for any subagent at all: two agents in one session, each alone
in `E:\CobolSharp` with no fleet running, were told `BLOCKED — 1 subagent transcript(s) were written in the last
120s, so a fleet is LIVE: subagents`, and because a caller rewrites its own transcript on every tool call, the
120-second window never cleared. The block was permanent for the whole agent. It cost the PB164 finisher its
`SpecTraceabilityInventoryDriftTests` reference check and a three-shape CLI probe of PB190.

The note hypothesised "a differently-rooted or differently-cased spelling of the agent file". That was close
enough to sound like a diagnosis and wrong enough to have produced a fourth string comparison. So the payload
was measured instead of imagined — the hook was instrumented to dump its stdin, and re-fired from this
subagent:

```
"session_id":      "17d093ad-48d4-41f7-acc0-cfc45e58b424"
"transcript_path": "C:\\Users\\brent\\.claude\\projects\\E--CobolSharp\\17d093ad-….jsonl"
"agent_id":        "acc9d0d75b870eb9d"
```

The file the glob finds — the one the caller actually writes — is
`…\projects\E--CobolSharp\17d093ad-…\subagents\agent-acc9d0d75b870eb9d.jsonl`. These are not two spellings of one
path. A subagent's `transcript_path` is the SESSION transcript, exactly as the main session's is, so no
normalization of either string could ever have made the equality hold. What the payload *does* carry, and what
nobody had read, is `agent_id`.

That is also the shape of the fix. The guard's question is "is any transcript OTHER THAN MINE live?", and the
reliable answer is an identity, not a path: `caller_identity` takes `agent_id`, whose transcript is
`agent-<agent_id>.jsonl` **wherever in the `subagents/` tree the host puts it** — including
`subagents/workflows/wf_*/`, which a path-prefix rule would also have missed. `transcript_path` survives as a
second, independent spelling for hosts that do point it at the agent file, now compared on a resolved
`pathlib.Path` rather than raw text; a payload with neither identity excludes nothing, which is right, because
the main session's transcript is not under `subagents/` and never enters the candidate set. The decision became
a pure function, `foreign_transcripts(payload, candidates)`, with the I/O at the edges.

A guard whose failure mode is *failing closed* cannot be trusted on silence — three of this hook's defects were
the ALLOW branch failing, and every one of them looked identical to a guard working perfectly. So both branches
were fired. `python scripts/hooks/fleet_active_build.py --self-test` is a new mode needing no fleet: subagent
alone → allow, own file nested under `workflows/` → allow, subagent inside a real fleet → deny, main session +
fleet → deny, case-folded id → allow, no identity → deny, plus the verb matcher both ways so `dotnet --version`
stays out of scope. 12/12. It earned its keep immediately by failing on its own first draft — a `PurePosixPath`
that never reached the `resolve()` line, i.e. a probe that would have passed without executing the code it
claimed to cover. Then the live script was driven over stdin while this agent's transcript was genuinely fresh:
a main-session payload DENIED and the subagent payload ALLOWED in the same run. And in the harness itself,
`dotnet build --help` was BLOCKED twice before the change and returned exit 0 after it.

Real fleet protection is unchanged. The `dotnet.exe` spelling is still uncaught, deliberately, as in PB103: this
is a reminder for the model, fail-open by construction, not a security boundary.


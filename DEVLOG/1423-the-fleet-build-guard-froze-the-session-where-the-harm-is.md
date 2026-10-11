## Entry 1423 — 2026-09-01 22:13 PDT — The fleet build guard froze the SESSION where the harm is the TREE: PB279 makes the unit of the freeze the working tree, derived, and two implementers in two worktrees can finally both build

The 2026-08-04 guard exists because ~6 rebuilds under a running 60-agent fleet made all 60 verdicts unusable, and
it has been right about the harm and wrong about the scope ever since. It denied `dotnet build|test` whenever ANY
foreign transcript of this session had been touched in the last 120 s — wherever that agent was working. Today's
direction for MAXIMUM SUBAGENT PARALLELISM in the fix lane turned that into the lane's binding constraint: N
implementer agents dispatched with `isolation: "worktree"` into N private checkouts each waited on the others'
mere liveness, for a collision that cannot happen. Two `git worktree` checkouts share no `bin/obj`, no
`cobol.exe`, no source file.

**The rule is now: deny iff a FOREIGN live agent works in the SAME working tree as the caller** — and both trees
in that comparison are DERIVED, never listed, because a hand-maintained agent→tree map is the shape rule 5
forbids and would go stale the first time a directory was renamed. The caller's tree is the repository root of
the payload `cwd`: the nearest ancestor holding a `.git` entry, which is a DIRECTORY in the main checkout and a
FILE in a worktree — stopping at the FIRST one is exactly what makes a worktree resolve to itself rather than to
the repository it was cut from. The main checkout is that root, or the `gitdir:` target parsed out of the
worktree's `.git` file (parsed in-process, relative forms included; a PreToolUse hook runs on every `dotnet` call
and must not pay a subprocess). And a foreign agent's tree is `<main>/.claude/worktrees/agent-<agentId>` when
that directory EXISTS, else the main checkout — true by construction, since `Agent(isolation="worktree")` creates
precisely that path, so the directory's existence IS the launch mode. All three consequences are intended: N
worktree agents build in parallel; a main-tree build is still denied by a live main-tree agent (the original
purpose, untouched); a main-tree build is allowed while only worktree agents are live.

⚠ **The fallback is toward the OLD rule, never toward allowing.** An unreadable or unparseable `.git` means the
agent→tree map cannot be built at all, so the decision reverts to the session-wide deny. That is a different
posture from the hook's fail-OPEN construction, which covers a BROKEN guard; this covers an UNKNOWN tree, where
allowing is the one outcome that reproduces 2026-08-04.

Two extras came with it, both deliberate. PB103's blunt `/.claude/worktrees/` bail is **removed** — it allowed
any build whose `cwd` or `cd` merely mentioned that path, which under the new rule would MASK the same-tree deny
and let a worktree agent build while a second agent probed that very worktree; the precise comparison subsumes
it. And `deny_reason` had been reading `p.parent.name`, whose value for `…/subagents/agent-bbb.jsonl` is the
literal directory `subagents` — so every denial for months blamed a non-existent agent named "subagents". It now
names the agents, names the shared tree, and offers `isolation: "worktree"` as remedy (d): the guard teaches the
way out of itself.

Proof is 37/37 `--self-test` over REAL temporary worktrees — a fake for either the `.git`-file parse or the
`agent-<id>` existence check would be the well-formed-but-worthless probe this guard exists to prevent — plus an
end-to-end run against the real `E:\CobolSharp` checkout and its real `.claude/worktrees/agent-*` directories,
with only the fleet's config root synthetic so no live transcript was touched. With a worktree agent and a
main-tree agent live at once, the main-tree build is denied naming ONLY the main-tree agent, the worktree build
denied naming ONLY that worktree's agent, a nested `cwd` walks up to the same answer, and the ALLOW arm fires
when only the worktree agent is foreign. That probe's FAIL branch fired once — on my own backwards expectation,
since a caller excludes ITSELF — which is the only reason its silence is worth anything.

**And a citation this landing had to correct.** The hook's own docstring recorded its defect history as `(PB101
worktree agents, PB103 main-tree agents, PB185 every subagent)`, and the landing brief inherited that verbatim.
PB101 is the COLLATION subsystem note; it appears in this history only because its agent was the one who HIT the
worktree mode. The defect note is PB103, which records BOTH the worktree mode and the main-tree self-count as one
landing. Corrected at both spellings in the hook. This is rule 1's failure mode exactly — not an invented
citation but an INHERITED one, carried forward because the id was never re-derived — and it is worth logging that
it reproduces on work-item ids, not only on spec clauses. The two DEVLOG occurrences stand: they record what was
believed when they were written, and this log is the one document that is history rather than current state.

Registered as `kb/Work/PB279.md` (defect · landed · process_only). Design: `DESIGN-test-build-ci.md` §1.7, a new
subsection, with plan §0's stale present-tense statement of the old rule corrected and the "Mechanics that have
burned us" bullet pointing at it. No compiler source changed; no gate is owed.


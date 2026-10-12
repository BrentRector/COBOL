## Entry 1741 — 2026-09-27 13:14 PDT — The model follows the role: mechanical roles on Sonnet (clerk + new read-only locator), enforced

Owner, 2026-09-27: "Move mechanical roles to Sonnet", then "Update skills appropriate for the mechanical roles".
`cobol-clerk` was already declared `model: sonnet`, but MANDATORY-PRACTICES P1 said "`model: 'opus'` on every agent",
and a per-call `model` OVERRIDES a role's frontmatter. So any clerk dispatched from a workflow ran on Opus. There was
also no read-only mechanical role: today's code-site location pass (30 lookups, no judgment, about 165k tokens) ran
on the Opus adjudicator at high effort.
**Now:**
- **P1: the model follows the ROLE.** Judgment roles (implementer, lander, adjudicator, refuter, registrar,
  reviewer) stay on Opus by alias, and the workflow passes `model: 'opus'` for them. Mechanical roles take Sonnet
  from their own frontmatter.
- **The mechanical roles:** `cobol-clerk` for chores that write. The NEW `.claude/agents/cobol-locator.md` (Sonnet,
  medium, 120 turns, read-only by the same `readonly_repo.py` hook as the adjudicator) handles code-site location,
  orient.py / fix_clusters.py / where.py summaries, and transcript or log measurements. Either returns
  `NEEDS-OPUS: <why>` on a judgment call, and that item goes to a judgment role.
- **Enforcement:** `check_practices.py` FAILS any workflow template that passes `model` on a clerk or locator
  `agent()` call. Proven with a probe template (RED), then removed (GREEN).
- **Registration:** `tooling_check.py` lists the new role.
- **Docs:** the workstream skill, O4 and `docs/DOC_INDEX.md` describe the two model tiers.
**Fable (owner question "should any skills use Fable?"): no role defaults to it.** Anthropic's help article "Claude
Fable models on your plan" says Fable models "draw from your plan's regular weekly usage limits" and "use them
faster than other Claude models"; the Fable row on the usage page is a CEILING (at most 50 % of the weekly limit may
go to Fable), not a separate pool. I first read that row's wording ("Separate weekly limit for Fable") as extra
capacity and proposed a pilot; the owner corrected it, and the help article confirms the correction. Fable costs
more of the same weekly quota per unit of work (about 2x, owner's estimate; the article gives no rate), and Opus
5.5 is close to it, so every role stays on Opus or Sonnet. The only use is a per-item escalation for one defect
Opus has failed twice, decided case by case. No pilot.


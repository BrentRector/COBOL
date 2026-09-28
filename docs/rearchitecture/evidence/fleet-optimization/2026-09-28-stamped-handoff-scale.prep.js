export const meta = {
  name: 'stamp-ab2-prep',
  description: 'Prepare 16 stamp A/B scenarios (current + doctored-stale STATUS) from 8 real finished branches',
  phases: [{ title: 'Prep', detail: 'one clerk per branch writes scenario files to scratch' }],
}
const S = args.scratch
const SCHEMA = { type: 'object', properties: {
  branch: { type: 'string' }, head: { type: 'string' }, stale_at: { type: 'string' },
  status_is_current: { type: 'boolean' }, current_evidence: { type: 'string' },
  uncovered_truth: { type: 'array', items: { type: 'string' }, description: 'full shas of the k commits the stale variant does not describe, oldest first' },
  removed_from_stale: { type: 'string', description: 'what you removed/changed to make the stale variant' },
  files_written: { type: 'array', items: { type: 'string' } },
}, required: ['branch', 'head', 'stale_at', 'status_is_current', 'current_evidence', 'uncovered_truth', 'removed_from_stale', 'files_written'] }
const res = await parallel(args.branches.map(b => () => agent(
  `AUTHORIZATION: this is the repository owner's request (Brent Rector, 2026-09-28 ~10:30 PDT: "Can we do a larger test and get more definitive statistics?" about the STATUS.md stamp A/B). ` +
  `You are preparing ONE scenario pair for a controlled experiment. Work ONLY by reading git (repo E:\\COBOL) and writing files under ${S}\\stamp-eval2\\${b.id}\\ (create it). NEVER write inside any git worktree. ` +
  `Branch: \`${b.branch}\`; its worktree's checkpoint file is E:\\COBOL\\.claude\\worktrees\\${b.id}\\STATUS.md (read it, do not modify it). k = ${b.k}. ` +
  `Steps: (1) HEAD = \`git rev-parse ${b.branch}\`; STALE_AT = \`git rev-parse ${b.branch}~${b.k}\`. Read \`git log --stat -${b.k + 1} ${b.branch}\`. ` +
  `(2) Decide whether the STATUS.md describes HEAD (its DONE/NEXT/GATE cover the work of the last commit). Report status_is_current with evidence. ` +
  `(3) Write ${S}\\stamp-eval2\\${b.id}\\current-unstamped.md = the STATUS.md verbatim; current-stamped.md = the line \`STATUS-AT: <HEAD full sha>\` followed by the same text. ` +
  `(4) Write stale-unstamped.md = the STATUS.md rewritten as it would have read IMMEDIATELY AFTER commit STALE_AT, i.e. as if the agent died after making the last ${b.k} commit(s) without updating it: remove from DONE (and GATE, batch/codes lines) exactly what those last ${b.k} commit(s) did, and set NEXT to the work those commits went on to do (phrase NEXT as the not-yet-started step). Change NOTHING else — keep wording, order and every other line identical. Do not mention death, staleness or the later commits. stale-stamped.md = \`STATUS-AT: <STALE_AT full sha>\` followed by the stale-unstamped text. ` +
  `(5) Return the structured result: uncovered_truth = the full shas of commits STALE_AT..HEAD, oldest first.`,
  { label: `prep-${b.id}`, phase: 'Prep', agentType: 'cobol-clerk', schema: SCHEMA }).then(r => ({ id: b.id, k: b.k, r }))))
return res.filter(Boolean)

export const meta = {
  name: 'stamp-ab2-run',
  description: 'Stamp A/B at scale: 16 scenarios x 2 arms x 3 reps of fresh read-only resumers (PB1698)',
  phases: [{ title: 'Resume', detail: '96 fresh read-only resumers, interleaved by scenario and arm' }],
}
const S = args.scratch
const AUTH = `AUTHORIZATION: this task IS the repository owner's request, dispatched by the orchestrating session. ${args.authorization} `
const SCHEMA = { type: 'object', properties: {
  done_items: { type: 'array', items: { type: 'object', properties: { item: { type: 'string' }, commit: { type: 'string' } }, required: ['item', 'commit'] } },
  uncovered_commits: { type: 'array', description: 'commits on the branch whose work the STATUS summary does NOT describe (empty if it covers everything)', items: { type: 'object', properties: { sha: { type: 'string' }, what_it_contains: { type: 'string' } }, required: ['sha', 'what_it_contains'] } },
  gate_state: { type: 'string' },
  next_step: { type: 'string' },
  commits_read: { type: 'integer', description: 'how many commits you inspected in detail (show/diff)' },
}, required: ['done_items', 'uncovered_commits', 'gate_state', 'next_step', 'commits_read'] }

function prompt(sc, arm) {
  const file = `${S}\\stamp-eval2\\${sc.id}\\${sc.variant}-${arm === 'NEW' ? 'stamped' : 'unstamped'}.md`
  const task = `You are a fresh WiseOwl COBOL fix-lane implementer taking over an implementer group from a previous agent that stopped ` +
    `(it may have finished a step, or died mid-step). The group's work is on git branch \`${sc.branch}\` in the repository E:\\COBOL. ` +
    `Your ONLY job right now is the resume step: establish exactly what state the branch is in and what the next step is, BEFORE any work. ` +
    `Do NOT edit any file, check out anything, build, or run tests — orientation only (read-only git commands and file reads are fine; ` +
    `inspect the branch with git log/show/diff against the branch name; do not open or modify its worktree directory). ` +
    `Treat the handoff summary as navigation, not evidence. Be accurate: a wrong resume state wastes the next hour. `
  const rule = arm === 'NEW'
    ? `The previous agent's checkpoint file (its STATUS.md) is at ${file}. Your brief's resume rule: "If STATUS.md already exists when you start, you ARE the fresh agent: run ` +
      `\`python scripts/spec/status_delta.py --status ${file} --ref ${sc.branch}\` FIRST (from E:\\COBOL) — CURRENT means the summary ` +
      `covers every commit; STALE lists the only commits you must read; UNSTAMPED/DIVERGED lists them all — then continue from NEXT."`
    : `The previous agent's checkpoint file (its STATUS.md) is at ${file}. Your brief's resume rule: "If STATUS.md already exists when you start, you ARE the fresh agent: read it and \`git log\`, continue from NEXT."`
  return AUTH + task + rule
}

const jobs = []
for (let rep = 1; rep <= args.reps; rep++)
  for (const sc of args.scenarios)
    for (const arm of (rep % 2 ? ['OLD', 'NEW'] : ['NEW', 'OLD']))
      jobs.push({ sc, arm, rep })
log(`${jobs.length} resumers over ${args.scenarios.length} scenarios`)
phase('Resume')
// Bounded width (args.concurrency): a rolling pool, so the session meter is not spiked by 16 Opus agents at once.
const res = []
let next = 0
async function worker() {
  while (next < jobs.length) {
    const j = jobs[next++]
    const r = await agent(prompt(j.sc, j.arm), { label: `ab2-${j.sc.id.slice(-6)}-${j.sc.variant}-${j.arm}-${j.rep}`, phase: 'Resume', agentType: 'cobol-adjudicator', model: 'opus', schema: SCHEMA })
    res.push({ id: j.sc.id, variant: j.sc.variant, arm: j.arm, rep: j.rep, result: r })
  }
}
await Promise.all(Array.from({ length: args.concurrency }, () => worker()))
return res

export const meta = {
  name: 'rolling-wave',
  description: 'Rolling fix lane: a queue of implementer groups run N at a time (a freed slot is refilled at once), each DONE branch reviewed on the branch; the wave starts no lander: the loop\'s landing lane lands every branch reviewed CLEAN (kb/Work PB3023, PB3103)',
  whenToUse: 'The standard fix-lane dispatch (workstream SKILL §2, references/landing.md): fill a freed slot the moment it frees; review each DONE branch (kb/Work PB3027); the loop\'s landing lane lands them (land_lane.py runs enqueue.py land, kb/Work PB3023, PB3103).',
  phases: [
    { title: 'Implement', detail: 'cobol-implementer per group, isolated worktrees, rolling pool; a review finisher per round of findings' },
    { title: 'Review', detail: 'one read-only cobol-reviewer per DONE branch (the review skill branch form)' },
  ],
}

// args: { scratch, wave, stop_file, global_stop, concurrency (6), groups: [{letter, lead, notes, codes}],
//         review_fix_rounds (2), reviewer_ceiling_min (90) }
// THE WAVE STARTS NO LANDER (kb/Work PB3023; DESIGN-test-build-ci.md section 3.17.5): it produces DONE branches reviewed
// CLEAN, and the loop's landing lane (land_lane.py, kb/Work PB3103) lands each with `scripts/orchestrator/enqueue.py
// land` as it comes back, beside this wave (a script: main merged in,
// the gate, the DEVLOG stamp, the landing checks, one pull request into GitHub's merge queue). The train, its size and
// its final-train threshold went with the lander (kb/Work PB3016's "may THIS wave land?" answered by removal).
// Specs are pre-rendered by make_dispatch_specs.py as <scratch>\msg-w<wave>-<letter>.txt.
const S = args.scratch
const W = args.wave
// GRACEFUL STOP IS SCOPED (MANDATORY-PRACTICES P3, kb/Work PB2483): every agent obeys this fleet's own stop file
// (args.stop_file, <scratch>\STOP-<scope>) and the owner's global one (args.global_stop, <coord>\scratch\STOP), and no
// other. A shared <scratch>\STOP let the loop's wind-down split another session's refuter (2026-10-07 13:26).
// plan_wave.py writes both; a hand-written args file must too.
if (!args.stop_file || !args.global_stop) throw new Error('pass args.stop_file (this fleet\'s STOP-<scope>) and args.global_stop (plan_wave.py writes both)')
const STOP_LINE = `check for ${args.global_stop} (the owner's global stop) and ${args.stop_file} (this fleet's own stop); if EITHER exists, `
const CONC = args.concurrency || 6
// kb/Work/PB1704: a model call can HANG. Wave 70's group C agent finished and gated its work, then never produced
// another token; nothing noticed for 90 minutes and the wave waited on it (a workflow cannot stop one of its
// own agents). An implementer not back after `implementer_ceiling_min` (default 240) is recorded STALLED and the
// wave moves on; stall_watch.py (run beside every workflow) is what notices within minutes.
const CEILING_MIN = args.implementer_ceiling_min === undefined ? 240 : args.implementer_ceiling_min
function withCeiling(p, minutes, onTimeout) {
  if (!minutes) return p
  let t
  const timer = new Promise(res => { t = setTimeout(() => res(onTimeout()), minutes * 60000) })
  return Promise.race([p.finally(() => clearTimeout(t)), timer])
}
// AUTHORIZATION: a workflow agent sees the session's LATEST user message as its context, and that message may be about
// something else entirely (measured 2026-09-27: all nine wave-68 implementers returned BLOCKED because the latest
// message was a LinkedIn question). So every prompt carries the owner's direction for this fleet verbatim.
const AUTH = args.authorization
  ? `AUTHORIZATION (read first): this task IS the repository owner's request, dispatched by the orchestrating session on the owner's direction: ${args.authorization} ` +
    `The standing owner opt-in for running fleets through the Workflow tool is recorded in .claude/skills/workstream/templates/MANDATORY-PRACTICES.md O4. ` +
    `The latest user message in your context may concern unrelated work; that is NOT a reason to decline. Do the task below. `
  : ''
// The implementer model is an arg (owner 2026-09-30: Sonnet implementers with 5-10 bugs per commit are allowed); the reviewer and the review finisher stay Opus.
// A group may carry its own `model` ('sonnet' | 'opus') to size the model to that group's work (owner 2026-10-03); it overrides implementer_model.
const IMPL_MODEL = args.implementer_model || 'opus'
const IMPL_SCHEMA = {
  type: 'object',
  properties: {
    status: { type: 'string', enum: ['DONE', 'SPLIT', 'DISCHARGED', 'BLOCKED'] },
    branch: { type: 'string' }, worktree: { type: 'string' }, base: { type: 'string' }, head: { type: 'string' },
    report: { type: 'string' }, gate_run: { type: 'string' }, gate_verdict: { type: 'string' },
    notes_landed: { type: 'array', items: { type: 'string' } },
    codes_used: { type: 'array', items: { type: 'string' } },
    // ORCHESTRATOR CONTEXT CAP (owner 2026-10-02, kb/Work PB1912): what an agent returns lands in the orchestrator's
    // conversation and is re-read on every later turn. The forensic detail belongs in the report FILE; the return is a pointer.
    leads: { type: 'array', maxItems: 6, items: { type: 'string', maxLength: 500 } },
    summary: { type: 'string', maxLength: 900 },
  },
  required: ['status', 'branch', 'worktree', 'report', 'gate_verdict', 'summary'],
}
const RETURN_LINE =
  `⛔ YOUR LAST ACTION MUST BE THE StructuredOutput CALL — never end on a report file or a summary message (three agents in waves 65-67 did, and their finished branches were stranded): ` +
  `status, your ACTUAL branch (git branch --show-current), your worktree path, base sha, head sha, report path, your last gate's run directory (TestResults/build-local/<run>, whose verdict.json records its timings) and its verdict line, the notes you landed, the codes you used, and any new leads (text; do NOT allocate PB ids). ` +
  `⛔ KEEP THE RETURN SHORT: summary at most 900 characters, at most 6 leads of at most 500 characters each (each lead: repro path and code site), and every detail goes in your report file, which the orchestrator reads only on demand.`

// THE BRANCH REVIEW (kb/Work PB3027; DESIGN-test-build-ci.md §3.17.2 step 2; MANDATORY-PRACTICES I10). The review of
// finished work used to run inside the lander, between its gates (lander-train-brief step 5b, retired): on 2026-10-10
// the review shards were 23 % of the land units' subagent tokens and sat on the landing's critical path, and every
// finding fixed in the train re-gated every cluster (kb/Work PB3015, its evidence table 6). Now a DONE branch is
// reviewed where it was made: ONE read-only cobol-reviewer (the review skill's branch form) records its decisions in
// <scratch>\review-w<wave>-<letter>.jsonl through review_findings.py, which computes the verdict and stamps it into the
// report's header (`**Review:** CLEAN <reviewer> <head>`). FINDINGS → a REVIEW FINISHER (a fresh cobol-implementer:
// a workflow cannot message an agent that has returned) merges the branch, answers every finding by its id (fixed, or
// no_change_needed with the reason), re-gates and returns; its branch is reviewed again. Only a branch reviewed CLEAN
// at its head is ready to land; at the round cap the FINDINGS stamp stays, and the next wave's planner dispatches a
// finisher for it (plan_wave.py). prune_worktrees.py lists a DONE branch WAITING TO LAND only under a CLEAN stamp at
// its tip, so the landing lane lands no unreviewed branch either.
const REVIEW_FIX_ROUNDS = args.review_fix_rounds === undefined ? 2 : args.review_fix_rounds
const REVIEWER_CEILING_MIN = args.reviewer_ceiling_min === undefined ? 90 : args.reviewer_ceiling_min
const REVIEW_SCHEMA = {
  type: 'object',
  properties: {
    verdict: { type: 'string', enum: ['CLEAN', 'FINDINGS', 'STOPPED'] },
    findings: { type: 'integer' },
    head: { type: 'string' },
    stamp: { type: 'string', maxLength: 400 },
    summary: { type: 'string', maxLength: 600 },
  },
  required: ['verdict', 'findings', 'head', 'stamp'],
}
const reviews = []

// SAME-FILE SUCCESSORS: a group with `after: '<letter>'` runs only once that predecessor has returned. It is told the
// predecessor's branch, head and report, merges that branch, and orients from the predecessor's "For the next
// implementer" section instead of re-surveying the file (owner direction 2026-09-27: fix related work together and
// minimize repetitive context gathering). The predecessor's branch is then HELD from landing and lands through the
// successor's branch, which contains it (so the successor's review, over origin/main...HEAD, reviews it too); if the
// successor produces nothing landable, the predecessor is reviewed and lands alone.
const queue = [...args.groups]
const results = []
const byLetter = {}
const held = {}          // predecessor letter -> {g, r}: its finished, unreviewed result, waiting for its successor
const ready = []          // DONE and reviewed CLEAN: the landing lane lands them
let running = 0
let wake = () => {}

function hasSuccessorQueued(letter) { return queue.some(g => g.after === letter) }

function successorNote(g) {
  if (!g.after) return ''
  const p = byLetter[g.after]
  if (!p || !(p.head && p.head !== p.base)) {
    return `Your predecessor group ${g.after} produced NO branch (status ${p ? p.status : 'never ran'}); start from main as usual. `
  }
  return `⛔ SAME-FILE SUCCESSOR: your predecessor group ${g.after} (${p.notes}) returned ${p.status} on branch ${p.branch} ` +
    `(worktree ${p.worktree}, head ${p.head}, report ${p.report}). FIRST \`git merge ${p.branch}\` into your branch, then read that ` +
    `report's "For the next implementer" section and its STATUS.md as your orientation for the shared file — run ` +
    `\`python tools/claude-skills/skills/agent-fleet/references/status_delta.py ${p.worktree}\` and read ONLY the commits it lists beyond the summary; its notes land through ` +
    `YOUR branch (list them as landed-via-predecessor). `
}

function runGroup(g) {
  return withCeiling(agent(
    AUTH + `You are the wave-${W} fix-lane implementer for group ${g.letter} (${g.notes}). ` + successorNote(g) +
    `Your dispatch spec is the file ${S}\\msg-w${W}-${g.letter.toLowerCase()}.txt — read it WHOLE and follow it exactly; ` +
    `it names your brief, codes, report path, scratch dir, gate and checkpoint protocol. ` +
    `Before EACH new step ${STOP_LINE}checkpoint-commit, write STATUS.md NEXT and your report, and return status SPLIT. ` +
    RETURN_LINE,
    { label: `impl-${g.letter}-${g.lead}`, phase: 'Implement', agentType: 'cobol-implementer', isolation: 'worktree', schema: IMPL_SCHEMA, model: g.model || IMPL_MODEL }
  ).then(r => r ? { ...r, letter: g.letter, lead: g.lead, notes: g.notes, codes: g.codes } : { letter: g.letter, lead: g.lead, notes: g.notes, status: 'NO-RESULT' }),
  CEILING_MIN, () => {
    log(`${g.letter}: no return after ${CEILING_MIN} min; recorded STALLED, the wave moves on (PB1704)`)
    return { letter: g.letter, lead: g.lead, notes: g.notes, codes: g.codes, status: 'STALLED' }
  })
}

function findingsFile(g) { return `${S}\\review-w${W}-${g.letter.toLowerCase()}.jsonl` }

function sameHead(a, b) { return !!a && !!b && (a.startsWith(b) || b.startsWith(a)) }

function reviewBranch(g, r, round) {
  const file = findingsFile(g)
  const label = `w${W}${g.letter.toLowerCase()}-review-r${round}`
  return withCeiling(agent(
    AUTH + `You are the wave-${W} BRANCH REVIEWER for group ${g.letter} (${g.notes}), review round ${round} — READ-ONLY (kb/Work PB3027). ` +
    `Follow "The branch form" of E:\\COBOL\\.claude\\skills\\review\\SKILL.md exactly, with: branch ${r.branch}; worktree ${r.worktree}; head ${r.head}; ` +
    `the implementer's report ${r.report}; the findings file ${file}; your reviewer label ${label}; round ${round}. ` +
    (round > 1
      ? `This is a RE-REVIEW of the review finisher's branch: round ${round - 1}'s findings are in the same file (review_findings.py show ${file} --round ${round - 1}) and the report's "## Review findings" section answers each by its id; raise again only a finding that is neither fixed nor answered with a reason you accept, and any new defect the fix introduced. `
      : '') +
    `Before EACH new step ${STOP_LINE}return verdict STOPPED and stamp nothing. ` +
    `Your LAST action is the StructuredOutput call: the verdict review_findings.py verdict printed (CLEAN or FINDINGS), the number of findings, the head you reviewed, the stamp line review_findings.py stamp wrote, and a summary of at most 600 characters.`,
    { label, phase: 'Review', agentType: 'cobol-reviewer', schema: REVIEW_SCHEMA, model: 'opus' }
  ), REVIEWER_CEILING_MIN, () => {
    log(`${g.letter}: reviewer round ${round} not back after ${REVIEWER_CEILING_MIN} min; the branch stays unreviewed (PB1704)`)
    return null
  })
}

function fixFindings(g, r, v, round) {
  return withCeiling(agent(
    AUTH + `You are the wave-${W} REVIEW FINISHER for group ${g.letter} (${g.notes}), after branch review round ${round} (kb/Work PB3027). ` +
    `Your dispatch spec is the file ${S}\\msg-w${W}-${g.letter.toLowerCase()}.txt — read it WHOLE and follow its brief, codes, gate, checkpoint and stop rules; its notes are already implemented on the reviewed branch. ` +
    `FIRST \`git merge ${r.branch}\` into your branch (the reviewed head ${r.head}; worktree ${r.worktree}), then run \`python tools/claude-skills/skills/agent-fleet/references/status_delta.py ${r.worktree}\` and read ${r.report} as your orientation. ` +
    `The reviewer recorded ${v.findings} finding(s): \`python scripts/orchestrator/review_findings.py show ${findingsFile(g)} --round ${round}\`. Answer EVERY one by its id: fix it at its root, with a regression test or golden and the sibling sweep, or answer no_change_needed with the reason. ` +
    `Re-gate (the spec's gate, blocking on its verdict line). Then REWRITE the report at ${r.report} for YOUR branch: its header names your worktree, branch, base and HEAD, carries no Review line (your branch is reviewed again when you return DONE), and it gains a "## Review findings" section with one line per finding id: fixed (the commit) or no_change_needed (the reason). ` +
    `Before EACH new step ${STOP_LINE}checkpoint-commit, write STATUS.md NEXT and the report, and return status SPLIT. ` +
    RETURN_LINE,
    { label: `fix-${g.letter}-r${round}`, phase: 'Implement', agentType: 'cobol-implementer', isolation: 'worktree', schema: IMPL_SCHEMA, model: g.model || IMPL_MODEL }
  ), CEILING_MIN, () => {
    log(`${g.letter}: review finisher round ${round} not back after ${CEILING_MIN} min; recorded STALLED (PB1704)`)
    return { status: 'STALLED' }
  })
}

// A branch with finished work: DONE, or DISCHARGED with commits. A SPLIT branch is unfinished and is never reviewed or
// landed here: the next wave's planner gives it a finisher (plan_wave.py FINISH_STATUSES, kb/Work PB2575), as the land
// unit lands only DONE branches.
function hasWork(r) {
  return r.status === 'DONE' || (r.status === 'DISCHARGED' && r.head && r.head !== r.base)
}

// The review loop: review the head; on FINDINGS a review finisher answers them and its branch is reviewed again, up to
// REVIEW_FIX_ROUNDS finishers. Returns the newest result (the finisher's branch contains the reviewed one) with
// `review`: CLEAN, FINDINGS <n> (the stamp stays; a later wave's finisher answers it), STOPPED or NO-RESULT.
async function reviewed(g, r) {
  let cur = r
  for (let round = 1; ; round++) {
    let v = null
    try { v = await reviewBranch(g, cur, round) } catch (e) { v = null }
    const verdict = !v ? 'NO-RESULT' : (v.verdict === 'CLEAN' && !sameHead(v.head, cur.head)) ? 'STALE' : v.verdict
    reviews.push({ letter: g.letter, round, branch: cur.branch, head: cur.head, verdict, findings: v ? v.findings : null })
    log(`${g.letter}: review round ${round} of ${cur.branch}: ${verdict}${v && v.verdict === 'FINDINGS' ? ' ' + v.findings : ''}`)
    if (verdict === 'CLEAN') return { ...cur, review: 'CLEAN', review_rounds: round }
    if (verdict !== 'FINDINGS') return { ...cur, review: verdict, review_rounds: round }
    if (round > REVIEW_FIX_ROUNDS) {
      log(`${g.letter}: still FINDINGS after ${REVIEW_FIX_ROUNDS} review finisher(s); the stamp stays and the next wave plans a finisher`)
      return { ...cur, review: `FINDINGS ${v.findings}`, review_rounds: round }
    }
    let f = null
    try { f = await fixFindings(g, cur, v, round) } catch (e) { f = null }
    const next = f && f.head && f.head !== cur.head ? { ...f, letter: g.letter, lead: g.lead, notes: g.notes, codes: g.codes, reviewed_from: cur.branch } : null
    if (!next || f.status !== 'DONE') {
      log(`${g.letter}: review finisher round ${round} returned ${f ? f.status : 'NO-RESULT'}; not landed by this wave`)
      return { ...(next || cur), review: `FINDINGS ${v.findings}`, review_rounds: round, finisher: f ? f.status : 'NO-RESULT' }
    }
    cur = next
  }
}

function landable(r) {
  return hasWork(r) && r.review === 'CLEAN'
}

function wakeAll() { const w = wake; wake = () => {}; w() }

// A held predecessor (or one whose successor never ran) lands alone after all: it is reviewed first, like any branch.
async function landAlone(p) {
  const pr = await reviewed(p.g, p.r)
  Object.assign(p.r, pr)          // its entry in `results` becomes the reviewed (possibly finisher's) result
  if (landable(p.r)) ready.push(p.r)
}

async function onResult(g, r) {
  results.push(r)
  byLetter[r.letter] = r
  wakeAll()                       // a successor parked on this predecessor may start now, while this one is reviewed
  if (hasWork(r) && hasSuccessorQueued(r.letter)) {
    held[r.letter] = { g, r }
    log(`${r.letter}: ${r.status}; HELD — it lands through its same-file successor, whose review covers its commits`)
  } else if (landable(r)) {
    ready.push(r)
  } else if (hasWork(r)) {
    log(`${r.letter}: ${r.status} but review ${r.review}; not landed by this wave (prune_worktrees.py names who acts next)`)
  }
  if (g.after && held[g.after]) {
    // The successor merged its predecessor: land the successor alone if it is landable, else fall back to the predecessor.
    const p = held[g.after]
    delete held[g.after]
    if (!landable(r)) await landAlone(p)
  }
  log(`${r.letter}: ${r.status}; ${ready.length} branch(es) reviewed CLEAN for the enqueue unit, ${queue.length} group(s) queued`)
}

function nextEligible() {
  const i = queue.findIndex(g => !g.after || byLetter[g.after])
  return i < 0 ? null : queue.splice(i, 1)[0]
}

async function worker() {
  while (queue.length > 0) {
    let g = nextEligible()
    if (!g) {
      if (running === 0) { g = queue.shift() }                    // a predecessor never ran: start the successor fresh
      else { await new Promise(res => { const prev = wake; wake = () => { prev(); res() } }); continue }
    }
    // ⛔ An agent that dies on an API error REJECTS rather than returning null. Before this try/finally, that rejection
    // escaped the worker with `running` still counted and no wake-up sent, so a same-file successor parked on `wake`
    // waited forever: wave 68 (2026-09-27) sat "running" for 16 h after its group U agent died. A rejection is now a
    // NO-RESULT like a null return, and the count and the wake-up happen whatever the outcome.
    running++
    let r
    try {
      r = await runGroup(g)
      // the branch review holds this group's slot: reviewing and answering findings are part of finishing it
      if (hasWork(r) && !hasSuccessorQueued(g.letter)) r = await reviewed(g, r)
    } catch (e) {
      r = { letter: g.letter, lead: g.lead, notes: g.notes, status: 'NO-RESULT', error: String(e && e.message || e) }
    } finally {
      running--
    }
    try {
      await onResult(g, r)
    } finally {
      wakeAll()
    }
  }
}

phase('Implement')
await parallel(Array.from({ length: Math.min(CONC, queue.length) }, () => () => worker()))
for (const k of Object.keys(held)) { const p = held[k]; delete held[k]; await landAlone(p) }   // a successor that never ran

log(`${ready.length} branch(es) ready to land (${ready.map(r => r.letter).join(' ') || 'none'}): the landing lane lands them`)
return { results, ready: ready.map(r => ({ letter: r.letter, branch: r.branch, report: r.report })), reviews }

export const meta = {
  name: 'rolling-wave',
  description: 'Rolling fix lane: a queue of implementer groups run N at a time (a freed slot is refilled at once), and a lander train starts whenever enough branches are ready',
  whenToUse: 'The standard fix-lane dispatch (workstream SKILL §2-§3): fill a freed slot the moment it frees; land in trains of 4-6 as branches finish.',
  phases: [
    { title: 'Implement', detail: 'cobol-implementer per group, isolated worktrees, rolling pool' },
    { title: 'Land', detail: 'cobol-lander trains, serialized, each over 4-6 ready branches' },
  ],
}

// args: { scratch, wave, concurrency (6), train_size (5), min_final_train (3), devlog_n, previous_train,
//         lead_id_blocks: ["PB1665-PB1669", ...] (one block per train), groups: [{letter, lead, notes, codes}] }
// Specs are pre-rendered by make_dispatch_specs.py as <scratch>\msg-w<wave>-<letter>.txt.
const S = args.scratch
const W = args.wave
const CONC = args.concurrency || 6
const TRAIN = args.train_size || 5
const MIN_FINAL = args.min_final_train || 3
// kb/Work/PB1704: a model call can HANG. Wave 70's group C agent finished and gated its work, then never produced
// another token; nothing noticed for 90 minutes and the final train waited on it (a workflow cannot stop one of its
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
// The implementer model is an arg (owner 2026-09-30: Sonnet implementers with 5-10 bugs per commit are allowed); the lander stays Opus.
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

// SAME-FILE SUCCESSORS: a group with `after: '<letter>'` runs only once that predecessor has returned. It is told the
// predecessor's branch, head and report, merges that branch, and orients from the predecessor's "For the next
// implementer" section instead of re-surveying the file (owner direction 2026-09-27: fix related work together and
// minimize repetitive context gathering). The predecessor's branch is then HELD from the train and lands through the
// successor's branch, which contains it; if the successor produces nothing landable, the predecessor lands alone.
const queue = [...args.groups]
const results = []
const byLetter = {}
const held = {}          // predecessor letter -> its landable result, waiting for its successor
const buffer = []
const trains = []
let trainNo = 0
let landChain = Promise.resolve()
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
    `Before EACH new step check for ${S}\\STOP; if it exists, checkpoint-commit, write STATUS.md NEXT and your report, and return status SPLIT. ` +
    `⛔ YOUR LAST ACTION MUST BE THE StructuredOutput CALL — never end on a report file or a summary message (three agents in waves 65-67 did, and their finished branches were stranded): ` +
    `status, your ACTUAL branch (git branch --show-current), your worktree path, base sha, head sha, report path, your last gate's run directory (TestResults/build-local/<run>, whose verdict.json records its timings) and its verdict line, the notes you landed, the codes you used, and any new leads (text; do NOT allocate PB ids). ` +
    `⛔ KEEP THE RETURN SHORT: summary at most 900 characters, at most 6 leads of at most 500 characters each (each lead: repro path and code site), and every detail goes in your report file, which the orchestrator reads only on demand.`,
    { label: `impl-${g.letter}-${g.lead}`, phase: 'Implement', agentType: 'cobol-implementer', isolation: 'worktree', schema: IMPL_SCHEMA, model: IMPL_MODEL }
  ).then(r => r ? { ...r, letter: g.letter, lead: g.lead, notes: g.notes, codes: g.codes } : { letter: g.letter, lead: g.lead, notes: g.notes, status: 'NO-RESULT' }),
  CEILING_MIN, () => {
    log(`${g.letter}: no return after ${CEILING_MIN} min; recorded STALLED, the wave moves on (PB1704)`)
    return { letter: g.letter, lead: g.lead, notes: g.notes, codes: g.codes, status: 'STALLED' }
  })
}

function landable(r) {
  return r.status === 'DONE' || ((r.status === 'DISCHARGED' || r.status === 'SPLIT') && r.head && r.head !== r.base)
}

function land(batch) {
  const n = trainNo++
  const label = n === 0 ? `${W}` : `${W}${String.fromCharCode(97 + n)}`
  const leadIds = (args.lead_id_blocks || [])[n] || 'none (ask the orchestrator)'
  const manifest = JSON.stringify(batch.map(r => ({
    cluster: r.letter, lead: r.lead, notes: r.notes, status: r.status, report: r.report, worktree: r.worktree, branch: r.branch,
    base: r.base, head: r.head, gate_run: r.gate_run, codes: r.codes, codes_used: r.codes_used || [], leads: r.leads || [],
  })), null, 1)
  const clusters = batch.map(r => `${r.letter} (${r.notes}${r.status === 'SPLIT' ? ', SPLIT: land only what its report says is complete' : ''})`).join(', ')
  const prev = n === 0 ? args.previous_train : `train ${W}${n === 1 ? '' : String.fromCharCode(96 + n)} of this same wave`
  log(`train ${label}: landing ${batch.map(r => r.letter).join(' ')}`)
  return agent(
    AUTH + `You are the train-${label} LANDER. Read E:\\COBOL\\.claude\\skills\\workstream\\templates\\lander-train-brief.md WHOLE and follow it, with these substitutions: ` +
    `{CLUSTERS} = ${clusters}; {DEVLOG_N} = ${args.devlog_n} (ALWAYS re-read the top entry of DEVLOG.md first and use top+1 — an earlier train of this wave may have landed); ` +
    `{TRAIN_MANIFEST} = ${S}\\train${label}-manifest.json — FIRST write that file with exactly this JSON:\n${manifest}\n` +
    `PIPELINED: ${prev} may have just landed; fetch origin and rebase onto it before gating, and before push-main confirm origin/main has not moved again (rebase and re-gate per the brief if it has). ` +
    `New leads found in the reports get kb/Work notes with ids from ${leadIds} (orchestrator-allocated; use in order, return the unused). ` +
    `Before EACH new step check for ${S}\\STOP; if it exists, checkpoint-commit in your worktree, write STATUS.md NEXT, and return. ` +
    `Land ONLY through bash scripts/push-main.sh. Run the CI audits locally before it (audit_code_citations, audit_doc_citations, audit_evidence_supersession, audit_witness_loss, drift_rules --check, work.py check). ` +
    `Your final text, at most 25 lines (the orchestrator re-reads it every turn; the detail is in the DEVLOG entry and the PB notes you wrote): the landed sha (or why not), the DEVLOG entry number, GAP before -> after, clusters landed/dropped with a one-line reason each, lead ids used, and the unused codes.`,
    { label: `lander-train${label}`, phase: 'Land', agentType: 'cobol-lander', isolation: 'worktree', model: 'opus' }
  ).then(t => { trains.push({ train: label, clusters: batch.map(r => r.letter), result: t }); return t })
}

function onResult(g, r) {
  results.push(r)
  byLetter[r.letter] = r
  if (landable(r)) {
    if (hasSuccessorQueued(r.letter)) {
      held[r.letter] = r
      log(`${r.letter}: ${r.status}; HELD — it lands through its same-file successor`)
    } else {
      buffer.push(r)
    }
  }
  if (g.after && held[g.after]) {
    // The successor merged its predecessor: land the successor alone if it is landable, else fall back to the predecessor.
    if (!landable(r)) buffer.push(held[g.after])
    delete held[g.after]
  }
  log(`${r.letter}: ${r.status}; ${buffer.length} branch(es) waiting for a train, ${queue.length} group(s) queued`)
  if (buffer.length >= TRAIN) {
    const batch = buffer.splice(0, buffer.length)
    landChain = landChain.then(() => land(batch))
  }
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
    } catch (e) {
      r = { letter: g.letter, lead: g.lead, notes: g.notes, status: 'NO-RESULT', error: String(e && e.message || e) }
    } finally {
      running--
    }
    try {
      onResult(g, r)
    } finally {
      const w = wake; wake = () => {}; w()
    }
  }
}

phase('Implement')
await parallel(Array.from({ length: Math.min(CONC, queue.length) }, () => () => worker()))
for (const k of Object.keys(held)) { buffer.push(held[k]); delete held[k] }   // a successor that never ran

if (buffer.length >= MIN_FINAL) {
  const batch = buffer.splice(0, buffer.length)
  landChain = landChain.then(() => land(batch))
} else if (buffer.length > 0) {
  log(`holding ${buffer.map(r => r.letter).join(' ')} for the next wave's first train (${buffer.length} < ${MIN_FINAL})`)
}
await landChain
return { results, trains, held: buffer }

// test_wf_r2_review.mjs — a dry run of the R2 review workflow (.claude/skills/workstream/templates/wf_r2_review.js) with
// stubbed agents over a SIMULATED DISK (kb/Work PB2560): every control-flow arm the R2 adversarial review asked for, and
// the w1034 claim refuter's resume scenarios (C2: work lost and repeated across agents on a relaunch, and findings
// orphaned by an agent that returns nothing), are driven and observed.
// The stub agents obey the checkpoint rule exactly as the prompt states it: they ask the pair's state (the same
// semantics as `r2_collect.py --status`), skip what the PAIR has decided, append one line per decision, and return the
// pair's state — unless the scenario makes them die (a turn cap) or return nothing (an API error).
//   node scripts/arch/test_wf_r2_review.mjs        -> "=== WF R2 REVIEW DRY RUN: PASS ===" (exit 0) or FAIL (exit 1)
//   WF_R2_PATH=<another wf_r2_review.js> node ...  -> the same arms against another version (the pre-fix one fails C2)
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import path from 'node:path'

const here = path.dirname(fileURLToPath(import.meta.url))
const wfPath = process.env.WF_R2_PATH || path.join(here, '..', '..', '.claude', 'skills', 'workstream', 'templates', 'wf_r2_review.js')
const body = readFileSync(wfPath, 'utf8').replace(/^export const meta = /m, 'const meta = ')
const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor
const wf = new AsyncFunction('args', 'agent', 'parallel', 'pipeline', 'log', 'phase', body)

let ok = true
const arm = (name, cond) => { console.log((cond ? 'PASS  ' : 'FAIL  ') + name); ok = ok && !!cond }
const LENSES = ['site', 'rule', 'scenario']
const files = n => Array.from({ length: n }, (_, i) => `src/F${i}.cs`)
const shard = (id, n, lines) => ({ id, name: id, lines, input: `I/in-${id}.json`, files: files(n), fileLines: files(n).map(() => Math.round(lines / n)) })

// ── the simulated disk, and the pair state r2_collect.py computes from it (--status, --launch) ──
function makeWorld(plan = {}) {
  const disk = new Map()
  const ap = (p, r) => { if (!disk.has(p)) disk.set(p, []); disk.get(p).push(r) }
  const pairState = pair => {
    const s = { read: new Set(), findings: new Set(), finders: 0, done: 0, null_checked: false, decided: { site: [], rule: [], scenario: [] } }
    for (const [p, recs] of disk) {
      let m = p.match(/^O\\(review|null)-(.+?)(?:--f(\d+))?\.jsonl$/)
      if (m && m[2] === pair) {
        if (m[3]) s.finders = Math.max(s.finders, +m[3])
        for (const r of recs) {
          if (r.type === 'read') s.read.add(r.file)
          if (r.type === 'finding') s.findings.add(r.id)
          if (r.type === 'done') s.done++
          if (r.type === 'null-check') s.null_checked = true
        }
      }
      m = p.match(/^O\\refute-(.+?)--c\d+--(site|rule|scenario)\.jsonl$/)
      if (m && m[1] === pair) for (const r of recs) if (!s.decided[m[2]].includes(r.finding)) s.decided[m[2]].push(r.finding)
    }
    return { ...s, read: [...s.read].sort(), findings: [...s.findings].sort() }
  }
  const pairsOnDisk = () => new Set([...disk.keys()].map(p => (p.match(/^O\\(?:review|null|refute)-(.+?)(?:--f\d+|--c\d+--\w+)?\.jsonl$/) || [])[1]).filter(Boolean))
  const launchState = () => Object.fromEntries([...pairsOnDisk()].map(p => [p, pairState(p)]))
  const calls = []
  let live = 0, peak = 0
  const agent = async (prompt, opts) => {
    live++; peak = Math.max(peak, live)
    await new Promise(r => setTimeout(r, 1))
    calls.push({ label: opts.label, agentType: opts.agentType, prompt })
    try { return act(prompt, opts.label) } finally { live-- }
  }
  function act(prompt, label) {
    const ck = (prompt.match(/YOUR CHECKPOINT FILE: (\S+?)(?:;|\s)/) || [])[1]
    const [kind, slug] = label.split(':')
    const pair = slug
    const ret = extra => plan.returnsNothing && plan.returnsNothing(label) ? null
      : { ...(() => { const s = pairState(pair); return { read: s.read, findings: s.findings, files_read: s.read, finding_ids: s.findings } })(), stopped: false, summary: '', what_i_tried: '', ...extra }
    if (kind === 'find' || kind === 'finish') {
      if (pair === 'global--duplication') { ap(ck, { type: 'finding', id: 'global--duplication--f1#1' }); ap(ck, { type: 'done' }); return ret() }
      const m = prompt.match(/YOUR FILES \((\d+)\)[^\n]*:\n([\s\S]*?)\nRead each WHOLE/)
      const list = m && +m[1] ? m[2].split('\n') : []
      const prefix = (prompt.match(/your finding id prefix: (\S+?)\./) || [])[1]
      let n = 0, budget = plan.budget ? plan.budget(label) : 99
      for (const f of list) {
        if (pairState(pair).read.includes(f)) continue          // another finder of the pair read it
        if (budget-- <= 0) return plan.capReturnsNothing ? null : ret()   // the turn cap: no `done`
        ap(ck, { type: 'read', file: f })
        if (plan.findingAt && plan.findingAt(pair, f)) ap(ck, { type: 'finding', id: `${prefix}#${++n}` })
      }
      ap(ck, { type: 'done' })
      return ret()
    }
    if (kind === 'null') { ap(ck, { type: 'null-check', missed: [] }); return ret() }
    if (kind === 'skeptic') {
      const lens = label.split(':').pop()
      const ids = prompt.match(/FINDINGS: ([^\n]*)/)[1].split(', ')
      let budget = plan.skepticBudget ? plan.skepticBudget(label) : 99
      for (const id of ids) {
        if (pairState(pair).decided[lens].includes(id)) continue
        if (budget-- <= 0) return { verdicts: [], stopped: true }   // the stop file appeared: graceful stop
        ap(ck, { type: 'verdict', finding: id, lens, refuted: false })
      }
      return { verdicts: [], stopped: false }
    }
    throw new Error('unexpected agent ' + label)
  }
  const parallel = async thunks => Promise.all(thunks.map(t => t().catch(e => { console.log('  agent error: ' + e.message); return null })))
  const logs = []
  const launch = async args => {
    calls.length = 0
    return wf({ ...args, stateOf: 'O', state: launchState() }, agent, parallel, null, m => logs.push(m), () => {})
  }
  // the batch's verdict as r2_collect.py reaches it: per pair, files read how often, findings decided twice, unverified
  const audit = pair => {
    const reads = {}, verdicts = {}
    for (const [p, recs] of disk) {
      if (p.startsWith(`O\\review-${pair}--f`) || p === `O\\null-${pair}.jsonl`) for (const r of recs) if (r.type === 'read') reads[r.file] = (reads[r.file] || 0) + 1
      if (p.startsWith(`O\\refute-${pair}--c`)) for (const r of recs) { const k = r.finding + '/' + r.lens; verdicts[k] = (verdicts[k] || 0) + 1 }
    }
    const s = pairState(pair)
    return { s, readTwice: Object.entries(reads).filter(([, c]) => c > 1).map(([f]) => f),
      decidedTwice: Object.entries(verdicts).filter(([, c]) => c > 1).map(([k]) => k),
      unverified: s.findings.filter(id => LENSES.filter(l => verdicts[id + '/' + l]).length < 2) }
  }
  return { disk, calls, launch, audit, peak: () => peak, logs }
}

const base = {
  batch: 't', pinnedTree: 'P', pinCommit: 'c'.repeat(40), outDir: 'O', inputsDir: 'I', stopFile: 'S-r2', globalStopFile: 'S',
  width: 3, dimensions: ['architecture'], duplicationPass: false,
  shards: [shard('small', 3, 1000), shard('big', 6, 5000), shard('empty', 2, 200)],
}
const finds = { small: ['src/F0.cs', 'src/F2.cs'], big: ['src/F1.cs', 'src/F4.cs', 'src/F5.cs', 'src/F0.cs', 'src/F2.cs', 'src/F3.cs'], empty: [] }
const findingAt = (pair, f) => (finds[pair.split('--')[0]] || []).includes(f)

{ // ── the first launch: every arm of the plan ──
  const W = makeWorld({ findingAt, budget: l => l === 'find:big--architecture:f1' ? 2 : l === 'find:big--architecture:f2' ? 2 : 99 })
  const result = await W.launch(base)
  const L = W.calls.map(c => c.label)
  arm('a shard of 4,000 lines or fewer gets one finder', L.filter(l => l.startsWith('find:small')).length === 1)
  arm('a shard over 4,000 lines gets two finders from opposite ends',
    L.includes('find:big--architecture:f1') && L.includes('find:big--architecture:f2') &&
    W.calls.find(c => c.label === 'find:big--architecture:f2').prompt.includes('far end'))
  arm('the unread remainder gets exactly one finisher, for exactly those files',
    L.filter(l => l.startsWith('finish:big')).length === 1 &&
    W.calls.find(c => c.label.startsWith('finish:big')).prompt.includes('YOUR FILES (2)') && W.audit('big--architecture').s.read.length === 6)
  arm('a pair that found nothing is examined, and only it', L.filter(l => l.startsWith('null:')).join() === 'null:empty--architecture')
  arm('skeptics: chunks of four, three lenses each (2 findings -> 3; 6 findings -> 6)',
    L.filter(l => l.startsWith('skeptic:small')).length === 3 && L.filter(l => l.startsWith('skeptic:big')).length === 6)
  arm('no skeptic for a null that stands', !L.some(l => l.startsWith('skeptic:empty')))
  arm('every agent runs as the cobol-reviewer role', W.calls.every(c => c.agentType === 'cobol-reviewer'))
  arm('never more agents at once than the width', W.peak() <= 3)
  arm('every prompt names both stop files, its own checkpoint file and the one checkpoint tool',
    W.calls.every(c => c.prompt.includes('S-r2') && c.prompt.includes(' and S') && /CHECKPOINT FILE: O\\(review|null|refute)-/.test(c.prompt) &&
      c.prompt.includes('r2_collect.py --status O') && c.prompt.includes('r2_collect.py --append')))
  arm('two finders meeting in the middle read each file once', W.audit('big--architecture').readTwice.length === 0)
  arm('the return reports read and unread per pair',
    result.pairs.find(p => p.pair === 'big--architecture').unread === 0 && result.pairs.length === 3)
}
{ // ── C2 (the refuter's S1, the brief's literal case): one finder half-written, then a relaunch ──
  const W = makeWorld({ findingAt })
  W.disk.set('O\\review-small--architecture--f1.jsonl', [{ type: 'read', file: 'src/F0.cs' }, { type: 'finding', id: 'small--architecture--f1#1' }])
  await W.launch({ ...base, shards: [base.shards[0]] })
  const a = W.audit('small--architecture')
  arm('resume: a relaunch reads only what no finder read, under a NEW finder number',
    W.calls.some(c => c.label === 'find:small--architecture:f2' && c.prompt.includes('YOUR FILES (2)')) && a.readTwice.length === 0)
  arm('resume: the finding on disk before the relaunch is verified with the new one', a.s.findings.length === 2 && a.unverified.length === 0)
}
{ // ── C2 (the refuter's S2): finders cap out, a finisher reads the rest, a stop lands in the skeptics, relaunch twice ──
  const big = shard('big', 8, 5000)
  let run = 1
  const W = makeWorld({
    findingAt: (pair, f) => ['src/F0.cs', 'src/F3.cs', 'src/F4.cs', 'src/F7.cs', 'src/F2.cs', 'src/F5.cs'].includes(f),
    budget: l => run === 1 && l.startsWith('find:') ? 3 : 99,
    skepticBudget: l => run === 1 ? (l.includes(':c1:site') ? 3 : 0) : 99,
  })
  await W.launch({ ...base, shards: [big] })
  const a1 = W.audit('big--architecture')
  run = 2
  await W.launch({ ...base, shards: [big] })
  const L2 = W.calls.map(c => c.label)
  const a2 = W.audit('big--architecture')
  arm('resume after a stop: no finder is relaunched on a pair whose every file is read',
    a1.s.read.length === 8 && !L2.some(l => l.startsWith('find:') || l.startsWith('finish:')))
  arm('resume after a stop: every file was read exactly once across all launches', a2.readTwice.length === 0)
  arm('resume after a stop: no finding is decided twice under a lens', a2.decidedTwice.length === 0)
  arm('resume after a stop: every finding — the finisher\'s included — is verified', a2.s.findings.length === 6 && a2.unverified.length === 0)
  run = 3
  await W.launch({ ...base, shards: [big] })
  arm('a decided pair starts no agent on a relaunch', W.calls.length === 0)
}
{ // ── C2 addendum (the refuter's sim3): a finder writes its findings, then returns NOTHING, in one launch, no stop ──
  const W = makeWorld({ findingAt: () => true, returnsNothing: l => l === 'find:small--architecture:f1' })
  await W.launch({ ...base, shards: [base.shards[0]] })
  const a = W.audit('small--architecture')
  arm('a lone finder that returned nothing loses no finding: the finisher skips what is on disk and every finding is verified',
    W.calls.some(c => c.label.startsWith('finish:small')) && a.readTwice.length === 0 && a.s.findings.length === 3 && a.unverified.length === 0)
  // two finders: the silent one read everything, the other returned the pair's state with every file read — nothing is
  // unread, yet the silent finder's findings are unseen, so a finisher with an EMPTY list recovers them
  const W2 = makeWorld({ findingAt: () => true, returnsNothing: l => l === 'find:big--architecture:f1' })
  await W2.launch({ ...base, shards: [base.shards[1]] })
  const b = W2.audit('big--architecture')
  arm('a silent finder whose files another finder saw read still has its findings verified (a finisher with an empty list)',
    W2.calls.some(c => c.label.startsWith('finish:big') && c.prompt.includes('YOUR FILES (0)')) && b.s.findings.length === 6 && b.unverified.length === 0)
}
{
  const W = makeWorld({ findingAt })
  await W.launch({ ...base, duplicationPass: true, shards: [base.shards[0]] })
  const L = W.calls.map(c => c.label)
  arm('the whole-codebase clone pass is one finder plus its skeptics',
    L.filter(l => l === 'find:global--duplication').length === 1 && L.filter(l => l.startsWith('skeptic:global')).length === 3)
  await W.launch({ ...base, duplicationPass: true, shards: [base.shards[0]] })
  arm('a finished clone pass starts no agent on a relaunch', W.calls.length === 0)
}
{ // the first agent reports a stop: no other agent starts
  const calls = []
  const agent = async (prompt, opts) => { calls.push(opts.label); return { read: [], findings: [], stopped: true, summary: 'STOPPED', files_read: [], finding_ids: [] } }
  const r = await wf({ ...base, width: 1, stateOf: 'O', state: {} }, agent, async t => Promise.all(t.map(x => x())), null, () => {}, () => {})
  arm('after an agent reports a stop, no new agent starts', calls.length === 1 && r.stopped === true)
}
for (const [name, args, re] of [
  ['args without the fleet stop file are refused', { ...base, stopFile: undefined, stateOf: 'O', state: {} }, /stopFile/],
  ['args without the on-disk launch state are refused', { ...base }, /launch state/],
  ['a launch state of another batch directory is refused', { ...base, stateOf: 'X', state: {} }, /launch state/],
  ['a shard without per-file lines is refused', { ...base, stateOf: 'O', state: {}, shards: [{ ...base.shards[0], fileLines: undefined }] }, /fileLines/],
]) {
  let threw = false
  try { await wf(args, async () => null, async t => Promise.all(t.map(x => x())), null, () => {}, () => {}) } catch (e) { threw = re.test(e.message) }
  arm(name, threw)
}
console.log(`=== WF R2 REVIEW DRY RUN: ${ok ? 'PASS' : 'FAIL'} ===`)
process.exit(ok ? 0 : 1)

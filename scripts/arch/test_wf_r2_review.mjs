// test_wf_r2_review.mjs — a dry run of the R2 review workflow (.claude/skills/workstream/templates/wf_r2_review.js) with
// stubbed agents (kb/Work PB2560): every control-flow arm the R2 adversarial review asked for is driven and observed.
//   node scripts/arch/test_wf_r2_review.mjs        -> "=== WF R2 REVIEW DRY RUN: PASS ===" (exit 0) or FAIL (exit 1)
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import path from 'node:path'

const here = path.dirname(fileURLToPath(import.meta.url))
const src = readFileSync(path.join(here, '..', '..', '.claude', 'skills', 'workstream', 'templates', 'wf_r2_review.js'), 'utf8')
const body = src.replace(/^export const meta = /m, 'const meta = ')
const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor
const wf = new AsyncFunction('args', 'agent', 'parallel', 'pipeline', 'log', 'phase', body)

let ok = true
const arm = (name, cond) => { console.log((cond ? 'PASS  ' : 'FAIL  ') + name); ok = ok && !!cond }

async function runWith(args, behave) {
  const calls = []
  let live = 0, peak = 0
  const agent = async (prompt, opts) => {
    live++; peak = Math.max(peak, live)
    await new Promise(r => setTimeout(r, 2))
    calls.push({ label: opts.label, agentType: opts.agentType, prompt })
    const r = behave(opts.label, prompt)
    live--
    return r
  }
  const parallel = async thunks => Promise.all(thunks.map(t => t().catch(() => null)))
  const logs = []
  const result = await wf(args, agent, parallel, null, m => logs.push(m), () => {})
  return { calls, peak, logs, result }
}

const files = n => Array.from({ length: n }, (_, i) => `src/F${i}.cs`)
const base = {
  batch: 't', pinnedTree: 'P', pinCommit: 'c'.repeat(40), outDir: 'O', inputsDir: 'I', stopFile: 'S-r2', globalStopFile: 'S',
  width: 3, dimensions: ['architecture'], duplicationPass: false,
  shards: [
    { id: 'small', name: 'small', lines: 1000, input: 'I/in-small.json', files: files(3) },
    { id: 'big', name: 'big', lines: 5000, input: 'I/in-big.json', files: files(6) },
    { id: 'empty', name: 'empty', lines: 200, input: 'I/in-empty.json', files: files(2) },
  ],
}

const behave = (label, prompt) => {
  if (label.startsWith('find:small')) return { files_read: files(3), finding_ids: ['a#1', 'a#2'], stopped: false, summary: '' }
  if (label === 'find:big--architecture:f1') return { files_read: files(3), finding_ids: ['b#1', 'b#2', 'b#3'], stopped: false, summary: '' }
  if (label === 'find:big--architecture:f2') return { files_read: ['src/F5.cs', 'src/F4.cs'], finding_ids: ['c#1', 'c#2', 'c#3'], stopped: false, summary: '' }
  if (label.startsWith('finish:big')) return { files_read: prompt.includes('src/F3.cs') ? ['src/F3.cs'] : [], finding_ids: [], stopped: false, summary: '' }
  if (label.startsWith('find:empty')) return { files_read: files(2), finding_ids: [], stopped: false, summary: '' }
  if (label.startsWith('null:')) return { finding_ids: [], stopped: false, what_i_tried: 'x' }
  if (label.startsWith('find:global')) return { files_read: ['R0-1'], finding_ids: ['g#1'], stopped: false, summary: '' }
  if (label.startsWith('skeptic:')) return { verdicts: [], stopped: false }
  throw new Error('unexpected agent ' + label)
}

{
  const { calls, peak, result } = await runWith(base, behave)
  const L = calls.map(c => c.label)
  arm('a shard of 4,000 lines or fewer gets one finder', L.filter(l => l.startsWith('find:small')).length === 1)
  arm('a shard over 4,000 lines gets two finders from opposite ends',
    L.includes('find:big--architecture:f1') && L.includes('find:big--architecture:f2') &&
    calls.find(c => c.label === 'find:big--architecture:f2').prompt.includes("far end"))
  arm('the unread remainder gets exactly one finisher, for exactly those files',
    L.filter(l => l.startsWith('finish:big')).length === 1 &&
    calls.find(c => c.label.startsWith('finish:big')).prompt.includes('YOUR FILES (1)'))
  arm('a pair that found nothing is examined, and only it', L.filter(l => l.startsWith('null:')).join() === 'null:empty--architecture')
  arm('skeptics: chunks of four, three lenses each (2 findings -> 3; 6 findings -> 6)',
    L.filter(l => l.startsWith('skeptic:small')).length === 3 && L.filter(l => l.startsWith('skeptic:big')).length === 6)
  arm('no skeptic for a null that stands', !L.some(l => l.startsWith('skeptic:empty')))
  arm('every agent runs as the cobol-reviewer role', calls.every(c => c.agentType === 'cobol-reviewer'))
  arm('never more agents at once than the width', peak <= 3)
  arm('every prompt names both stop files and its own checkpoint file',
    calls.every(c => c.prompt.includes('S-r2') && c.prompt.includes(' and S') && /CHECKPOINT FILE: O\\(review|null|refute)-/.test(c.prompt)))
  arm('the return reports read and unread per pair',
    result.pairs.find(p => p.pair === 'big--architecture').unread === 0 && result.pairs.length === 3)
}
{
  const { calls } = await runWith({ ...base, duplicationPass: true, shards: [base.shards[0]] }, behave)
  const L = calls.map(c => c.label)
  arm('the whole-codebase clone pass is one finder plus its skeptics',
    L.filter(l => l === 'find:global--duplication').length === 1 && L.filter(l => l.startsWith('skeptic:global')).length === 3)
}
{
  const stopAt = (label, prompt) => label.startsWith('find:small') ? { files_read: ['src/F0.cs'], finding_ids: ['a#1'], stopped: true, summary: 'STOPPED' } : behave(label, prompt)
  const { calls, result } = await runWith({ ...base, width: 1 }, stopAt)
  arm('after an agent reports a stop, no new agent starts', calls.length === 1 && result.stopped === true)
}
{
  let threw = false
  try { await runWith({ ...base, stopFile: undefined }, behave) } catch (e) { threw = /stopFile/.test(e.message) }
  arm('args without the fleet stop file are refused', threw)
}
console.log(`=== WF R2 REVIEW DRY RUN: ${ok ? 'PASS' : 'FAIL'} ===`)
process.exit(ok ? 0 : 1)

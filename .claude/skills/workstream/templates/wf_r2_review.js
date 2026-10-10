export const meta = {
  name: 'r2-review-fleet',
  description: 'R2 architecture review (DESIGN-architecture-review §3 R2): per (shard x dimension) finders over computed file sets, a completeness pass to the unread remainder, an examiner for every null result, three lens skeptics per finding; every decision a JSON line on disk, the launch state rebuilt from disk, decided by r2_collect.py',
  whenToUse: 'Never directly: `r2_collect.py --out <batch dir> --launch` sizes the batch and writes <batch dir>\\launch.js, this script with its plan from disk (first launch and every relaunch); launch that file with no args; collect with r2_collect.py; file with file_census_notes.py --r2',
  phases: [
    { title: 'Review', detail: 'finders read the unread remainder of their shard WHOLE (two, from opposite ends, over 4,000 lines); a finisher reads what is still unread' },
    { title: 'Examine nulls', detail: 'a pair that found nothing is examined by an agent trying to find what the finders missed' },
    { title: 'Verify', detail: 'every finding not yet decided under a lens, in chunks of four, by that lens\'s skeptic (site, rule, scenario); two must fail to refute' },
  ],
}

// kb/Work PB2558-PB2561; the R2 adversarial review (B1-B7, N1-N7) and the w1034 claim refuter (C2, C3). Inputs come from
// scripts/arch/r2_inputs.py (the shard table, the mechanical checks run ONCE, one input file per shard); decisions go to
// disk as JSON lines through `r2_collect.py --append`, and scripts/arch/r2_collect.py — never this script's return —
// decides what the batch found.
// ⛔ RESUME IS FROM DISK, PER PAIR (the w1034 refuter's C2), AND THE LAUNCH IS A FILE (kb/Work PB2707). A Workflow script
// has no filesystem, so every launch — the first and each relaunch — is `<batch dir>\launch.js`, which
// `r2_collect.py --out <batch dir> --launch` writes: this script with the plan line below replaced by the PLAN it built
// from disk, after sizing it against the quota left (r2_cost.py). The operator's whole call is
// Workflow({scriptPath: '<batch dir>\\launch.js'}) with no args, whatever the batch's progress: batch 2's relaunch needed
// a 95,644-byte state transcribed by hand into six calls. The plan holds one entry per pair with WORK LEFT (the files no
// finder read, with their lines; per lens the findings no skeptic decided); a decided pair is not in it. This script
// plans only from it, never from what an earlier launch's agents returned, so a relaunch skips decided work, runs the
// undecided, decides nothing twice and orphans no finding a finder wrote before it died. Within a launch every agent
// asks `--status` for its PAIR's decisions, not only its own file's.
const A = args   // the plan line: `r2_collect.py --launch` replaces it in launch.js; the template run directly has no plan
if (!A || typeof A !== 'object' || !A.planOf || !Array.isArray(A.pairs) || !A.shards || typeof A.shards !== 'object')
  throw new Error('no launch plan: run `python scripts/arch/r2_collect.py --out <batch dir> --launch` and launch the <batch dir>\\launch.js it writes, with no args (a launch never plans from agent returns, and nobody transcribes a plan: kb/Work PB2707)')
const PIN = A.pinnedTree, PIN_COMMIT = A.pinCommit, OUT = A.outDir, INPUTS = A.inputsDir
const SHARDS = A.shards
const WIDTH = A.width || 8
const STOPS = [A.stopFile, A.globalStopFile].filter(Boolean)
if (!PIN || !PIN_COMMIT || !OUT || !INPUTS || A.planOf !== OUT || !A.stopFile || !A.globalStopFile)
  throw new Error('the plan lacks pinnedTree, pinCommit, outDir (= planOf), inputsDir, args.stopFile or args.globalStopFile: rewrite it with r2_collect.py --launch')
for (const e of A.pairs)
  if (e.slug !== 'global--duplication' && (!SHARDS[e.shard] || !Array.isArray(e.unread) || !e.unread.every(u => Array.isArray(u) && u.length === 2)))
    throw new Error('pair ' + e.slug + ' has no shard or no [file, lines] unread list: rewrite the plan with r2_collect.py --launch')
const COLLECT = 'python scripts/arch/r2_collect.py'
const TWO_FINDERS_OVER = 4000   // lines: a remainder this big gets a second finder starting from its other end (base review Scale: audit)
const CHUNK = 4                 // findings per skeptic transcript (one 80-turn refuter deciding 15 capped out at #11; B6)
const LENSES = {
  site: 'SITE: is every site real and current in the pin (read it), are the files and members exactly the ones a wave would edit, and is it already an open kb/Work note (grep kb/Work for the file and the member) or already planned by §8 (§8.3 decompositions, §8.7 notes)?',
  rule: 'RULE: does the rule cited really apply here (read the rule where it lives: the design §5/§8 text, the engineering-standards rule, the CLAUDE.md rule, the drift rule), is the target consistent with the approved §8 (or does it name the §8 section it would amend), is the wave kind right, is the severity calibrated (long-lived, consequence high)?',
  scenario: 'SCENARIO: does the concrete scenario actually occur — follow the code path, count the callers, and for a performance claim check the measurement on the pin; for a defect, is it really a defect per the ISO spec (its spec_refs pass cite.py --check, and the clause says what the finding claims) rather than a design preference?',
}

// ⛔ GRACEFUL STOP (MANDATORY-PRACTICES P3): the fleet's own STOP-r2 and the owner's global STOP; an agent obeys either, no other session's.
const COMMON = `You are an R2 reviewer of WiseOwl COBOL (docs/rearchitecture/DESIGN-architecture-review.md §3 R2). Your role file states the bar
and the checkpoint rule; this prompt adds the batch. Authority: the owner approved R1 (§8, PB2118) on 2026-10-07 17:42 PDT ("Yes,
approve and land"), and §3 R2 runs after that approval; the Workflow opt-in is standing (kb/Work R49); the owner approved this
fleet's rebuild on 2026-10-07 20:40 PDT (w1034).
⛔ PUBLIC SKILLS (MANDATORY-PRACTICES P10): the review lens is tools/claude-skills/skills/review/SKILL.md with its
references/dimensions.md and references/verification-and-siblings.md, under the project overlay .claude/skills/review/SKILL.md;
whole-codebase mode is tools/claude-skills/skills/architecture-audit/SKILL.md Phase 2. Read the parts your task needs; the
overlay and this prompt win on conflict.
CODE: read ONLY the pinned tree ${PIN} (commit ${PIN_COMMIT}); paths in your records are repository-relative ("src/...").
THE TARGET: design §8 (approved). If §8 or an open note already plans a change, cite it (design_ref "§8.3", existing_note
"PBnnnn") instead of re-proposing it. The census record and member index OF the pin were recorded after it, so they are
in the repository checkout you run in, not in the pinned tree: docs/rearchitecture/evidence/arch-census/${PIN_COMMIT}.* (your
shard input already carries the rows for your files); cite those numbers, never an older record's.
⛔ GRACEFUL STOP: before EACH file or finding check ${STOPS.join(' and ')}; if either exists, make sure every decision is
appended, then return at once with stopped=true. Never start a new file once one exists.
⛔ CHECKPOINT PER DECISION, THROUGH ONE TOOL (never a heredoc, never your own file writes):
  ${COLLECT} --status ${OUT} <pair>      — what your PAIR has decided: "read" (files ANY of its finders read whole),
                                          "findings" (every id on disk), "decided" (per lens), "next_n" (per id prefix);
  ${COLLECT} --append <your checkpoint file> '<one JSON object>'   — records one decision; it REFUSES a record the
                                          collector would reject, telling you why: fix it and append again.
Ask --status FIRST, and again before each new file or finding: skip every file in "read" and every finding your lens has
decided — another agent of your pair may have done it since you started. Number your findings from your prefix's "next_n" (1
when absent). Your structured return is the pair's state from your LAST --status call. Turn cap: your role's (120); at the
cap, return what is decided — a file nobody marked read is handed to the next agent, so an honest short list is correct.
A FINDING LINE (--append validates every field):
{"type": "finding", "id": "<your prefix>#<n>", "kind": "finding|lead|defect", "title": "...", "rule": "the rule it breaks and where
that rule is written", "scenario": "what goes wrong or what it costs, concretely", "target": "the proposed end state",
"files": ["src/..."], "sites": ["src/...:12-40 (Type.Member)"], "members": ["Ns.Type.Member"], "census_ids": ["R0-nnnn"],
"design_ref": "§8.x or PBnnnn", "wave_kind": "extract|unify|move-and-rename|data-ize|delete|modernize|defect-for-fix-lane",
"analyzer_rule": "CAnnnn/IDEnnnn/SYSLIBnnnn (modernize only)", "measurement": "the number on the pin and how (performance only)",
"severity": "Critical|Warning|Suggestion", "harm": ["wrong-answer|crashes|silent|rejects-legal-source|under-rejects"] and
"spec_refs": [{"clause": "14.9.8.4", "text": "words of that clause"}] (defects only; each must pass cite.py --check),
"existing_note": "PBnnnn or empty", "owner_question": "empty, or the one question only the owner can answer"}`

const DIM_LENS = {
  architecture: 'ARCHITECTURE (dimensions.md 1 and the overlay): layers and allowed edges (§8.1: your input lists the measured namespace edges touching this shard), single responsibility, phase boundaries, one mechanism per job, layout and naming (§8.2, §5.3). God classes are already designed (§8.3, notes PB2296-PB2347): report only what §8.3 misses, citing it. A census test-only row is judged by §8.7\'s test-only rule (a table\'s enumeration keeps; an observation seam moves or is renamed; the rest is dead code a test keeps alive).',
  code: 'FULL CODE (dimensions.md 2 and the overlay): correctness against the cited rule, sibling and paired functions agreeing, loud failure, lying comments, and the drift rules your input lists for each file (a change that breaks one is a finding). A behaviour wrong per the ISO spec is a defect-for-fix-lane with harm, spec_refs and repro.',
  performance: 'PERFORMANCE (dimensions.md 3; design §4 item 5, §5.4): hot paths, allocation, complexity at a stated input size. Read ' + INPUTS + '\\inputs\\perf.json: a claim needs a measurement on the pin (a benchmark row of the baseline, or a count at a stated size read from the code); without one it is kind "lead".',
  duplication: 'DUPLICATION, THIS SHARD\'S LENS (dimensions.md 4; §5.2): two mechanisms for one job, one rule written in two places, recomputation of what an earlier stage resolved. CLONE FAMILIES ARE NOT YOURS: the whole-codebase clone report is one separate pass; your input lists the families touching this shard only so you do not report them again.',
  'modern-csharp': 'MODERN C# (design §5.5, analyzer-first): your input\'s "analyzers" lists every analyzer warning for this shard\'s files — the CA rules at AnalysisLevel=latest-all AND the IDE/SYSLIB rules of §5.5\'s features ("modern_rules" maps each feature to its rule ids). A finding is one analyzer RULE applied across the shard (wave_kind "modernize", analyzer_rule set), counted from the input; a modern-C# point with no analyzer rule id is kind "lead". The SDK and C# version move is an owner decision (PB1754), never a finding here.',
}

// ── a concurrency limiter: at most WIDTH agents at once across every pipeline stage ──
let running = 0
const waiting = []
async function slot(fn) {
  if (running >= WIDTH) await new Promise(r => waiting.push(r))
  running++
  try { return await fn() } finally { running--; const n = waiting.shift(); if (n) n() }
}
let STOPPED = false
// the stop flag is set INSIDE the slot, before it is released, so the next queued agent sees it
const run = (prompt, opts) => STOPPED ? Promise.resolve(null) : slot(async () => {
  if (STOPPED) return null
  const r = await agent(prompt, { agentType: 'cobol-reviewer', ...opts })
  if (r && r.stopped) STOPPED = true
  return r
})

const PAIR_OUT = {
  read: { type: 'array', items: { type: 'string' }, description: 'the pair\'s "read" from your last --status call' },
  findings: { type: 'array', items: { type: 'string' }, description: 'the pair\'s "findings" from your last --status call' },
  stopped: { type: 'boolean' },
}
const FINDER_OUT = { type: 'object', properties: { ...PAIR_OUT, summary: { type: 'string', description: 'at most 300 characters' } },
  required: ['read', 'findings', 'stopped', 'summary'] }
const NULL_OUT = { type: 'object', properties: { ...PAIR_OUT, what_i_tried: { type: 'string' } },
  required: ['read', 'findings', 'stopped', 'what_i_tried'] }
const SKEPTIC_OUT = { type: 'object', properties: {
  verdicts: { type: 'array', items: { type: 'object', properties: {
    finding: { type: 'string' }, refuted: { type: 'boolean' }, why: { type: 'string' },
  }, required: ['finding', 'refuted', 'why'] } },
  stopped: { type: 'boolean' },
}, required: ['verdicts', 'stopped'] }

function finderPrompt(p, k, files, order) {
  const list = files.length ? files.join('\n') : '(none: every file of the shard is marked read — run --status once and return the pair\'s state)'
  return `${COMMON}

TASK: review the shard "${p.shard.id}" (${p.shard.name}, ${p.shard.lines} lines) along the dimension ${p.dim}; your pair is ${p.slug}.
LENS: ${DIM_LENS[p.dim]}
YOUR INPUT FILE: ${p.shard.input} — read it first: the shard's files with their lines, the semgrep hits, analyzer warnings, drift
rules and open notes per file, the census rows (god classes, unreachable and test-only families, folder/namespace disagreements),
the clone families touching it and the measured namespace edges. Those facts are computed; do not re-run the checks.
YOUR FILES (${files.length}), in this order${order === 'reverse' ? ' (you start from the far end; another finder of your pair starts from the near end, and you meet where --status shows the files read)' : ''}:
${list}
Read each WHOLE in the pinned tree, then append {"type": "read", "file": "<path>"}. Findings may span files of other shards
when the mechanism does; the "files" field names every file a wave would edit.
YOUR CHECKPOINT FILE: ${OUT}\\review-${p.slug}--f${k}.jsonl; your finding id prefix: ${p.slug}--f${k}. When your list is done,
append {"type": "done"}.`
}

function nullPrompt(p) {
  return `${COMMON}

TASK: EXAMINE A NULL RESULT. The finders of shard "${p.shard.id}" (${p.shard.name}) found NOTHING along the dimension ${p.dim}
(LENS: ${DIM_LENS[p.dim]}); your pair is ${p.slug}. A null result is accepted only when someone tried to break it. Read the input
file ${p.shard.input} (the shard's ${p.shard.files} files with their lines and facts), then look where a finding would most
likely hide: the largest files, the most fanned-in types, the files with analyzer warnings, drift rules or census rows. Append any
finding you are sure of; then append {"type": "null-check", "missed": [<the ids you wrote>]} — that line is what records the
examination, even when it is empty.
YOUR CHECKPOINT FILE: ${OUT}\\null-${p.slug}.jsonl; your finding id prefix: ${p.slug}--null.`
}

function skepticPrompt(p, ids, j, lens) {
  return `${COMMON}

TASK: you are a SKEPTIC of the pair ${p.slug}. Try to REFUTE each finding below; default to refuted=true when the evidence is not
decisive. Your lens:
${LENSES[lens]}
Read each finding's full record FROM DISK: the "finding" lines with these ids in ${OUT}\\review-${p.slug}--f*.jsonl or
${OUT}\\null-${p.slug}.jsonl (a missing record is refuted: "not on disk"). The shard's input file is ${p.shard.input || '(the whole-codebase clone report) ' + INPUTS + '\\inputs\\in-duplication.json'}.
FINDINGS: ${ids.join(', ')}
Skip a finding --status already shows "decided" under "${lens}" (another skeptic of this lens decided it).
YOUR CHECKPOINT FILE: ${OUT}\\refute-${p.slug}--c${j}--${lens}.jsonl — one line per finding:
{"type": "verdict", "finding": "<id>", "lens": "${lens}", "refuted": true|false, "why": "...", "corrected": null or {the fields of
YOUR lens you would change: ${'site: files, sites, members, existing_note; rule: design_ref, wave_kind, severity; scenario: measurement, harm, spec_refs'.split('; ').find(x => x.startsWith(lens))}}}.`
}

// A finding is NEW to this launch when an agent of this launch wrote it: a finder number above the plan's (every new
// agent takes a new number) or the null examiner of a pair whose null was unexamined. Every other id the agents return
// was on disk when the plan was made, and the plan's `undecided` already lists it under each lens that has not decided it.
function isNew(e, id) {
  const f = /--f(\d+)#\d+$/.exec(id)
  return f ? +f[1] > e.finders : /--null#\d+$/.test(id) && !e.null_checked
}

// every finding of the pair that a lens has not decided, in chunks of four per skeptic transcript
async function verify(p, e, ids) {
  const fresh = [...ids].filter(i => isNew(e, i))
  const jobs = []
  for (const lens of Object.keys(LENSES)) {
    const todo = [...new Set([...(e.undecided[lens] || []), ...fresh])].sort()
    for (let i = 0, j = 1; i < todo.length; i += CHUNK, j++) {
      const c = todo.slice(i, i + CHUNK)
      jobs.push(() => run(skepticPrompt(p, c, j, lens), { label: `skeptic:${p.slug}:c${j}:${lens}`, phase: 'Verify', schema: SKEPTIC_OUT }))
    }
  }
  return (await parallel(jobs)).filter(Boolean).length
}

async function reviewPair(e) {
  const p = { slug: e.slug, dim: e.dim, shard: { id: e.shard, ...SHARDS[e.shard] } }
  const linesOf = new Map(e.unread)
  const unreadAtStart = e.unread.map(([f]) => f)
  const read = new Set()       // the files this launch's agents report read (the plan already left out the earlier ones)
  const ids = new Set()        // the findings this launch's agents report on disk (the pair's whole list)
  let k = e.finders            // a new agent always takes a new finder number, so no two agents share a file or an id prefix
  let blind = false            // an agent returned nothing: what it wrote is on disk, but this launch has not seen it
  const absorb = r => { if (!r) { blind = true; return } r.read.forEach(f => read.add(f)); r.findings.forEach(i => ids.add(i)) }
  async function readRound(rest, kind) {
    const lines = rest.reduce((n, f) => n + (linesOf.get(f) || 0), 0)
    const lists = lines > TWO_FINDERS_OVER ? [['forward', rest], ['reverse', [...rest].reverse()]] : [['forward', rest]]
    const got = await parallel(lists.map(([order, list]) => { const kk = ++k; return () =>
      run(finderPrompt(p, kk, list, order), { label: `${kind}:${p.slug}:f${kk}`, phase: 'Review', schema: FINDER_OUT }) }))
    got.forEach(absorb)
  }
  // the review: what no finder of the pair has read, from disk; a pair the plan shows fully read starts no finder
  if (unreadAtStart.length && !STOPPED) await readRound(unreadAtStart, 'find')
  // the completeness pass: a finisher for exactly the unread remainder, twice at most (B2); after a blind return it runs
  // even with nothing unread, because only an agent can read the disk and recover what the silent one wrote
  for (let round = 0; round < 2 && !STOPPED; round++) {
    const rest = unreadAtStart.filter(f => !read.has(f))
    if (!rest.length && !blind) break
    blind = false
    log(`${p.slug}: ${rest.length} of ${unreadAtStart.length} unread files still unread — finisher`)
    await readRound(rest, 'finish')
  }
  const unread = unreadAtStart.filter(f => !read.has(f))
  if (unread.length) log(`${p.slug}: INCOMPLETE — ${unread.length} file(s) unread; a null result here is not accepted (r2_collect.py)`)
  if (!e.findings && !ids.size && !unread.length && !e.null_checked && !STOPPED) {
    const n = await run(nullPrompt(p), { label: `null:${p.slug}`, phase: 'Examine nulls', schema: NULL_OUT })
    absorb(n)
  }
  const skeptics = (e.findings || ids.size) && !STOPPED ? await verify(p, e, ids) : 0
  return { pair: p.slug, files: p.shard.files, unreadAtStart: unreadAtStart.length, unread: unread.length,
    findings: Math.max(e.findings, ids.size), skeptics }
}

async function clonePass(e) {
  const g = { slug: e.slug, dim: 'duplication', shard: { id: 'global', name: 'the whole codebase', lines: 0, files: 0, input: '' } }
  const ids = new Set()
  if (!e.done && !STOPPED) {
    const r = await run(`${COMMON}

TASK: THE ONE WHOLE-CODEBASE DUPLICATION PASS (design §5.2; the R2 adversarial review's B4); your pair is ${g.slug}. Read
${INPUTS}\\inputs\\in-duplication.json: every census clone family (type-2, by structure), each copy mapped to its shard, cross-shard
families marked. For EACH family decide, reading the copies in the pin: is it one rule in two places (one finding, wave_kind unify,
every copy's file in "files"), an accident of shape that should stay, or already a note (existing_note)? Then look for the duplicate
RULES the clone detector cannot see across subsystems — the same decision (an edition gate, a usage classification, a name mangling)
written in two subsystems — and report those you can show. One family is ONE finding however many shards it touches. Append
{"type": "read", "file": "<family id>"} per family decided (skip the families --status shows read) and {"type": "done"} at the end.
YOUR CHECKPOINT FILE: ${OUT}\\review-${g.slug}--f${e.finders + 1}.jsonl; your finding id prefix: ${g.slug}--f${e.finders + 1}.`,
      { label: `find:${g.slug}`, phase: 'Review', schema: FINDER_OUT })
    if (r) r.findings.forEach(i => ids.add(i))
  }
  const skeptics = (e.findings || ids.size) && !STOPPED ? await verify(g, e, ids) : 0
  return { pair: g.slug, files: 0, unreadAtStart: 0, unread: 0, findings: Math.max(e.findings, ids.size), skeptics }
}

log(`R2 batch ${A.batch}: ${A.pairs.length} pair(s) with work left (${A.decidedPairs} decided, not planned), ${WIDTH} agents at a time, pin ${PIN_COMMIT.slice(0, 12)}, the plan of ${A.planOf} from disk`)
const results = (await parallel(A.pairs.map(e => () => e.slug === 'global--duplication' ? clonePass(e) : reviewPair(e)))).filter(Boolean)
if (STOPPED) log('STOPPED: an agent saw a stop file')
log(`next: python scripts/arch/r2_collect.py --out ${OUT} — COMPLETE files the batch; LAUNCH NEEDED means \`r2_collect.py --out ${OUT} --launch\` and launch the launch.js it rewrites`)
return { batch: A.batch, stopped: STOPPED, pairs: results }

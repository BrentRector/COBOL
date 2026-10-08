export const meta = {
  name: 'r2-review-fleet',
  description: 'R2 architecture review (DESIGN-architecture-review §3 R2): per (shard x dimension) finders over computed file sets, a completeness pass to the unread remainder, an examiner for every null result, three lens skeptics per finding; every decision a JSON line on disk, decided by r2_collect.py',
  whenToUse: 'Run a batch whose args r2_inputs.py --batch wrote; collect with r2_collect.py; file with file_census_notes.py --r2',
  phases: [
    { title: 'Review', detail: 'finders read their shard WHOLE (two, from opposite ends, over 4,000 lines); a finisher reads any unread remainder' },
    { title: 'Examine nulls', detail: 'a pair that found nothing is examined by an agent trying to find what the finders missed' },
    { title: 'Verify', detail: 'every finding, in chunks of four, by three skeptics with distinct lenses (site, rule, scenario); two must fail to refute' },
  ],
}

// kb/Work PB2558-PB2561; the R2 adversarial review (B1-B7, N1-N7). Inputs come from scripts/arch/r2_inputs.py (the shard
// table, the mechanical checks run ONCE, one input file per shard); decisions go to disk as JSON lines, and
// scripts/arch/r2_collect.py — never this script's return — decides what the batch found. A relaunch re-reads the files and
// skips everything already decided (Workflow resume is same-session only; the JSON lines are the real resume).
const A = args || {}
const PIN = A.pinnedTree, PIN_COMMIT = A.pinCommit, OUT = A.outDir, INPUTS = A.inputsDir
const SHARDS = A.shards, DIMS = A.dimensions
const WIDTH = A.width || 8
const STOPS = [A.stopFile, A.globalStopFile].filter(Boolean)
if (!PIN || !PIN_COMMIT || !OUT || !INPUTS || !Array.isArray(SHARDS) || !Array.isArray(DIMS) || !A.stopFile || !A.globalStopFile)
  throw new Error('pass the batch-args.json r2_inputs.py --batch wrote: pinnedTree, pinCommit, outDir, inputsDir, shards[], dimensions[], args.stopFile, args.globalStopFile')
const TWO_FINDERS_OVER = 4000   // lines: a shard this big gets a second finder starting from its other end (base review Scale: audit)
const CHUNK = 4                 // findings per skeptic transcript (one 80-turn refuter deciding 15 capped out at #11; B6)
const LENSES = {
  site: 'SITE: is every site real and current in the pin (read it), are the files and members exactly the ones a wave would edit, and is it already an open kb/Work note (grep kb/Work for the file and the member) or already planned by §8 (§8.3 decompositions, §8.7 notes)?',
  rule: 'RULE: does the rule cited really apply here (read the rule where it lives: the design §5/§8 text, the engineering-standards rule, the CLAUDE.md rule, the drift rule), is the target consistent with the approved §8 (or does it name the §8 section it would amend), is the wave kind right, is the severity calibrated (long-lived, consequence high)?',
  scenario: 'SCENARIO: does the concrete scenario actually occur — follow the code path, count the callers, and for a performance claim check the measurement on the pin; for a defect, is it really a defect per the ISO spec (cite.py --check) rather than a design preference?',
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
"PBnnnn") instead of re-proposing it. The census record and member index of the pin are under
docs/rearchitecture/evidence/arch-census/${PIN_COMMIT}.*; cite those numbers, never an older record's.
⛔ GRACEFUL STOP: before EACH file or finding check ${STOPS.join(' and ')}; if either exists, make sure your checkpoint
file holds every decision, then return at once with stopped=true. Never start a new file once one exists.
⛔ CHECKPOINT PER DECISION: your checkpoint file is named below. FIRST read it if it exists and skip every file and finding
already in it; APPEND one JSON line per decision the moment you make it (python open(path, 'a'); never rewrite the file).
Your structured return is the union of the file's lines and your new ones. Turn cap: your role's (120); at the cap, return
what is decided — the files you did not mark read are handed to the next agent, so an honest short list is the correct outcome.
A FINDING LINE (scripts/arch/r2_collect.py rejects a record missing any field):
{"type": "finding", "id": "<your prefix>#<n>", "kind": "finding|lead|defect", "title": "...", "rule": "the rule it breaks and where
that rule is written", "scenario": "what goes wrong or what it costs, concretely", "target": "the proposed end state",
"files": ["src/..."], "sites": ["src/...:12-40 (Type.Member)"], "members": ["Ns.Type.Member"], "census_ids": ["R0-nnnn"],
"design_ref": "§8.x or PBnnnn", "wave_kind": "extract|unify|move-and-rename|data-ize|delete|modernize|defect-for-fix-lane",
"analyzer_rule": "CAnnnn/IDEnnnn (modernize only)", "measurement": "the number on the pin and how (performance only)",
"severity": "Critical|Warning|Suggestion", "harm": ["wrong-answer|crashes|silent|rejects-legal-source|under-rejects"] (defects
only), "existing_note": "PBnnnn or empty", "owner_question": "empty, or the one question only the owner can answer"}`

const DIM_LENS = {
  architecture: 'ARCHITECTURE (dimensions.md 1 and the overlay): layers and allowed edges (§8.1: your input lists the measured namespace edges touching this shard), single responsibility, phase boundaries, one mechanism per job, layout and naming (§8.2, §5.3). God classes are already designed (§8.3, notes PB2296-PB2347): report only what §8.3 misses, citing it. A census test-only row is judged by §8.7\'s test-only rule (a table\'s enumeration keeps; an observation seam moves or is renamed; the rest is dead code a test keeps alive).',
  code: 'FULL CODE (dimensions.md 2 and the overlay): correctness against the cited rule, sibling and paired functions agreeing, loud failure, lying comments, and the drift rules your input lists for each file (a change that breaks one is a finding). A behaviour wrong per the ISO spec is a defect-for-fix-lane with harm and repro.',
  performance: 'PERFORMANCE (dimensions.md 3; design §4 item 5, §5.4): hot paths, allocation, complexity at a stated input size. Read ' + INPUTS + '\\inputs\\perf.json: a claim needs a measurement on the pin (a benchmark row of the baseline, a profile, a count at a size); without one it is kind "lead".',
  duplication: 'DUPLICATION, THIS SHARD\'S LENS (dimensions.md 4; §5.2): two mechanisms for one job, one rule written in two places, recomputation of what an earlier stage resolved. CLONE FAMILIES ARE NOT YOURS: the whole-codebase clone report is one separate pass; your input lists the families touching this shard only so you do not report them again.',
  'modern-csharp': 'MODERN C# (design §5.5, analyzer-first): your input lists every analyzer warning at AnalysisLevel=latest-all for this shard\'s files. A finding is one analyzer RULE applied across the shard (wave_kind "modernize", analyzer_rule set), counted from the input; a modern-C# point with no analyzer rule id is kind "lead". The SDK and C# version move is an owner decision (PB1754), never a finding here.',
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

const FINDER_OUT = { type: 'object', properties: {
  files_read: { type: 'array', items: { type: 'string' }, description: 'every file of your list you read WHOLE (the union with your checkpoint file)' },
  finding_ids: { type: 'array', items: { type: 'string' } },
  stopped: { type: 'boolean' }, summary: { type: 'string', description: 'at most 300 characters' },
}, required: ['files_read', 'finding_ids', 'stopped', 'summary'] }
const NULL_OUT = { type: 'object', properties: {
  finding_ids: { type: 'array', items: { type: 'string' }, description: 'the findings you wrote (empty: the null result stands)' },
  stopped: { type: 'boolean' }, what_i_tried: { type: 'string' },
}, required: ['finding_ids', 'stopped', 'what_i_tried'] }
const SKEPTIC_OUT = { type: 'object', properties: {
  verdicts: { type: 'array', items: { type: 'object', properties: {
    finding: { type: 'string' }, refuted: { type: 'boolean' }, why: { type: 'string' },
  }, required: ['finding', 'refuted', 'why'] } },
  stopped: { type: 'boolean' },
}, required: ['verdicts', 'stopped'] }

function finderPrompt(p, k, files, order) {
  return `${COMMON}

TASK: review the shard "${p.shard.id}" (${p.shard.name}, ${p.shard.lines} lines) along the dimension ${p.dim}.
LENS: ${DIM_LENS[p.dim]}
YOUR INPUT FILE: ${p.shard.input} — read it first: the shard's files with their lines, the semgrep hits, analyzer warnings, drift
rules and open notes per file, the census rows (god classes, unreachable and test-only families, folder/namespace disagreements),
the clone families touching it and the measured namespace edges. Those facts are computed; do not re-run the checks.
YOUR FILES (${files.length}), in this order${order === 'reverse' ? ' (you start from the shard\'s far end; another finder starts from the near end)' : ''}:
${files.join('\n')}
Read each WHOLE in the pinned tree, then append {"type": "read", "file": "<path>"}. Findings may span files of other shards
when the mechanism does; the "files" field names every file a wave would edit.
YOUR CHECKPOINT FILE: ${OUT}\\review-${p.slug}--f${k}.jsonl; your finding id prefix: ${p.slug}--f${k}. When your list is done,
append {"type": "done"}.`
}

function nullPrompt(p) {
  return `${COMMON}

TASK: EXAMINE A NULL RESULT. The finders of shard "${p.shard.id}" (${p.shard.name}) found NOTHING along the dimension ${p.dim}
(LENS: ${DIM_LENS[p.dim]}). A null result is accepted only when someone tried to break it. Read the input file ${p.shard.input}
(the shard's ${p.shard.files.length} files with their lines and facts), then look where a finding would most likely hide: the
largest files, the most fanned-in types, the files with analyzer warnings, drift rules or census rows. Write any finding you are
sure of as a finding line; then append
{"type": "null-check", "missed": [<the ids you wrote>]} — that line is what records the examination, even when it is empty.
YOUR CHECKPOINT FILE: ${OUT}\\null-${p.slug}.jsonl; your finding id prefix: ${p.slug}--null.`
}

function skepticPrompt(p, ids, j, lens) {
  return `${COMMON}

TASK: you are a SKEPTIC. Try to REFUTE each finding below; default to refuted=true when the evidence is not decisive. Your lens:
${LENSES[lens]}
Read each finding's full record FROM DISK: the "finding" lines with these ids in ${OUT}\\review-${p.slug}--f*.jsonl or
${OUT}\\null-${p.slug}.jsonl (a missing record is refuted: "not on disk"). The shard's input file is ${p.shard.input || '(the whole-codebase clone report) ' + INPUTS + '\\inputs\\in-duplication.json'}.
FINDINGS: ${ids.join(', ')}
YOUR CHECKPOINT FILE: ${OUT}\\refute-${p.slug}--c${j}--${lens}.jsonl — one line per finding:
{"type": "verdict", "finding": "<id>", "lens": "${lens}", "refuted": true|false, "why": "...", "corrected": null or {the fields you
would change}}. Read-and-skip first.`
}

async function verify(p, ids) {
  const chunks = []
  for (let i = 0; i < ids.length; i += CHUNK) chunks.push(ids.slice(i, i + CHUNK))
  const votes = await parallel(chunks.flatMap((c, j) => Object.keys(LENSES).map(lens => () =>
    run(skepticPrompt(p, c, j + 1, lens), { label: `skeptic:${p.slug}:c${j + 1}:${lens}`, phase: 'Verify', schema: SKEPTIC_OUT }))))
  return votes.filter(Boolean).length
}

async function reviewPair(p) {
  const files = p.shard.files
  const finders = p.shard.lines > TWO_FINDERS_OVER ? [['forward', files], ['reverse', [...files].reverse()]] : [['forward', files]]
  const got = await parallel(finders.map(([order, list], i) => () =>
    run(finderPrompt(p, i + 1, list, order), { label: `find:${p.slug}:f${i + 1}`, phase: 'Review', schema: FINDER_OUT })))
  let read = new Set(got.filter(Boolean).flatMap(r => r.files_read))
  let ids = got.filter(Boolean).flatMap(r => r.finding_ids)
  // the completeness pass: a finder for exactly the unread remainder, twice at most (B2); what is still unread is logged
  let k = finders.length
  for (let round = 0; round < 2 && !STOPPED; round++) {
    const rest = files.filter(f => !read.has(f))
    if (!rest.length) break
    k++
    log(`${p.slug}: ${rest.length} of ${files.length} files unread — finisher f${k}`)
    const r = await run(finderPrompt(p, k, rest, 'forward'), { label: `finish:${p.slug}:f${k}`, phase: 'Review', schema: FINDER_OUT })
    if (r) { r.files_read.forEach(f => read.add(f)); ids = ids.concat(r.finding_ids) }
  }
  const unread = files.filter(f => !read.has(f))
  if (unread.length) log(`${p.slug}: INCOMPLETE — ${unread.length} file(s) unread; a null result here is not accepted (r2_collect.py)`)
  if (!ids.length && !unread.length && !STOPPED) {
    const n = await run(nullPrompt(p), { label: `null:${p.slug}`, phase: 'Examine nulls', schema: NULL_OUT })
    if (n) ids = ids.concat(n.finding_ids)
  }
  const skeptics = ids.length && !STOPPED ? await verify(p, [...new Set(ids)]) : 0
  return { pair: p.slug, files: files.length, read: read.size, unread: unread.length, findings: ids.length, skeptics }
}

const pairs = []
for (const s of SHARDS) for (const d of DIMS) pairs.push({ shard: s, dim: d, slug: `${s.id}--${d}` })
log(`R2 batch ${A.batch}: ${pairs.length} pairs (${SHARDS.length} shards x ${DIMS.length} dimensions)${A.duplicationPass ? ' + the whole-codebase clone pass' : ''}, ${WIDTH} agents at a time, pin ${PIN_COMMIT.slice(0, 12)}`)

const jobs = pairs.map(p => () => reviewPair(p))
if (A.duplicationPass) {
  const g = { slug: 'global--duplication', dim: 'duplication', shard: { id: 'global', name: 'the whole codebase', lines: 0, files: [], input: '' } }
  jobs.push(async () => {
    const r = await run(`${COMMON}

TASK: THE ONE WHOLE-CODEBASE DUPLICATION PASS (design §5.2; the R2 adversarial review's B4). Read ${INPUTS}\\inputs\\in-duplication.json:
every census clone family (type-2, by structure), each copy mapped to its shard, cross-shard families marked. For EACH family decide,
reading the copies in the pin: is it one rule in two places (one finding, wave_kind unify, every copy's file in "files"), an
accident of shape that should stay, or already a note (existing_note)? Then look for the duplicate RULES the clone detector cannot see
across subsystems — the same decision (an edition gate, a usage classification, a name mangling) written in two subsystems — and report
those you can show. One family is ONE finding however many shards it touches. Append {"type": "read", "file": "<family id>"} per
family decided and {"type": "done"} at the end.
YOUR CHECKPOINT FILE: ${OUT}\\review-global--duplication--f1.jsonl; your finding id prefix: global--duplication--f1.`,
      { label: 'find:global--duplication', phase: 'Review', schema: FINDER_OUT })
    const ids = r ? r.finding_ids : []
    const skeptics = ids.length && !STOPPED ? await verify(g, [...new Set(ids)]) : 0
    return { pair: g.slug, files: 0, read: r ? r.files_read.length : 0, unread: 0, findings: ids.length, skeptics }
  })
}
const results = (await parallel(jobs)).filter(Boolean)
if (STOPPED) log('STOPPED: an agent saw a stop file; relaunch with the same args to resume from the checkpoint files')
log(`next: python scripts/arch/r2_collect.py --out ${OUT}`)
return { batch: A.batch, stopped: STOPPED, pairs: results }

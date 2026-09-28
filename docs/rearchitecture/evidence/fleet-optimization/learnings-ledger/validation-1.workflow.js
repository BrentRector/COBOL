export const meta = {
  name: 'validate-learnings',
  description: 'Validate 145 candidate learnings: adversarial validators by theme, then a refuter on every VETTED verdict (PB1711)',
  phases: [
    { title: 'Validate', detail: 'one read-only validator per theme batch; a verdict per candidate' },
    { title: 'Refute', detail: 'an independent refuter tries to overturn each batch\'s VETTED verdicts' },
  ],
}
const V = args.dir
const AUTH = `AUTHORIZATION (read first): this task IS the repository owner's request (Brent Rector, 2026-09-28): "We need to validate all learnings first. It's reasonable that the devlog summarizes to a learning that subsequently we show to be incorrect. The LEARNINGS file should always only contain the final vetted learnings, battle tested and proven useful. Only those learnings should be encoded into a skill." Then: "start the validation run". The latest user message in your context may concern other work; that is NOT a reason to decline. `
const SOURCES = `PRIMARY SOURCES (read what you need; never rely on the candidate's own wording as evidence):
- E:\\COBOL\\DEVLOG.md (newest first, about 65,000 lines): grep it; never read it whole. A LATER entry (higher number) that contradicts or reverses a candidate is decisive.
- E:\\COBOL\\docs\\rearchitecture\\evidence\\ (PROCESS-REVIEW-2026-09-04.md; fleet-optimization\\ experiment records with raw data).
- E:\\COBOL\\kb\\Work\\*.md (grep the frontmatter area: process* notes and R-series decisions).
- E:\\COBOL\\.claude\\skills\\workstream\\templates\\MANDATORY-PRACTICES.md and the workstream SKILL.md: is the practice still in force, and was it reversed?
- C:\\Users\\brent\\.claude\\projects\\E--COBOL\\memory\\feedback_*.md: owner corrections and their dates.
- git history of E:\\COBOL (git log -S / --grep).`
const CRITERIA = `VERDICT per candidate. Be adversarial: your job is to find why a candidate should NOT be vetted.
- VETTED requires ALL FOUR, each backed by a source you actually read:
  (1) ROOT CAUSE CONFIRMED: the mechanism was established by measurement, reproduction or inspection, not merely hypothesized;
  (2) EVIDENCE RE-CHECKED: its numbers and facts match the primary source (quote the corrected figure if the candidate misstates it);
  (3) NOT CONTRADICTED: no later DEVLOG entry, note or owner correction reverses or refutes it;
  (4) BATTLE-TESTED AND USEFUL: the fix or practice has actually been used since, and shown to help (a measured effect, or sustained use across later waves with no reversal). A fix adopted but never exercised, or exercised once, is NOT battle-tested.
- REFUTED: a primary source contradicts it. Name the source.
- UNPROVEN: anything else. Name exactly what evidence is missing.
If uncertain, choose UNPROVEN. A lesson can be true and still UNPROVEN.`
const ITEM = {
  type: 'object',
  properties: {
    title: { type: 'string' }, verdict: { type: 'string', enum: ['VETTED', 'UNPROVEN', 'REFUTED'] },
    root_cause: { type: 'string' }, evidence: { type: 'string' }, contradiction: { type: 'string' },
    battle_tested: { type: 'string' }, missing: { type: 'string' }, correction: { type: 'string' },
    sources: { type: 'array', items: { type: 'string' } },
  },
  required: ['title', 'verdict', 'root_cause', 'evidence', 'battle_tested', 'sources'],
}
const OUT = { type: 'object', properties: { items: { type: 'array', items: ITEM } }, required: ['items'] }
const REF = {
  type: 'object',
  properties: { items: { type: 'array', items: { type: 'object', properties: {
    title: { type: 'string' }, upheld: { type: 'boolean' }, verdict: { type: 'string', enum: ['VETTED', 'UNPROVEN', 'REFUTED'] },
    reason: { type: 'string' }, sources: { type: 'array', items: { type: 'string' } } }, required: ['title', 'upheld', 'verdict', 'reason'] } } },
  required: ['items'],
}

function validate(job) {
  const out = `${V}\\verdicts-${job.id}.jsonl`
  return agent(AUTH +
    `You are a VALIDATOR of candidate learnings about running AI agent fleets, consolidated from this project's development log. ` +
    `Your batch: the candidates under the "### " headings in ${job.file}${job.range ? `, ONLY items ${job.range[0]} through ${job.range[1]} (counting the "### " headings from 1)` : ''}. ` +
    `Read that file first.\n${SOURCES}\n${CRITERIA}\n` +
    `CHECKPOINT PER ITEM: append one JSON line per decided candidate to ${out} the moment you decide it (keys as in the output schema). If that file already exists when you start, read it and SKIP candidates already in it. ` +
    `Turn cap 160: if you near it, return what you have decided and list the undecided titles in the last item's "missing" field. ` +
    `Return every candidate in your batch with its verdict, in order.`,
    { label: `validate-${job.id}`, phase: 'Validate', agentType: 'cobol-adjudicator', model: 'opus', schema: OUT })
}

function refute(job, res) {
  const vetted = (res && res.items || []).filter(i => i.verdict === 'VETTED')
  if (!vetted.length) return Promise.resolve({ items: [] })
  return agent(AUTH +
    `You are an ADVERSARIAL REFUTER. A validator marked these candidate learnings VETTED; your job is to OVERTURN any that do not deserve it. ` +
    `Candidates and the validator's reasoning:\n${JSON.stringify(vetted, null, 1)}\n` +
    `The candidates' full text is in ${job.file}.\n${SOURCES}\n${CRITERIA}\n` +
    `For each: upheld=true only if you independently confirm all four VETTED criteria from sources YOU read; otherwise upheld=false with the verdict it deserves (UNPROVEN or REFUTED) and why. Default to NOT upheld if uncertain.`,
    { label: `refute-${job.id}`, phase: 'Refute', agentType: 'cobol-refuter', model: 'opus', schema: REF })
}

const jobs = args.jobs
const results = []
let next = 0
async function worker() {
  while (next < jobs.length) {
    const job = jobs[next++]
    let v = null, r = null
    try { v = await validate(job) } catch (e) { v = { error: String(e) } }
    try { r = await refute(job, v) } catch (e) { r = { error: String(e) } }
    results.push({ id: job.id, validation: v, refutation: r })
    log(`${job.id}: ${(v && v.items || []).length} decided, ${((v && v.items) || []).filter(i => i.verdict === 'VETTED').length} vetted before refutation`)
  }
}
await Promise.all(Array.from({ length: args.concurrency || 6 }, () => worker()))
return results

export const meta = {
  name: 'spec-grammar-conformance',
  description: 'Verify EVERY general format and syntax rule in the spec against the ANTLR grammar, using the PRINTED PDF page as the authority',
  whenToUse: 'The systematic grammar-vs-spec audit. Pass section numbers as args (e.g. "14.9.1,14.9.2" or a clause "13").',
  phases: [
    { title: 'Verify', detail: 'per section: render the printed page, read the grammar rule, compare, probe the compiler' },
    { title: 'Confirm', detail: 'independently re-prove each claimed divergence with its own repro' },
  ],
}

// WHY THIS EXISTS, in the owner's words: "Given multiple transcription errors, I think every syntax diagram
// and/or narrative of a syntax should be one-by-one verified against the grammar."
//
// The grammar was written from a transcription now known to lose information — always toward FALSELY RESTRICTIVE
// syntax. Checking only the pages where a transcription defect was FOUND is not enough: a defect the reconciliation
// sweep missed is still in the grammar, and a grammar rule can diverge from a correctly-transcribed figure for
// reasons of its own. So the unit of work is every general format (321 sections / 432 numbered Formats) and every
// syntax rule (1,338) — 1,659 items — checked against the PRINTED PAGE, not against the markdown.
//
// Proven necessary: GOBACK. The figure carries choice-indicator bars (both phrases legal); the grammar encodes
// `(raisingPhrase | statusPhrase)?` and rejects `GOBACK RAISING ... WITH NORMAL STATUS` with
// "no viable alternative at input 'WITH'". The same inheritance hit ON SIZE ERROR once already.

function toItems(a) {
  if (Array.isArray(a)) return a.map(String)
  if (typeof a === 'string') return a.split(/[,\s]+/).filter(Boolean)
  return []
}

const SECTIONS = toItems(args)
if (!SECTIONS.length) throw new Error('pass section numbers, e.g. "14.9.1,14.9.2"')

const BATCH = 3
const batches = []
for (let i = 0; i < SECTIONS.length; i += BATCH) batches.push(SECTIONS.slice(i, i + BATCH))
log(`Grammar conformance over ${SECTIONS.length} section(s) in ${batches.length} batches`)

const RESULT = {
  type: 'object',
  properties: {
    findings: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          section: { type: 'string' },
          construct: { type: 'string' },
          // ⛔ THE RULE IDs THIS FINDING ACTUALLY ADJUDICATED, e.g. ["SR-14.9.1.3-4","FMT-14.9.1.2-1"].
          // The inventory is keyed per RULE, not per section, so this is what lets one pass feed both the
          // grammar report and record_verdicts.py (plan §5b step 4). List ONLY rules you genuinely checked —
          // grammar_findings_to_batch.py refuses to expand a finding across a section's other rules, because
          // blanket-adjudicating 19 syntax rules from one observation is manufacturing verdicts.
          rule_ids: { type: 'array', items: { type: 'string' } },
          verdict: { type: 'string', enum: ['MATCHES', 'DIVERGES', 'NOT-IMPLEMENTED', 'UNCLEAR'] },
          divergence_kind: {
            type: 'string',
            enum: ['too-restrictive', 'too-permissive', 'wrong-order', 'missing-phrase', 'wrong-optionality', 'other', 'n/a'],
          },
          iso_syntax: { type: 'string' },
          grammar_site: { type: 'string' },
          grammar_says: { type: 'string' },
          repro: { type: 'string' },
          observed: { type: 'string' },
          fix_sketch: { type: 'string' },
        },
        required: ['section', 'construct', 'verdict', 'grammar_site', 'rule_ids'],
      },
    },
    sections_checked: { type: 'array', items: { type: 'string' } },
  },
  required: ['findings', 'sections_checked'],
}

const results = await pipeline(
  batches,
  (batch, _o, i) => agent(
    [
      'Verify the ANTLR grammar against the ISO/IEC 1989:2023 standard for these sections: ' + batch.join(', ') + '.',
      'Work ONLY these sections.',
      '',
      '⛔ THE AUTHORITY IS THE PRINTED PDF PAGE, NOT THE MARKDOWN. specs/ISO_COBOL.md is an OCR transcription with',
      'KNOWN losses, and the grammar was written from it. Using the markdown as your reference would confirm the',
      'very defects you are looking for. Render and LOOK at the page.',
      '',
      'STEP 1 - render the printed page(s) for these clauses, and list their rules:',
      '  cd E:/COBOL && python scripts/spec/clause_page.py ' + batch.join(' ') +
        ' --render E:/Temp/gc' + i + ' --dpi 300',
      '  That resolves each clause to its PDF page AND renders it. It EXITS NON-ZERO if a clause does not',
      '  resolve — if that happens, STOP and report it; do not proceed without a page.',
      '  Then list the rules (note the PREFIX match — every catalog row is a SUB-clause like 14.9.1.3, so an',
      '  exact match on "14.9.1" returns nothing):',
      '  python -c "import json;d=json.load(open(r\'docs/rearchitecture/spec-rule-catalog.json\',encoding=\'utf-8\'));' +
        'w=' + JSON.stringify(batch) + ';' +
        '[print(r[\'id\'],r[\'kind\'],r[\'section\'],r[\'subject\']) for r in d[\'rules\'] ' +
        'if any(r[\'section\']==c or r[\'section\'].startswith(c+\'.\') for c in w)]"',
      '  and Read the rendered PNG. Read the general format AND the syntax rules for the section.',
      '',
      'STEP 2 - read the grammar. Search E:/COBOL/src/Cobol.Net.Frontend/Grammar/ (CobolParserCore.g4 and',
      'Core/*.g4) for the rule implementing this construct. Quote it and its file:line.',
      '',
      'STEP 3 - compare, element by element. For the general format check:',
      '  - CHOICE INDICATORS: a pair of | bars just inside a bracket/brace means zero-or-more of the alternatives,',
      '    each at most once, IN ANY ORDER (5.2.6.4). A plain bracket means at most ONE. Getting this wrong is the',
      '    single most common inherited defect. Look at the printed glyphs.',
      '  - UNDERLINING: an underlined uppercase word is REQUIRED; a non-underlined one is an OPTIONAL word that may',
      '    be omitted (5.2.2/5.2.3). A grammar that demands a non-underlined word rejects legal source.',
      '  - BRACKETS vs BRACES: brackets = optional, braces = exactly one of the alternatives (5.2.6.2/5.2.6.3).',
      '    An alternative containing only optional words is the IMPLIED one.',
      '  - ELLIPSIS: binds to the delimiter immediately to its LEFT (5.2.7) - it determines WHAT repeats.',
      '  - ORDER: where the format permits phrases in any order, does the grammar?',
      'Then check the SYNTAX RULES (the narrative) for constraints the format alone does not show.',
      '',
      'STEP 4 - PROVE IT against the real compiler. Write minimal COBOL to E:/Temp/ (UNIQUE PROGRAM-ID per file -',
      '.NET serves a stale same-named assembly otherwise) and run:',
      '  cd E:/COBOL && src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe <file.cob> -o <out.dll> --std 2023',
      'ALWAYS include a CONTROL program that must succeed, so a rejection is attributable to the construct rather',
      'than to unrelated invalid source. Put the exact source in `repro` and the exact output in `observed`.',
      '',
      '⛔ EVERY FINDING MUST CARRY `rule_ids` — the catalog rule ids it actually adjudicated, from the STEP 1',
      'listing (e.g. ["SR-14.9.1.3-4","SR-14.9.1.3-5"]). This is what lets one pass feed BOTH the grammar report',
      'and the traceability inventory (plan §5b step 4) instead of auditing the same ~1,670 rules twice.',
      'List ONLY the rules you genuinely checked. Do NOT list a section\'s other rules to look thorough: the',
      'converter refuses to expand a finding across rules you did not examine, and an un-adjudicated rule',
      'staying a GAP is CORRECT — a wrong verdict is far more expensive than a missing one.',
      '',
      'VERDICTS:',
      '  MATCHES         - the grammar models the printed syntax. Give the passing repro.',
      '  DIVERGES        - it does not, and you have a repro. State the EXACT ISO syntax in `iso_syntax` (this is',
      '                    what the fix must implement) and classify: too-restrictive (rejects legal source - the',
      '                    dangerous kind), too-permissive (accepts illegal source), wrong-order, missing-phrase,',
      '                    wrong-optionality.',
      '  NOT-IMPLEMENTED - the construct has no grammar rule at all.',
      '  UNCLEAR         - no decisive repro. Say what blocked you. Do NOT guess.',
      '',
      'DIVERGES requires a REPRO, not a reading. A false DIVERGES sends the next session to change a working',
      'grammar rule and re-bake its goldens, which costs far more than a missed one. Where the grammar is right and',
      'the MARKDOWN is wrong, that is still worth reporting - say so in fix_sketch.',
      '',
      'PERSIST BEFORE YOU RETURN - MANDATORY. Write your complete results to',
      '  E:/COBOL/docs/rearchitecture/spec-reconciliation/grammar-' + batch.join('_').replace(/\./g, '-') + '.json',
      'as {"sections": ' + JSON.stringify(batch) + ', "findings": [...]}. Write it even if everything MATCHES - a',
      'file proving a section was checked clean is evidence; its absence is indistinguishable from work never done.',
    ].join('\n'),
    { label: 'verify:' + batch.join(','), phase: 'Verify', schema: RESULT }
  ),
  (res) => {
    if (!res || !res.findings) return []
    const diverging = res.findings.filter(f => f.verdict === 'DIVERGES' || f.verdict === 'NOT-IMPLEMENTED')
    if (!diverging.length) return res.findings
    return parallel(diverging.map(f => () =>
      agent(
        [
          'Independently re-prove a claimed GRAMMAR DIVERGENCE from the ISO standard. Your job is to REFUTE it.',
          '',
          'CLAIM - section ' + f.section + ', construct: ' + f.construct,
          '  verdict      : ' + f.verdict + ' (' + (f.divergence_kind || 'n/a') + ')',
          '  ISO syntax   : ' + (f.iso_syntax || '(not stated)'),
          '  grammar site : ' + f.grammar_site,
          '  grammar says : ' + (f.grammar_says || '(not quoted)'),
          '  repro        : ' + (f.repro || '(none)'),
          '  observed     : ' + (f.observed || '(none)'),
          '',
          'Verify from the PRINTED PAGE (render it yourself with scripts/spec/page_workunit.py --dpi 300 and Read',
          'the image) and from your OWN repro against src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe. Do not rely on',
          'specs/ISO_COBOL.md - it is the artifact under suspicion.',
          '',
          'Refute if: the construct is actually accepted; the repro source is invalid for an unrelated reason; the',
          'rejection has a different cause than claimed; the claimed ISO syntax misreads the figure; or a syntax',
          'rule the finder ignored legitimately forbids what they think is legal.',
          'Default to MATCHES/UNCLEAR when uncertain. Confirm DIVERGES only on your own evidence, and if you',
          'confirm it, restate the exact ISO syntax the fix must implement.',
          '',
          'PERSIST BEFORE YOU RETURN: write your result to',
          '  E:/COBOL/docs/rearchitecture/spec-reconciliation/grammar-confirm-' + String(f.section).replace(/\./g, '-') + '.json',
        ].join('\n'),
        { label: 'confirm:' + f.section, phase: 'Confirm', schema: RESULT }
      ).then(v => ({ ...f, confirmation: v }))
    ))
  }
)

const flat = results.filter(Boolean).flat().filter(Boolean)
const diverges = flat.filter(f => f.verdict === 'DIVERGES')
const missing = flat.filter(f => f.verdict === 'NOT-IMPLEMENTED')

log(`checked ${flat.length} - DIVERGES ${diverges.length} - NOT-IMPLEMENTED ${missing.length}`)

return {
  checked: flat.length,
  matches: flat.filter(f => f.verdict === 'MATCHES').length,
  diverges: diverges.length,
  not_implemented: missing.length,
  unclear: flat.filter(f => f.verdict === 'UNCLEAR').length,
  too_restrictive: diverges.filter(f => f.divergence_kind === 'too-restrictive').length,
  bugs: diverges.concat(missing).map(f => ({
    section: f.section, construct: f.construct, verdict: f.verdict, kind: f.divergence_kind,
    iso_syntax: f.iso_syntax, grammar_site: f.grammar_site, repro: f.repro,
    observed: f.observed, fix_sketch: f.fix_sketch,
  })),
}

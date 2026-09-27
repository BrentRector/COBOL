export const meta = {
  name: 'spec-grammar-impact',
  description: 'For each normative transcription defect, determine whether the GRAMMAR inherited it and the compiler rejects legal COBOL',
  whenToUse: 'After a spec-reconcile sweep. Pass the pages carrying normative findings as args (array or ranges).',
  phases: [
    { title: 'Impact', detail: 'grep the grammar and probe the CLI for each normative finding' },
    { title: 'Confirm', detail: 'independently re-prove each claimed compiler bug with its own repro' },
  ],
}

// A transcription defect matters twice. The markdown is wrong (a section-C repair), AND the grammar may have been
// BUILT from the wrong diagram — in which case the compiler rejects legal COBOL and it is a section-B fix-queue
// bug. Proven on GOBACK: the figure lost its choice-indicator bars, the grammar encodes
// `(raisingPhrase | statusPhrase)?` with a comment calling them "mutually-exclusive", and
// `GOBACK RAISING ... WITH NORMAL STATUS` is rejected with "no viable alternative at input 'WITH'".
// This has bitten before: CobolParserCore.g4:773 records the same inheritance for ON SIZE ERROR, fixed 2026-07-19.

function toPages(a) {
  if (Array.isArray(a)) return a.map(Number).filter(n => Number.isInteger(n) && n > 0)
  if (typeof a === 'string') {
    const out = []
    for (const tok of a.split(/[,\s]+/)) {
      if (!tok) continue
      const r = tok.match(/^(\d+)-(\d+)$/)
      if (r) { const lo = Number(r[1]), hi = Number(r[2]); if (hi >= lo && hi - lo < 5000) for (let n = lo; n <= hi; n++) out.push(n) }
      else if (/^\d+$/.test(tok)) out.push(Number(tok))
    }
    return out
  }
  return []
}

const PAGES = toPages(args)
if (!PAGES.length) throw new Error('pass the pages carrying normative findings')

const BATCH = 4
const batches = []
for (let i = 0; i < PAGES.length; i += BATCH) batches.push(PAGES.slice(i, i + BATCH))
log(`Grammar-impact over ${PAGES.length} pages in ${batches.length} batches`)

const IMPACT = {
  type: 'object',
  properties: {
    results: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          page: { type: 'integer' },
          construct: { type: 'string' },
          inherited: { type: 'string', enum: ['INHERITED', 'NOT-INHERITED', 'NOT-APPLICABLE', 'UNCLEAR'] },
          grammar_site: { type: 'string' },
          repro: { type: 'string' },
          observed: { type: 'string' },
          expected: { type: 'string' },
          fix_sketch: { type: 'string' },
        },
        required: ['page', 'construct', 'inherited', 'grammar_site', 'observed'],
      },
    },
  },
  required: ['results'],
}

const results = await pipeline(
  batches,
  (batch, _o, i) => agent(
    [
      'A defect was confirmed in the ISO spec TRANSCRIPTION (specs/ISO_COBOL.md) for each of these printed pages:',
      '  ' + batch.join(', '),
      '',
      'Your question is NOT whether the transcription is wrong - that is already established and verified. Your',
      'question is whether the COMPILER INHERITED the defect: was the ANTLR grammar written from the WRONG diagram,',
      'so that WiseOwl COBOL now rejects legal COBOL (or accepts illegal COBOL)?',
      '',
      'This is not hypothetical. Worked example, already proven:',
      '  - The GOBACK general format (p661) lost its choice-indicator bars in transcription, so the markdown read',
      '    "raising-phrase OR status-phrase" when the standard permits BOTH.',
      '  - CobolParserCore.g4:1183 encodes `GOBACK ... (raisingPhrase | statusPhrase)?` with a comment calling them',
      '    "the mutually-exclusive tail alternatives" - the defect, inherited verbatim.',
      '  - `GOBACK RAISING EC-PROGRAM-ARG-MISMATCH WITH NORMAL STATUS.` is REJECTED:',
      '    "error COBOL0001: no viable alternative at input \'WITH\'", while the status phrase alone compiles.',
      '  - Precedent: CobolParserCore.g4:773 records the SAME inheritance for ON SIZE ERROR, found and fixed',
      '    2026-07-19 after the transcription was found to have dropped the bars.',
      '',
      'FOR EACH page in your batch:',
      '1. Read the finding. The ledger is E:/COBOL/docs/rearchitecture/spec-reconciliation/LEDGER.json and the',
      '   readable form is REPORT.md in the same directory. Find the entries for your page and note the construct',
      '   and what the PRINTED figure actually permits.',
      '2. Locate the grammar rule. Search E:/COBOL/src/Cobol.Net.Frontend/Grammar/ (CobolParserCore.g4 and',
      '   Core/*.g4). Quote the rule and its file:line in grammar_site.',
      '3. Decide whether the rule encodes the PRINTED syntax or the mis-transcribed one. Watch for: alternation',
      '   `(a | b)?` where the figure has choice-indicator bars and both are permitted; a REQUIRED token where the',
      '   figure prints the word un-underlined (optional word); a missing `?` where the figure has an outer bracket',
      '   making a clause optional; a missing repetition where an ellipsis binds outside a brace.',
      '4. PROVE IT with the real compiler. Write a minimal COBOL program to E:/Temp/ and run:',
      '     cd E:/COBOL && src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe <file.cob> -o <out.dll> --std 2023',
      '   Put the exact source in `repro` and the exact compiler output in `observed`. ALWAYS also compile a',
      '   CONTROL program that should succeed, so a rejection is attributable to the construct and not to unrelated',
      '   invalid source. Use a unique PROGRAM-ID per file - .NET serves a stale same-named assembly otherwise.',
      '',
      'VERDICTS:',
      '  INHERITED      - the grammar encodes the mis-transcribed syntax AND you have a repro showing legal source',
      '                   rejected (or illegal source accepted). This is a COMPILER BUG, not a doc fix.',
      '  NOT-INHERITED  - the grammar is correct despite the bad transcription (it was written from the rule text,',
      '                   or from the correct figure). Say how you know, and give the passing repro.',
      '  NOT-APPLICABLE - the defect is in prose, a heading, front matter, or otherwise cannot reach the grammar.',
      '  UNCLEAR        - you could not build a decisive repro. Say exactly what blocked you. Do NOT guess.',
      '',
      'Be conservative: INHERITED requires a REPRO, not a reading of the grammar. A wrong INHERITED verdict sends',
      'the next session to change a working grammar rule, which is far more expensive than a missed one.',
      '',
      'PERSIST BEFORE YOU RETURN - MANDATORY. Write your complete results to',
      '  E:/COBOL/docs/rearchitecture/spec-reconciliation/impact-p' + batch[0] + '-p' + batch[batch.length - 1] + '.json',
      'as {"pages": ' + JSON.stringify(batch) + ', "results": [...]}. Write it even if every verdict is',
      'NOT-APPLICABLE. A rate or session limit can end this run at any moment and the file is the durable record.',
    ].join('\n'),
    { label: 'impact:p' + batch[0] + '-' + batch[batch.length - 1], phase: 'Impact', schema: IMPACT }
  ),
  (res) => {
    if (!res || !res.results) return []
    const claimed = res.results.filter(r => r.inherited === 'INHERITED')
    if (!claimed.length) return res.results
    return parallel(claimed.map(r => () =>
      agent(
        [
          'Independently re-prove a claimed COMPILER BUG inherited from a spec-transcription defect.',
          '',
          'CLAIM - page ' + r.page + ', construct: ' + r.construct,
          '  grammar site : ' + r.grammar_site,
          '  repro        : ' + (r.repro || '(none given)'),
          '  observed     : ' + r.observed,
          '  expected     : ' + (r.expected || '(none given)'),
          '',
          'Do NOT take the claim on trust. Write your OWN minimal program (unique PROGRAM-ID) and run it:',
          '  cd E:/COBOL && src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe <file.cob> -o <out.dll> --std 2023',
          'Also compile a control that must succeed. Then confirm against the PRINTED page, not the markdown:',
          '  python scripts/spec/page_workunit.py ' + r.page + ' --out E:/Temp/gi' + r.page + ' --dpi 300',
          'and Read the rendered image. The markdown is the thing under suspicion; do not use it as evidence.',
          '',
          'Set inherited=INHERITED only if YOUR repro reproduces it. If the source you were given is invalid for an',
          'unrelated reason, or the construct is rejected for a different cause than claimed, say so and set',
          'NOT-INHERITED or UNCLEAR. Report what you actually observed.',
          '',
          'PERSIST BEFORE YOU RETURN: write your result to',
          '  E:/COBOL/docs/rearchitecture/spec-reconciliation/impact-confirm-p' + r.page + '.json',
        ].join('\n'),
        { label: 'confirm:p' + r.page, phase: 'Confirm', schema: IMPACT }
      ).then(v => ({ ...r, confirmation: v }))
    ))
  }
)

const flat = results.filter(Boolean).flat().filter(Boolean)
const inherited = flat.filter(r => r.inherited === 'INHERITED')
log(`checked ${flat.length} - INHERITED (compiler bugs) ${inherited.length}`)

return {
  checked: flat.length,
  inherited: inherited.length,
  not_inherited: flat.filter(r => r.inherited === 'NOT-INHERITED').length,
  not_applicable: flat.filter(r => r.inherited === 'NOT-APPLICABLE').length,
  unclear: flat.filter(r => r.inherited === 'UNCLEAR').length,
  compiler_bugs: inherited.map(r => ({
    page: r.page, construct: r.construct, grammar_site: r.grammar_site,
    repro: r.repro, observed: r.observed, expected: r.expected, fix_sketch: r.fix_sketch,
  })),
}

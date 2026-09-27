export const meta = {
  name: 'spec-reconcile',
  description: 'Compare the canonical ISO PDF against specs/ISO_COBOL.md page by page and ledger every discrepancy',
  whenToUse: 'Reconciling the OCR transcription against its canonical source. Pass a page-number array as args.',
  phases: [
    { title: 'Compare', detail: 'one agent per page batch: read the rendered PDF page, diff against the markdown slice' },
    { title: 'Verify', detail: 'adversarially re-check each claimed discrepancy against the same page' },
  ],
}

// args may arrive as a real array OR as a JSON-encoded string depending on how the caller passed it. A string
// silently "works" for .length and .slice — slicing the TEXT into substrings — so the failure surfaces late as
// "batch.join is not a function" after the page count reads as the string length. Normalise up front.
// ALWAYS pass a COMPUTED page list, never a typed one. A hand-transcribed list cost a whole 4.3M-token sweep:
// the args were typed as a plausible-looking regular sequence (155, 157, ... 441) instead of the computed figure
// pages, so only 64 of 173 target pages were covered while 105 non-target pages were swept — and the run still
// reported "169 pages swept" as though it were the intended set. Scripts have no filesystem access, so the list
// cannot be read here; the guard is instead `merge_reconciliation.py --expect N`, which compares the pages
// actually swept against the intended file and FAILS LOUD on a gap. That check is what caught this.
// Accepts an array, or a string of pages and RANGES: "3-65,67,70-72". Ranges exist so a long page set can be
// passed compactly and derived mechanically instead of transcribed by hand — hand-transcription is what caused
// the mis-targeted sweep. Fewer characters typed, fewer chances to invent one.
function toPages(a) {
  if (Array.isArray(a)) return a.map(Number).filter(n => Number.isInteger(n) && n > 0)
  if (typeof a === 'string') {
    const out = []
    for (const tok of a.split(/[,\s]+/)) {
      if (!tok) continue
      const r = tok.match(/^(\d+)-(\d+)$/)
      if (r) {
        const lo = Number(r[1]), hi = Number(r[2])
        if (hi >= lo && hi - lo < 5000) for (let n = lo; n <= hi; n++) out.push(n)
      } else if (/^\d+$/.test(tok)) {
        out.push(Number(tok))
      }
    }
    return out.filter(n => Number.isInteger(n) && n > 0)
  }
  return []
}

const PAGES = toPages(args).length ? toPages(args) : [1, 2, 24, 50]
const BATCH = 12
const batches = []
for (let i = 0; i < PAGES.length; i += BATCH) batches.push(PAGES.slice(i, i + BATCH))

log(`Reconciling ${PAGES.length} pages in ${batches.length} batches of up to ${BATCH}`)
// Echo the actual boundaries so a mis-transcribed list is visible AT LAUNCH, not after the tokens are spent.
log(`first=${PAGES[0]} last=${PAGES[PAGES.length - 1]} distinct=${new Set(PAGES).size}`)

const FINDINGS = {
  type: 'object',
  properties: {
    findings: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          page: { type: 'integer' },
          kind: { type: 'string', enum: ['missing-text', 'incorrect-text', 'misplaced-content', 'missing-figure', 'incorrect-figure', 'missing-heading', 'ocr-error', 'summarised-not-transcribed', 'other'] },
          severity: { type: 'string', enum: ['normative', 'structural', 'cosmetic'] },
          pdf_says: { type: 'string' },
          markdown_says: { type: 'string' },
          why_it_matters: { type: 'string' },
          suggested_repair: { type: 'string' },
        },
        required: ['page', 'kind', 'severity', 'pdf_says', 'markdown_says', 'why_it_matters'],
      },
    },
    pages_checked: { type: 'array', items: { type: 'integer' } },
  },
  required: ['findings', 'pages_checked'],
}

const VERDICT = {
  type: 'object',
  properties: {
    real: { type: 'boolean' },
    reasoning: { type: 'string' },
    corrected_description: { type: 'string' },
  },
  required: ['real', 'reasoning'],
}

const results = await pipeline(
  batches,
  (batch, _orig, i) => agent(
    [
      'You are reconciling the canonical ISO/IEC 1989:2023 PDF against its markdown transcription at',
      'E:/COBOL/specs/ISO_COBOL.md. Pages in YOUR batch: ' + batch.join(', ') + '. Work ONLY these pages.',
      '',
      'These pages were selected because they contain FIGURES (general formats / syntax skeletons). Diagram',
      'fidelity is the priority for this run.',
      '',
      'STEP 1 - build your work units (renders the PDF pages and extracts the matching markdown slices):',
      '  cd E:/COBOL && python scripts/spec/page_workunit.py ' + batch.join(' ') + ' --out E:/Temp/specrecon/f' + i + ' --dpi 300',
      '',
      'STEP 2 - for EACH page P in your batch:',
      '  a) Read the image E:/Temp/specrecon/f' + i + '/spec_pP.png (the Read tool renders it visually)',
      '  b) Read the markdown E:/Temp/specrecon/f' + i + '/page-P.md',
      '  c) Compare them carefully and completely.',
      '',
      'THE PRIORITY DEFECT CLASS - lost CHOICE INDICATORS. Per section 5.2.6.4, a pair of vertical bars just',
      'inside a bracket or brace means: zero or more of the enclosed alternatives may be specified, each at most',
      'once, IN ANY ORDER. Without the bars, a bracket around stacked alternatives means AT MOST ONE of them.',
      'This transcription is KNOWN to drop those bars, and the loss is ALWAYS toward falsely restrictive syntax -',
      'legal source made to look illegal. A confirmed instance: the GOBACK general format lost its bars, so the',
      'markdown said "raising-phrase OR status-phrase" when the standard permits BOTH.',
      'For every figure on your pages, look at the printed glyphs closely (crop/zoom if needed) and ask: is there',
      'a bar just inside the bracket or brace? Does the markdown preserve it?',
      '',
      'ALSO CHECK, in figures:',
      '  - a brace or bracket that OPENS but never CLOSES (a real confirmed defect in CALL Format 2)',
      '  - an ellipsis on the wrong side of a delimiter. Per 5.2.7 the ellipsis binds to the delimiter',
      '    immediately to its LEFT, so misplacing it changes what repeats.',
      '  - underlining: underlined words are REQUIRED words; losing that changes the grammar',
      '  - a Figure notes paragraph that contradicts what the diagram actually shows',
      '',
      'AND, in prose:',
      '  - missing-text: normative text printed in the PDF but absent from the markdown',
      '  - incorrect-text: present but WRONG - altered wording, numbers, operators, cross-references',
      '  - misplaced-content: present but in the wrong place (wrong section, wrong order, a label attached to the',
      '    previous item, a sub-item detached from its parent)',
      '  - missing-heading: a heading absent, or demoted to bold text, or at the wrong level',
      '  - ocr-error: a character-level error that changes meaning',
      '  - summarised-not-transcribed: the markdown DESCRIBES the page instead of reproducing it - third-person',
      '    prose ("the introduction explains that...", "the page notes that..."), a bulleted digest, or any',
      '    transcription-tool meta-text left in the file. Report this as NORMATIVE severity whenever the page',
      '    carries normative content. It is the worst class here: a summary READS like content, so nothing',
      '    surfaces the hole. One confirmed instance (page 28) destroyed the sentence naming which annexes are',
      '    normative and sat undetected until this sweep. If you see a page that talks ABOUT the standard rather',
      '    than BEING it, say so plainly.',
      '',
      'WHAT IS NOT A DISCREPANCY - do not report these:',
      '  - The page-break furniture the markdown adds: the page anchor, the "## Page N" heading, the running',
      '    header "ISO/IEC 1989:2023 (E)", the "---" separators, the copyright footer, the "Licensed to ..." line.',
      '    That is deliberate navigational scaffolding. The markdown is the PDF text PLUS scaffolding.',
      '  - Reflowed line breaks or hyphenation differences from PDF justification.',
      '  - Markdown formatting choices that preserve meaning (LaTeX vs ASCII art is fine IF no information is lost).',
      '',
      'Quote both sources VERBATIM in pdf_says / markdown_says so a verifier can check you without re-reading the',
      'page. If a page is clean, report nothing for it but still list it in pages_checked. Reporting a non-issue',
      'is worse than missing one - a false finding sends the next session to "fix" correct text.',
      '',
      'STEP 3 - PERSIST BEFORE YOU RETURN. This is MANDATORY, not a nicety. Use the Write tool to save your',
      'complete findings as JSON to:',
      '  E:/COBOL/docs/rearchitecture/spec-reconciliation/compare-p' + batch[0] + '-p' + batch[batch.length - 1] + '.json',
      'Shape: {"batch": ' + JSON.stringify(batch) + ', "pages_checked": [...], "findings": [ ...same objects you return... ]}',
      'Write it EVEN IF you found nothing (findings: []) - a file proving a batch was swept clean is evidence;',
      'its absence is indistinguishable from work that never ran. The run may be interrupted by a rate or session',
      'limit at any moment, and your file is then the ONLY durable record of this work. Write it, then return.',
    ].join('\n'),
    { label: 'compare:p' + batch[0] + '-' + batch[batch.length - 1], phase: 'Compare', schema: FINDINGS }
  ),
  (res, batch, i) => {
    if (!res || !res.findings || res.findings.length === 0) return []
    return parallel(res.findings.map((f, vi) => () =>
      agent(
        [
          'Adversarially VERIFY a claimed discrepancy between the ISO PDF and its markdown transcription.',
          '',
          'CLAIM - page ' + f.page + ', kind=' + f.kind + ', severity=' + f.severity,
          '  PDF allegedly says      : ' + f.pdf_says,
          '  Markdown allegedly says : ' + f.markdown_says,
          '  Why it allegedly matters: ' + f.why_it_matters,
          '',
          'Check it yourself:',
          '  cd E:/COBOL && python scripts/spec/page_workunit.py ' + f.page + ' --out E:/Temp/specrecon/v' + f.page + ' --dpi 300',
          'Then Read E:/Temp/specrecon/v' + f.page + '/spec_p' + f.page + '.png and E:/Temp/specrecon/v' + f.page + '/page-' + f.page + '.md.',
          '',
          'Your job is to REFUTE. Default to real=false when uncertain. Reject the claim if:',
          '  - the "missing" text is actually present (possibly reflowed or on an adjacent line)',
          '  - the difference is only page-break furniture, line wrapping, hyphenation, or formatting',
          '  - the claim misquotes either source',
          '  - the difference does not change meaning',
          'For a claimed lost choice indicator, look at the printed glyphs yourself and say what you see.',
          'Set real=true ONLY if you independently confirm a genuine content discrepancy. If the finding is real',
          'but was described inaccurately, set real=true and give the corrected description.',
          '',
          'PERSIST BEFORE YOU RETURN - MANDATORY. Use the Write tool to save your verdict as JSON to:',
          '  E:/COBOL/docs/rearchitecture/spec-reconciliation/verify-p' + f.page + '-' + f.kind + '-' + vi + '.json',
          'Shape: {"page": ' + f.page + ', "kind": "' + f.kind + '", "severity": "' + f.severity + '",',
          '        "claim": {"pdf_says": ..., "markdown_says": ..., "why_it_matters": ...},',
          '        "verdict": {"real": true|false, "reasoning": ..., "corrected_description": ...}}',
          'Include the claim so the file stands alone. A verdict that exists only in a return value is lost the',
          'moment the run is interrupted, and re-verifying costs another full page render and read.',
        ].join('\n'),
        { label: 'verify:p' + f.page, phase: 'Verify', schema: VERDICT }
      ).then(v => Object.assign({}, f, { verdict: v }))
    ))
  }
)

const flat = results.filter(Boolean).flat().filter(Boolean)
const confirmed = flat.filter(f => f.verdict && f.verdict.real)
const refuted = flat.filter(f => f.verdict && !f.verdict.real)

log('claimed ' + flat.length + ' - confirmed ' + confirmed.length + ' - refuted ' + refuted.length)

return {
  pages_swept: PAGES.length,
  claimed: flat.length,
  confirmed: confirmed.length,
  refuted: refuted.length,
  by_severity: {
    normative: confirmed.filter(f => f.severity === 'normative').length,
    structural: confirmed.filter(f => f.severity === 'structural').length,
    cosmetic: confirmed.filter(f => f.severity === 'cosmetic').length,
  },
  confirmed_findings: confirmed.map(f => ({
    page: f.page, kind: f.kind, severity: f.severity,
    pdf_says: f.pdf_says, markdown_says: f.markdown_says,
    why_it_matters: f.why_it_matters, suggested_repair: f.suggested_repair,
    verifier: f.verdict.corrected_description || f.verdict.reasoning,
  })),
  refuted_summary: refuted.map(f => ({ page: f.page, kind: f.kind, why_refuted: f.verdict.reasoning })),
}

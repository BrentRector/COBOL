## Entry 1026 — 2026-07-26 17:12 PDT — Owner: "validate against the PDF, not the markdown." Four more extractor bugs, and the denominator settles at 3,247

Entry 1024 shipped a denominator built entirely from `specs/ISO_COBOL.md` and validated only against ITSELF. The
owner rejected that basis: *"This should be validated against the ISO spec PDF, not the markdown, which we
previously found slightly corrupted compared to the PDF due to page breaks."* Correct — and the correction found
four distinct bugs, two of which INFLATED the count, which is the more dangerous direction because inflation reads
as thoroughness.

**First, the prior art.** DEVLOG 932 repaired 151 mid-sentence page-break splits to a fixpoint. But that repair
MOVED the page block to after the completed sentence and deliberately PRESERVED the anchors, which are load-bearing
for `render-spec-page.py`. So the furniture still sits between paragraphs — including between the rules of a block.
The markdown is not, and was never intended to be, byte-identical to the PDF: it is the PDF's text plus
navigational scaffolding. What must be identical is the NORMATIVE CONTENT, and that is what needed checking.

**Bug 1 — 15 rule blocks never seen (undercount).** The standard writes "Syntax rule" SINGULAR when a block holds
exactly one (§10.6.3, §11.9.4, §13.4.3 …). My kind map had only the plurals. The empty-block check could not
possibly catch this: a heading that is never RECOGNISED is never counted as seen. What caught it was a new
completeness critic the design doc had asked for all along — cross-checking against the spec's OWN table of
contents, an independent witness of what the printed standard contains. It is now a FATAL check.

**Bug 2 — a runaway block (inflation to 3,949).** Fixing bug 1 made the count leap, which was wrong in the other
direction. §16.2.2.2 is the last rule block in the standard's body, and a block was only terminated by another
NUMBERED heading — so it ran straight through the un-numbered annex headings, turning every "N." line in Annexes
A–F into a phantom rule. 601 fakes in one block.

**Bug 3 — over-termination (undercount to 2,314; 896 rules lost).** Making ANY heading close a block broke on the
running header `# ISO/IEC 1989:2023 (E)`, which IS emitted as a heading and appears INSIDE rule blocks at every
page boundary. Terminating there truncates every block that spans a page break — precisely the corruption the
owner was pointing at, and I had briefly made the extractor maximally sensitive to it. Page furniture must be
SKIPPED, not treated as a boundary.

**Bug 4 — 592 contaminated rules (18%).** With furniture skipped only when it looked like a heading, the bold and
bare forms of the running header, the `---` separators, the anchors and the licence footer were being ABSORBED AS
RULE TEXT. `GR-7.3.12.4-5` read: *"If the UPON phrase is specified: --- --- &lt;a id="page-95"&gt;&lt;/a&gt;
**ISO/IEC …"*. A rule whose text is contaminated is worse than a missing one — it reads as authoritative and would
be cited that way.

**Then the actual PDF validation, which found two more.** Rendering page 95 at 300 dpi and comparing:

- The repaired `GR-7.3.12.4-5` now matches the printed rule verbatim, sub-items and all. ✓
- **`GR-7.3.12.4-6` was recorded on page 96; the PDF prints it on 95.** `flush()` runs when the NEXT ordinal or
  heading arrives, by which time a `## Page N` marker may already have advanced the counter — so the last rule of
  every block ending near a page boundary was mis-attributed. The page is now captured when the rule STARTS.
- **A `section-` anchor had leaked into rule 6's text** — the furniture filter covered `page-` anchors only.

**Independent second sample.** §14.9.24.3 (MERGE syntax rules) against rendered page 688: the PDF prints rules
5–13 ending at 13; the catalog holds exactly 13 rules across pages 687–688, text verbatim, page attribution
correct. Rules 5, 6, 7, 8 and 13 checked word for word.

**THE DENOMINATOR IS 3,247** — 1338 SR · 1470 GR · 216 AR · 223 RV. It moved 2,974 → 3,210 → 3,949 → 2,314 →
3,247 across this work, and only the last figure has been checked against the canonical PDF rather than against
the transcription that produced it.

**The lesson, stated plainly because it is now the fourth instance today.** Every one of these bugs produced a
clean-looking run. The extractor reported "no parse gaps" while missing 15 blocks, while inventing 601 rules, and
while contaminating 592. Self-consistency is not validation — a parser checked only against the artifact it parses
will confirm its own mistakes. What broke each of them open was an INDEPENDENT witness: the spec's TOC, and the
PDF itself. The owner was right to refuse the markdown-only basis.

`invent : 3247 rows · 3247 GAP`. No verdicts existed yet, so the re-key was free; after Phase B it would not have
been.


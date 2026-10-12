## Entry 1076 — 2026-07-28 13:41 PDT — The standard's rule labels were being parsed as Markdown lists, and it was falsifying a normative rule

Owner reported that `>>IF` in §7.3.16.3 rendered as a blue vertical bar, then corrected my framing with the
sentence that turned the whole thing around: **"It's not even a list in the original PDF."** He was right, and
the list-ness was the root cause of the bar rather than an unrelated cosmetic issue.

**What was actually happening.** Every top-level rule is written `1) …` at column 0. That is a CommonMark
ordered-list marker, so all 4,161 of them were list items — and a list item's content is re-parsed as BLOCK
syntax. Three consequences, in increasing severity:

- The marker changes. `<ol>` has no per-item delimiter, so the printed `1)` renders `1.` — and the standard
  prints `1.` for its THIRD level. A top-level rule and a third-level sub-item became indistinguishable.
- The document contradicted itself. `a)` is not a Markdown marker, so the SECOND level always rendered as the
  literal `a)` it is printed as. One structure, two renderings, in the same clause.
- ⛔ **Content was destroyed, and in one place falsified.** Eight rules open with `>`, which opens a blockquote
  and is EATEN. Six are the compiler directives the owner spotted. The other two are in §8.7.5.2, and nobody
  had noticed them: rule 3 rendered as "is an abbreviation for GREATER THAN." with the operator gone, and rule
  9 — `>= is an abbreviation for GREATER THAN OR EQUAL TO.` — rendered as **"= is an abbreviation for GREATER
  THAN OR EQUAL TO."** A false normative statement about `=`, sitting in the published document.

**Why de-listing rather than escaping the eight `>`.** I swept the whole document for every way list-item
content gets re-parsed — `>`, `#`, `-`/`*`/`+`, `N.`, fences, pipes — and `>` is the ONLY one that bites. So
escaping eight characters would have fixed the corruption and left the marker wrong on 4,161 lines; de-listing
fixes both, and makes level 1 consistent with level 2's existing (correct) treatment.

**The consequence I nearly shipped.** Removing the parent list exposes two shapes that only survived because
they sat INSIDE a list item: sub-rules indented 4 spaces (17) and sub-items indented 6 (93). At 4+ spaces
outside a list, Markdown reads a CODE BLOCK. The first candidate rendering did exactly that — the level-3
items came out in monospace — which is why the repair also de-indents them to 3. Found by rendering the
candidate, not by reasoning about it.

**Evidence, throughout.** Pandoc is on PATH, so every claim here is a rendered artifact rather than a reading
of the spec for CommonMark: the `<blockquote><blockquote>` proving the bar, the `<pre><code>` proving the
code-block regression, and the final three-region render showing `1)`, `a)` and `1.` all landing as printed.
Printed pages 61 and 105 confirm the three-level marker scheme.

**Guards.** New lint check RULE LABEL covers all three shapes; it reports **4,277 on the pre-fix revision and
0 now** — the same 4,277 the repair changed. The repair itself asserts word conservation (409,313 words,
backslashes normalised on both sides) and refuses to write if a single word moves; that check fired on my
first run, correctly, because my tokenizer split on the new backslash.

**One sibling fixed in the same change set.** `extract_rule_catalog.py` matches `^(\d+)[.)]` to find rules —
it is the DENOMINATOR of the P14 traceability inventory (1,338 SR + 1,470 GR). Left alone it would have
matched nothing and reported a clause as ruleless rather than as broken. Its regex now takes both forms. The
script is still halted on the page-keying issue; this only ensures it is correct when re-keyed.

Still outstanding from the same sweep, next commit: §8.9's reserved-word list, where the special-character
words are destroyed outright — `+` renders as an empty bullet, `>` and `>>` vanish, and `<`/`<>` become an
`<h1>` because the `=` beneath them parses as a setext underline. `gen-reserved-words.ps1`, which reads that
list to build `ReservedWords.Table.cs`, is ALREADY broken for an unrelated reason: it looks for `^##\s+8\.9`
and the heading is now `### 8.9`, so it throws rather than extracting.


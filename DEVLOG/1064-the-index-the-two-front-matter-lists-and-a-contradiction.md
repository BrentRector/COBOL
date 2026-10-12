## Entry 1064 — 2026-07-28 00:38 PDT — The index, the two front-matter lists, and a contradiction between two docs

Asked what remained in the transcription, I checked the file rather than answering from memory, and the answer
was worse than expected: the largest single structure in the document had never been written as Markdown at all.

**The index was 3,123 lines with 30 list markers in it.** Markdown joins consecutive lines into one paragraph,
so every letter section rendered as one run-on block — 2,446 terms and their sub-entries flowing together. The
links inside it were all fine. The structure around them simply was not there. Same defect as the table of
contents, at forty times the size.

Converting it was straightforward; getting the LEVELS right was not. 410 sub-entries were indented with
`&nbsp;` entities — a rendering hack for something a list expresses natively — but another 723 had lost their
indentation entirely and sat at column 0, indistinguishable from terms. The index is alphabetical and sectioned
by letter, so most are recoverable by inference: inside `### B`, an entry not beginning with B cannot be a
top-level term, which is how `COMPUTE statement`, `EVALUATE statement` and `Parenthesis in` give themselves away
as sub-entries of `Boolean expressions`.

That inference is right 2,485 times out of 2,503, and **structurally blind to the rest**. A sub-entry labelled
`Definition` sitting in section D begins with D. Nothing about the letter can tell. `Definition` appears 75
times in the printed index and is a sub-entry every one of them.

So I measured the page instead. A printed sub-entry is set about 18 pt right of its term; the index is in two
columns, so a level is "which of my column's two edges am I at", never an absolute x — grouping by y alone
merged the left and right columns of each line and made 2,843 of 2,856 entries unpairable on the first run.
`measure_index_levels.py` writes the result to `data/index-levels.json`, which is COMMITTED so `relist_index.py`
still works in the public repo where the licensed PDF is absent. Only entries whose every printed instance
carries the same level are recorded — 1,632 of 1,833 distinct texts — because the entry text cannot say WHICH
printed line it came from, and the ambiguous ones fall back to the inference. Measured and inferred now agree
**2,506 out of 2,506, zero disagreements.**

Two things fell out of doing it. `>`, `>=` and `>>` are index TERMS in the Symbols section — the relation
operators and the directive indicator — and my pass was treating any line starting with `>` as a blockquote,
rendering three symbols as quoted text and orphaning their sub-entries. **This is the second time the
`>`-is-not-a-blockquote assumption has cost me**; the first was `>>POP` being re-emitted as a figure note during
the sweep. An index of punctuation necessarily contains terms that ARE Markdown syntax, so `*`, `**` and the
`>` family are escaped now. Word conservation gated the whole pass at 19,577 words in and out.

**The list of figures was worse than unhelpful — it was wrong.** Its entries linked `D.11` to `#section-d-11`,
which is CLAUSE D.11 "Character sets", not Figure D.11. Figure numbering and clause numbering are independent
sequences that happen to share the annex letter, and whatever generated those links matched on the number
alone. Twelve of fifteen entries pointed at the wrong place, three were not links at all, and two printed
folios sat orphaned on their own lines. The Tables list still carried a printed `Page` column.

Both are GENERATED from the body captions now. That is the actual fix: a list maintained beside the document
drifts from it, and this one had drifted into confident wrongness. 39 captions anchored, and the lists are
derived from those anchors.

Two more split tables surfaced while doing it, both hidden because the ANNEX captions omit the em dash the
earlier merge keyed on. **Table A.1's joint is the worst variant yet**: the second piece repeats the caption AND
the sentence beneath it, then promotes a genuine data row — `| START | any | no | no |` — to a header with its
own separator, purely because it fell at the top of a printed page. Merging had to KEEP that row while deleting
the separator under it, the opposite of every other joint.

**Then a wrong link of my own, caught before it shipped.** 57 index references were still bare folios, so I
mapped folio → clause from the PDF. The first attempt produced `Boolean character [5](#section-7)` while
`Character-string 6` mapped to clause 3 — two adjacent pages in different clauses, which cannot both be right.
Two bugs: the scan included the FRONT MATTER, whose table of contents is thousands of lines of
`heading … number`, so it harvested TOC entries as folios and the bad values landed first and stuck; and it took
the LAST heading on a page rather than the clause in effect at its top, though most pages carry no heading at
all — folios 5, 6 and 16 are all deep inside clause 3. I reverted it. **A wrong link is worse than a plain
number**: it misdirects silently, which is exactly the defect I had just finished fixing in the figures list.
Redone correctly, all 48 resolve, and the index note now says every reference is a link rather than describing
leftovers that no longer exist.

**A contradiction between two docs, resolved by checking rather than by picking.** Plan §0 said Batch 1's outer
brackets and the TOC/anchor findings were OUTSTANDING; DEVLOG 1062 said the ledger closed at 210/210. Both were
written in good faith. The reconciliation ledger tracks findings but not repair status, so neither doc could be
settled from it — I checked the artifacts. CURRENCY SIGN reads `[ CURRENCY SIGN IS literal-7 [ WITH PICTURE
SYMBOL literal-8 ] ] …` with both brackets present, and `sweep_figures --check` is clean; the TOC findings were
about `#page-NNN` links with a +30 folio offset, and there are no page anchors any more. **Those batches were
closed by a CHANGE OF MECHANISM rather than item by item** — figures are generated from measured geometry, and
pages are gone — which is exactly why they read as unworked. REPAIR-PLAN.md's own note had anticipated it,
saying to sequence the anchor repair after the de-paging batch and decide whether those links survive at all.
They did not. Both docs now say so.

The lint gained RUN-ON LIST for this whole class: four or more consecutive link-per-line entries with no list
marker. Clean on HEAD, **231 regions on the revision before this work** — and the lint's total there is now 767.

Gates: lint clean · figures 484/484 · publishable at 47,801 lines · acknowledgment verbatim · 3,811 internal
links, zero dangling.


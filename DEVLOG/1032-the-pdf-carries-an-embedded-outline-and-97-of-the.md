## Entry 1032 — 2026-07-26 22:05 PDT — The PDF carries an embedded outline, and 97% of the transcription's headings were at the wrong depth

I was about to settle the heading-depth question the wrong way. Having established that the printed body does not
distinguish heading levels typographically, and that the printed Contents pages carry no indentation either, I
measured which level was most COMMON in the transcription and prepared to normalise toward that. That would have
enshrined the majority error as the standard.

The owner asked whether the level could come from the ToC. It can — not from the printed Contents page, but from
the **PDF's embedded outline**: 2,090 entries with explicit levels 1–5, the document's own machine-readable
statement of its structure. It was there the whole time, and I had already used `get_toc()` earlier in this project
without registering what it implied.

**The measurement reframed the batch.** Of 1,724 markdown headings matched to an outline entry, **59 sat at the
right depth — 3%.** The markdown was essentially flat while the standard nests five deep. So the 19 heading
findings the sweep confirmed were symptoms of one systemic defect, and their verifiers had normalised each to its
LOCAL siblings — which is why they proposed `###` for §10.6.3 where the outline says `####`. Local consistency is
not depth, and a sweep that only fixed the 19 would have left ~2,020 instances of the same defect in place.

**Two decisions worth recording.** The mapping is outline level + 1, because `#` is taken by the transcription's
running header, so levels 1–5 fill `##`–`######` exactly — asserted rather than assumed, since silent truncation
at level 6 would be the classic failure. And coverage extends past the outline: it omits 374 leaves, and repairing
§15.7 while leaving §15.7.1 behind would swap one inconsistency for another. That is safe only because outline
level EQUALS dotted depth for all 1,728 numbered entries — verified, zero exceptions — so depth is a proven proxy.
The script asserts that equality on every matched heading rather than trusting today's measurement to survive a
future spec revision.

**2,098 headings processed · 2,039 rewritten · 0 residual on a re-run.** Gates: `--check` 0, anchors 1261, catalog
3,790. The unchanged catalog is the meaningful one — 2,039 heading rewrites moved no rule, which is exactly what a
stable denominator is for, and it confirms the extractor is level-agnostic as the p475 verifier predicted.

**The lesson is about where I looked.** Twice today the authority was a machine-readable artifact I had not thought
to ask: the spec's own TOC caught 15 unrecognised rule blocks, and now the PDF outline settles a question I was
about to answer by counting. Both times my instinct was to derive truth from the corpus I already had, and both
times the document could simply be asked. "What does the source say" beats "what is most common in my copy of it"
— which is the same lesson as the whole reconciliation, one level up.


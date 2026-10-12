## Entry 1036 — 2026-07-26 23:14 PDT — The spec PDF was never obfuscated; it was missing a CMap, and now it greps

The ISO PDF's text layer extracted as Greek-looking garbage, and we recorded that as **deliberate obfuscation**.
Everything downstream followed from it: `render-spec-page.py`, the "render the page and squint at it" rule for
diagrams, and the reliance on agent verification for figure questions that should always have been mechanical.

**The diagnosis was wrong, and the error was assuming malice where there was only an omission.** The font
dictionary is plain about it:

    << /BaseFont /MLLGNI+Cambria  /Encoding /Identity-H  /Subtype /Type0  /Type /Font >>

**No `/ToUnicode` entry at all** — in 16 of the 26 fonts. Under `Identity-H` the character codes in the content
stream ARE glyph indices, and `/ToUnicode` is the thing that says which character each index means. With none
present every extractor emits the raw glyph index as a codepoint, and indices in the 0x300–0x400 range print as
Greek. Nothing was scrambled or encrypted. The mapping was simply never written down — a defective export.

**There is no cipher to document, which is worth stating because the data invites one.** The first look showed
uppercase at `GID = ord(c)-'A'+4`, lowercase at `0x83+`, digits at `0x372+d`. Real, but coincidental: glyph order
is assigned per font by the subsetter. In `MLLGNI+Cambria` SPACE is GID `0x002` and punctuation is scattered
(`,`=0x1E1, `:`=0x1E3, `.`=0x1E4, `-`=0x1E6, `+`=0x3AA). The mapping must be recovered per font.

**Recovered by geometry — deliberately NOT from our own transcription**, which is the artifact under repair and
cannot be its own reference. Subsetting copies glyph OUTLINES verbatim, so an outline is an exact key into the
stock Windows font, which does have a `cmap`: subset GID → outline → stock glyph → Unicode.

**Blank glyphs were the trap.** A SPACE has no outline, so shape matching structurally cannot map it; the first
build ran every word together — `TheCOMPUTEstatementassigns`. Blanks are matched on ADVANCE WIDTH instead. That
was caught by the verification, not by reading the output, which looked plausible.

**Coverage is not proof, so the check is built to fail — and it failed twice.** Once on the missing spaces, once
on a naive substring test that did not allow for the cover setting "INTERNATIONAL STANDARD" across two lines.
That second failure was my test being wrong, not the decoder; worth distinguishing rather than "fixing" the
decoder to satisfy a bad assertion. Two further guards on the replacement: **53 pages rendered at 110 dpi and
compared by pixel-buffer SHA-256, zero differences**; and an **incremental save**, so the publisher's bytes are
copied verbatim and only new objects appended — a full re-serialisation doubles the file (9.4 → 18.7 MB), the
delta is 34 KB.

**What it unlocks is the point.** General-format figures now extract as text WITH coordinates. Combined with
`figure_geometry.py` measuring the delimiter rectangles on the same page, a printed general format can be
reconstructed mechanically — words from the text layer, brackets, bars and underlining from the geometry. The
1,659-item grammar↔spec audit stops being agent judgement and becomes verification that can fail.

New: `scripts/spec/pdf_deobfuscate.py`, `docs/rearchitecture/spec-reconciliation/PDF-TEXT-LAYER.md`. The
`reference_spec_pdf` memory said the opposite and has been rewritten.


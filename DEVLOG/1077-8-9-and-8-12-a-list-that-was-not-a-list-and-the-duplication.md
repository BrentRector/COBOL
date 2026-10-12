## Entry 1077 — 2026-07-28 14:01 PDT — §8.9 and §8.12: a list that was not a list, and the duplication my own repair nearly shipped

Continues the sweep from entry 1076. Same root class — Markdown re-parsing the standard's notation as its
syntax — in the two word lists.

**§8.9's 409 reserved words had no list markers**, so Markdown joined them into run-on paragraphs. The
existing RUN-ON LIST check could not see it: that check keys on a cross-reference link per line, and a word
list has none. They are `- WORD` items now, as the TOC, index and figure lists already are.

**The fifteen special-character words were destroyed outright**, each differently: `+` became an EMPTY BULLET
(it is a bullet marker), `>` and `>>` VANISHED into blockquotes, `>=` rendered as `=`, and `<`/`<>` became an
`<h1>` because the `=` on the next line parsed as a setext heading underline. All fifteen are inline code now
— which is what they are, a character sequence rather than prose, and immune to re-parsing without escape soup.

**One transcription defect repaired:** the subtract operator was an EN DASH. The PDF text layer returns U+FFFD
there (an unmapped glyph), so U+2013 was a guess; the printed glyph is the arithmetic minus, no COBOL program
contains U+2013, and the equivalent list on folio 215 extracts a plain `-`. It had already forced a downstream
workaround — `gen-reserved-words.ps1` carries `.Replace([char]0x2013, '-')`.

**⚠ THE MISTAKE, AND IT IS THE ENTRY'S POINT.** Fixing `gen-reserved-words.ps1` (see below) let me diff §8.9
against `ReservedWords.Table.cs`: 410 words flagged ISO-2023, section yielding 392. Eighteen real COBOL words
named — OCCURS, OF, THAN, six END-* verbs. An independent extraction of the printed pages named the SAME 18.
Two methods agreeing, so I wrote a repair that INSERTED them. **They were never missing.** They sat collapsed
six-to-a-line on three run-together lines — a printed COLUMN read across instead of down — which my
entry-counting saw as absent. The insert duplicated all eighteen. Caught by the OWNER reading the applied
diff, not by me.

The tell I ignored: `reserved_word_list.py`, written twenty minutes earlier, has a word-conservation check and
fired correctly on its first run. I did not put one in the gaps script — the one script whose whole premise
was "words are missing" got no check that words were conserved. It has one now, and with it the repair is a
SPLIT, not an insert. §8.9 now equals the printed page exactly, 409 = 409, gated by
`reserved_word_gaps.py --check`.

**§8.12 (compiler-directive words) had two words split across table rows** — `COMPILE-TIME-` /
`ARITHMETIC-EXPRESSIONS` and `EXTERNAL-FILE-FILE-` / `STATUS`, each one word wrapped across two printed lines.
Proved two ways: same x, one line pitch apart on the page; and ALPHABETICAL POSITION — a standalone `STATUS`
between `EXCLUSIVE-OR` and `FIXED` is impossible, while `EXTERNAL-FILE-FILE-STATUS` is exactly in order.
`COMPILE-TIME-ARITHMETIC-EXPRESSIONS` appears whole in three other places in the document.

**`gen-reserved-words.ps1` was ALREADY BROKEN, independently.** It locates §8.9 with `^##\s+8\.9` and the
heading became `### 8.9` when heading depths were normalised, so it threw on every run rather than building
`ReservedWords.Table.cs`. Fixed (`#{2,}`), plus bullet/code-fence stripping for the new format. It now
extracts 409 including `I-O` and `I-O-CONTROL`.

**The `>>IF` vs `>> IF` question — I was wrong to file it as an ISO defect, and the SPEC says so.** The owner
noticed the general formats set `>> IF` while the syntax rules write `>>IF`, and asked what other compilers
do. The standard answers it directly and in two places: §7.3 SR5 — "the compiler directive indicator,
OPTIONALLY FOLLOWED BY the COBOL character space … shall be treated as though it were followed by a space if
no space is specified" — and the floating-indicator table, "`>>` … with or without an intervening space".
BOTH SPELLINGS ARE LEGAL; the standard is not inconsistent, it is exercising its own stated option, and the
transcription reproduces each occurrence as printed (29 spaced, 42 glued). I had already drafted a
TRANSCRIPTION-STATE row calling it an ISO defect; that row is gone. Our compiler is correct — all seven
recognizers in `Frontend/Preprocessor/` slice `>>` then `TrimStart()` (or match `>>\s*`). Per the owner, the
transcription now carries a marked editorial note at §7.3 so a reader meeting both forms is not confused.

**Gates:** lint CLEAN · sweep 484/484 · publishable 47,195 lines · acknowledgment verbatim · §8.9 == printed.


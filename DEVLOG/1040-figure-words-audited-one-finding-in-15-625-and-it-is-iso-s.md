## Entry 1040 — 2026-07-27 00:24 PDT — Figure words audited: one finding in 15,625, and it is ISO's typo, not ours

Swept the last big mechanical class — the WORDS of every printed general format against the transcription. This
was the one I most expected to find damage in, because `ISO_COBOL.md` was produced by sending each page IMAGE to
a vision model, and a dense general format is exactly what such a model guesses at.

**820 pages, 15,625 figure tokens, ONE discrepancy** — and it is a defect in the standard:

- **p315** prints `locae-name-1` for `locale-name-1`. The transcription had silently corrected it; the
  correction is now visible and attributed instead of invisible.
- **p236** — found along the way — the reserved-word list prints **`EMD-START`** for `END-START`. Left AS
  PRINTED per the standing rule, with a flag. A compiler generated naively from that list would reserve
  `EMD-START` and fail to reserve `END-START`.

**⛔ Every other apparent finding was a bug in MY CHECKER, and there were three of them.** The audit began at 76
findings across 41 pages and ended at 1. In order:

1. `<[^>]+>` to strip HTML tags — the negated class matches newlines, so in a table containing `<=` and `>=` it
   swallowed everything between them. That invented "PUSH and READ-PREVIOUS are missing from the
   compiler-directive word table". 16 false findings.
2. Comparing each page in isolation, when content legitimately drifts across a page boundary — general rules
   that span a break are merged into the preceding page, and the column-wrapped reserved-word lists attribute
   their last entries to the previous page. That invented "OBJECT, OBJECT-COMPUTER, OCCURS, OF are missing from
   page 237" when all four sit two lines above its anchor. 51 false findings.
3. Restricting the tag strip to one line still ate `< left-part OR selection-subject >` and the `<blank>` /
   `<Control Heading lines>` placeholders of the report-layout figures — angle brackets are ordinary characters
   in this document. The tag names are now spelled out. 8 false findings.

**That is the honest headline of the last two sessions: my checkers have been buggier than the transcription.**
Underlining came back 0/2,215 after the detector accused two correct pages; figure words came back 1/15,625 after
three separate false-finding classes. Every one was caught by checking a finding against raw evidence before
acting on it, which is now the written rule in the repair plan — and each would have damaged the reference
document had I trusted the tool.

The picture that leaves: the OCR'd transcription is far better than the 210-item defect list implied. What was
actually wrong is concentrated in NOTATION (choice indicators, underlining claims) and in editorial matter the
vision model INVENTED — figure notes asserting things the page does not say, including a fabricated notation
rule. Not in the words.

Gates: `--check` 0 · anchors 1261 · catalog 3,790.


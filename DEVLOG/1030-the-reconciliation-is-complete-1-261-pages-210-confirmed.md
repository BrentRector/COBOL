## Entry 1030 — 2026-07-26 20:24 PDT — The reconciliation is COMPLETE: 1,261 pages, 210 confirmed defects, 41 of them normative and every one falsely restrictive

The owner directed a full PDF-vs-markdown reconciliation after four rule-catalog bugs traced back to transcription
artefacts. It is done. **Every printed page of ISO/IEC 1989:2023 has been compared against `specs/ISO_COBOL.md`,
and every claim has an adversarial verdict.**

    1261 / 1261 pages · 346 claims · 210 CONFIRMED · 136 refuted · 0 unverified
    41 normative · 123 structural · 46 cosmetic

Cost across all runs: roughly 470 agents and 30M tokens. The refutation rate held at **39%** across five
independent runs — the verification layer never degenerated into agreement, which matters because these findings
will drive grammar changes.

**Every one of the 41 normative defects is FALSELY RESTRICTIVE.** Not one makes the markdown more permissive than
the standard. That asymmetry is the finding: the transcription did not lose information randomly, it lost the
notation that GRANTS permission — choice-indicator bars, outer brackets, the absence of an underline. Three
mechanisms:

- **Misstated underlining (21 findings, 20 pages).** An underlined word is REQUIRED, a non-underlined one is
  OPTIONAL (§5.2.2/§5.2.3). Figure notes claim words are underlined that the printed page leaves plain, so legal
  omissions read as illegal. `ON` in ON SIZE ERROR is the sharpest: bare `SIZE ERROR …` is legal COBOL.
- **Lost choice-indicator bars (11 findings, 8 pages).** A bracket without bars means at most ONE alternative;
  with them, zero or more, each once, in any order (§5.2.6.4). GOBACK is the proven case.
- **Lost brackets (6 findings, 5 pages).** The bracket making a whole clause optional was dropped, so an optional
  clause reads as mandatory — CURRENCY SIGN, LOCALE, report-group USAGE.

**The defect classes nobody predicted.** Two only became visible because the comparison was page-by-page against
the source rather than a search for known problems:

- **Page 28 was not a transcription at all** — an AI summary plus a refusal message ("I'm not able to reproduce
  the full page verbatim as a complete copyrighted standards document"), committed as though it were the standard.
  The refusal was baseless: the owner is licensed, and *the page it refused is the page granting permission* — the
  Introduction permits reproducing the standard in whole or in part provided the acknowledgment paragraphs are
  carried. It destroyed the sentence naming which annexes are NORMATIVE, which D13 depends on. Repaired verbatim,
  and a meta-text critic now fails `--check` on any recurrence.
- **A duplicated normative block.** An entire printed page's content appears twice, with four section anchors
  emitted twice. Because the stray copy comes first in file order, every intra-document link and every
  heading-derived TOC entry resolves to the wrong one — and a grep-based citation workflow gets two hits for one
  rule with no way to tell which is canonical. That is exactly how the `spec-lookup` skill works.

**Verification quality, since it is the thing that makes 210 actionable rather than 346 suspicious.** Verifiers
measured underline rules in pixels ("a 2-px rule 14 px below the OFF baseline"; "no gap inside either printed
digit run exceeds 8 px against 15–17 px for a real word-space"), corrected finders' section citations, caught
under-scoped claims (the PI constant is broken in TWO literals at an identical digit offset — the signature of a
fixed-width column split, so the fix must sweep other long literals rather than patch two lines), and repeatedly
accepted a finding while REJECTING its stated consequence. On the compilation-group ellipsis one demonstrated that
both nestings collapse to the same language and downgraded the severity accordingly.

**Tracker §C is published** — generated from the ledger, checkboxed by DEFECT FAMILY rather than per finding,
because 210 rows would drift and because the families are the actual unit of repair: the seven ON/OFF directive
notes must be corrected together or the inconsistency merely moves.

**One tooling gap closed at the boundary.** `tracker_section.py` treated "final" as one condition — every page
swept — which says the SEARCH is complete but nothing about whether each claim is ADJUDICATED. At the pause the
ledger was exactly in that state (1261/1261 swept, 83 unverified), so the old logic would have published 83
hypotheses as confirmed work items. Now two independent conditions with a distinct message for each. Same class as
every other silent-green problem in this work: a check that could only fail one way, so the other way looked like
success.

**Next**, in order: repairs by family, then the grammar↔spec audit (1,659 items — 321 general formats and 1,338
syntax rules) with the PRINTED page as authority. A diagram defect is TWO items, and GOBACK already proves the
second: the grammar encodes `(raisingPhrase | statusPhrase)?` and rejects legal source today.


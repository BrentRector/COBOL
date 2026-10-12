## Entry 1237 — 2026-08-08 17:06 PDT — R34: the citation the note demanded flipped its premise — GR10 forbids REPLACING over nested COPY, and COBOLNET1640 now says so

The R34 note asserted ISO requires COPY REPLACING to reach text a nested COPY brings in, and its own
closing instruction — "cite §7.2.3's operative GR before fixing" — killed the claim: §7.2.3.4 GR10
reads "If the REPLACING phrase is specified, the library text shall not contain a COPY statement", and
GR12 permits nesting (at least 5 levels) only WITHOUT replacing. GnuCOBOL's "COPY: recursive
replacement" testcase is a GnuCOBOL extension over ISO-illegal source; the premise had been read off
the oracle, not the standard — validate_the_premise_not_only_the_rule, working exactly as written.

The real defect, measured with four probes: the preprocessor recursed into the nested copybook OUTSIDE
the replacement scope, so the illegal combination expanded to arbitrary partial text and failed as a
misleading downstream COBOLNET1639 on whatever name never materialized — the right verdict for the
wrong reason. COBOLNET1640 (copy-replacing-nested-copy) now fires in CopyProcessor.ResolveOneCopy at
the outer COPY's line, citing GR10 verbatim, detected with the SAME FindCopyKeyword the expander
splices by so the report and the recursion cannot disagree. Expansion continues after the report; the
legal shapes — nested ×2, nested ×5 (GR12's floor), flat REPLACING — measured clean before and after.
rejects_legal_source corrected to false in the note; the GnuCOBOL semantics stay a vendor-axis
candidate.

Tests: CopyReplacingNestedCopyTests (4 facts) + golden copy_nested_five_levels (the exact five-level
chain GR12 names, sibling copybooks). DIAGNOSTICS.md regenerated (+1640). Gate: Unit copy/diagnostic
set 17/17 · full Corpus 503/503 · the golden green by name · characterization 33/33.


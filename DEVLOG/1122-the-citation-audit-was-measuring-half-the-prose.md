## Entry 1122 - 2026-07-30 00:25 PDT - The citation audit was measuring half the prose

**A ONE-LINE WIDENING, recorded because of what it found in the first second.** `audit_doc_citations.py` scanned
`docs/` plus `CLAUDE.md`. PB3 had just shown that to be half the prose: the fabricated "§12.3.7 GR7 k3" survived a
complete `docs/` sweep in **thirteen** places across five SOURCE files — including the doc comments on
`NationalCollation`, the one implementation that gets that rule right.

The justification is not "more coverage is better". **A source doc-comment carries a citation at the same density
and with the same authority as a design doc, and is read by the same person for the same reason.** Someone
checking why `Ord` places an unspecified character reads the XML comment above `Ord`, not
`COBOLNET_DESIGN.md` §…. Splitting prose by file extension had no basis; the audit's own scope was the defect.

Widened to `src/**/*.cs` and `src/**/*.g4`. It found one more immediately, of the family the plan already names:
`DataItem.cs` cited **§13.18.22** for text that is really in **§13.18.22.4 GR3** — one level short, the CA37/CA38
shape, four bare references corrected together.

Now **0 misfiled across docs and source together**, 30 verbatim citations verified.

⚠ **The residue is honest, not zero.** The audit only checks citations that pair a clause with a quoted fragment
of ≥30 characters on one line; a citation with no quote cannot be checked mechanically at all, and the broad form
that tries reported 133 defects of which essentially none was real. What this closes is one exact defect class —
the quoted text is genuinely in the standard, filed under the wrong clause — which is precisely the INHERITED
citation CLAUDE.md rule 1 is about, and precisely how a fabrication reached a Phase-B finding from our own design
doc.

**GATES.** Build clean, Unit **963/963**. Comments and one script; no behaviour change.


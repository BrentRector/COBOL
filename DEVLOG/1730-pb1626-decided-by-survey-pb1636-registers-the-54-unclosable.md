## Entry 1730 — 2026-09-27 04:37 PDT — PB1626 decided by survey; PB1636 registers the 54 unclosable rows; three DEVLOG stamps corrected

**PB1626 (owner question → decided under the standing protocol).** GnuCOBOL 3.2 classifies a `>>DEFINE … PARAMETER`
value SYNTACTICALLY (`cobc/ppparse.y`, `ppp_check_needs_quote`, read from the local GPL tarball; nothing copied): an
optional sign, digits and at most one `.` make a numeric literal of any length; a quoted value is the literal as
written; an empty value leaves the name undefined. Ours goes through `decimal.TryParse(NumberStyles.Number)`, which
makes 29-plus-digit values alphanumeric and accepts `1,000` and surrounding spaces. §7.3.11.4 GR4 leaves the form
to the implementor, so rule 1's precedence (GnuCOBOL next) decides: classify syntactically, carry the value in
`CtNumeric`. Two points must be cited before implementing (the over-long diagnostic's site, and whether an empty
value defines a name), and DOC-A.1-49 is rewritten in the same change. The note is now an implementation item, and
no owner question remains.
**PB1636 (new).** Golden lane 2's 54 test-unclosable rows were "recorded nowhere" (DEVLOG 1715), a rule-8 gap. The
note registers them through the inventory's own selector (CONFORMS and GAP, plus the one DNS row, less the 6 held),
points at the writers' reasons, and sets the PB386 route: draft 54 `DRV-` determinations with each claim re-derived,
then put the signatures to the owner as one question.
**Correction.** I estimated the headings of entries 1727–1729 (04:12, 04:45, 05:05) instead of reading the clock,
and two of them were ahead of real time. They now carry their commits' real times (04:07, 04:22, 04:32). Every
stamp comes from `date`.


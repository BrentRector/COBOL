## Entry 932 — 2026-07-20 01:30 PDT — 151 normative sentences were cut in half by page breaks; and Annex A.1's documentation obligation was never tracked

Owner instruction: *"Read the actual spec on fixed-form. I suspect the compiler simply ignores characters past
column 72."* Correct — and reading it properly overturned my own justification and exposed a v1.0 conformance gap.

**§6.3.1 says margin R is IMPLEMENTOR-DEFINED.** *"The rightmost character position of the program-text area is a
fixed position defined by the implementor."* The standard does **not** mandate column 72; characters beyond
margin R are not an error, they are simply outside the program-text area (§6.3.4 — comment-text likewise runs
only "up to margin R"). ISO 2023 has **no "identification area"** at all; that was a COBOL-85 card-image
convention I had asserted from memory in a code comment. §6.3.2 further: the sequence number area is optional and
*"may consist of any character"* — so my `seqOk` digits-or-blank test is itself a heuristic, not a spec rule. The
column-72 veto still stands, but as a DETECTION heuristic justified by OUR margin R, not as a rule of the
standard. Comment corrected to say so.

**THE GAP THAT FOUND: Annex A item 158** — *"Reference format (rightmost character position of program-text
area). This item is **required**. This item **shall be documented** in the implementor's user documentation."* We
did not document it. And it is not isolated: **A.1 lists 222 implementor-defined elements — 164 required, 26
conditionally required, 29 optional — of which 199 carry the shall-be-documented obligation**, and
`docs/CONFORMANCE.md` had **no A.1 section at all** (it covers A.3 and A.4 only). D13 defines "100% conforming" as
the mandatory core complete **plus every required implementor documentation item**, so those 199 are part of the
definition of done and nothing was tracking them. Opened `CONFORMANCE.md §7` with item 158 written in full;
registered the remaining 198 as **§11 row A11**, scheduled into P14 Step 0 (A.1 rows ARE inventory rows). Per D15
this is registered, not crammed into P13 — but it is conformance-BLOCKING, not tidiness.

**Then: "Fix the markdown where pagebreaks alter the context."** At every printed page boundary the transcription
injects `--- / <a id="page-N"> / ## Page N / # ISO/IEC 1989:2023 (E)`. Where a boundary fell MID-SENTENCE that
block landed *inside* a rule, splitting one sentence into two paragraphs with ~6 lines of furniture between them.
**151 normative sentences were broken this way** — §4.2.7 (*"the elements that are supported and those that
are"* ‖ *"not supported"*), §7.2 COPY REPLACING (*"the entire REPLACING phrase"* ‖ *"operand that precedes the
reserved word BY"*), §4.2.9, §8.x, §12.3.6, §13.x. A reader who stops at the break gets half a rule — the exact
mechanism behind the C5 PERFORM inversion that reached an owner escalation earlier in this work.

Repaired to a fixpoint over four passes (`scripts/fix_spec_pagebreaks.py`, committed). Conservative by
construction: joins only when the first half lacks terminal punctuation AND the second begins lowercase AND
neither side is a heading/anchor/table/fence/list item. **The DRY RUN caught a corrupting bug before it applied**
— the running header has three transcribed shapes (`# ISO/IEC…`, `**ISO/IEC…**`, and BARE `ISO/IEC…`), and the
first matcher missed the bare form, so on page 53 it would have spliced the header string into the middle of the
§4.2.7 conformance rule. A hard assertion now makes that class impossible to land silently. Given I had already
shipped two silent-tooling failures this session, applying unseen was not an option.

Integrity verified rather than assumed: **anchors 1261 → 1261** (they are load-bearing — `render-spec-page.py`
and the figure audit map markdown lines to PDF pages through them), A.1 still 222 items numbered 1..222, item 158
intact, zero furniture spliced into prose. Four detector hits deliberately NOT joined: the §8.9 reserved-word
LIST, whose entries legitimately lack terminal punctuation.

**Also restored A.4.3 "Commit and Rollback"** — a page break had demoted it from a `##` heading to bold text and
stripped its section anchor, which is why my first heading scan reported it "missing". Its five items were
present and correct all along; only the heading was lost. Swept for the pattern: it was the only instance.

**Correction to Entry 931's framing:** I reported "91 required-documented items" from a regex that silently failed
on entries straddling page breaks. The true figures are above (222 / 199). Same lesson as the harness: a count
produced by a pattern that can fail silently is not a measurement.


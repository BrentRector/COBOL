## Entry 1222 â 2026-08-08 11:48 PDT â R15: the one operand slot that never consulted the keyword-omitted hook â and both spellings now share one body

`INSPECT EXCEPTION-STATUS TALLYING â¦` under FUNCTION ALL INTRINSIC compiled clean and died at run time
("INSPECT of unresolvable item") â the PB7 shape surviving in the INSPECT identifier-1 slot, whose
resolution path never asked IntrinsicBinder.KeywordOmittedFunction. Â§8.4.3.2.3 SR2 makes the omitted and
keyword forms ONE reference, so the fix funnels both spellings into one local: one SR1 receiving screen
(REPLACING/CONVERTING still draw COBOLNET1632 with the citation â probe-verified on the omitted form too),
one Format-1 bind. The hook itself yields to a declared data item, so a genuine subscripted reference still
resolves as data.

Golden `inspect_keyword_omitted_function` pins the SR2 agreement (both spellings tally 2 over
UPPER-CASE("a-b-c")); negative `inspect-kof-replacing` pins the receiving bar (reject-at 2002+, since the
omitted form needs a REPOSITORY paragraph). Two harness lessons re-learned at speed: negative fixtures carry
a `*> reject-at:` header, and the .err substring must match the RAW diagnostic â the Â§ characters are in
the text; the console had been stripping them.

Gate: wave-local Inspect|Corpus 499/499. Also this stretch: the CI artifact actions had to move AGAIN â
v5 still declares node20 (the owner's warning list was the authority; verified against the repos this time:
upload-artifact went node24 at v6, download at v7) â pinned to the current majors v7/v8.


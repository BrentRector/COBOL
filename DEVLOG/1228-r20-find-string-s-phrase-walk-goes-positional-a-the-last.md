## Entry 1228 â 2026-08-08 13:24 PDT â R20: FIND-STRING's phrase walk goes positional â the last order-free arm, and the dangling START AFTER stops eating source

FIND-STRING accepted its phrase words anywhere, repeated, and â the case that made this a defect rather
than over-permissiveness â a dangling `START AFTER` with no argument-3 silently DISCARDED the two written
words and degraded the call to the plain two-argument form. Â§15.37.2's general format fixes order and
multiplicity; the binder now walks it as SLOTS (one per bracket), the same discipline
BindSubstitute/BindConvert always had â this was the last order-free arm.

Eleven probe shapes before landing: five legal forms compute their Â§15.37.4-derived positions (plain=1,
LAST=7, skip-one=4 both spellings, both-flags=7) and six malformed forms â dangling introducer, phrase
before the operands, phrase between them, repeated LAST, ANYCASE-then-LAST, START without AFTER â each
draw COBOLNET1504 with a reason naming the specific violation.

Golden find_string_phrase_order + negative find-string-dangling-start-after. Gate: Intrinsic|Corpus
648/648 Â· characterization 33/33.


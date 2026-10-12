## Entry 953 — 2026-07-21 15:45 PDT — Wave D (cont.): FLAG flagging Incr 1b — j VALUE-EDITING (derived from §13.18.63 SR6/SR11 + E.2 item 29); k VALUE-FIG-CON-LENGTH scheduled with its derivation recorded

The subtle VALUE options. Derived j's predicate from the spec BEFORE coding (the point of flagging it subtle): a
numeric-edited item's VALUE that is a LITERAL (numeric or nonnumeric, NOT a figurative constant) carrying **no
editing symbols** is flagged — §13.18.63 SR6/SR11 (a numeric literal is converted/edited; editing chars apply only
when the literal is numeric) + E.2 item 29 (at 2023 editing is auto-supplied for a numeric literal and *compulsory*
for an alphanumeric/national literal — both changed 2014→2023). `VisitDataDescriptionEntry` now branches: figurative
ZERO ⇒ g/l; else a literal with no editing symbols ⇒ j. "Editing symbols" are scanned from the literal via the
UNAMBIGUOUS numeric-editing insertion set `{space / , . + - $ *}` + a trailing CR/DB; `0` (zero insertion) and `B`
(space insertion) are omitted — indistinguishable from a digit / a letter in the literal text without re-deriving
the picture mask, a documented rare false-negative for this advisory flag. A numeric literal (no editing symbols by
definition) is always flagged; a nonnumeric literal is scanned; concat/boolean/hex are not analyzed.

**k VALUE-FIG-CON-LENGTH — SCHEDULED, derivation recorded (not silent debt).** A figurative-constant VALUE on an
item with "no specified length" (E.2 item 11: "with the ALL phrase where the length is unspecified"). I confirmed
the construct IS reachable (`01 X VALUE SPACE.` with no PICTURE compiles cleanly), but a correct predicate must
EXCLUDE a group item — a group's figurative VALUE has a specified length via its subordinates (§13.18.63 SR13), so
it is not flaggable — which requires group-vs-elementary discrimination (sibling lookahead: an item is a group iff a
following entry carries a higher level number). Deferred to a dedicated increment because that discrimination
warrants its own careful build; E.2 item 11 itself notes the case affects "few if any programs" and "could be
expected to cause a compiler error." Recorded in the design SSOT + §0; must land (or get owner sign-off) before Wave I.

**Gate.** Build 0W/0E · CLI-probed j (numeric literal + all-digits nonnumeric → flagged; comma-bearing literal,
figurative ZERO, and a non-numeric-edited item → not) · 29 FlagDirectiveTests (4 new) · characterization 33/33
byte-exact. **Detectors done: 7 of 19.** NEXT = Incr 2 (the frontend-inline options b COMPILE-TIME-ARITHMETIC-EXPRESSIONS
+ c EVALUATE, emitted in `ConditionalCompilationProcessor`), then k, then Incr 3/4.


## Entry 1358 — 2026-08-29 01:18 PDT — PB124 wave 4: the §15.3 variable-length-group screens land on the §8.5.1.12.1 DEFINITION — after the gate caught my first cut using the broader runtime-length predicate on legal ODO source

AR-15.3-14's three unimplemented charges (batch 7). (a) "A variable-length group shall be referenced as an
argument to a function only when explicitly permitted in the function definition" — nothing screened it. The
spec grep finds exactly two permitting definitions (§15.50.4 r7/r8 LENGTH, §15.14.4 r6/r7 BYTE-LENGTH), so
CheckArgumentClasses now rejects a variable-length group everywhere else, placed before the Verified
early-return. (b) A type-declaration argument shall not describe one — lands in BindLengthFamily, which
previously answered a VLG typedef's width silently. (c) Table 21 lists `Type1` for CONVERT, but §15.19.3/.4
define NO semantics for a type-name — recorded in CONFORMANCE.md as a standard-text inconsistency (beside
the §14.9.28.4 precedent), with a targeted COBOLNET1514 naming the note.

THE GATE EARNED ITS KEEP TWICE. My first cut tested "variable-length" with HasRuntimeLength — whose ODO arm
covers groups §8.5.1.12.1 explicitly calls FIXED-length ("a group item whose data description has at least
one dynamic-length elementary item or dynamic-capacity table as a subordinate item. All other group items
are referred to as fixed-length groups", cite-checked) — and the wave gate went red on pb61's §15.50.4 r4a
typedef-with-ODO leg, which the standard itself defines. The screens now ride a named IsVariableLengthGroup
predicate carrying the definition; the negatives were rewritten from ODO shapes to DYNAMIC LENGTH
subordinates (the first drafts encoded the same misreading). And earlier, the first apply script died on a
CRLF anchor after "applying" its first site in memory — the single-write-at-end saved the tree, but the
repair script carried only the remaining sites, and the missing screen surfaced through a reject probe
printing NOTHING (a probe's silence is a result too). AR-15.3-14 → CONFORMS. PB124 stays open — the
class/category model completions (GR-15.2-2/-6, AR-15.3-1/-5/-10/-11/-13) are the last wave.


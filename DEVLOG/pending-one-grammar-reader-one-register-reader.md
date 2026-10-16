## The grammar and the A.1 register each get ONE Python reader (PB3204, PB3209)

Wave 1092 group D, two R2 unify findings of the PB1754 architecture review, both in the Phase-B dossier generator
`scripts/spec/phase_b_batch.py`.

**PB3204 — three private ANTLR grammar readers.** The re-probe on this tree reproduced it: the optional-word audit and
the frozen-evidence audit each read 645 parser rules, the dossier's line reader 642. It ended a rule only at a line
ENDING in `;`, so `methodDefinition` (`;   // the ONLY ISO method attributes …`) swallowed `methodName` and
`className`, and `cceRelationOrBoolean` swallowed the DISPLAY directive's `displayDirectiveFragment`; it also missed 74
lexer rules and quoted 40 more with the wrong extent. The evidence audit's own reader had two holes the note did not
name: `NAME_BODY` (a name-alone head with a trailing comment) was invisible, and `PIC_STRING` ended at a `;` inside its
C# action. The fix is one module, `scripts/spec/grammar_source.py`: `read()` blanks comments and `{…}` actions (literals
and `[…]` sets kept, newlines kept), takes a column-0 identifier followed by `:` as a head, and ends a rule at the
first `;` outside a literal or set; it returns `Rule(name, kind, file, first_line, last_line, body, source, lead)` and
refuses an empty population. The three readers (`RULE_HEAD` ×2, `grammar_rules` ×3, `strip_comments`, `LEXER_RULE`)
are deleted and their scripts import it. Neutrality was measured by snapshotting every derived fact before and after:
the optional-word audit's required tokens, alternatives, references and 504 keyword spellings are identical, the
evidence audit's `--check` output is identical, and the dossier gains the 78 rules and corrects the 40 extents. Its
`--self-test` plants each trap and refuses a script that declares a retired reader's name again; that guard fails on
the pre-change optional-word audit.

**PB3209 — the dossier's own A.1 register reader.** Reproduced: `A1_ROW` (a bare-number first cell) matched 35 rows,
all in §2's Annex A.3 table, and none of §7's `DOC-A.1-<n>` rows, so a clause-12.4.5 dossier labelled A.3 items 26 and
40 as A.1 items and left its 16 real A.1 determinations untagged. `inventory_schema.register_section_at` is now the one
register parser with each row's line (`register_section` and `section7_rows` are its views, `section7_rows_at` the
keyed one; lines split on `\n` as the C# twin splits them), and `doc_citing` tags a citing line with its §7 key. The
dossier's private `CATALOG`, `INVENTORY` and `CONFORMANCE_DOC` paths are deleted in favour of `inventory_schema`'s
loaders and `REGISTER_PATH`. `phase_b_batch.py --self-test` (new) asserts a §7 row carries its key and a §2 or §8 row
does not; the old pattern fails it twice.

DESIGN-architecture-review §8.2 now states the scripts rule: an artifact the tooling reads has one Python reader.
No inventory row is closed (behavior-neutral restructuring of tooling). Self-review: architecture (the readers are
one each; the consumers keep only projections), full code (the end-of-rule rule is ANTLR's, outside literals and
sets), performance (one cached read per process; the register map is cached per document), duplication (the guard
in `grammar_source --self-test` keeps the retired names out).

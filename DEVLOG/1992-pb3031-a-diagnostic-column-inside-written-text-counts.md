## Entry 1992 — 2026-10-12 23:19 PDT — PB3031 - a diagnostic column inside written text counts character positions, not UTF-16 units

Wave 1069 group D. A diagnostic's column is a CHARACTER POSITION of the physical line (ISO 6.1 1) a), 6.3.1; DOC-A.1-157),
and PB2983 made the reference-format stage split an origin span at each tab and each supplementary-plane character so
that one resultant UTF-16 unit is one position. Text a LATER stage writes (a REPLACE replacement, COPY ... REPLACING
text, the unexpanded COPY statement, the NIST substitutions) went through `OriginWriter.Append`, which gave it one span,
so each supplementary-plane character counted two columns.

**Re-probe.** The note said the period AFTER the written text drifts. It does not: the tail of the line is appended from
the mapped input with its own spans (`AppendSlice`), so only columns INSIDE the written text were wrong. With `REPLACE
==X== BY ==<two Adlam letters> TO TO==.` and `MOVE X.` the parser's error at the second TO was `(5,25)`; the programmer
counts 23.

**Fix.** `OriginWriter.Append` writes through a new `WriteText` that starts a new span at the second unit of every
surrogate pair, at the first unit's position, as `TextSite.Spans` does for read text. One rule, now in both writers of
spans. `CharacterPositions` moved from `Preprocessor` to `Common` beside the writer (the writer must not depend upward on
the preprocessor namespace); its three callers and its test gained the `using`.

**Goldens.** `DiagnosticPositionTests.ReplaceAndCopyReplacing_WrittenSupplementaryCharacters_NameTheirPosition` (REPLACE
at `(8,23)`, COPY ... REPLACING at `srepl.cpy(1,23)`) and the unit test
`SourceLineMapTests.OriginWriter_WrittenTextWithSupplementaryCharacters_CountsOnePositionPerCharacter`, expectations
computed from the position rule. No inventory row is claimed or closed. Docs: DESIGN-frontend-grammar.md and
DESIGN-cc-in-copy.md describe the writer's rule.

**Oracle.** The change touches the arch-oracle's inputs (the Frontend sources, and `DiagnosticPositionTests.cs` through
the oracle host's MSBuild closure), so the landing refused the baseline `60d47c6d52a9` (exit 7). `compare_oracle.py`
against it reports `IDENTICAL — 7780 cases`: no emitted C# and no diagnostic differs, because no corpus, NIST or
version-matrix case writes a supplementary-plane character through REPLACE or COPY ... REPLACING (the new goldens are
xunit tests, not oracle cases). The branch recorded `88213eb8a08b` over main's interim `c41f1b82af2c`; main then
re-recorded its own baseline three times (`f34b1aea29f0` with PR #26, `dd3d1d4bb002` with PR #31, `c8668abfeda9` with
PR #25). The first queued attempt (PR #27) bounced in the merge queue: its merge group (base 53788e78c, the PR #26
merge) held PR #26's `f34b1aea29f0` and the branch's `88213eb8a08b`, two baseline manifests, so
`ArchOracleDriftTests.TheRecordedBaseline_IsOneWellFormedManifest` failed (the kb/Work PB3107 race; PR #25 was not in
that group). After PR #25 merged, the merged head compares
`IDENTICAL — 7799 cases` against main's `c8668abfeda9`, and the baseline is re-recorded on it at `16b7de18c926`.


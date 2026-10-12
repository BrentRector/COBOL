## Entry 1740 — 2026-09-27 13:06 PDT — Every open defect has a code site: fix_clusters reads bare file names and grammars; 30 notes located

A read-only site-locator agent (about 165k tokens, 95 tool calls) located the code site of each of the 30 open
defects `fix_clusters.py` could not place. It found all 30: 28 in compiler code, 2 test-side (PB311, PB387). Each
note now carries a `**Code site (located 2026-09-27).**` line.
**The tool was half the problem.** 15 of the 30 already named their site in prose (`ReferenceResolver.cs#…`,
`CobolLexer.g4#NAME_BODY`), but `fix_clusters.py` recognized only full `src/…` paths and `Type.Member`. It never
matched a bare `File.cs` name, and it did not index `.g4` grammar files at all. It now does both, and that fix alone
took the unsited count from 30 to 16. With the located sites applied, the count is 0: 407 open actionable defects →
122 clusters, 49 of them with five notes (up from 42), so waves have more full clusters to take.
PB876 is flagged as possibly stale: PB492 (landed) removed the obstacle the note records.
The public `agent-fleet/references/fix_clusters.py` gets the same generalization: `--ext` takes a comma list (for
example `.cs,.g4`), and bare file names count as sites.


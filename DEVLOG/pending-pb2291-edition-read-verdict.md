## PB2291: the front end's edition reads adjudicated against D7, and the pin that computes them

kb/Work PB2291 (wave 1092 E, the PB1754 cluster) asked, of every place the front end reads the compile edition,
whether it decides ISO §8.9 word status (sanctioned: the superset parse depends on it) or refuses a construct by
edition (a D7 defect: `VersionConformancePass` is the one edition gate). DESIGN-architecture-review §8.4 defines the
reads by a rule, so the verdict landed as a test that applies the rule rather than a list.

**The pin.** `EditionReadDriftTests` computes the reads with Roslyn: members of `CobolParserCoreBase`, of the lexer
(`CobolLexer.g4`'s `@members` and its committed partials) and of `TokenRetypes` whose syntax reaches edition state, to a
fixpoint. The lexer's edition state is derived, not named: the retype stage's reads call two lexer setters with an
argument built from their `EditionInfo` (`SetReservedNonDataNames`, `SetCaseMappingEdition`), and the fields those
setters write (`_reservedNonNames`, `_caseMappingEdition`) are edition state from then on (the third setter,
`SetCobolWords`, is fed from the >>COBOL-WORDS plan and primes nothing). Sites come from a brace-block scanner over
every grammar (predicate and action bodies, attributed to their rule) and from Roslyn over `src/`: callers of a read,
front-end hand-offs of the edition to a parser (`Edition = …`), and callers that pass one on. On today's tree that is
16 parser reads, 4 lexer reads, 3 retype reads and 23 site keys, each recorded with a verdict, and a new read under any
name fails until it is adjudicated. A mutation check (`{reservedHere("CRT") && is2014()}?` in `CobolSpecialNames.g4`)
turned it red with `NEW site … specialNameEntry | is2014`. The counts the note carried were older (67
`keywordContinuesHere` alternatives, not 71; three `facilityWord` arms, not five), which is why the test computes them.

**The verdict.** Word status: bare LOCATION (2023), OPEN's RETRY (2002), `setLocaleAhead` (LOCALE and USER-DEFAULT,
2002), CRT/CURSOR, the facility verbs, DISPLAY's `screenPositionAhead`, `entryNameHere`, the generated `cobolWord`
predicates and `reservedGatedWord` action, the two listener calls, the lexer's Annex C spelling and subscript trigger,
and the re-lex decision. Carriers, which decide nothing: the edition handed to each lexer and parser by the driver,
`FragmentParse` and its callers. Construct gates, three, all on the declined Annex A.4 surface: `{is2023()}?
applyCommitClause` (APPLY is §8.10 context-sensitive and no earlier edition has an APPLY clause), `{is2002()}?
validationClause` (CLASS, DESTINATION, INVALID and VARYING are reserved at every edition) and the INVALID arm of
`({is2002()}? validateValidPhrase)?` (VALID itself is a §8.9 user word below 2002 and, per §8.3.3.6.3 SR1, a legal
VALUE operand as a symbolic-character, so that arm keeps word status). Re-probed on this build: below the introducing
edition each is a bare COBOL0001 where COBOLNET0900 belongs. Moving them needs a constructs.json row form the version
matrix lacks (a construct refused by name at its introduction), so they are recorded as D7 defects and handed to the
fix lane as one lead; `ConstructGates_AreOnlyTheOnesPb2291Found` refuses a fourth.

**Docs.** `COBOLNET_DESIGN.md` §1.1 (D7) and `DESIGN-architecture-review.md` §8.4 now state the verdict and name the
test; the `architecture-review` skill's grammar rule no longer says post-1985 features sit "behind `{isXXXX()}?`". Six
comments that claimed `{is2002()}?` gates the grammar no longer has (`CobolLexer.g4` twice, `CobolOO.g4`, `CobolIO.g4`,
`CobolData.g4#floatFormatPhrase`) were corrected, and the three gate sites, `CobolDeclined.g4`'s header and
`DeclinedFacilityPass.Run` carry the verdict.

**Branch review (round 1, finding R1-1): the pin saw only CALLS of a read, never edition state named directly.** A
grammar predicate `{Edition.Has(2014)}?`, a lexer action naming `_caseMappingEdition`, a listener reading
`parser.Edition` or `parser.DialectLevel`, or a committed `CobolParserCore` partial reading `Edition` would each have
passed the pin green. Fixed at both ends. The product: the parser's `Edition` getter is now `protected`, so the
compiler refuses an outside read (CS0271 when a listener names `parser.Edition`, measured by a temporary mutation of
`CobolErrorListener`), and the unused `DialectLevel` setter shim is deleted (the front end already set `Edition`, both
axes). The pin: a grammar site counts a read OR the stage's edition state (the parser's `Edition`, the computed primed
lexer fields); the parser stage's members now include every committed `CobolParserCore` partial and any parser
grammar's `@members`, as the lexer's already did; and the C# arm counts a read used as a property as well as one
called. `TheRule_SeesEditionStateNamedDirectly` applies the rule to synthetic text for each shape, and
`TheParsersEdition_IsWriteOnlyOutsideIt` pins the accessibility by reflection. A temporary mutation (`&& Edition.Has(2014)`
on the CRT arm, a `mutantGate` reading `Edition` in the `CompilationUnitTokenRetypes` partial) turned
`EditionReads_AreExactlyTheAdjudicatedSet` red with both rows named, and was reverted.

**Branch review (round 2, finding R2-1): a nested type of a stage was seen by neither arm.** R1-1 skipped every file
holding a `CobolParserCore` partial as a C# site source, but the parser's read set took only the partial's DIRECT
members, so a member of a nested type (`CompilationUnitTokenRetypes.cs` already nests `CompilationUnitContext`)
reading `p.Edition.Has(2014)` or calling `p.userWordHere(...)` through a parser instance (legal C#: a nested type sees
its container's protected members) was neither a read nor a site. The same hole held for the other skipped files
(the parser superclass, the retype stage, the lexer partials) and for a second top-level type in any of them. Fixed in
the rule, not the file list: a stage's members are every member lexically inside its types, nested types included, and
the C# site arm parses every file and skips a use only when it sits inside the read's OWN stage (by its enclosing
types), so a listener in a stage's file is a site. `TheRule_SeesTheStagesNestedTypes` states both spellings and the
site arm on synthetic text. Mutation runs on `CompilationUnitTokenRetypes.cs`: the reviewer's two nested `Strict`
members gave `NEW edition read …#Strict` and `…#Strict2`; a top-level `Leak` class calling `p.userWordHere` gave
`NEW site …CompilationUnitTokenRetypes.cs | Leak.L | userWordHere ×1`; both reverted. The live inventory did not move.

**Branch review (round 3, finding R1-1 of the second review): a stale count in the verdict of record.** The note and
this entry said the rule finds 17 parser superclass reads; the first round's `RecordedMembers` listed 17 because it
included the `DialectLevel` setter shim, which the round-1 fix deleted, so the pin has recorded 16 since. Both texts
now say 16, and the note names `RecordedMembers` as the list of record (checked: 16 parser, 4 lexer and 3 retype names
and 23 site keys in `EditionReadDriftTests.cs`). No code or test changed: the test computes the set and compares it
with the record itself, so a second copy of the count in a test would only be one more place to go stale. Merging main brought PB3022's
`[Trait("Sweep", "tree")]` rule, and `drift_rules.py --sweeps` lists `EditionReadDriftTests` (it scans every C#
member of `src/` and every grammar), so the class now carries the trait and the gate's tier 0c runs it on every change.

**Branch review (round 4, findings R1-1 and R1-2 of the third review).** R1-1 (first raised as w1092e R3-1 and
left unanswered by the round-3 finisher): the pin saw a stage's NESTED types but not its DERIVED ones.
`CobolParserCoreBase` is public abstract and the generated `CobolParserCore` a non-sealed public partial, so a class in
`src/` such as `sealed class StrictParser(ITokenStream s) : CobolParserCore(s) { public bool Strict() =>
Edition.Has(2014) || reservedHere("LOCALE"); }` reads edition state through `this`, and neither arm saw it: the read arm
took only the named stage types, and the site arm skips a bare `Edition` and never matches a protected read. Sealing
was not available as the structural fix (a unit test derives `IsolatedLexer` from `CobolLexer`), so the rule now
computes the derivation: `StagesOf` maps every type in `src/` whose base list names a stage type, transitively, to that
stage; `DerivedReadsOf` takes such a type's members that reach the stage's state or one of its reads (to a fixpoint)
as reads, recorded under the derived type's own file as `Type.Member`; a derived type's own use of a read is the read
arm's, and an outside use of a derived read is a site. The comparison also reports a recorded file that no longer
computes as STALE. `TheRule_SeesTheStagesDerivedTypes` pins it on synthetic text (the reviewer's spelling, a type
deriving from that one, a lexer subclass named by a qualified base, and a listener), and a probe on the real tree
(the reviewer's `StrictParser` plus a `CobolLexer` subclass in `src/Cobol.Net.Frontend/Parsing/`) failed
`EditionReads_AreExactlyTheAdjudicatedSet` with `NEW edition read …#StrictParser.Strict`, `…#StrictLexer.S` and the
listener's `NEW site`. The tree has no derived stage type today, so the record did not change. R1-2: the
`CobolDeclined.g4` header still filed APPLY under word status and called CLASS's gate "right either way", eleven lines
above the PB2291 verdict that says both are construct gates. The header now states the two reasons once, with the
verdict: (a) word status for the arms led by a §8.9 user word below 2002 (DEFAULT, PRESENT, VALIDATE-STATUS,
VAL-STATUS, VALID); (b) a recorded D7 construct gate for CLASS, DESTINATION, INVALID and VARYING (reserved at all four
editions, re-measured in `reserved-words.json`) and for APPLY COMMIT (APPLY is §8.10 context-sensitive, a keyword only
in the I-O-CONTROL paragraph). The hook-in comment further down now names all four (b) words.

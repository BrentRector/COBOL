## PB3153: a use of a name a containing program's SPECIAL-NAMES declares is a §8.3.2.2 use in the using element

**The defect (kb/Work PB3153, wave 1078 G).** ISO §8.3.2.2 says "Within a source element, a given user-defined word
may be used as only one type of user-defined word". A contained program that writes `IF X IS DIGS` over its
container's `CLASS DIGS` and also declares a paragraph `DIGS.` uses DIGS as two types in its own source element, yet
it compiled and ran (`DIG`, `IN`) while the flat program with the same two words was refused COBOLNET2692. The census
saw declarations and, since PB3132, object-class-name references; the class-condition, alphabet, mnemonic,
symbolic-character, switch-condition, locale, ordering and dynamic-length-structure lookups recorded nothing, and the
inherited name is declared in ANOTHER element. Re-probed on this worktree's build: reproduced. A sibling arm surfaced
at once: `InheritConfiguration` copied every SPECIAL-NAMES registry except the SYMBOLIC CHARACTERS, so a contained
program's `MOVE BEL TO X` over its container's symbolic character was refused COBOLNET1639 on conforming source
(§8.4.6.1 lists symbolic-character among the names a contained element may reference; §12.3.4 GR1).

**The fix, as a shape.** The seven data-binder registries are now `ConfigurationNames<T>` tables with no
`TryGetValue`: a reference the binder commits to resolves through `TryUse(site, name)`, which records the use in the
census of the element the site lies in through ONE recorder, `DataBinder.RecordConfigurationNameUse` (into PB3132's
`OoClassTable.RecordReferenceUse`); a predicate, a duplicate check or a re-read asks the non-recording `Declares` /
`TryPeek`. The mnemonic-names keep their parse-tree registry, whose map is now private behind the same `TryUse` /
`TryPeek` pair. So a new consumer of any of these names cannot resolve one without choosing, and
`UserWordDeclarationDriftTests.EveryConfigurationSectionNameType_ResolvesThroughARecordingTable` parses §8.4.6.1's
eight-type list from the standard and keeps every type's registry a recording one. The symbolic characters inherit
with their siblings.

**Where the two readings differ.** §8.4.6.1's second sentence ("a source element's reference to such a name, even when
it is a different type of user-defined word, is to the item which that source element describes") refuses the same
programs the census does, and decides one more case: where a format admits an identifier as well as the inherited
name, the element's own item wins. Inheriting the symbolic characters would otherwise have let an inherited `BEL`
shadow a contained program's own `01 BEL` (a wrong answer) and record a false use (COBOLNET2692 on legal source), so
the three such positions (a bare literal-alias operand, `SET LOCALE ... TO`, `WRITE ... ADVANCING`) ask the reference
resolver first.

**Goldens.** `negative/pb3153-contained-program-paragraph-spells-an-inherited-class-it-uses` (reject-at 85 2002 2014
2023; its second contained program declares the same paragraph without the use and draws nothing);
`85/pb3153_inherited_special_names_words` (`A`, `DIGIT`, `Q`, `PARA`: the inherited symbolic character is ordinal 66 of
the native set, §12.3.7.4 GR11 b; the local data item named like it wins); `InheritedConfigurationNameUseTests`, one
row per §8.4.6.1 type at its first edition plus a method over its class's SPECIAL-NAMES. No inventory row isolates the
rule; the note closes none (reason recorded).

**Self-review.** Architecture: one recorder, one table type, the funnels pinned by the drift tests. Full code: every
lookup site was classified as a use or a non-use; the class-condition alphabet arm keeps its fall-through to the
national table. Performance: one dictionary operation per committed use; the added probes run only when an inherited
name matched. Duplication: the two-map "is an alphabet-name" test that four sites spelled out now calls
`IsAlphabetName`.

**Gate.** The first gate (run 20261014T222553Z-2f9d3a) was RED on one case:
`LiteralAliasResolutionDriftTests` reads `LiteralAliasOf`'s body up to its first semicolon, and the new §8.4.6.1
comment inside that expression carried one; the comment now uses a comma. The re-gate, run 20261014T223239Z-4450f2:
`=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 6,174/11,694, Unit 33,048/33,637,
Characterization 6/36 in leg 1; leg 2 left to CI). `scripts/semgrep/verify.py` PASS, every count unchanged.

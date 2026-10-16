## PB578: WRITE ADVANCING PAGE on a logical page is a page overflow, and SR18 bars only the AT END-OF-PAGE phrase

**What the note said.** kb/Work PB578 (an analysis note in the GAP campaign PB2911) held GR-13.18.34.4-7 and
GR-8.4.3.14.4-2 PARTIAL. Its claim: the implicit-increment sentence of §13.18.34.4 GR7 c) 1 ("During the resetting
of LINAGE-COUNTER to the value one, the value of LINAGE-COUNTER is implicitly incremented to exceed the value specified
by integer-1 or the data item referenced by data-name-1", `cite.py --check` OK 7) c) 1.) was modelled nowhere, and the
argument that it was unobservable had one hole: a USE AFTER STANDARD ERROR declarative entered from inside the WRITE
could read LINAGE-COUNTER mid-statement, a path then unreachable only because the declarative never fired.

**What the re-probe showed.** The named hole no longer reproduced as written: the declarative fires (the overflow
arm's twin is `2023/pb526_linage_ec_condition`), and on a GR6 b) 2 value-rule violation during ADVANCING PAGE it reads
LC=000, GR6 b) 2's value, after the WRITE's own I-O operation. But the argument was wrong somewhere else. A counter past
the page size is §14.9.51.4 GR26 a)'s trigger ("This occurs when the associated LINAGE-COUNTER is equal to or exceeds
the page size", OK 26)), GR27 a) sets EC-I-O-EOP-OVERFLOW to exist for it, and GR28 runs a NOT END-OF-PAGE imperative
only when "the end-of-page condition does not occur". The old step (c), "its END-OF-PAGE flag cannot be seen because
SR18 forbids ADVANCING PAGE and END-OF-PAGE in one statement", missed two observers. GR27 a)'s condition needs no
END-OF-PAGE phrase: a USE AFTER EXCEPTION CONDITION EC-I-O-EOP-OVERFLOW declarative, an exception-checking PERFORM's
WHEN and FUNCTION EXCEPTION-STATUS all see it, and none fired after ADVANCING PAGE. And SR18 names the END-OF-PAGE
phrase, GR27 b)'s imperative-statement-1; SR19 ("If the END-OF-PAGE or the NOT END-OF-PAGE phrase is specified", OK
19), checked against the PDF text layer) and GR28 name the NOT END-OF-PAGE phrase separately. So `WRITE ... ADVANCING
PAGE NOT AT END-OF-PAGE ...` is legal source, and the compiler refused it with COBOLNET0861 (the SR18 row's earlier
adjudication had read "END-OF-PAGE" as the phrase group).

**The fix.** `SequentialConnector.PositionOnLogicalPage` loses its separate ADVANCING PAGE arm: `lines < 0` joins the
page-overflow arm (`lines < 0 || counter + lines > page body`), so ADVANCING PAGE takes the one transition
(`BeginNextLogicalPage`, which GR25 g) and GR26 a) describe with the same destination), the one reset, and the one
EC-I-O-EOP-OVERFLOW. `SequentialIoBinder.BindWrite` asks SR18 only when the AT phrase is present, through
`PhraseBlocks.HasOnBranch`, the predicate `FlagConformancePass` already used for "END-OF-PAGE phrase specified". The
determination is docs/CONFORMANCE.md §4 "WRITE ADVANCING PAGE on a logical page is a page overflow". The text decides
it; GnuCOBOL 3.2 accepts the NOT-only form and runs its imperative, but it also runs a NOT END-OF-PAGE imperative after
an `ADVANCING 9 LINES` overflow of a 5-line page with no FOOTING phrase, which no reading of GR26 a) allows, so its
end-of-page signal is no witness. docs/COBOLNET_FILES_DESIGN.md's GR26 mapping and END-OF-PAGE paragraph now say the
same (the latter also lost a stale "no USE hook competition" from before PB854).

**Goldens.** `85/pb578_advancing_page_overflow_85` (a NOT END-OF-PAGE imperative is silent after AFTER and BEFORE
ADVANCING PAGE and live after a plain write; the counter reads 001 after each page write).
`2002/pb578_advancing_page_eop_overflow` (an EC-I-O-EOP-OVERFLOW declarative fires on AFTER and BEFORE ADVANCING PAGE and
reads LINAGE-COUNTER 001; a GR6 b) 2 violation during ADVANCING PAGE enters the file's USE AFTER STANDARD ERROR
declarative with FS=90 LC=000 and no end-of-page declarative). Negative `pb578-write-advancing-page-at-and-not-eop`
(COBOLNET0861 at every edition). Unit tests `LinageConformanceTests.Sr18_AdvancingPageWithNotEopOnly_IsLegalAndTheEndOfPageConditionOccurs`
and `Sr18_AdvancingPageWithNotFirstEopPair_IsRejected`.

**Rows** (batch `w1095e-pb578`): GR-13.18.34.4-7 and GR-8.4.3.14.4-2 PARTIAL -> CONFORMS (GAP 91 -> 89);
SR-14.9.51.3-18 re-verdicted with the narrower reading; GR-14.9.51.4-27 and -28 gained the new goldens as witnesses.
PB578 landed with `closes_rows` for both of its rows.

**Self-review.** Architecture: one arm for one rule, so a page-overflow consequence added later reaches ADVANCING PAGE
automatically. Full code: the EOP-phrase branch in the emitter reads the connector's flag, so the NOT arm's silence
follows from the runtime without an emitter change; the sibling arm (a file with no LINAGE clause, GR25 h)'s form
feed) has no logical page and so no end-of-page condition, and is unchanged. Performance and duplication: nothing.

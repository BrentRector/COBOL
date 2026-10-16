## PB661: the INVOKE arms of §8.3.2.2 are documented and GR-8.3.2.2-L4.2 is closed

kb/Work PB661 asked for three §8.3.2.2 implementor determinations in `docs/CONFORMANCE.md`: (a) whether extended
letters may appear in externalized words, (b) how AS-phrase names are formed and mapped, and (c) the mapping when no
AS phrase is written. The re-probe on this wave's build found that other notes had already answered all three.
`DOC-A.1-219` (kb/Work PB1402) answers (a): `PROGRAM-ID. PRÖG.` with `01 ÄB PIC X(3) EXTERNAL.` and `01 ñame` compiles
and runs at 2002. `DOC-A.1-68` (kb/Work PB1539 and PB1383) answers (b) and (c): one function,
`CobolNet.Runtime.ExternalizedNames`, forms and maps every externalized name, and `>>CALL-CONVENTION` admits only
COBOL. `>>CALL-CONVENTION STDCALL` followed by a universal INVOKE gives COBOLNET1911 at 2002, 2014 and 2023.

That left one GAP row and a contradiction in the document. `GR-8.3.2.2-L4.2` holds three INVOKE sentences of §8.3.2.2,
each checked with `cite.py --check` (OK 2)). The first is the literal mapping under a call-convention-name-1. It has no
subject, because no call-convention-name is defined. The row's CALL twin, `GR-8.3.2.2-L5.2`, was closed on the same
ground. The second says a typed reference maps by the entry convention of the class or interface. That convention is
always COBOL, and §11.9.7.4 GR2 (OK 2)) gives it §8.3.2.2's mapping. The third says a universal reference ignores the
class's ENTRY-CONVENTION. The row is now CONFORMS, and GAP went from 88 to 87.

`DOC-A.1-68` now states both INVOKE arms. Items 13, 16, 19 and 141 said "there is no CALL-CONVENTION directive" or
that every name "is processed as a COBOL name". Both statements were stale after PB1383, so the four rows now give the
current state: the directive admits only COBOL. The new golden is `2014/w1095g_pb661_entry_convention_invoke_mapping`.
Its class states `ENTRY-CONVENTION IS COBOL` and declares `METHOD-ID. M2 AS "MixedExt"`. A typed `INVOKE T "MIXEDEXT"`
and a universal `INVOKE U "mixedext"` both reach M2, and `INVOKE U "M2"` sets EC-OO-METHOD. These expected values come
from the rules and the documented mapping. No compiler source changed.

## Entry 1074 — 2026-07-28 13:04 PDT — The checking-OFF doctrine, decided by surveying the field rather than reasoning about it

No code yet. This settles the first of the three owner decisions the EC + OO super-batch was blocked on, and it
changes what CA9 will be implemented as.

**The question.** With exception checking OFF, what does a fatal-EC raise site do? The batch had been drafted
site-by-site — each one preserving whatever it does today, except CA37/CA38 which flipped to lenient — which
left two doctrines coexisting. Owner decided: **lenient everywhere.**

**Which surfaced a site the answer does not fit.** `CobolPtr.Deref` returns a `StorageCell`. Every other site
has an outcome the standard NAMES and can therefore return — §14.9.39 GR30 "capacity unchanged", GR31 "SET not
executed", Format 10 GR19 "unsuccessful … the content of identifier-9 is unchanged". §13.18.5.4 GR3/GR4 name
none, so "lenient" at a dereference does not mean skipping the operation; it means fabricating a cell and
continuing on garbage. I put that back to the owner rather than picking.

**The owner's answer was to go and look, and looking overturned my framing.** Five compilers, and all five
hard-stop: GnuCOBOL and gcobol list EC-DATA-PTR-NULL and EC-BOUND-PTR as `(f)` fatal — program abort — and
generate no check at all when inactive, so the raw dereference SIGSEGVs (libcob: "attempt to reference
unallocated memory"); gcobol's own manual says "By default, no exception condition is enabled". Micro Focus
documents RTS 114 because "this COBOL system traps the error that would otherwise result in a general
protection violation". IBM Enterprise COBOL abends S0C4/CEE3204S. NetCOBOL terminates with JMP0071I-U.

The useful part was not the tally, it was what the tally MEANT: **"checking off" nowhere means "lenient" — it
means UNGUARDED.** Leniency is not an option those compilers considered and declined; on their platforms the
hardware traps before they get a say. Our managed `StorageCell` is the only reason it is reachable here, and
taking it would make COBOL.NET the sole COBOL where a null dereference yields a wrong answer instead of a
stop. That is not a conclusion I was going to reach from the spec text, and I had been about to present it as
an even choice.

**THE RULE, batch-wide:** checking-OFF is **LENIENT wherever the standard names the outcome, LOUD ABORT
wherever it names none.** One criterion, stateable in a line, drawn on the spec's own wording rather than per
family. Lenient: CA37/CA38, CA9's SET pointer UP/DOWN BY, CA10's scratch-read, CA11/V55. Abort: the four
`CobolPtr.Deref` sites, where our throw is what everyone else does and better than most, since it names the EC
and the ISO rule.

**⚠ It corrects a recipe already written.** The CA9 delta says "keeping the existing loud throw as the
checking-OFF fall-through" for all six raise sites. That now holds for `Deref` (:27/:30/:33/:36) ONLY —
`UpBy` (:54/:57) and `UpByScaled` (:71) must return leniently instead. Anyone implementing CA9 from the
finding text without reading the Risks bullet would get this wrong, so it is flagged in all three places a
session looks: plan §0 NEXT item 1, the queue's §OWNER-DECIDED, and the deep-dive §Risks.

Two owner decisions remain before the serial EC chain: CA12 co-land, and V55's method-side "enabled" literal.


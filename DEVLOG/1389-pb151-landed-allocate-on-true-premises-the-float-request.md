## Entry 1389 — 2026-08-29 10:06 PDT — PB151 LANDED: ALLOCATE on true premises — the float request ceilings, the not-available leg exists, the Int128 size cannot wrap, the OPTIONS INITIALIZE clause has its first consumer, and the BASED substrate reject moved to bind time

Three mechanisms (kb/Work PB151). GR1: a NATIVE-FLOAT arithmetic-expression-1 rounds UP on the DOUBLE
inside CobolPtr.AllocateReal — the emitter's Scale==0 split read a Real-carried double and (long)(double)
TRUNCATED 2.5 → 2, a silently undersized cell whose third position tripped EC-BOUND-PTR later; the
IDENTICAL split in EmitSetPointerUpDown let a float SET UP BY bypass GR19's integrality raise — both
swept (CobolPtr.UpByReal tests integrality on the double; the golden pins the pointer UNCHANGED after
SET P UP BY 1.5). GR5: the not-available leg EXISTS — Allocate carries the Free-twin notAvail flag, the
emitter renders the checking-gated EC-STORAGE-NOT-AVAIL block (the armed ambient gate finally has its
raise site), and the size travels as the FULL Int128 — the old (long) narrowing WRAPPED a 20-digit
request into a five-character VALID cell (the PB22 cast family's unswept sibling) and checked((int))
threw an unhandled OverflowException. GR8/GR9 half 2: the OPTIONS INITIALIZE clause — modelled with ZERO
consumers — now fills a non-INITIALIZED allocation with its specified-fill-character (determination in
CONFORMANCE.md: the rules key on the CLAUSE, not its section list). And the BASED-record substrate reject
is a BIND-TIME COBOLNET1695 — the bare continue compiled clean and crashed at run time while the EXTERNAL
twin always diagnosed (two-arm); the cell model itself stays kb/Work PB164's island.

Verdicts: GR-14.9.3.4-1 DIVERGES → CONFORMS, GR-14.9.3.4-5 NOT-IMPLEMENTED → CONFORMS, GR-14.9.3.4-2
CONFORMS (value-observed), GR-14.9.3.4-9 DIVERGES → PARTIAL (fill half landed; null-seeding rides
PB164), GR-14.9.3.4-3/-4/-7 PARTIAL (island). Goldens pb151_allocate / pb151_options_fill; negative
pb151-based-comp-leaf; three new CobolPtrTests facts.


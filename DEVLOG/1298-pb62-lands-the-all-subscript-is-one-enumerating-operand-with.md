## Entry 1298 — 2026-08-18 01:09 PDT — PB62 lands: the ALL subscript is one enumerating operand with the standard's three ranges, admissible only where the format repeats an argument

**All 12 PB62 rows CONFORMS; GAP 4079 → 4067.** `TryExpandAll` expanded a `table(ALL)` argument at BIND time into
one operand per OCCURS occurrence — for every catalogued function — and the sweep had listed what that model cannot
do: an OCCURS DEPENDING table's range is data-name-1's CURRENT value ("determined by the object of the OCCURS
DEPENDING ON clause"), so it staged loud at run time (MEAN/MEDIAN/MIDRANGE/RANGE/SUM over an ODO ALL, the
FMT-15.6x.2 family); a dynamic-capacity table has no `Occurs`, so its level was never built and `MIDRANGE(D(ALL))`
did not bind at all; and because the expansion ran for EVERY function before the arity gate, `MOD(E(ALL) B)` bound
and EVALUATED over a one-occurrence table while the three-occurrence twin was rejected only through the count of
the expansion ("takes 2 argument(s); 4 given" — about arguments the user never wrote). §15.3's own precondition —
"When the definition of a function permits an argument to be repeated a variable number of times" — was enforced
nowhere.

**The shape of the fix is the runtime enumeration seam the note asked for**, and it turned out to want less new
machinery than feared because the pieces already existed once PB61 minted the capacity register for every dynamic
table. `IntrinsicBinder.TryBindAllArgument` (the renamed and rewritten `TryExpandAll`) binds the argument as ONE
`BoundFieldOperand` whose place is a `TableAllPlace(Element, IndexVar, Counts)` — a `PlaceDecorator` over the ELEMENT
place, whose ALL subscripts are written as `__allN[k]` and whose fixed subscripts are rendered as before, carrying
one `AllCount` per ALL level: `Fixed(occurs)`, `Odo(depending, min, max)` (data-name-1 clamped to [integer-1,
integer-2] with EC-BOUND-ODO outside — `CobolTable.OdoExtent` with a unit element, the SAME clamp the sending image
applies), or `Capacity(register)` — and for a dynamic table NESTED in another, the register's path carries the outer
index variables, so each outer occurrence's own capacity is read (`ReferenceResolver.BuildTablePath` gained the
outer-indices overload). Because it is a decorator, every STATIC classifier — class, category, usage, width, the
§15.3 screen, MAX/MIN's type resolution — sees the element exactly as it would a single subscripted reference; only
the intrinsic ARGUMENT-LIST renderers expand it. `IntrinsicRenderer.ArgArray` turns any list holding an ALL into ONE
`T[]` expression (runs of written operands as array literals, each ALL a `CobolTable.AllArgs<T>(counts, __allN =>
element)` enumeration, joined by `CobolTable.ArgConcat`) that every `params T[]` body binds to; `AlignedArgsEx`,
`RawArgPairs`, `DecArgList`, `StrArgList` and `RenderFloat`'s list ride it; PRESENT-VALUE's leading rate and TRIM's
mode go through `LeadThenTail`; MEAN's divisor becomes the enumerated list's `.Length` under a `CobolTable.With`
binding when a range is a runtime value (the list is evaluated once). A single-value read of a `TableAllPlace` is
an internal error by construction (`PlaceRenderer`), and `NumericRenderer.FieldNum` renders the operand AS ITS
ELEMENT so the flag-only inspections (`AnyRealArgument`, `AnyDecOrRealRaw`) keep working. All-fixed lists render
through the same enumeration — one mechanism, no bind-time expansion left anywhere.

**Admissibility is decided FIRST, from the DEFINITION.** `IntrinsicSig.RepeatsAnArgument` is `MaxArgs` unbounded,
and `IntrinsicRepeatedArgumentDriftTests` pins that to the ellipsis in every §15.x.2 general format, BOTH ways —
CONVERT and FIND-STRING are Variadic through their PHRASE words (MaxArgs 4 and 3, no ellipsis) and repeat nothing;
TRIM's 2023 `[ argument-2 ] …` does (the figure notes: the ellipsis "denotes repetition of that bracketed portion").
So an ALL on MOD/ANNUITY/LOG/INTEGER-OF-DAY is COBOLNET1645 at ANY cardinality (four negatives), and the arity gate
counts the arguments as written — a fixed ALL as its elements, a runtime ALL as the ONE argument §15.3 guarantees.
"The evaluation of an ALL subscript shall result in at least one argument, otherwise the result … is undefined":
COBOL.NET defines the undefined case as EC-ARGUMENT-FUNCTION (set when checking is on) and terminates the reference
by that name either way — never an empty list into a body (CONFORMANCE.md determination).

**RV-15.60.4-1 rode along:** under a STANDARD mode the summing family (SUM/RANGE/MEAN/MEDIAN/MIDRANGE) now always
evaluates on the SDIDI carrier (`RenderDec`'s `alwaysDec`; the native `MeanScaled` standard branch, which summed on
Int128 and divided in SDIDI, is deleted). The native arms first ALIGN every argument to the list's maximum scale on
Int128 — a 31-digit integer beside a scale-18 item needs 49 digits — so `MEAN(10³⁰, 2.0)` raised a size error
where §15.4.1 + §8.8.1.5.2 hold 500000000000000000000000000001 exactly. Golden `pb62_standard_decimal_summing_family`
pins all five with receivers pre-set to 7.

**Measured (golden `pb62_all_subscript_runtime_ranges`, values derived in its header):** ODO ALL — MEAN 5, MEDIAN 5,
MIDRANGE 5, RANGE 8, SUM 15, MAX 9, ORD-MAX 3; at WS-N = 4 the EVEN median 3.5 and MEAN 4.25; two ALLs and a
written operand in source order 39; a dynamic-capacity table at capacity 3 SUM 60 / MIDRANGE 20; `C(ALL ALL)` 21
with the rightmost ALL fastest (ORD-MAX 6), `C(2 ALL)` 15, `C(ALL 3)` 9; the string body MAX/MIN/ORD-MIN over an
ODO ALL; a dynamic table nested in a fixed one — SUM(ND(ALL ALL)) = 6, each inner range read for its outer index;
PRESENT-VALUE(RT AM(ALL)) 481.5927; TRIM(X8 TC(ALL)) over an ODO list of trim characters; CONCAT(WD(ALL)); and
FMT-15.21.2's two CURRENT-DATE legs pinned (21 and 4).

**Registered:** `kb/Work/PB81` — SUBSTITUTE with a table(ALL) argument. Its format repeats the PAIR, so the ALL is
admissible and meaningful (a 2-element table is one from/to pair), but `BindSubstitute` pairs operands at bind time
and `RenderSubstitute` splits by index — a runtime-count contribution to that interleave needs a runtime pairing
with per-element mode flags. It is now a bind-time COBOLNET0899 stage (`substitute-all-subscript-argument`) where it
used to compile clean and throw at run time on the plain subscript path; the fix shape is in the note. (`PF`, which
a probe used as a data-name, is the §8.9 report-writer word — the probe was wrong, the compiler right.)

**Gates (wave-local).** Solution build · Characterization 33/33 (after routing the three new emissions through
`RuntimeApi.TableAllArgs/TableArgConcat/With` — `RuntimeApiGuardTests` caught the bare `CobolTable.` strings, as it
should) · Conformance `Intrinsic|Corpus|Function|Table|All|Occurs|Dyn|Negative|Statistic|Standard|SpecTraceability`
**1217/1217** · Unit `SpecTraceabilityInventory|Intrinsic|Diagnostic|Bound|Place|Renderer|Catalog|Manifest|Table`
**190/190** (the new `IntrinsicRepeatedArgumentDriftTests` green; DIAGNOSTICS.md regenerated for COBOLNET1645 and
the 0899 descriptor) · CLI probes. Battery owed for the batch. Docs: COBOLNET_INTRINSICS_DESIGN.md (the ALL
section), CONFORMANCE.md, kb/Work PB62 → landed, PB81 new, plan §0.


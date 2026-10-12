## Entry 1372 — 2026-08-29 05:25 PDT — PB133 wave C1 LANDED: the OMITTED-argument facility whole — GR11's two omission forms, the §8.8.4.8 condition, OPTIONAL formals, GR12's checked raise in the CALLEE's engine, GR1c's transitive omission, and the AS NESTED count/SR24 bind checks

ONE presence law end to end: an omitted argument (written OMITTED or a trailing omission) crosses the ABI
as a null carrier — ManagedPointer<T>.OmittedArgument answers IsNull — and everything that asks reads that
one fact: CobolArgAdapt.Present, the new `data-name IS [NOT] OMITTED` condition (a comparisonExpression
alternative bound against THIS element's formals per §8.8.4.8 SR1, rendered as the formal carrier's
IsNull), and GR1c's transitive omission (a carrier-resident formal forwarded BY REFERENCE passes its
CARRIER itself — presence rides to the next callee, and the sanctioned as-an-argument reference form never
touches the accessors). The §14.2.2 OPTIONAL phrase now CARRIES on LinkageFormal — the staged
OptionalFormal rejection is lifted on both sides.

GR12 rides the CA10 checked-raise posture: >>TURN EC-PROGRAM-ARG-OMITTED CHECKING ON makes a reference to
an omitted formal the fatal EC in the CALLEE's own engine — the old carrier threw CobolCallException
unconditionally, which the CALL SITE's catch arm treated as an ACTIVATION failure, unwinding an
in-execution raise into the CALLER's ON EXCEPTION phrase (GR3i forbids exactly that). Checking OFF stays
lenient with the documented benign-empty read (probed: B-LENIENT=0000; TURN ON: the fatal EC named).

With the AS NESTED callee's header known at BIND, two checks became diagnostics: the §14.8.2.1 argument
COUNT (equality except trailing OPTIONAL formals omitted — COBOLNET1684) and §14.9.4.3 SR24's
OMITTED-needs-OPTIONAL correspondence (COBOLNET1685, closing the row PB130 left PARTIAL); §8.8.4.8 SR1
draws COBOLNET1686. The facility is edition-gated whole as omitted-arguments-2002 (one construct row, four
parse-arm gates — OMITTED in both argument spellings, OPTIONAL in both header spellings, the condition;
85 draws COBOLNET0900 and the version matrix runs the probe at all four editions).

Verdicts: SR-14.9.4.3-24 → CONFORMS, GR-14.9.4.4-11 → CONFORMS, GR-14.9.4.4-12 → PARTIAL (named residue:
BY CONTENT forwarding of an omitted formal, and a non-resident GROUP formal's forwarding losing presence).
Wave C2 remains: §14.8.2/§14.8.3 description conformance + EC-PROGRAM-ARG-MISMATCH, the image-incapable
group arguments, and the §14.2.3 GR9 second-regime BY CONTENT allocation.


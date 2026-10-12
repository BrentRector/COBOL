## Entry 607 — 2026-07-04 19:55 PDT — Property REFERENCES live: the BoundStores polarity walk, the GR1–GR3 BoundSequence desugar, 0843 band (the D-P2 increment)

**What landed.** `P OF {identifier|Class-name}` (ISO §8.4.3.9) now BINDS — the DEVLOG-606 named-0899
stage is retired. The shape:

- **The hook** (ReferenceResolver's resolution-failure exit): the single-qualifier shape whose qualifier
  is a class name (→ FACTORY accessors, SR3/SR4 "or in the factory object" — live directly, the brief's
  0899 factory stage predated D11) or a TYPED object reference, and whose head names a roster accessor
  under the pinned `__GET_/__SET_` names, synthesizes a compiler temp (level-1 elementary cloned from the
  accessor's crossing description; `Roots`-declared post-resolve — FieldEmitter needed NO changes; uid
  band; `__prop<uid>_<PROP>`), registers a pending op, and returns the temp INTO the normal resolve tail —
  so refmod on the property value rides RefModPlace unchanged and subscripts reject on the OCCURS-less
  temp. SR1 (REPOSITORY PROPERTY specifier, §12.3.8 — captured new in DataBinder) and SR2 (universal
  receiver) = 0843 at the hook.
- **The polarity classifier** — NEW `Binding/Bound/BoundStores.StoreKindOf(stmt, item)`: a TOTAL explicit
  taxonomy over every BoundStatement returning None/Write/ReadWrite — the §8.4.3.9.4 rule selector
  (GR1 pure sending → get only; GR2 write-only receiving → set only, the get is NOT invoked; GR3
  read-modify-write → get before + set after, ONE temp). Polarity is per STORE POSITION, from the
  15-agent emitter-verified survey (`bound_stores_classification.md`, 119 nodes + 4 helpers, zero
  spot-check misclassifications): in-place arithmetic (ADD TO / SUBTRACT FROM / MULTIPLY BY / DIVIDE
  INTO / SET UP-DOWN / INSPECT counters / STRING Into per GR7 / pointers+tallies / CALL BY REFERENCE /
  INVOKE writeback) = ReadWrite; GIVING forms / MOVE / COMPUTE / ACCEPT / READ-RETURN INTO / UNSTRING
  receivers = Write. Child recursion covers every phrase body (SIZE ERROR / AT END / INVALID KEY /
  ON EXCEPTION / OVERFLOW / EOP), IF/EVALUATE arms, inline-PERFORM bodies, SEARCH arms, EcChecked.Inner,
  Sequence.Steps. An UNCLASSIFIED node → 0843 LOUD — never a guess about whether a side-effecting
  accessor runs.
- **The drain** — `StatementBinder.OoWrapPropertyOps` at the ONE `BindStatement` chokepoint,
  mark-on-entry / drain-own-suffix (a reference in an IF condition belongs to the IF, not to an arm that
  binds later). SR3/SR4 check HERE, against the CLASSIFIED need — so `PROPERTY WITH NO SET` stays
  readable (spine-tested). The wrap emits the NEW `BoundSequence` node (pre-GET invokes / statement /
  post-SET invokes) over the EXISTING BoundInvoke + D6 marshaling — exactly the spec's "as though the
  associated property method were invoked, in accordance with the rules of the INVOKE statement".
- **Goldens (3 new, greenfield-only; 19 oo_*):** oo_property_ref — DISPLAY/MOVE-TO/ADD-TO over a
  clause-synthesized property (00100/00555/00575; the FIRST runtime exercise of synthesized accessors);
  oo_property_explicit_ref — side-effecting explicit accessors PROVE the invocation counts (a
  receive-then-send pair prints exactly SET-CALLED then GET-CALLED — GR2 never invokes the get, GR1
  never the set); oo_property_factory_ref — `P OF Class-name` through the factory singleton (0042).
- **Spine tests (+4, one retired-to-positive):** SR1 0843; SR3 via WITH NO GET (sending); SR4 via
  WITH NO SET (receiving) + the SAME property readable as a sender; the 606 staging test became
  PropertyReference_Binds_WithSpecifier (the boundary it asserted no longer exists).
- **Groundwork shipped with the wave:** the `BoundSequence` statement node + emitter case (renders
  children consecutively; terminates iff the last child does), the REPOSITORY PROPERTY specifier capture
  (`DataBinder.OoRepositoryProperties`). GROUP-valued property references stage 0899 (temps over a group
  description — later refinement); interface GET/SET PROPERTY prototypes remain 0899 (ride the
  universal-reference/EC-OO waves).

**Process note (the workflow payoff).** The classifier's correctness burden was "know every store
position in a 119-node taxonomy" — fanned out as one survey agent per Bound/*.cs file + an
emitter-cross-checking synthesizer (15 agents, ~757k tokens, 8 min); the synthesized table (with
polarity and member paths, zero misclassifications on the 10 riskiest spot-checks) became
`BoundStores.cs` almost mechanically. Friction, honest: one bash-heredoc patch attempt violated the
Write-tool patch-script rule and mangled (caught by the shell, not the repo); the first hook draft used
a stale doc-comment anchor (assert caught it — the discipline works).

**Battery:** conformance **1558/1558** (+6: 3 goldens, the rewritten stage test, SR-family theories) ·
unit **101/101** · OoSpine+Corpus+Matrix 433/433 · **guard-fast ALL GREEN** (545 integration; no grammar
change this increment — the .g4 surface is untouched, so the FULL guard ran last commit stands).



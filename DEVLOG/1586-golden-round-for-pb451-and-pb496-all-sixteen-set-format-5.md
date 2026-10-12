## Entry 1586 — 2026-09-09 17:20 PDT — Golden round for PB451 and PB496: all sixteen SET Format-5 / USAGE OBJECT REFERENCE rows close, and one of the two notes was WRONG about the standard

**PB389 (train 25) closed both notes' mechanism; this round proved it, row by row, and refused to take one of
them at its word.** `kb/Work/PB451.md` (ten rows) and `kb/Work/PB496.md` (six) were written before the
object-reference descriptor existed and describe a compiler in which `USAGE OBJECT REFERENCE ACTIVE-CLASS` drew
COBOLNET0813 *"names the unknown class or interface"* beside COBOLNET0901 *"is a reserved word"*, a trailing
`ONLY` was a bare COBOL0307, `FACTORY OF` was loud-staged COBOLNET0899, and §14.9.39.3's whole receiver
discrimination lived on one arm of each of two dispatches. **Every one of those measurements is now false.**
Each of the sixteen rules was re-quoted from `specs/ISO_COBOL.md` and `cite.py --check`ed BEFORE anything was
derived (27 citations, all OK on the first run), the expected result was written into a golden's header before
the golden was run, and **every closing row measured exactly what was derived**. GAP **2659 → 2643**.

**⛔ PB451's repair 4 was WRONG, and implementing it would have made the compiler reject legal source.** The note
asked for §14.9.39.3 SR10 d) to screen DIRECT `IMPLEMENTS`-clause membership instead of the transitive closure,
on the reading that *"that factory definition shall be described with an IMPLEMENTS clause that references
int-1"* is a literal claim about the clause's operand list — and it recorded three probes (`r01`, `r02`, `r06`)
as evidence that `OoClassTable.ImplementsClosure` is over-permissive. It is not, on four independent grounds
from the standard: **(a)** §11.4.4 GR1 defines the clause THROUGH implementation — *"The IMPLEMENTS clause
specifies the names of the interfaces that are implemented by the factory object of the containing class
**according to 9.3.11, Interface implementation**"* — and GR2 then enumerates the relation (direct clause · an
implemented interface that INHERITS int-1 · an inherited class whose factory object implements it); **(b)**
§9.3.8.2.3 rule 5 b)2.a. states the SAME obligation about the SAME objects with the *other* spelling (*"the
factory object of the specified class shall be described with an IMPLEMENTS clause that references int-r"*)
where SET SR10 b)1. writes *"shall implement int-1"* — two phrasings, one rule; **(c)** Annex D.19.5 paraphrases
§13.18.60.4 GR22 c), normatively *"shall implement interface-1"*, as *"an object described with an IMPLEMENTS
clause that references the interface specified"*, so the standard glosses its own rule with the other spelling;
**(d)** the reductio — under the literal reading, inside a subclass whose own definition carries no IMPLEMENTS
clause `SET R TO SELF` is illegal while `SET T TO SELF` (T typed at the containing class) followed by
`SET R TO T` is legal under SR10 b)2., identical semantics one statement apart. All three of the note's
"over-permissive" probes now ship as POSITIVE legs of a golden, and SR10 d)1.'s REFUSING arm — which had no
shipped witness at all, only d)2.'s — is a new negative whose discriminator is the factory/instance axis: the
INSTANCE definition implements the interface and the FACTORY definition does not, so a screen keyed on the wrong
definition accepts the program. The note now carries the withdrawal inline, beside the paragraph that was wrong,
because the failure mode CLAUDE.md rule 1 names is INHERITING a claim, not inventing one.

**One live defect found and fixed: the class-NAME sender's diagnostic cited a rule whose precondition was false
for it.** `OoBinder`'s object-class-name-sender arm printed *"(ISO §14.9.39.3 SR13)"* for EVERY receiver kind.
SR13's own precondition is *"the data item referenced by identifier-3 is described with an object-class-name"* —
false for an interface-name receiver (SR11 governs) and for an ACTIVE-CLASS one (SR14, whose closed list of
senders excludes a class name) — and the claim the message was actually making, that the sender IS the class's
factory object, is **§14.9.39.4 GR10**, not a syntax rule at all. The governing rule is now READ OFF THE
RECEIVER by `OoConformance.ClassNameSenderRule` (SR11 · SR13 · SR14 · SR8), pinned by a drift theory that fails
if a new `ObjectRefKind` falls through to the discard and collides with SR8. A hand-maintained per-call-site
label is exactly the shape CLAUDE.md rule 5 forbids.

**A second citation trap, recorded because `--check` cannot catch it.** §13.18.60.4 GR22 d)2.'s a. and b.
sentences are STRICT PREFIXES of d)1.'s (*"… the factory object of the specified class"* vs *"… of the specified
class or of a subclass of the specified class"*), so `cite.py --check` on either body matches the **d)1.** line
and returns OK — the discriminating fragment is the lead-in *"If the ONLY phrase is specified"*. That is the
same trap §14.9.39.3 SR14 b)1./b)2. set, whose sentences are word-for-word SR12 c)3./c)4.; SR14 b)'s own
discriminator is its lead-in *"the predefined object SELF"*, where SR12 c) writes *"the predefined object
REFERENCE SELF"*. **A `--check` OK is evidence about the TEXT, never about which rule you meant.** It is also
the argument for ONE placement predicate: `rd.Factory != host.OoInFactory` is written once and serves all four.

**What shipped.** Four positive goldens and eleven negatives, one clause each. `pb451_set_format5_class_name_sender`
(SR11 direct and through §11.4.4 GR2 c); SR13's lead, a) and b); §14.9.39.4 GR10's multi-receiver order —
`SET F1 F2 TO PB451D` puts a SUBCLASS factory object in both). `pb451_set_format5_self_sender` (SR12 c)2./c)3./
c)4. and d)'s NULL sender, observed through §8.8.4.2.15's object relation condition; SR10 d)1./d)2. over all
three GR2 legs). `pb496_active_class_positions` (§13.18.60.3 SR16 names FOUR permitted positions and PB389's
golden writes one — this writes the other three, a factory definition's WORKING-STORAGE, an instance
definition's WORKING-STORAGE and a method's LINKAGE SECTION, and makes each OBSERVABLE by running the SAME
inherited method on a base and on a subclass: `FTAG-C`/`FTAG-D`, `WHO-C`/`WHO-D`, which a statically-bound
ACTIVE-CLASS cannot do). `pb496_object_reference_storage` (GR22 **a)**, the sub-rule nothing measured). The
negatives pin SR11, SR13's lead / a) / b), SR14's closed list, SR12 b)2. / c)3. / c)4., SR14 b)1. / b)2., and
SR10 d)1. The measured CS0030 — a Roslyn error against GENERATED user source naming a synthesized type that
appears nowhere in the program — is gone: SR12 c)3.'s violating program is refused at bind with its own clause.

**A required determination was owed and is now written.** GR22 a) — *"The amount of storage allocated for an
object reference data item is implementor-defined and is not necessarily the same for every object reference"* —
is Annex A.1 item **214**, REQUIRED user documentation, and there was no §7 row for it. `docs/CONFORMANCE.md`
gains **DOC-A.1-214**: one .NET managed reference, 8 bytes, the SAME for every one of the general format's
shapes, with no alignment and no character positions in any group image (§13.18.60.3 SR14 keeps an object
reference at level 1 or under a STRONG type, SR15 out of the file section). The amount is OBSERVABLE — §15.14.3
AR1 admits *"a data item of any class or category"* and §15.14.4 rule 1 returns the length in bytes — so the
golden measures all five declarable shapes and gets one number. GR22 a) expressly permits them to DIFFER, which
is what makes five equal numbers a determination rather than a tautology. The A.1 register moves 58 → 59 of 222.

**The dead descriptor is retired, and only the descriptor.** `DiagnosticCatalog.OoFactoryObjectReference`
(`oo-factory-object-reference`) became unraisable the moment PB389 landed, so `docs/DIAGNOSTICS.md` was
documenting a code the compiler cannot produce. It survived on purpose until now: `SpecTraceabilityInventory-
DriftTests` satisfies a `#Symbol` reference by a word search, and six of PB496's rows named it in their
`code-location` — deleting it earlier would have turned them red. It is deleted in the same change set that
re-points them, and `docs/DIAGNOSTICS.md` regenerated (one line). ⛔ **The CODE `COBOLNET0899` is NOT retired**:
it is the shared *recognized-but-not-implemented* code and ≈40 other descriptors still emit it. Only this NAME
is gone, and it is never to be reused (the COBOLNET1518 precedent).

Both notes flip to `status: landed` with `inventory_rows: []`. Wave-local gate GREEN — 27,384 tests, 0 failures,
five filter terms all live, citation audits at zero, `audit_annex_a1.py --check` with no findings,
`work.py check` at 857 items. **No diagnostic code was claimed.**


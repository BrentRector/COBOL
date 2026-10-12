## Entry 1704 — 2026-09-25 04:09 PDT — Train 63: w59j, the standard class BASE (PB1548+PB1506+PB1524+PB1428), landed alone

**Wave 59 J — the standard class BASE (kb/Work PB1548, with PB1506, PB1524, PB1428: one root).** §16.1 requires the
implementation to provide BASE ("A standard class BASE shall be provided by the implementation", OK §16.1). COBOL.NET
refused `INHERITS FROM BASE` (COBOLNET0821 even with `CLASS BASE` in the REPOSITORY) while treating New as predefined
on EVERY class — an under-reject of §14.9.23.3 SR3 ("The value of literal-1 shall be the name of a method defined in
the factory interface of object-class-name-1", OK §14.9.23.3), since New lives in BaseFactoryInterface (§16.2) and a
class has it only through INHERITS (§9.3.9, §9.3.14.3). Re-probe on the implementer's build: the misc-p28 draft drew
0821, and `2002/oo_hello` (no INHERITS, `INVOKE GREETER "NEW"`) compiled clean; the note's premise "0821 is correct
when CLASS BASE is declared" did NOT hold. Fix shape: BASE is one per-group symbol (`Oo/OoStandardClasses.cs`, never in
`Classes`; a group-defined class named BASE wins — §12.3.8.4 GR6 determination, `docs/CONFORMANCE.md` DOC-A.1-163)
carrying New + FactoryObject with their §16.2 signatures, answered by `OoClassTable.Find` only when no group class
claims the name; one binder tail `OoBinder.OoBindStandardInvoke` replaced three NEW copies (the deleted
`OoBindImplicitNew`), so every receiver arm — class name (SR3), FACTORY OF / ACTIVE-CLASS (SR4 a/c), SELF/SUPER in a
factory method (SR4 f/h) — refuses New without BASE with the new COBOLNET2448 `new-without-base`; the inline form of
New is refused by §8.4.3.4.3 SR4 (COBOLNET2140, PB1428). The runtime pair `CobolNet.Runtime.BASE` / `BASE__FACTORY`
carries one `__New()` body with §16.2.1.2 GR2 (resources exhausted → EC-OO-RESOURCE when checked, new
`OoResourceChecking` flag in the EcEmitter gate table, else NULL; PB1524), and universal object deliveries narrow
through `CobolObject.NarrowUniversal` (EC-OO-UNIVERSAL, never an unchecked cast). Every golden that pinned the
under-reject was non-conforming SOURCE and was fixed at the source, output unchanged: 97 positives, 21 negatives and
11 C# test files now write `INHERITS FROM BASE` + `CLASS BASE` on the root class of each NEW'd hierarchy;
`2002/pb428_inline_method_invocation` step 7 invokes a factory method instead of the now-refused `PB428ACC :: "NEW"`.
Goldens: `2002/pb1548_standard_class_base`, `2002/l1c28_self_super_invoking_object` (the held misc-p28 draft),
negatives `pb1548-new-without-base` (2448) and `pb1548-inline-new-active-class` (2140); tests
`StandardClassBaseTests`, `Unit/StandardClassBaseNewResourceTests`. Rows (batch of 7): CONFORMS GR-16.2.1.2-1/-2,
GR-16.2.2.2-1, SR-8.4.3.4.3-4; witnesses GR-8.4.3.8.4-1/-3, SR-14.9.23.3-3. Self-review: one symbol + one binder tail +
one runtime body; +1 null store and one virtual call per NEW; `OoNameResolutionDriftTests` count 6 → 4 (adjudicated).
The implementer filed one note-ready lead: COBOLNET0836 rejects every factory `METHOD-ID. NEW` (legal as an ordinary
method outside BASE, and as `NEW OVERRIDE` inside it — §11.7.3 SR3), site `Oo/OoClassTable.cs:411`.

**The train.** One cluster, landed alone on purpose (169 files; it unblocks the golden-lane rows held on PB1548). The
patch applied onto main `bd139c33b` with four whole-element conflicts (`docs/DIAGNOSTICS.md`, `kb/Work/PB1548.md`, the
2002 and negative corpus manifests), each resolved by keeping both elements; the inventory hunk was discarded and the
batch re-applied on the merged tree. Two conversions the rebase required, both the cluster's own fix applied to source
that landed after its base (output unchanged): the train-62 goldens `2014/w59h_vlg_override_signature` and
`negative/w59h-vlg-override-fixed-group` NEW a root class, so `W59HDB`/`W59HNB` now inherit BASE; and the lander's
whole-assembly gate went RED on 15 `VersionMatrixTests` cases — five construct samples in `constructs.json`
(`invoke-2002`, `inline-method-invocation-2002`, `exit-method-window`, `method-working-storage-window`,
`usage-object-reference-only-2002`, × 2002/2014/2023) NEW a class with no INHERITS and now draw COBOLNET2448 exactly as
SR3 requires; their root classes inherit BASE. (The implementer's gate carried no `~VersionMatrix` term — the
population no union names, again.) Gate: whole Conformance `Failed: 15, Passed: 8837` (all 15 the samples above) →
after the sample fix `~VersionMatrix|~Construct|~Vcr` `Passed: 2511`, full Unit `Passed: 29302`, Characterization
`Passed: 33`, legacy Integration `Passed: 503, Skipped: 1`; every new golden/negative seen passing by name (17 cases);
CITATIONS and DOC CITATIONS green; WITNESS LOSS green (1 retired, 10 re-sited); semgrep verify PASS. GAP 1254 → 1248.
Nothing dropped.


## Entry 647 — 2026-07-06 21:42 PDT — M2-OO-1i inc 4: OBJECT-paragraph files are per-object connectors (§9.1.4)

Increment 4: the OBJECT (instance) half of the object/factory file leg. An OBJECT-paragraph file connector belongs
to the object instance — §9.1.3 (one connector per FD, and the FD lives in the per-object instance definition) +
§9.1.4 (the runtime executes an implicit CLOSE "for file connectors in an object when the object is deleted"; the
NOTE licenses GC-deferred timing). So an object file gets a UNIQUE per-object connector key, not a static literal.

- **`OoQualifyClassFiles` instance branch:** a non-EXTERNAL object file keeps a class-qualified BASE key
  (`Class::INST::name`) AND gets a minted-key field name `f.InstanceKeyField = "__fkey_" + DataItem.Sanitize(name)`;
  an EXTERNAL object file keys `::EXT::name` (inc 5).
- **`OoEmitFileMembers` per-object field:** each instance file emits
  `private readonly string __fkey_X = CobolFile.MintInstanceKey("Class::INST::X");` (a field initializer, so it runs
  once per object BEFORE the ctor body), and the ctor registers under that minted key (`FileKeyExpr` → `this.__fkey_X`
  from inc 2) and calls `__TrackInstanceFile(this.__fkey_X)`. Two objects therefore hold DISTINCT connectors that
  never clobber each other.
- **Runtime (`CobolFile`):** `MintInstanceKey(base)` = `base + "#" + Interlocked.Increment(_instSeq)` (`_instSeq`
  resets in `Init` for determinism); `CloseAndDrop(key)` = the §9.1.4 implicit CLOSE (reuses `Close`, sequential OR
  keyed) + registry removal; the run-unit `CloseAll` remains the backstop.
- **Runtime (`CobolObject`):** `__instFiles` + `__TrackInstanceFile` + a `~CobolObject()` finalizer that
  `CloseAndDrop`s each tracked connector. **Production refinement:** the base ctor `GC.SuppressFinalize(this)`, and
  only `__TrackInstanceFile` (first file) `ReRegisterForFinalize` — so the §9.1.4 finalizer burdens the GC for exactly
  the file-owning objects, never the (overwhelmingly common) fileless object. `CloseAndDrop` is idempotent, so a
  program that already CLOSEd its files finalizes harmlessly.
- **GLOBAL guard (COBOLNET1520)** extended to the OBJECT half (the object gate, was COBOLNET0899, is removed):
  §13.18.27.3 SR4 bars GLOBAL in an instance definition.

Goldens (GreenfieldOnly): `oo_object_file` (an object OPEN OUTPUT→WRITE→CLOSE→OPEN INPUT→READ round-trip →
`GOT=HELLO OBJECT`); `oo_object_file_two_instances` (two objects open the SAME file on their OWN connectors — A holds
its read position across B's independent open: `R=AAA` / `R=AAA` / `N=BBB`; a SHARED connector would status-41 on B's
open-while-open and reset A's position). Plus the `ObjectFile_Global_1520` fact. Battery: **1968 conformance (+3) ·
216 unit · legacy integration 78 GREEN**, zero regressions. NEXT: inc 5 — EXTERNAL object/factory FD (the `::EXT::`
qualification already lands here; the record re-basing already runs on the class binder via `CallBindExternalAndGlobal`
in `BindResolve` — inc 5 is the golden that verifies program↔object sharing).


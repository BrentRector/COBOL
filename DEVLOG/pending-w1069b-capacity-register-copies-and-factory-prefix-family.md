## PB2988 + PB2921: a TYPEDEF's CAPACITY register is reproduced by every copy; synthesized C# names are prefix families

Wave 1069 group B (PB3030, PB2921, PB2988 — one root in the data model's naming and copy identity).

**PB2988 — a CAPACITY IN register written once in a type, used by two TYPE entries.** Re-probed on the wave's build
(ddee217e3): the note's e6 (`01 ST TYPEDEF` holding `OCCURS DYNAMIC CAPACITY IN DC`, then `01 A TYPE ST` and
`01 B TYPE ST`) still drew COBOLNET1523 at B's expansion and COBOLNET2127 at `DC OF B`. §13.18.38.3 SR30 says
"Data-name-3 shall not be defined elsewhere in the source element. If qualifiers are required for uniqueness, it shall
be treated as though implicitly defined at the same level as the entry containing the OCCURS clause" — the second
sentence exists exactly for the copies a TYPE (or SAME AS) entry makes of one written definition. The screen in
`DataBinder.DynamicResolve` counted each copy as another definition. It now compares WRITTEN definitions
(`DataItem.WrittenDefinition`, the `ClonedFrom` chain's end): copies of one written entry are listed side by side under
the name (the register map was already a list per name, for inherited GLOBAL registers), and only a different written
entry or an explicit data-name stays the SR30 duplicate.

The sibling arm was the resolver. `ReferenceResolver.CapacityRegisterFor` was documented "at most one register per
spelling in one element" and took the first qualifier match, so once copies could coexist the bare `DC` would have read
A's register silently. It now counts the registers the qualifiers reach in the nearest declaring element and states
§8.4.2.2.3 SR1 through the one ambiguity verdict, `DataBinder.UniqueOrReportAmbiguous` (COBOLNET1639; `--permissive`
warns and takes the first), as the new `CapacityRefFault.Ambiguous`. `SetBinder.BindSetCapacity` takes the resolver's
place for every non-None fault, so a SET Format 14 receiver obeys the same verdict. Goldens:
`2014/pb2988_capacity_register_type_copies` (two TYPE copies, a SAME AS copy, two TYPE copies inside one group read
through two qualifiers, SET through a qualified register — capacities 0 0, 1 3, 3 7, 2 4 5 6 from §8.5.1.9.1,
§8.5.1.9.3 and §14.9.39.4 GR30), negatives `pb2988-capacity-register-copies-unqualified` (COBOLNET1639) and
`pb2988-capacity-register-two-written-entries` (COBOLNET1523 still holds).

**PB2921 — the factory class's name was a suffix.** Re-probed: a group with `CLASS-ID. X.` and `CLASS-ID. X--FACTORY.`
was refused COBOLNET0820, because X's factory class was `X` + `__FACTORY`, which is also the C# identifier of the legal
class-name `X--FACTORY` (§8.3.2.1 allows inner hyphens; the injective word mapping writes `-` as `_`). The structural
fact is that only a name that BEGINS with `__` lies outside the mapping's image (no identifier begins with `__`), so the
factory class became the prefix family `NamingConvention.FactoryClassName` (`__FACTORY_X`, the runtime's
`__FACTORY_BASE`), every site routed through it, and `OoClassTable.Build` now treats a taken factory name as an internal
error. The same defect in another arm: the CS0542 rename of a METHOD-ID spelled like its CLASS-ID was the suffix `_M`
(the identifier of the method-name `X-M`); it is now the family `__method_`. Both join
`CobolWordDerivedPrefixes`, so `SynthesizedNameFamilyDriftTests` holds every emitter literal out of them, and
`IdentifierInjectivityTests` pins that no identifier begins with `__` and that the runtime's BASE factory type is the
convention's name for BASE. Golden `2002/pb2921_factory_named_class`. All 244 OO positive goldens re-run on the build.

Renaming the runtime's public `BASE__FACTORY` to `__FACTORY_BASE` is an incompatible change of the shipped surface, so
the gate's `RuntimeAbiPinDriftTests` asked for the documented step (DESIGN-external-repository §15.4): the product
version goes 2.0.0 → 3.0.0, the test pins major 3 beside the new surface hash, and the 14 characterization snapshots
name the runtime `3.0.0.0`. A local harness that re-ran the 244 OO goldens in their own directories rewrote a tracked
data file with CRLF and the gate's LINE ENDINGS audit caught it (`line_endings.py --fix`).

PB3030 (the bit-bearing OCCURS DEPENDING member beside a runtime-length table) re-probed and still the COBOLNET1756
stage; it is a third mechanism (the current-extent composer's ODO prefix in bit units, through the record layout) and
was split to a finisher with its design written into the note. Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched
gating, PB2515): GREEN ===` (run 20261012T194921Z-2feabf).

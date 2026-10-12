## Entry 1385 — 2026-08-29 09:15 PDT — PB143 LANDED: the keyed record store is PER PHYSICAL FILE — a DELETE through one connector is a deletion for all, and the close order cannot resurrect it

The architectural item (kb/Work PB143; §14.9.10.4 GR5 — 'removed from THE PHYSICAL FILE'). Every keyed
connector used to load a PRIVATE snapshot at OPEN and persist its WHOLE view at CLOSE: a record DELETEd
through one connector stayed readable through another over the same host path, and the CLOSE ORDER decided
which private view survived on disk — silent undeletion / silent data loss (reversing file_sharing_mutate's
two CLOSEs would have resurrected the deleted record and dropped the other connector's WRITE). Now
KeyedStoreTable (registry-owned, keyed by resolved host path like the PhysicalFileTable) holds ONE
RelativeStore / IndexedStore per host: first opener loads, later openers ATTACH to the live store (never
reload — the in-memory store is the truth while any connector holds it), every mutation is instantly
visible across connectors, any CLOSE persists the one shared state, the last detach drops the entry, OPEN
OUTPUT empties the shared view, and the indexed ARRIVAL MINT is shared so cross-connector writes keep one
§14.9.30 GR26 retrieval order. Position/key state stays per-connector; sequential connectors are out of
scope (their OS-backed streams already share); a store-less connector (a focused unit test) keeps a private
store. The connectors' fields became property views over the attached store, so the ~40 verb sites compile
unchanged. Also swept: the three keyed READ registry entries still carried PB140's fail-open
'30'-without-SetStatus miss arms — now Require/MisroutedVerb like their siblings; and DeleteShared's
missing LocksEffective consult was VERIFIED A NON-DEFECT (DELETE acquires no locks; GR7's releases are
vacuously correct) and documented at the site.

Verdict: GR-14.9.10.4-5 DIVERGES → CONFORMS. Golden 85/pb143_shared_visibility (the row's exact demanded
trace, close-order-reversed durability included) + four SharedRecordStoreTests facts over a private
registry. docs/COBOLNET_FILES_DESIGN.md carries the design.


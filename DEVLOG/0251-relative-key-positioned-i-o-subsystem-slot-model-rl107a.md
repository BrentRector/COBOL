## Entry 251 — Relative key-positioned I/O subsystem (slot model); RL107A CLEAN

Built the RELATIVE-file key-positioned I/O subsystem from the spec (not the tests). The old
`RelativeFileHandler` appended every WRITE to the stream tail and seeked by record number on read,
ignoring the relative key and unable to represent gaps — so random/dynamic creation, occupancy, and
INVALID KEY were all wrong. Rewrote it to the slot model the spec describes (§9.1.2: "a serial string
of areas, each denominated by a relative record number ... whether or not records have been written
in any of the first through ninth areas"): an in-memory `SortedDictionary<int,byte[]>` of OCCUPIED
slots (like the indexed handler), persisted to a sparse flat file (slot N at offset (N-1)*recLen,
gap slots written 0xFF; on load a slot is occupied unless all-0x00/all-0xFF).

Operations, grounded in the WRITE/READ/REWRITE/DELETE general rules:
- **WRITE** — §14.9.51 GR 29: *sequential* access auto-assigns the next ascending relative number
  (OUTPUT from 1, EXTEND from max+1) and MOVEs it into the RELATIVE KEY; a number exceeding the key's
  digit size → 24. *random/dynamic* access requires the program to set the RELATIVE KEY first; an
  occupied slot → 22; a key < 1 → 34.
- **READ random** — §14.9.30 GR 29 / §9.1.14: the record at the RELATIVE KEY, absent → 23 (INVALID KEY).
- **READ NEXT** — §14.9.30: next *existing* slot (skips gaps); GR 25 MOVEs its relative number into
  the RELATIVE KEY; if the selected record's number exceeds the key digit size → 14 (an at-end
  condition per §14.7.4 — so `FileRuntime.IsAtEnd` now also treats "14" as at-end). An empty file →
  10 (no record found), per the same rule — so the spec does NOT yield "14" on an empty file.
- **REWRITE** — §14.9.35 GR 21: random/dynamic replaces the keyed record, absent → 23; sequential uses
  the current record. **DELETE** — §14.9.12 GR 4: random/dynamic removes the keyed record, absent → 23.

Compiler plumbing: `Binder` emits `FileRuntime.SetRelativeAccess` (sequential vs random/dynamic per
ACCESS MODE) and carries the RELATIVE KEY's digit capacity; `FileIoLowerer` emits `IrSetRelativeKey`
before a random/dynamic WRITE/REWRITE/DELETE (conveying the program's key to the runtime) and
`IrStoreRelativeKey` after a sequential WRITE / sequential READ (moving the assigned number back into
the key, §14.9.51/§14.9.30). New runtime: `SetRelativeAccess`, `SetRelativeKey`, `GetRelativeSlot`.

A previously-passing integration test (`FileIO_RelativeKey_RandomReadByKeyField`) was **non-conformant**
— it wrote a DYNAMIC file in OUTPUT mode without setting the RELATIVE KEY, relying on the old append.
Per §14.9.51 GR 29b a random/dynamic WRITE must set the key, so the corrected (spec-true) behavior is
status 34. Fixed the test to set the key before each WRITE (the conformant DYNAMIC form).

Result: **RL107A CLEAN** (random creation with gaps + read-by-key with INVALID KEY) — deterministic
and idempotent, baselined. **NIST baselines 196 → 197** (… **5 RL** …). RL103A improved (6→4 FAIL*);
RL101A/201A/209A/302M still pass. Full guard ALL GREEN: 1000 unit / 348 integration (347+1 skip) /
197 NIST, 0 regressions. Remaining relative tail (each a distinct follow-up, NOT the core subsystem):
RL117A/RL103A use `RELATIVE data-name` *without* `KEY` (non-conformant CCVS source, §12.4.5.13 — the
key never binds so random WRITE has no slot; needs the documented no-KEY leniency); RL203A/RL208A are
producer/consumer chains (a producer program creates the file, the consumer reads it) whose counts
depend on cross-program persistence; RL110A is sequential-access creation.


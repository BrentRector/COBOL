## Entry 157 — 2026-03-27: Remaining COBOL-85 Gaps — 12 Items via 4 Agents

**Context:** After the P2 sweep (Entry 156), the spec compliance audit still listed 12 partially-
implemented COBOL-85 features + 2 missing validations. Dispatched 4 parallel agents.

**Agent 1: SYNCHRONIZED + COMP-1/COMP-2 IEEE 754**
- StorageLayoutComputer rounds offset to natural boundary (2/4/8-byte) for SYNC items
- COMP-1: DecodeComp1/EncodeComp1 (IEEE 754 single via BitConverter)
- COMP-2: DecodeComp2/EncodeComp2 (IEEE 754 double)
- New IrPrimitiveType.Float32/Float64 (was mapping to Int32/Int64)
- FieldSizeCalculator early-return for COMP-1/COMP-2 (no PIC clause)

**Agent 2: LOCAL-STORAGE + EXTERNAL + GLOBAL**
- LOCAL-STORAGE: ProgramState now has separate LocalStorage byte array. SnapshotLocalStorageDefaults()
  after VALUE init, ReinitializeLocalStorage() on every Entry call. StorageLayoutComputer uses
  separate offset namespace. CilEmitter.EmitLoadBackingArray maps LocalStorage correctly.
- EXTERNAL: new ExternalStorage.cs with ConcurrentDictionary<string, byte[]>. CilEmitter redirects
  all storage access for EXTERNAL items to shared arrays via TryGetExternalField().
- GLOBAL: CBL3119 warning removed. Full nested visibility deferred — programs are separate classes,
  no parent scope chain exists yet.

**Agent 3: File I/O completions**
- 5 missing status codes (02, 04, 14, 34, 39) — 02 wired with HasDuplicateAlternateKey()
- CLOSE WITH LOCK: _lockedFiles set, status "38" on reopen attempt
- READ PREVIOUS: ReadDirection enum, IrReadPreviousToStorage, backward iteration in IndexedFileHandler
- USE declarative execution: EmitUseDeclarative checks status after OPEN/CLOSE/READ, PERFORMs handler

**Agent 4: REDEFINES/RENAMES validation**
- CBL0808 warning: REDEFINES not first clause after data-name
- CBL0813 error: RENAMES THRU item must follow FROM in storage

**Deferred:** Report Writer (XL, not NIST Nucleus), SORT external merge sort (production concern).

**Results:** 260 unit + 236 integration + 60 NIST guard.
Only remaining COBOL-85 gaps: Report Writer, SORT external merge, GLOBAL nested visibility.

---


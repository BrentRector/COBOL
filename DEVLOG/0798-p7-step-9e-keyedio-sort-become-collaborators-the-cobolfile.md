## Entry 798 — 2026-07-11 16:37 PDT — P7 Step 9e: KeyedIo · Sort become collaborators; the CobolFile/CobolSort façade surface lands (60 bare sites routed)

**What (the phase doc's 9e — the file-I/O tier).** `Verbs/KeyedIoEmitter.cs` (git-mv; over ctx+num+**refs**+host —
the first collaborator needing the per-unit `ReferenceResolver` for RELATIVE KEY / record-area resolution) and
`Verbs/SortEmitter.cs` (over ctx+**DispatchState**+host — the INPUT/OUTPUT PROCEDURE bounded dispatch reads
`DispatchName` from the 9b state object, exactly the explicit threading the census demanded; the census also
said Sort never reads `_num`, and CS9113 confirmed it when my ctor over-provisioned — param dropped). The seven
file-I/O common helpers the two consume went internal on the host (`EmitUseHook`/`EmitStoreFileStatus`/
`EmitImageInto`/`EmitReadLengthStore`/`VaryingLengthArg`/`RenderRetry`/`RuntimeRecordLock`) — they RELOCATE into
`SequentialIoEmitter` at 9j. The core file-registration loop calls `_keyedIo.EmitRegistration`.

**Ratchet:** `RuntimeApi` grew the keyed-file surface (RegisterRelative/RegisterIndexed/AddAlternateKey/
SetRelativeKey/ReadKeyed{,Next,Previous}/ReadLockGovern/RelativeSlot/WriteKeyed/RewriteKeyed/DeleteRecord/
DeleteFile/StartFirstLast/StartRelative/StartIndexed + the implicit-loop OpenInput/OpenOutput/Close/Read/Write/
LastReadLength) and the SORT surface (Init/Release/Sort/Merge/NextInput/Close/Rewind/Return/LastReturnedLength +
the `SortKeyArray` literal builder) + `StrRefMod`/`StrCompare`/`TableOcc`. The two old entries (35+25) DELETED;
typed-`CobolRounding` residues pinned (KeyedIo 2 · Sort 1). The ratchet again caught a doc-comment mention
(`CobolSort.Merge` in the MERGE summary — the second catch this phase; the census discipline earns its keep).

**Verify.** Sln Debug + Release clean; 3166 conformance · 281 unit · 33 characterization (32 snapshots
byte-exact) — verdicts read as separate actions.


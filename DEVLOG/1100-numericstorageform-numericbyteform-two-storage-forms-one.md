## Entry 1100 — 2026-07-29 01:09 PDT — `NumericStorageForm` → `NumericByteForm`: two "storage forms" one dereference apart

Caught while surveying the 129 `ImageWidth` references for the re-base: the compiler ALREADY has a
`StorageForm` — `Binding/Model/StorageForm.cs`, the closed classification of how a value is stored in C#
(`NativeInt` / `NativeFloat` / `CharImage` / `TierBWindow` / …), computed by `StorageFormPass` and held on
`DataItem.Storage`. And I had just put a `StorageForm` property on `PicInfo`, **in the same namespace**, meaning
something different: the COBOL byte representation. `item.Storage` is a `StorageForm`; `item.Pic.StorageForm`
was a `NumericStorageForm`. Two concepts, one word, one dereference apart — the maintenance trap the
decades-sustainable bar exists to refuse.

Renamed to **`NumericByteForm`** / `NumProfile.ByteForm` / `PicInfo.ByteForm` before it could spread. The new
name is better than merely non-colliding: it pairs with `DataItem.ByteWidth` and `FUNCTION BYTE-LENGTH`, which
is exactly the invariant V59 is about — the FORM and the WIDTH of the same bytes. `StorageWidth` /
`StorageLength` keep their names (they are widths, and they were never ambiguous).

Two commits old, so the cost was a mechanical rewrite of 8 code files, the drift-test file rename, four docs and
a snapshot re-bake — 127 lines, all of them the same word. Left alone deliberately: DEVLOG entries 1098 and 1099
still say `NumericStorageForm`, because the log is what happened, not what the tree currently says.

The renaming rule I want to keep: a name that collides with an existing concept in the same namespace is not a
style question, it is a correctness question for the next reader — and the moment to fix it is while the
compiler is the only thing that knows the old name.

Gates: build green · characterization 33/33 after re-bake (the emitted profile field is now `ByteForm = …`) ·
the four affected unit classes 281/283 (2 skipped = the V59 facts).


## PB3339: the SORT/MERGE store holds every record in memory

The owner, on PB3330's keyed files (2026-10-17): "Reading the entire file into memory as a design requirement is a fatal
flaw, IMO". The operator swept the runtime for the same pattern. The sequential organizations stream through their
handle, and the collation loaders read locale data, not user files. The SORT/MERGE store does not stream.
`SortFileTable.Records` is a `List<StoredFrame>` of UTF-16 record images, and `CobolSort` sorts it into a second full
list and never writes to disk. A SORT whose input exceeds the process's memory ends in OutOfMemoryException.

GnuCOBOL 3.2 bounds its sort memory (`COB_SORT_MEMORY`, default 128M), writes sorted runs to temporary files and merges
them. kb/Work PB3339 (BLOCKER) sets the same bar here: bounded, configurable memory, temporary runs with a k-way merge,
unchanged ordering rules (GR5 collation, DUPLICATES IN ORDER), no disk cost for a sort that fits, and a measured test.
It is routed to the fix lane, independent of PB3330's storage engine.

Notes only. No code change.

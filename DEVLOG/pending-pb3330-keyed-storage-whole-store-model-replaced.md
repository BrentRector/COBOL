## PB3330: the owner rules the keyed organizations' whole-store model unacceptable

The operator explained PB1636's last unsigned derivation and quoted the design doc's recorded weak spot: under
`SHARING WITH ALL OTHER`, every changing statement rewrites the whole indexed or relative file, so a bulk load there is
quadratic. The owner answered (2026-10-17): "THis is a completely unacceptable design".

The quoted cost is one symptom of the model, and kb/Work PB3330 records the model as measured on origin/main 20161db10:
- OPEN reads the whole file into memory (`KeyedConnector.ReadImage`), so reading one record means reading the file.
- The file is written back whole: at CLOSE, or after every changing statement under ALL OTHER.
- A file is capped at `RecordFraming.MaxStoreBytes` (about 2 GiB, .NET's `Array.MaxLength`), which DOC-A.1-107
  documents as the boundary.
- A process that dies before CLOSE loses every change since OPEN to every keyed file. §9.1.18.5 rolls back only the
  files under an active APPLY COMMIT.

GnuCOBOL 3.2, read the same day, stores RELATIVE as fixed slots it seeks to in place and INDEXED in Berkeley DB B-trees
or an ISAM library. PB3330 sets the bar for the replacement: record-level reads and writes, O(log n) and O(1) per
statement, a size limit from the file system, crash-consistent statements, coherence across run units at record or page
granularity with today's record locks, measured performance tests and one engine. The design belongs to the Mythos lane.
`DESIGN-runtime-library.md` now names the ruling where it had called a record-level format "the next step if that cost is
ever measured to matter".

Notes and docs only. No code change.

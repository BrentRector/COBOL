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

The design followed the same day (branch `design-pb3330-keyed-storage-20261017`), as `DESIGN-runtime-library.md` §2.2.1,
from a survey read at the sources (GnuCOBOL 3.2's `fileio.c`, Berkeley DB's reference, IBM VSAM, Micro Focus, ACUCOBOL
Vision, Informix C-ISAM) and a measured prototype (a 4 KiB write 7-8 µs, an fsync 2.9-3.1 ms, today's 64 MiB whole-store
persist 77-92 ms per changing statement). It decides one paged data file of 4 KiB pages (store format 5, no sidecar), B+
trees for the prime key, each alternate key as (key, release ordinal) composites and the relative record number, with the
PB2754 frame codec as the leaf value; shadow paging with a generation ledger for crash consistency (a committed meta slot
never overwritten, fresh pages rewritten in place, freed pages quarantined until promotion, a bounded verification at OPEN,
no log and no rebuild utility); coherence across run units by one meta-page read and a cache drop under the existing
statement mutex; the locks unchanged; the boundary as the host's file size capped at 2^32 pages; COMMIT and ROLLBACK as the
ledger's promotion, discard and adoption rule; and count-based performance witnesses. Every citation passed `cite.py --check`
(one inherited quote was re-derived from §12.4.5.6.4 to §14.9.30.4 GR21 c)). The work is filed as kb/Work PB3331-PB3338,
sequenced by `blocked_by`; PB3330 lands with the last of them, and it carries one owner question (the RELATIVE O(1) bar
against one engine). The DOC-A.1-107 and DOC-A.1-75 rows are marked as the groups' targets and still describe the current
compiler.

Notes and docs only. No code change.

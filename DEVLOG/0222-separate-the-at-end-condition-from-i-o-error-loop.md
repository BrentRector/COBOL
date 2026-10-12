## Entry 222 — 2026-05-29: Separate the AT END condition from I/O-error loop termination (spec audit follow-up)

Entry 215's hang fix had made `FileRuntime.IsAtEnd` true for terminal error statuses
(FileNotOpen/FileNotFound/PermanentError) as well as EOF. That stops the SORT-USING spin, but it
is too blunt for a plain `READ … AT END`: a non-EOF I/O error would then drive the AT END
*imperative*, whereas ISO §14.9.21 reserves AT END for end-of-file (status "10") and routes other
unsuccessful reads to FILE STATUS / a USE procedure.

Split the two meanings: `IsAtEnd` is strict ("10") again, for the AT END / NOT AT END condition; a
new `IsReadExhausted` (EOF or any terminal unreadable status) is used only by compiler-generated
read loops that must terminate. `IrCheckFileAtEnd` gained a `TreatErrorsAsEnd` flag — the SORT …
USING input pass sets it; the general READ leaves it default. ST102A (a former SORT-USING hang)
still completes; guard ALL GREEN (1000 / 336 / 149).


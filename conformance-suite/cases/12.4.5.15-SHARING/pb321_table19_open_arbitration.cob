*>--- conformance-case
*> id: pb321_table19_open_arbitration
*> rules: [GR-12.4.5.15.3-1, GR-14.9.27.4-4, GR-14.9.27.4-22,
*>    GR-14.9.27.4-25, FMT-12.4.5.15.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB321T19.
      *> kb/Work PB321 — ISO 14.9.27.4 Table 19 ("Opening available shared
      *> files that are currently open by another file connector") is THE
      *> OPEN arbiter, for every file connector, not only the ones that
      *> declared a SHARING clause. Five SELECTs on ONE physical file give
      *> the printed table's rows and columns real connectors.
      *>
      *> Expected values are derived from the printed table plus 9.1.13.9
      *> item 1 ("I-O status = 61 ... a file sharing conflict condition")
      *> and 14.9.27.4 GR25 ("If the execution of the OPEN statement is
      *> unsuccessful, the file is not affected").
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SEED ASSIGN TO "pb321t19.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SEED-ST.
      *> 14.9.27.3 SR8 requires LOCK MODE alongside SHARING WITH ALL OTHER.
           SELECT F-ALL1 ASSIGN TO "pb321t19.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS A1-ST.
           SELECT F-ALL2 ASSIGN TO "pb321t19.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS A2-ST.
           SELECT F-RO ASSIGN TO "pb321t19.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH READ ONLY
               FILE STATUS IS RO-ST.
           SELECT F-NO ASSIGN TO "pb321t19.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH NO OTHER
               FILE STATUS IS NO-ST.
      *> No SHARING and no LOCK MODE clause: 9.1.15's UNDETERMINED
      *> implementor default. It takes part in the arbitration all the same.
           SELECT F-PLAIN ASSIGN TO "pb321t19.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS PL-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-SEED.
       01 SEED-REC  PIC X(9).
       FD F-ALL1.
       01 A1-REC    PIC X(9).
       FD F-ALL2.
       01 A2-REC    PIC X(9).
       FD F-RO.
       01 RO-REC    PIC X(9).
       FD F-NO.
       01 NO-REC    PIC X(9).
       FD F-PLAIN.
       01 PL-REC    PIC X(9).
       WORKING-STORAGE SECTION.
       01 SEED-ST   PIC XX.
       01 A1-ST     PIC XX.
       01 A2-ST     PIC XX.
       01 RO-ST     PIC XX.
       01 NO-ST     PIC XX.
       01 PL-ST     PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-SEED.
           MOVE "SEEDVALUE" TO SEED-REC.
           WRITE SEED-REC.
           CLOSE F-SEED.
      *> S1 — 9.1.13.9 1) e): "An attempt is made to open a physical file in
      *> the output mode and the physical file is currently open by another
      *> file connector." Table 19 row SHARING WITH ALL OTHER / OUTPUT is
      *> "Unsuccessful open" in ALL FIVE columns, so the OUTPUT request loses
      *> against an ALL OTHER / input holder just as against an exclusive one.
           OPEN INPUT F-ALL1.
           OPEN OUTPUT F-ALL2.
           DISPLAY "S1-A1=" A1-ST " S1-A2=" A2-ST.
           CLOSE F-ALL1.
      *> S2 — 9.1.13.9 1) a): "An attempt is made to open a physical file
      *> that is currently open by another file connector in the sharing with
      *> no other mode." Column "sharing with no other" is "Unsuccessful
      *> open" for every row, so the plain connector's OUTPUT request loses
      *> even though it wrote no SHARING clause of its own.
           OPEN INPUT F-NO.
           OPEN OUTPUT F-PLAIN.
           DISPLAY "S2-NO=" NO-ST " S2-PL=" PL-ST.
           CLOSE F-NO.
      *> S3 — 9.1.13.9 1) b): the incoming SHARING WITH NO OTHER request.
      *> Table 19's first row is "Unsuccessful open" in every column, so the
      *> exclusive request loses against the plain holder.
           OPEN INPUT F-PLAIN.
           OPEN INPUT F-NO.
           DISPLAY "S3-PL=" PL-ST " S3-NO=" NO-ST.
           CLOSE F-PLAIN.
      *> 14.9.27.4 GR25 — every refused OPEN above left the file alone: the
      *> seed record is still the only record, so no OPEN OUTPUT truncated it.
           OPEN INPUT F-SEED.
           READ F-SEED.
           DISPLAY "GR25-REC=" SEED-REC " ST=" SEED-ST.
           READ F-SEED.
           DISPLAY "GR25-EOF=" SEED-ST.
           CLOSE F-SEED.
      *> S4 — Table 19 row SHARING WITH ALL OTHER / INPUT, column "sharing
      *> with all other / input" is "Normal open": two ALL OTHER readers
      *> coexist.
           OPEN INPUT F-ALL1.
           OPEN INPUT F-ALL2.
           DISPLAY "S4-A1=" A1-ST " S4-A2=" A2-ST.
           CLOSE F-ALL1.
           CLOSE F-ALL2.
      *> S5 — row SHARING WITH ALL OTHER / INPUT, column "sharing with read
      *> only / input" is "Normal open": 9.1.15 rule 2 restricts other
      *> connectors "to input mode", and this one asks for input.
           OPEN INPUT F-RO.
           OPEN INPUT F-ALL1.
           DISPLAY "S5-RO=" RO-ST " S5-A1=" A1-ST.
           CLOSE F-RO.
           CLOSE F-ALL1.
      *> S6 — an already-open connector re-OPENed is 9.1.13.7 item 1's '41',
      *> and 14.9.27.4 GR25 leaves it in its ORIGINAL open mode. F-ALL1 is
      *> therefore still registered as INPUT, so F-RO's INPUT request meets
      *> column "all other / input" ("Normal open"), not column "all other /
      *> extend I-O output" ("Unsuccessful open").
           OPEN INPUT F-ALL1.
           OPEN OUTPUT F-ALL1.
           DISPLAY "S6-41=" A1-ST.
           OPEN INPUT F-RO.
           DISPLAY "S6-RO=" RO-ST.
           CLOSE F-RO.
           CLOSE F-ALL1.
      *> S7 — the four combinations 9.1.13.9's sub-cases (c) and (d) do NOT
      *> enumerate: they say "I-O or extend" where Table 19's existing-side
      *> column groups are "extend I-O output". An existing connector open in
      *> the OUTPUT mode is column "all other / extend I-O output", so the
      *> incoming SHARING WITH READ ONLY / INPUT request is "Unsuccessful
      *> open" — 9.1.15 rule 2, "unsuccessful if the physical file is
      *> associated with another file connector whose open mode is other than
      *> input", is the prose that says so.
           OPEN OUTPUT F-ALL1.
           DISPLAY "S7-A1=" A1-ST.
           OPEN INPUT F-RO.
           DISPLAY "S7-RO=" RO-ST.
      *> S8 — 9.1.15: "The file lock is removed by an explicit or implicit
      *> CLOSE statement executed for that file connector", so the closed
      *> connector stops arbitrating and the OUTPUT request now succeeds.
           CLOSE F-ALL1.
           OPEN OUTPUT F-ALL2.
           DISPLAY "S8-A2=" A2-ST.
           CLOSE F-ALL2.
           STOP RUN.

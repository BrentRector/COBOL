*>--- conformance-case
*> id: l1_sync_file_record
*> rules: [GR-13.18.55.4-11]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.55.4 GR11 — the implementor's rules for synchronizing the
      *> RECORDS OF A FILE, as that affects the synchronization of elementary
      *> items. COBOL.NET's stated rule is that there are none: a file record
      *> is laid out exactly as the equivalent WORKING-STORAGE group.
      *>
      *> THE RULE. §13.18.55.4 GR11: "Any rules for synchronization of the
      *> records of a file, as this affects the synchronization of elementary
      *> items, shall be specified by the implementor."
      *> This is the FILE-level companion to GR9 a), which covers "the format
      *> on the external media of records or groups containing elementary
      *> items whose data description contains the SYNCHRONIZED clause".
      *> GR11 asks the separate question: do records of a FILE follow a
      *> different synchronization law from data in WORKING-STORAGE?
      *>
      *> THE DETERMINATION, docs/CONFORMANCE.md DOC-A.1-195: "GR11 adds no
      *> file-level synchronization", under the same item-195 determination
      *> that COBOL.NET performs NO physical alignment for SYNCHRONIZED and
      *> generates NO implicit FILLER for it, and that "the external-media
      *> format of a record or group containing a SYNCHRONIZED elementary item
      *> is byte-identical to the same record written without the clause".
      *> Annex A.1 item 196 makes documenting the file-record rules optional;
      *> the determination is stated inside the item-195 row.
      *>
      *> THE LEGS.
      *> RECLEN-S / RECLEN-P — the two FD record descriptions differ ONLY in
      *>   the SYNCHRONIZED clause on their middle item, and both answer 4.
      *>   Each is measured while its own file is OPEN, so neither reads a
      *>   record area of a closed file.
      *>   Derived: PIC X + PIC S9(4) COMP + PIC X = 1 + 2 + 1, from
      *>   DOC-A.1-209 (USAGE DISPLAY is one byte per character position) and
      *>   DOC-A.1-205 (a 3-4 digit COMP item is 2 bytes). This is the same
      *>   number the equivalent WORKING-STORAGE group gives — the AUTO leg of
      *>   conformance:cases/8.5.1.6.4-Item-alignment-for-increased-object-code/l1_sync_occurs_and_no_auto_align measures exactly
      *>   that shape outside a file — so a file-level rule that padded RS-N
      *>   to a natural boundary would answer 5 or 6 here and split the two
      *>   sections apart.
      *> XREAD — the DECISIVE leg, and the one a length cannot fake. FS and FP
      *>   name the SAME external file. The record written through FS carries
      *>   the SYNCHRONIZED clause; the record read back through FP does NOT.
      *>   If a file-level synchronization rule inserted or moved bytes for
      *>   the synchronized description, the unsynchronized description would
      *>   decode the SAME bytes into different fields and at least one of the
      *>   three comparisons would fail. YES therefore says the two record
      *>   layouts are byte-for-byte the same on the external medium.
      *>   -1234 is used rather than a small value because its two's-
      *>   complement image is X"FB2E" (DOC-A.1-205: two's complement, most
      *>   significant byte first), two DIFFERENT non-printable bytes, so a
      *>   one-byte shift cannot survive the comparison by coincidence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SYN05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "l1syn05.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FP ASSIGN TO "l1syn05.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 RS.
          05 RS-A PIC X.
          05 RS-N PIC S9(4) COMP SYNC.
          05 RS-Z PIC X.
       FD FP.
       01 RP.
          05 RP-A PIC X.
          05 RP-N PIC S9(4) COMP.
          05 RP-Z PIC X.
       WORKING-STORAGE SECTION.
       01 OKF PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FS
           DISPLAY "RECLEN-S=" FUNCTION BYTE-LENGTH(RS)
           MOVE "a" TO RS-A
           MOVE -1234 TO RS-N
           MOVE "z" TO RS-Z
           WRITE RS
           CLOSE FS
           MOVE "NO" TO OKF
           OPEN INPUT FP
           DISPLAY "RECLEN-P=" FUNCTION BYTE-LENGTH(RP)
           READ FP
               AT END CONTINUE
               NOT AT END
                   IF RP-A = "a" AND RP-N = -1234 AND RP-Z = "z"
                       MOVE "YES" TO OKF
                   END-IF
           END-READ
           CLOSE FP
           DISPLAY "XREAD=" OKF
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1192RB.
      *> ISO/IEC 1989:2023 §9.1.13.5 item 4 and §14.9.51.4 GR29 b) /
      *> GR33 b) — the externally-defined boundary of a RELATIVE file,
      *> tested AT THE WRITE (kb/Work PB1192).
      *>
      *> §9.1.13.5 4): "I-O status = 24. An attempt is made to write
      *> outside the externally-defined boundaries of a physical
      *> relative or indexed file. The implementor specifies the
      *> manner in which these boundaries are defined."
      *> §14.9.51.4 GR33 b): "When an attempt is made to write outside
      *> the externally defined boundaries of the file, the I-O status
      *> associated with the write file connector is set to '24'."
      *> GR29 b): "If the relative key data item contains a value that
      *> is less than 1 or greater than the highest relative record
      *> number permitted for the file, the execution of the WRITE
      *> statement is unsuccessful, and the I O status for the write
      *> file connector is set to '34'."
      *> GR15: "If the execution of a WRITE statement is unsuccessful,
      *> the write operation does not take place, the content of the
      *> record area is unaffected".
      *>
      *> The implementor's determination (docs/CONFORMANCE.md
      *> DOC-A.1-107): the boundary of a relative file is its store's
      *> capacity, 2 147 483 591 bytes (Array.MaxLength), counting a
      *> 4-byte tag for EVERY empty slot below the highest relative
      *> record number; the highest relative record number permitted
      *> is 2 147 483 647.
      *>
      *> Why each leg can fail:
      *>  K600M - RRN 600 000 000 needs 599 999 999 empty-slot tags,
      *>          about 2.4 GB: outside the boundary, so '24' and the
      *>          INVALID KEY branch, AT the WRITE. The defect wrote
      *>          '00' and then failed at CLOSE (and at a larger key
      *>          killed the run unit there with OverflowException).
      *>  K5    - an in-bounds key: '00', NOT INVALID KEY.
      *>  CLOSE1- the store holds ONE record; the CLOSE persists it.
      *>  K3E9  - RRN 3 000 000 000 is above the highest permitted
      *>          number: '34' (GR29 b), which is a permanent error,
      *>          NOT the invalid key condition, so neither branch
      *>          runs. The CLOSE that follows ends the condition
      *>          (Annex A.1 item 105) and completes normally.
      *>  READ1/READ2 - GR15's "does not take place": the only record
      *>          in the file is RRN 5; a released 600M or 3E9 record
      *>          would be read next instead of the at-end '10'.
      *>          RVR still holds FIVE after K3E9 (record area
      *>          unaffected) and is overwritten by the READ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RV ASSIGN TO "pb1192rb.dat"
               ORGANIZATION RELATIVE ACCESS DYNAMIC
               RELATIVE KEY RK FILE STATUS FS.
       DATA DIVISION.
       FILE SECTION.
       FD RV.
       01 RVR PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 RK PIC 9(18).
       01 BR PIC X(7).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RV.
           MOVE 600000000 TO RK.
           MOVE "BIG " TO RVR.
           MOVE "NONE" TO BR.
           WRITE RVR INVALID KEY MOVE "INVALID" TO BR
               NOT INVALID KEY MOVE "VALID" TO BR
           END-WRITE.
           DISPLAY "K600M=" FS " " BR.
           MOVE 5 TO RK.
           MOVE "FIVE" TO RVR.
           MOVE "NONE" TO BR.
           WRITE RVR INVALID KEY MOVE "INVALID" TO BR
               NOT INVALID KEY MOVE "VALID" TO BR
           END-WRITE.
           DISPLAY "K5=" FS " " BR.
           CLOSE RV.
           DISPLAY "CLOSE1=" FS.
           OPEN I-O RV.
           MOVE 3000000000 TO RK.
           MOVE "NONE" TO BR.
           WRITE RVR INVALID KEY MOVE "INVALID" TO BR
               NOT INVALID KEY MOVE "VALID" TO BR
           END-WRITE.
           DISPLAY "K3E9=" FS " " BR " " RVR.
           CLOSE RV.
           DISPLAY "CLOSE2=" FS.
           OPEN INPUT RV.
           MOVE SPACES TO RVR.
           READ RV NEXT RECORD AT END DISPLAY "READ1 AT END".
           DISPLAY "READ1=" FS " " RK " " RVR.
           READ RV NEXT RECORD AT END DISPLAY "READ2 AT END".
           DISPLAY "READ2=" FS.
           CLOSE RV.
           STOP RUN.

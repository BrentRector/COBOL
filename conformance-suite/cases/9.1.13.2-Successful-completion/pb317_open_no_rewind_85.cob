      *>--- conformance-case
      *> id: pb317_open_no_rewind_85
      *> rules: [GR-9.1.13.2-6, GR-14.9.27.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB317N8.
      *> kb/Work PB317, the EDITION-INVARIANCE half. ISO 14.9.27.4 GR11
      *> ("The NO REWIND phrase will be ignored if it does not apply to
      *> the storage medium on which the file resides. If the NO REWIND
      *> phrase is ignored, the OPEN statement is successful and the I-O
      *> status associated with file-name-1 is set to '07'.") carries no
      *> edition gate: the phrase is in the OPEN general format of every
      *> edition this compiler targets, and VERSION_CHANGE_REFERENCE row
      *> 7.12 records that only its SIBLING phrase REVERSED was deleted
      *> in 2002. This program is the 2023 golden's core assertions run
      *> at --std 85, so a future gate on the phrase breaks HERE and not
      *> only in the 2023 corpus. Expected values are computed exactly
      *> as they are for 2023/pb317_open_no_rewind: '07' from GR11 plus
      *> 9.1.13.2 item 6 on the category (a) Non-unit medium, '00' where
      *> no phrase is written (9.1.13.2 item 1), and AAAAAAAA because an
      *> IGNORED phrase leaves 14.9.27.4 GR14's file position indicator
      *> of 1 exactly as a plain OPEN INPUT does.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb317n8.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "AAAAAAAA" TO F-REC
           WRITE F-REC
           CLOSE F
           OPEN INPUT F WITH NO REWIND
           DISPLAY "IN-NOREW=" WS-ST
           READ F AT END CONTINUE END-READ
           DISPLAY "READ1=" WS-ST " " F-REC
           CLOSE F
           DISPLAY "CLOSE-PLAIN=" WS-ST
           OPEN OUTPUT F WITH NO REWIND
           DISPLAY "OUT-NOREW=" WS-ST
           CLOSE F WITH NO REWIND
           DISPLAY "CLOSE-NOREW=" WS-ST
           STOP RUN.

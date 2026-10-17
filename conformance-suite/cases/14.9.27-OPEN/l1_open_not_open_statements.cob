      *>--- conformance-case
      *> id: l1_open_not_open_statements
      *> rules: [GR-14.9.27.4-6, GR-14.9.35.4-3, GR-14.9.51.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.27.4 GR6 — "When a file connector is not open, no
      *> statement shall be executed that references the associated
      *> file-name, either explicitly or implicitly, except for a MERGE
      *> or SORT statement with the USING or GIVING phrase, the COMMIT
      *> and ROLLBACK statements, a DELETE FILE statement, or an OPEN
      *> statement."
      *> The rule prohibits the PROGRAM; an implementation honours it by
      *> giving each forbidden statement that statement's §9.1.13.7
      *> logic-error status and leaving the file alone. F is never
      *> opened before the four attempts below; the expected values are
      *> quoted from §9.1.13.7:
      *>   CLOSE   item 2 — "A CLOSE or UNLOCK statement is attempted
      *>           for a file connector that is not in an open mode" =
      *>           '42'.
      *>   READ    item 7 — "a READ or START statement … referencing a
      *>           file connector that is not open in the input or I-O
      *>           mode" = '47'.
      *>   WRITE   item 8a — access mode sequential, "the file connector
      *>           is not open in the extend or output mode" = '48'.
      *>   REWRITE item 9 — "a DELETE RECORD or REWRITE statement …
      *>           referencing a file connector that is not open in the
      *>           I-O mode" = '49'. (Item 3's '43' presupposes an OPEN
      *>           connector whose LAST I-O statement was not a
      *>           successful READ; with no open mode at all only item
      *>           9 applies, so §9.1.13.1's "if more than one value
      *>           applies" tie-break is not reached.) §14.9.35.4 GR3
      *>           states the same value outright, without needing the
      *>           elimination: "If the open mode is some other value
      *>           or the file is not open, the I-O status in the
      *>           rewrite file connector is set to '49'".
      *> Then the EXEMPT statement: OPEN is the one verb the rule
      *> permits on a not-open connector, and it succeeds ('00',
      *> §9.1.13.2 item 1). The read-back proves the file holds exactly
      *> the ONE record written after that OPEN. The one forbidden
      *> statement that could have added a record is the WRITE, and its
      *> own rule says it did not — §14.9.51.4 GR15: "If the execution
      *> of a WRITE statement is unsuccessful, the write operation does
      *> not take place, the content of the record area is unaffected".
      *> (§14.9.27.4 GR25 is NOT the authority for this: its stem is
      *> scoped to "If the execution of the OPEN statement is
      *> unsuccessful", and none of the four forbidden statements is an
      *> OPEN.) The trailing '10' is §9.1.13.4 item 1a — "NEXT was
      *> specified or implied and the end of the physical file has been
      *> reached".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OPN06.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1opn06.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
      *> Nothing has opened F.
           CLOSE F
           DISPLAY "CLOSE=" ST
           READ F AT END CONTINUE END-READ
           DISPLAY "READ=" ST
           WRITE F-REC
           DISPLAY "WRITE=" ST
           REWRITE F-REC
           DISPLAY "REWRITE=" ST
      *> The exempt statement.
           OPEN OUTPUT F
           DISPLAY "OPEN=" ST
           MOVE "REALREC1" TO F-REC
           WRITE F-REC
           DISPLAY "WRITE2=" ST
           CLOSE F
           DISPLAY "CLOSE2=" ST
      *> Exactly one record is in the file.
           OPEN INPUT F
           DISPLAY "REOPEN=" ST
           READ F AT END CONTINUE END-READ
           DISPLAY "REC1=" F-REC
           READ F AT END CONTINUE END-READ
           DISPLAY "EOF=" ST
           CLOSE F
           STOP RUN.

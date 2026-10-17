      *>--- conformance-case
      *> id: l1_open_status_updated
      *> rules: [GR-14.9.27.4-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.27.4 GR1 — "The execution of the OPEN statement
      *> causes the value of the I-O status associated with file-name-1
      *> to be updated to one of the values in 9.1.13, I-O status."
      *> The rule is an INVARIANT over every OPEN outcome, so the
      *> program drives four structurally different OPEN executions and
      *> shows the §9.1.13 value each one left behind. Every expected
      *> value is stated outright by the standard:
      *>   A-OUT    OPEN OUTPUT, file absent. Table 18 (GR4): "Open
      *>            causes the file to be created" — successful with no
      *>            further information ⇒ §9.1.13.2 item 1, '00'.
      *>   A-AGAIN  OPEN on a connector already open. GR2: "the
      *>            execution of the OPEN statement is unsuccessful and
      *>            the I-O status … is set to '41'" (= §9.1.13.7 item
      *>            1). GR25: "the file is not affected", so the CLOSE
      *>            that follows still succeeds.
      *>   B-IN     OPEN INPUT, file absent, NOT optional. §9.1.13.6
      *>            item 5: "an OPEN statement with the INPUT, I-O, or
      *>            EXTEND phrase is attempted on a file that is not
      *>            described as optional and the physical file is not
      *>            present" ⇒ '35'. Table 18 INPUT/unavailable: "Open
      *>            is unsuccessful".
      *>   C-IN     OPEN INPUT, file absent, OPTIONAL. §9.1.13.2 item
      *>            4a ⇒ '05'.
      *>   A-IN     OPEN INPUT of the now-existing file ⇒ '00'.
      *> Four distinct outcomes, four §9.1.13 values, none of them the
      *> stale content of the FILE STATUS item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1OPN01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "l1opn01a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-A.
           SELECT FB ASSIGN TO "l1opn01b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT OPTIONAL FC ASSIGN TO "l1opn01c.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-C.
       DATA DIVISION.
       FILE SECTION.
       FD FA.
       01 A-REC PIC X(6).
       FD FB.
       01 B-REC PIC X(6).
       FD FC.
       01 C-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01 ST-A PIC XX.
       01 ST-B PIC XX.
       01 ST-C PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FA
           DISPLAY "A-OUT=" ST-A
           OPEN OUTPUT FA
           DISPLAY "A-AGAIN=" ST-A
           CLOSE FA
           DISPLAY "A-CLOSE=" ST-A
           OPEN INPUT FB
           DISPLAY "B-IN=" ST-B
           OPEN INPUT FC
           DISPLAY "C-IN=" ST-C
           CLOSE FC
           DISPLAY "C-CLOSE=" ST-C
           OPEN INPUT FA
           DISPLAY "A-IN=" ST-A
           CLOSE FA
           DISPLAY "A-CLOSE2=" ST-A
           STOP RUN.

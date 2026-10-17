      *>--- conformance-case
      *> id: l1_read_at_end_transfer
      *> rules: [GR-14.9.30.4-24]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.30.4 GR24 — what happens, in the order specified,
      *> when the at end condition exists during a READ.
      *> a) "The I-O status ... is set to '10' to indicate the at end
      *>    condition" (the EC half of a) is pinned by the companion
      *>    golden l1_read_at_end_ec).
      *> c) "If the AT END phrase is specified in the READ statement
      *>    causing the condition, control is transferred to
      *>    imperative-statement-1.  Any other applicable exception
      *>    processing statements are not executed."
      *> d) "If the AT END phrase is not specified in the input-output
      *>    statement, any applicable at end exception processing
      *>    statements are executed."
      *> A USE AFTER STANDARD ERROR declarative per file stands in for
      *> "exception processing statements" (§9.1.12 item 2); its DISPLAY
      *> is the observable for whether they ran.
      *>
      *> LEG A (file F, AT END written).  A2 is the at end read: the
      *> imperative runs ("A2-ATEND=10", GR24 c first sentence) and NO
      *> "DF=" line may appear (c second sentence).  A3 proves that
      *> silence is meaningful rather than a dead declarative: the READ
      *> after an unsuccessful one is '46' (GR21 first sentence), which
      *> is NOT the at end family, so the AT END phrase does not cover
      *> it and the SAME declarative fires — "DF=46" with no
      *> "A3-ATEND".  A3 is also GR24 b)'s only observable: the file
      *> position indicator now designates no next or previous logical
      *> record, and the following sequential READ is unsuccessful.
      *> LEG B (file G, no AT END phrase).  B2 is the at end read: the
      *> declarative runs — "DG=10" — per GR24 d).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RD24A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1rd24f.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS F-ST.
           SELECT G ASSIGN TO "l1rd24g.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS G-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       FD G.
       01 G-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       01 G-ST PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-F-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       ERR-F-PARA.
           DISPLAY "DF=" F-ST.
       ERR-G-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON G.
       ERR-G-PARA.
           DISPLAY "DG=" G-ST.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "AAAA" TO F-REC.
           WRITE F-REC.
           CLOSE F.
           OPEN OUTPUT G.
           MOVE "BBBB" TO G-REC.
           WRITE G-REC.
           CLOSE G.
      *> LEG A - the AT END phrase is written (GR24 c).
           OPEN INPUT F.
           READ F AT END DISPLAY "A1-ATEND" END-READ.
           DISPLAY "A1=" F-ST " " F-REC.
           READ F AT END DISPLAY "A2-ATEND=" F-ST END-READ.
           DISPLAY "A2=" F-ST.
           READ F AT END DISPLAY "A3-ATEND" END-READ.
           DISPLAY "A3=" F-ST.
           CLOSE F.
      *> LEG B - no AT END phrase (GR24 d).
           OPEN INPUT G.
           READ G.
           DISPLAY "B1=" G-ST " " G-REC.
           READ G.
           DISPLAY "B2=" G-ST.
           CLOSE G.
           STOP RUN.

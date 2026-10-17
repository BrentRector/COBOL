      *>--- conformance-case
      *> id: l1_read_into_unsuccessful
      *> rules: [GR-14.9.30.4-4, GR-14.9.30.4-5]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.30.4 GR5 — "If the execution of a READ statement with
      *> the INTO phrase is unsuccessful, the content of the data item
      *> referenced by identifier-1 is unchanged and item identification
      *> of the data item referenced by identifier-1 is not done."
      *> Two obligations, one per leg.
      *>
      *> LEG B - CONTENT UNCHANGED.  identifier-1 is loaded with "KEEP"
      *> immediately before an at end READ INTO.  GR5 requires "KEEP"
      *> back; an implementation that ran the GR4b move anyway would
      *> hand back the record area, which still holds the "AAAA" of the
      *> successful LEG A read (GR18 leaves it undefined after an
      *> unsuccessful read, so ANY value other than "KEEP" fails GR5).
      *>
      *> LEG C - ITEM IDENTIFICATION NOT DONE.  identifier-1 is TE(IDX)
      *> with IDX = 5 over a 3-occurrence table, so identifying the item
      *> sets EC-BOUND-SUBSCRIPT (§8.4.2.3.4 GR2, Table 13 fatal).
      *> C1 is the SUCCESSFUL read: GR4b says "Item identification of
      *> the data item referenced by identifier-1 is done after the
      *> record has been read and immediately before it is moved", so
      *> the condition MUST be raised there — that is the positive
      *> control proving the declarative is live and the subscript
      *> really is out of range.  C2 is the at end read: GR5 forbids the
      *> identification, so NO "IDENT=" line may stand between "C1=00"
      *> and "C2-ATEND=10".  The trailing MOVE re-fires the declarative
      *> to show it is still armed after the READs.
      *> The I-O status is stored before the implicit move, so C1 prints
      *> '00' even though the fatal condition was raised inside it.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RD05A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "l1rd05a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS F-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       01 WS-T PIC X(4).
       01 TBL.
          05 TE PIC X(4) OCCURS 3 TIMES.
       01 IDX PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H-P.
           DISPLAY "IDENT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT F.
           MOVE "AAAA" TO F-REC.
           WRITE F-REC.
           CLOSE F.
           OPEN INPUT F.
      *> LEG A - the successful READ INTO (GR4b) loads the record.
           MOVE "ZZZZ" TO WS-T.
           READ F INTO WS-T AT END DISPLAY "A-ATEND" END-READ.
           DISPLAY "A=" F-ST " " WS-T.
      *> LEG B - the at end READ INTO leaves identifier-1 unchanged.
           MOVE "KEEP" TO WS-T.
           READ F INTO WS-T AT END DISPLAY "B-ATEND=" F-ST END-READ.
           DISPLAY "B=" F-ST " " WS-T.
           CLOSE F.
      *> LEG C - item identification of identifier-1.
           OPEN INPUT F.
           READ F INTO TE (IDX) AT END DISPLAY "C1-ATEND" END-READ.
           DISPLAY "C1=" F-ST.
           READ F INTO TE (IDX)
               AT END DISPLAY "C2-ATEND=" F-ST
           END-READ.
           DISPLAY "C2=" F-ST.
           CLOSE F.
      *> The declarative is still armed - a direct reference fires it.
           MOVE TE (IDX) TO WS-T.
           DISPLAY "DONE".
           STOP RUN.

*>--- conformance-case
*> id: pb344_read_previous_indexed_2023
*> rules: [GR-14.9.30.4-21]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB344 — the ISO edition the compilation targets reaches the file connectors and the
      *> generated USE-declarative selector.  Until PB344 no edition reached either, so the 2023 rules
      *> were served to --std 85, 2002 and 2014 as well.
      *>
      *> THE RULES, and every expected value below derived from them (no observation):
      *>  14.9.30.4 GR21, the "When the file is an indexed file" block, rule d) 3 — "If no such record is
      *>    found or PREVIOUS is specified and the previous operation on the file was an OPEN statement, the
      *>    at end condition exists and execution proceeds as indicated in General rule 24."   (P1 at 2023, P3)
      *>  14.9.30.4 GR24 a)/c) — the at end condition sets '10' and transfers control to the AT END
      *>    imperative.                                                                        (P1 at 2023, P3)
      *>  14.9.27.4 GR14 — OPEN INPUT sets the file position indicator to "the characters that have the lowest
      *>    ordinal position in the collating sequence associated with the file", so the first record made
      *>    available under the key of reference is the LOWEST key, K002.                      (P1, P2)
      *>  14.9.41.4 GR17 / GR9 — START positions the indicator at the first record whose key satisfies the
      *>    comparison; a later READ NEXT makes that record available.                         (P4A, P4B)
      *>
      *> EDITIONS.  Rule d) 3 IS THE 2023 RULE and 2023 amended it: Annex E.2 item 22, "READ PREVIOUS statement
      *> following an OPEN statement.  Ensure that an at end condition occurs", whose justification records what
      *> the PRIOR rule did — "the rule itself stated that the first record would be retrieved.  The rule itself
      *> has been amended such that an at end condition would occur."  So at 2002 and 2014 P1 makes the FIRST
      *> EXISTING RECORD under the key of reference available, exactly as NEXT does (P2 is that control), and
      *> only at 2023 is it the at end condition.  (Annex E.2 also mentions a prior NOTE saying at end normally
      *> exists; this document is drafted to the ISO/IEC Directives Part 2, under which a note states no
      *> requirement, so the prior standard's normative content is its RULE.  And were the prior behaviour
      *> already at end, Annex E.2 — the list of SUBSTANTIVE changes potentially affecting existing programs —
      *> would carry no item 22 at all.)  VERSION_CHANGE_REFERENCE row 29;
      *> CobolNet.Runtime.DialectBehavior.IndexedReadPreviousAfterOpenAtEnd.
      *> INDEXED ONLY: the relative block prints rule b) unamended (kb/Work PB343,
      *> tests/conformance/*/pb343_read_previous_relative.cob).  READ ... PREVIOUS is a 2002 introduction, so
      *> there is no --std 85 copy (negative/pb334-read-previous-85 owns that rejection).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB344P23.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX ASSIGN TO "pb344p23.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS I-K
               FILE STATUS IS I-ST.
       DATA DIVISION.
       FILE SECTION.
       FD IDX.
       01 I-REC.
          05 I-K PIC X(4).
          05 I-V PIC X(6).
       WORKING-STORAGE SECTION.
       01 I-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IDX
           MOVE "K002" TO I-K MOVE "BBBBBB" TO I-V WRITE I-REC
           MOVE "K004" TO I-K MOVE "DDDDDD" TO I-V WRITE I-REC
           MOVE "K006" TO I-K MOVE "FFFFFF" TO I-V WRITE I-REC
           CLOSE IDX
      *> P1 — READ ... PREVIOUS as the FIRST operation after OPEN INPUT (the edition-varying leg).
           OPEN INPUT IDX
           READ IDX PREVIOUS RECORD
               AT END DISPLAY "P1=ATEND|" I-ST
               NOT AT END DISPLAY "P1=" I-K "|" I-ST
           END-READ
           CLOSE IDX
      *> P2 — the NEXT control from the same position: the lowest key, at every edition.
           OPEN INPUT IDX
           READ IDX NEXT RECORD
               AT END DISPLAY "P2=ATEND|" I-ST
               NOT AT END DISPLAY "P2=" I-K "|" I-ST
           END-READ
      *> P3 — rule d) 3's OTHER leg, "if no such record is found": a PREVIOUS whose previous operation was a
      *>      successful READ of the lowest record finds nothing.  At end at EVERY edition.
           READ IDX PREVIOUS RECORD
               AT END DISPLAY "P3=ATEND|" I-ST
               NOT AT END DISPLAY "P3=" I-K "|" I-ST
           END-READ
           CLOSE IDX
      *> P4 — the backward walk itself, unchanged by the edition: START to K006, READ NEXT makes it available,
      *>      READ PREVIOUS then makes K004 available.
           OPEN INPUT IDX
           MOVE "K006" TO I-K
           START IDX KEY IS EQUAL TO I-K
               INVALID KEY DISPLAY "P4=STARTFAIL|" I-ST
           END-START
           READ IDX NEXT RECORD
               AT END DISPLAY "P4A=ATEND|" I-ST
               NOT AT END DISPLAY "P4A=" I-K "|" I-ST
           END-READ
           READ IDX PREVIOUS RECORD
               AT END DISPLAY "P4B=ATEND|" I-ST
               NOT AT END DISPLAY "P4B=" I-K "|" I-ST
           END-READ
           CLOSE IDX
           STOP RUN.

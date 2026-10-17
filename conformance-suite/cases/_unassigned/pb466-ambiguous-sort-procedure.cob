*>--- conformance-case
*> id: pb466-ambiguous-sort-procedure
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB466 - the SORT INPUT PROCEDURE / OUTPUT PROCEDURE phrases, the last pair of operand positions
*> that enter the one procedure-name resolution step. ISO 14.9.40.2 prints procedure-name-1 through
*> procedure-name-4, and 8.4.2.2.1's "uniqueness shall be established through qualification for each
*> user-defined name explicitly referenced" reaches each of them; rule 1 is false for DUP-FEED and DUP-DRAIN,
*> which S-ONE and S-TWO both declare, and rule 6 cannot excuse a reference written in S-MAIN, which declares
*> neither.
*> The SORT phrases are worth their own witness because their operand is a RANGE, not a point (14.9.40.4: the
*> input procedure is executed as though it were "the range of a PERFORM statement"), so an arbitrary pick
*> selects which span of the program feeds or drains the sort - and the sort still completes, with different
*> data and no diagnostic. Rejected at all four editions: 8.4.2.2.1 is unchanged across them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB466SORT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "pb466sort.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SRTF.
       01 SRT-NUM PIC 9(3).
       PROCEDURE DIVISION.
       S-MAIN SECTION.
       P-MAIN.
           SORT SRTF ON ASCENDING KEY SRT-NUM
               INPUT PROCEDURE IS DUP-FEED
               OUTPUT PROCEDURE IS DUP-DRAIN
           STOP RUN.
       S-ONE SECTION.
       DUP-FEED.
           MOVE 1 TO SRT-NUM.
           RELEASE SRT-NUM.
       DUP-DRAIN.
           DISPLAY "ONE-DRAIN".
       S-TWO SECTION.
       DUP-FEED.
           MOVE 2 TO SRT-NUM.
           RELEASE SRT-NUM.
       DUP-DRAIN.
           DISPLAY "TWO-DRAIN".

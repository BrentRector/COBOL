*>--- conformance-case
*> id: l1_release_from_sender_available
*> rules: [SR-14.9.32.3-1, GR-14.9.32.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.32.4 GR5 — RELEASE … FROM identifier-1: "After the execution of the RELEASE
      *> statement is complete, the information in the area referenced by identifier-1 is
      *> available". The sending area is read back AFTER each RELEASE and again after the SORT.
      *> DERIVATION — every expected line follows from the rule text, nothing from the compiler.
      *>  · SEND1 / SEND2 are GR5 itself: nothing between the MOVEs and the DISPLAY writes to
      *>    WS-SEND, and GR5 requires the RELEASE to leave identifier-1's area available, so each
      *>    DISPLAY shows exactly the eight bytes the preceding MOVEs put there.
      *>  · The SORTED lines are GR4 + §14.9.40.4 GR8 a): the FROM phrase is "equivalent to the
      *>    execution of the following statements in the order specified" — MOVE identifier-1 TO
      *>    record-name-1, then the same RELEASE without FROM — so both records reach the sort
      *>    carrying WS-SEND's content, and ASCENDING SRT-KEY returns "the record containing the
      *>    key data item with the lower value" first: AAA then BBB. This leg is what stops the
      *>    availability check above from passing vacuously on a RELEASE that transferred nothing.
      *>  · SEND3 is GR5 again, past the end of the input procedure: no statement in the output
      *>    procedure names WS-SEND, so it still holds the last value moved into it.
      *> The whole area is displayed inside [ ] so a trailing-blank difference cannot be trimmed
      *> away by the corpus runner's per-line trailing-space normalisation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RLS01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "l1rls01-sort.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SRTF.
       01 SRT-REC.
          05 SRT-KEY PIC X(3).
          05 SRT-TXT PIC X(5).
       WORKING-STORAGE SECTION.
       01 WS-SEND.
          05 WS-KEY PIC X(3).
          05 WS-TXT PIC X(5).
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           SORT SRTF ON ASCENDING KEY SRT-KEY
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN
           DISPLAY "SEND3=[" WS-SEND "]"
           DISPLAY "DONE"
           STOP RUN.
       FEED.
           MOVE "BBB" TO WS-KEY
           MOVE "bbbbb" TO WS-TXT
           RELEASE SRT-REC FROM WS-SEND
           DISPLAY "SEND1=[" WS-SEND "]"
           MOVE "AAA" TO WS-KEY
           MOVE "aaaaa" TO WS-TXT
           RELEASE SRT-REC FROM WS-SEND
           DISPLAY "SEND2=[" WS-SEND "]".
       DRAIN.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SRTF RECORD
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY "SORTED=[" SRT-REC "]"
               END-RETURN
           END-PERFORM.

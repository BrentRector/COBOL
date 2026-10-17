*>--- conformance-case
*> id: pb348_release_from_figurative_image_2002
*> rules: [GR-14.9.32.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.9.32.4 GR4 a): `RELEASE record-name-1 FROM literal-1` is "MOVE literal-1 TO
      *> record-name-1 according to the rules specified for the MOVE statement", followed by the same
      *> RELEASE without the FROM phrase.  This program writes that statement beside the explicit pair
      *> and displays the record area after each, so "equivalent" is what is measured.
      *> DERIVATION.  14.9.25.3 SR5 permits an alphanumeric figurative constant to move to a numeric
      *> receiver at this edition, and Table 16 makes it an alphanumeric-to-alphanumeric move of the
      *> quotation-mark character into the receiver's three character positions, so both the FROM form
      *> and the explicit MOVE leave SRT-NUM holding three quotation marks; 14.9.40.4 GR8 a) then
      *> returns the two equal-keyed records unchanged.  Every displayed line is three quotation marks
      *> inside [ ].
      *> WHY THIS EXACT PROGRAM (kb/Work PB348).  `MOVE QUOTE TO SRT-NUM` + `RELEASE SRT-NUM` ran; the
      *> equivalent `RELEASE SRT-NUM FROM QUOTE` ABORTED THE RUN with an unhandled
      *> NotImplementedCobolFeatureException before the record was released, so the sort never
      *> completed.  The cause was not a missing rule but a missing FACT: MoveBinder marks the numeric
      *> DISPLAY receiver image-forced, StorageFormPass consumes that mark, and the implicit move was
      *> being constructed in the EMITTER -- downstream of both.  Its 2023 twin, where the same
      *> statement is REMOVED, is conformance:negative/pb348-release-from-quote-numeric-removed-2023.
      *> The 2002 leg: SR5's sender is VALID here, with no diagnostic at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB348Q2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "pb348q2s.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SRTF.
       01 SRT-NUM PIC 9(3).
       WORKING-STORAGE SECTION.
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           SORT SRTF ON ASCENDING KEY SRT-NUM
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN
           DISPLAY "DONE"
           STOP RUN.
       FEED.
           RELEASE SRT-NUM FROM QUOTE
           DISPLAY "AFTER-FROM=[" SRT-NUM "]"
           MOVE QUOTE TO SRT-NUM
           RELEASE SRT-NUM
           DISPLAY "AFTER-MOVE=[" SRT-NUM "]".
       DRAIN.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SRTF RECORD
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY "SORTED=[" SRT-NUM "]"
               END-RETURN
           END-PERFORM.

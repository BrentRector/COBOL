*>--- conformance-case
*> id: pb1556_elementary_record_area_2014
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1556 - the record kinds 1985 has no usage for, each a
      *> record description entry of ONE elementary item: category
      *> national, the national-form NUMERIC (ISO 13.18.60.3 SR12 - usage
      *> national with a numeric picture), USAGE BIT, and FLOAT-LONG.
      *> ISO 14.9.51.4 GR5: WRITE FROM = MOVE identifier TO record, then
      *>   WRITE; ISO 14.9.30.4 GR13 c): READ makes "the record ...
      *>   available in the record area"; GR4 b): the INTO move is "from
      *>   the record area ... according to the rules for the MOVE".
      *> ISO 14.9.51.4 GR4: after the WRITE the record is "no longer
      *>   available in the record area", so every value below came back
      *>   through the file.
      *> ISO 14.9.25.4 GR4: a group move is an alphanumeric move with "no
      *>   conversion of data from one form of internal representation to
      *>   another" - the FLOAT-LONG receiver takes the sender's bytes,
      *>   so its value is the sender's 1.5.
      *> Numeric values are shown through edited items; national and
      *> boolean values through a comparison, so no implementor-defined
      *> DISPLAY conversion is involved.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1556E14.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FN ASSIGN TO "PB1556N.DAT".
           SELECT FU ASSIGN TO "PB1556U.DAT".
           SELECT FB ASSIGN TO "PB1556T.DAT".
           SELECT FF ASSIGN TO "PB1556F.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD FN.
       01 RN PIC N(3).
       FD FU.
       01 RU PIC 9(3) USAGE NATIONAL.
       FD FB.
       01 RB PIC 1(10) USAGE BIT.
       FD FF.
       01 RFL USAGE FLOAT-LONG.
       WORKING-STORAGE SECTION.
       01 WN PIC N(3).
       01 WU PIC 9(3) USAGE NATIONAL.
       01 WB PIC 1(10) USAGE BIT.
       01 WF USAGE FLOAT-LONG.
       01 GF.
          05 GF-V USAGE FLOAT-LONG VALUE 1.5.
       01 F USAGE FLOAT-LONG.
       01 E3 PIC ZZ9.
       01 EF PIC 9.9.
       PROCEDURE DIVISION.
       M-1.
           OPEN OUTPUT FN.
           MOVE N"ABC" TO WN.
           WRITE RN FROM WN.
           CLOSE FN.
           MOVE N"ZZZ" TO WN.
           OPEN INPUT FN.
           READ FN INTO WN AT END DISPLAY "FN AT END".
           IF RN = N"ABC" AND WN = N"ABC"
               DISPLAY "FN ABC"
           ELSE
               DISPLAY "FN WRONG"
           END-IF.
           CLOSE FN.
           OPEN OUTPUT FU.
           MOVE 789 TO WU.
           WRITE RU FROM WU.
           CLOSE FU.
           MOVE 0 TO WU.
           OPEN INPUT FU.
           READ FU INTO WU AT END DISPLAY "FU AT END".
           MOVE RU TO E3.
           DISPLAY "FU " E3.
           MOVE WU TO E3.
           DISPLAY "FU " E3.
           CLOSE FU.
           OPEN OUTPUT FB.
           MOVE B"1010011101" TO WB.
           WRITE RB FROM WB.
           CLOSE FB.
           MOVE B"0000000000" TO WB.
           OPEN INPUT FB.
           READ FB INTO WB AT END DISPLAY "FB AT END".
           IF RB = B"1010011101" AND WB = B"1010011101"
               DISPLAY "FB 1010011101"
           ELSE
               DISPLAY "FB WRONG"
           END-IF.
           CLOSE FB.
           OPEN OUTPUT FF.
           MOVE 2.5 TO WF.
           WRITE RFL FROM WF.
           CLOSE FF.
           MOVE 0 TO WF.
           OPEN INPUT FF.
           READ FF INTO WF AT END DISPLAY "FF AT END".
           MOVE RFL TO EF.
           DISPLAY "FF " EF.
           MOVE WF TO EF.
           DISPLAY "FF " EF.
           CLOSE FF.
           MOVE GF TO F.
           MOVE F TO EF.
           DISPLAY "GF " EF.
           STOP RUN.

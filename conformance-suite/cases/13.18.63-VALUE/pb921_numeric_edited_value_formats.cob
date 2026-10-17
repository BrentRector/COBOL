*>--- conformance-case
*> id: pb921_numeric_edited_value_formats
*> rules: [SR-13.18.63.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB921 - the OVER-REJECTION GUARD for the one ISO 13.18.63.3 SR6 edition screen, at the edition
      *> that introduced the rule. SR6 - "If the item is of category numeric-edited, then, subject to Syntax rules
      *> 2 and 3, literals in formats 1, 2, and 4 of the VALUE clause may be numeric when they shall be converted
      *> to their numeric-edited forms according to the rules for the MOVE statement" - is dated by Annex E.3.3
      *> item 43 ("It is now permitted to allow numeric-edited data items to be assigned values specified as
      *> numeric literals"), a COBOL-2023 addition. The screen that enforces that edition used to be written at
      *> ONE call site and so reached only formats 1 and 2; it is now asked once, from the funnel every format's
      *> literal passes through. This program is what proves the wider reach did not become a wider REJECTION:
      *> every arm below is legal at COBOL-2023 and shall compile and produce its computed value. The gating
      *> negatives are conformance-suite/cases/13.18.63-VALUE/pb921-condition-name-numeric-edited-below-2023 (format 3) and
      *> pb921-report-numeric-edited-below-2023 (format 4).
      *>
      *> EXPECTED VALUES, COMPUTED FROM THE RULES - never measured:
      *>   A  format 3 over PIC ZZ9.99. 14.9.39.4 GR6 places the literal "according to the rules for the VALUE
      *>      clause", i.e. SR6's MOVE-rules conversion. 13.18.40.5 editing rule 7 a) puts the replacement
      *>      character (a space, for 'Z') in "any character position immediately preceding ... the first nonzero
      *>      numeric character in the item": the integer positions ZZ9 hold 010, so Z->space, Z->'1', 9->'0',
      *>      and the fraction is 00. A = [ 10.00]. 8.8.4.5.3 then makes A-TEN true right after the SET.
      *>   B  format 3 with the ALPHANUMERIC edited image instead - SR7's spelling, legal at EVERY edition and
      *>      untouched by the edition screen (the screen asks about a NUMERIC literal only). B = [ 10.00].
      *>   C  format 3 with the literal ZERO. SR6 exempts "the figurative constant ZERO or ZEROES and the integer
      *>      and decimal forms of the literal zero" at all editions, so this is NOT gated. PIC ZZ9 holding 0
      *>      suppresses both leading zeros and prints the 9 position: C = [  0].
      *>   D  format 4 (report-section) over PIC ZZ9.99, the same conversion into a printable item, read back
      *>      through a LINE SEQUENTIAL view of the report file: D = [ 10.00].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB921-NE-VALUE-FORMATS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb921nevf.txt".
           SELECT CHK ASSIGN TO "pb921nevf.txt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-NE.
       FD  CHK.
       01  CHK-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-A PIC ZZ9.99.
          88 A-TEN VALUE 10.
       01  WS-B PIC ZZ9.99.
          88 B-TEN VALUE " 10.00".
       01  WS-C PIC ZZ9.
          88 C-ZERO VALUE 0.
       REPORT SECTION.
       RD  R-NE PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC ZZ9.99 VALUE 10.
       PROCEDURE DIVISION.
       MAIN.
           SET A-TEN TO TRUE
           DISPLAY "A=[" WS-A "]"
           IF A-TEN DISPLAY "A-TRUE" ELSE DISPLAY "A-FALSE" END-IF
           SET B-TEN TO TRUE
           DISPLAY "B=[" WS-B "]"
           IF B-TEN DISPLAY "B-TRUE" ELSE DISPLAY "B-FALSE" END-IF
           SET C-ZERO TO TRUE
           DISPLAY "C=[" WS-C "]"
           IF C-ZERO DISPLAY "C-TRUE" ELSE DISPLAY "C-FALSE" END-IF
           OPEN OUTPUT PRT
           INITIATE R-NE
           GENERATE DET
           TERMINATE R-NE
           CLOSE PRT
           OPEN INPUT CHK
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM SHOW-LINE
               END-READ
           END-PERFORM
           CLOSE CHK
           STOP RUN.
       SHOW-LINE.
           IF CHK-REC NOT = SPACES
               DISPLAY "D=[" CHK-REC(1:6) "]"
           END-IF.

      *> kb/Work PB1139 - THE WORD COLLATING OF THE SORT/MERGE COLLATING SEQUENCE PHRASE IS OPTIONAL: NOT A LENIENCY.
      *>   The general format (14.9.40.2, 14.9.24.2) prints `COLLATING SEQUENCE` with only SEQUENCE underlined; ISO
      *>   5.2.2 (keywords are underlined and required) and 5.2.3 (optional words are not underlined) make the
      *>   underlining, not the absence of brackets, what requires a word. The finding that `SEQUENCE IS
      *>   alphabet-name` is "a nonstandard extension (leniency L5) accepted silently" read the unbracketed COLLATING as
      *>   required; the underlining says otherwise, and CCVS85 ST139A (a conforming program) writes the short form.
      *> WHY EACH LEG CAN FAIL (expected values derived from the rules):
      *>   REV-A is ALPHABET "Z" THRU "A", which gives Z the lowest position and A the highest (12.3.7.4), so a table
      *>   of the three letters A, C, B sorted ASCENDING under it reads C, B, A in BOTH spellings.
      *>   1  SORT TE ASCENDING SEQUENCE IS REV-A         -> CBA (the word omitted, §14.9.40.4 GR5 a))
      *>   2  SORT TE ASCENDING COLLATING SEQUENCE IS REV-A -> CBA (the word written)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1139CW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV-A IS "Z" THRU "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 TE PIC X OCCURS 3.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ACB" TO TBL
           SORT TE ASCENDING SEQUENCE IS REV-A
           DISPLAY "1 " TBL
           MOVE "ACB" TO TBL
           SORT TE ASCENDING COLLATING SEQUENCE IS REV-A
           DISPLAY "2 " TBL
           STOP RUN.

      *> reject-at: 2002 2014 2023
      *> THE SAME FOR PHRASE TWICE IN A FILE-LEVEL COLLATING SEQUENCE CLAUSE (kb/Work PB1075).
      *> ISO/IEC 1989:2023 §12.4.5.7.2 Format 1 encloses FOR ALPHANUMERIC / FOR NATIONAL in
      *> choice indicators, and §5.2.6.4: "any single alternative may be specified only
      *> once". The second phrase used to win in silence (REV was dropped for DREV); now
      *> COBOLNET2104, through the one reader the four clauses printing this pair share.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1075FR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "Z" THRU "A"
           ALPHABET DREV IS "A" THRU "Z".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "pb1075fr.dat"
               ORGANIZATION INDEXED ACCESS MODE DYNAMIC
               RECORD KEY IS IX-KEY
               COLLATING SEQUENCE FOR ALPHANUMERIC IS REV
                                  FOR ALPHANUMERIC IS DREV.
       DATA DIVISION.
       FILE SECTION.
       FD IX.
       01 IX-REC.
          05 IX-KEY PIC X.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.

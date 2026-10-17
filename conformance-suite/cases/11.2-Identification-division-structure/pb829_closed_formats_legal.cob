*>--- conformance-case
*> id: pb829_closed_formats_legal
*> rules: [FMT-11.2.1, FMT-12.3.2, FMT-12.3.7.2, FMT-12.4.5.1, FMT-12.4.6.2, FMT-13.4.5.2, FMT-13.4.6.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB829 - the POSITIVE half of closing the grammar's last total sink.  Six alternative lists
      *> spanning EIGHT closed general formats used to end in `genericClause : IDENTIFIER (IDENTIFIER|literal)*`
      *> and now end in the ONE `unrecognizedClause` error production, refused by name (COBOLNET1941/1970/1971).
      *> Closing a general-format clause list against a lossy OCR diagram REJECTS LEGAL SOURCE, which is
      *> strictly worse than the silence it replaces (kb/Work PB487's ALIGNED trap), so every list was RENDERED
      *> from the printed page first.  THIS program is the witness that the rendering was right: it writes a
      *> LEGAL clause or paragraph in each newly-closed format and must compile and run.
      *>   11.2.1   IDENTIFICATION DIVISION  -> the OPTIONS paragraph
      *>   12.3.2   CONFIGURATION SECTION    -> SOURCE-COMPUTER, OBJECT-COMPUTER, SPECIAL-NAMES, REPOSITORY
      *>   12.3.7.2 SPECIAL-NAMES paragraph  -> CURRENCY SIGN, ALPHABET, CLASS
      *>   12.4.5.1 file control entry       -> ORGANIZATION, ACCESS MODE, FILE STATUS
      *>   12.4.6.2 I-O-CONTROL paragraph    -> SAME RECORD AREA
      *>   13.4.5.2 file description entry   -> BLOCK CONTAINS, RECORD CONTAINS, CODE-SET
      *>   13.4.6.2 sort-merge file desc.    -> RECORD CONTAINS (the format's ONE clause)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB829OK.
       OPTIONS.
           ARITHMETIC IS NATIVE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ-9000.
       OBJECT-COMPUTER. XYZ-9000.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "#"
           ALPHABET ALPHA-1 IS STANDARD-1
           CLASS DIGITS IS "0" THROUGH "9".
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb829ok.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
           SELECT G ASSIGN TO "pb829ok2.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SF ASSIGN TO "pb829ok3.dat".
       I-O-CONTROL.
           SAME RECORD AREA FOR F G.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           BLOCK CONTAINS 1 RECORDS
           RECORD CONTAINS 10 CHARACTERS
           CODE-SET IS ALPHA-1.
       01 FREC PIC X(10).
       FD  G
           RECORD CONTAINS 10 CHARACTERS.
       01 GREC PIC X(10).
       SD  SF
           RECORD CONTAINS 10 CHARACTERS.
       01 SREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 WS-STAT PIC XX.
       01 WS-AMT  PIC #99.99 VALUE 12.34.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "AMT=" WS-AMT
           IF "7" IS DIGITS
               DISPLAY "CLASS-OK"
           END-IF
           DISPLAY "PB829-LEGAL-FORMATS-OK"
           STOP RUN.
       END PROGRAM PB829OK.

      *> reject-at: 2023
      *> ISO 13.18.63.3 SR1, SECOND SHAPE, REACHED THROUGH A TYPE CLAUSE: "The
      *> subject of the entry shall not be a strongly-typed group item or a
      *> variable-length group."  13.18.57.4 GR2 a) gives R the subordinate
      *> elements of T, so R has a dynamic-length elementary item subordinate to
      *> it and is a variable-length group by 8.5.1.12.1; 13.16.3 SR14 permits
      *> TYPE and VALUE in one entry, so SR1 is the rule that refuses it.
      *> The VALUE is on R's entry and the dynamic-length item on T's: neither
      *> entry carries the violation alone, which is why the screen walks the
      *> entries AS COMPOSED (kb/Work PB574 - this compiled clean when the TYPE
      *> copy dropped the DYNAMIC LENGTH clause; kb/Work PB522 fixed the copy).
      *> The dynamic-length item sits one level deeper than R's own children, so
      *> the screen's walk is the whole composed subtree.
      *> Twin: negative/pb184-group-value-variable-length-subject (inline spelling).
      *> Edition band: DYNAMIC LENGTH is COBOL-2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB574N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF.
          05 G.
             10 D PIC X DYNAMIC LENGTH.
       01 R TYPE T VALUE "AB".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X"
           STOP RUN.

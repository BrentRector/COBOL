      *> kb/Work PB574 - a TYPE clause composes a DYNAMIC LENGTH subordinate.
      *> ISO 13.18.57.4 GR2 a): the subject of a TYPE entry "is a group whose
      *> subordinate elements have the same names, descriptions, and hierarchy as
      *> the subordinate elements of type-name-1", so D IN R is a dynamic-length
      *> elementary item exactly as the inline D IN Q is, and FUNCTION LENGTH of
      *> each after MOVE "ABCDE" is 5 (8.5.1.10: a dynamic-length item's length
      *> is the length of its current content).
      *> MEASURED ON THE TREE THE NOTE WAS FILED FROM: the TYPE copy dropped the
      *> DYNAMIC LENGTH clause and D IN R answered 1 (fixed by kb/Work PB522).
      *> Twin: negative/pb574-group-value-typedef-dynamic-length (13.18.63.3 SR1
      *> reached through the same composition).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB574DYN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF.
          05 D PIC X DYNAMIC LENGTH.
       01 R TYPE T.
       01 Q.
          05 D PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
           MOVE "ABCDE" TO D IN R.
           MOVE "ABCDE" TO D IN Q.
           DISPLAY "TYPE=" FUNCTION LENGTH(D IN R)
                   " INLINE=" FUNCTION LENGTH(D IN Q).
           DISPLAY "R=[" D IN R "]".
           STOP RUN.

      *> kb/Work PB2844 - a constant's LENGTH OF operand may name a subordinate that a SAME AS clause
      *> supplies (ISO 13.18.49.4 GR2 a): "the subject of the entry is a group whose subordinate elements
      *> have the same names, descriptions, and hierarchy as the subordinate elements of data-name-1"),
      *> whether the subject is described before or after the constant. Expected (13.10.4 GR6):
      *>   KS = 3   LENGTH OF N OF S, S SAME AS U described after the constant (N is PIC X(3))
      *>   KP = 5   LENGTH OF M OF P, P SAME AS U described before the constant
      *>   Y is X(3) (demands KS before S is described)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2844SAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U.
          05 M PIC X(5).
          05 N PIC X(3).
       01 P SAME AS U.
       01 KP CONSTANT AS LENGTH OF M OF P.
       01 KS CONSTANT AS LENGTH OF N OF S.
       01 Y PIC X(KS).
       01 S SAME AS U.
       PROCEDURE DIVISION.
           DISPLAY KS " " KP " " FUNCTION LENGTH(Y)
           STOP RUN.

      *> PB1179 - the national, dynamic-length and USAGE DISPLAY boolean
      *>   arms of the STRING statement's identifier-3 (ISO 14.9.43).
      *> cite.py --check 14.9.43.4 "Before each move of a character to the
      *>   data item referenced by identifier-3" -> OK  14.9.43.4 8)
      *> cite.py --check 14.9.43.4 "the current length of the data item is
      *>   used to determine the number of characters moved" -> OK 1)
      *> cite.py --check 14.9.43.4 "only the portion of the data item
      *>   referenced by identifier-3 that was referenced during the
      *>   execution of the STRING statement is changed" -> OK  7)
      *> cite.py --check 14.9.43.4 "If, at the time of execution of a STRING
      *>   statement with the NOT ON OVERFLOW phrase" -> OK  9)
      *> Derivation (national receiver NOUT PIC N(3) "...", sender
      *>   NS PIC N(4) "AB-C"; positions count national characters):
      *>   A pointer 1: "AB-" move at 1-3 (pointer 4); before C the pointer
      *>     4 exceeds 3 (GR8): ON OVERFLOW runs, NOUT "AB-", pointer 04.
      *>   B pointer 0 is below one before the first move (GR8): nothing is
      *>     transferred, NOUT keeps "...", the pointer stays 00 (GR6).
      *>   C pointer 2, "XY" fills 2-3, pointer 4, no GR8 condition (GR9):
      *>     NOT ON OVERFLOW runs, NOUT ".XY" (GR7), pointer 04.
      *>   D pointer 4 exceeds 3 before the first move: ON OVERFLOW, NOUT
      *>     unchanged, pointer 04.
      *>   E "AB-C" delimited by N"B" sends "A" into "12345": "A2345".
      *>   F dynamic-length sender "XYZ" delimited by SIZE moves its
      *>     CURRENT length 3, not the limit 8 (GR1): "XYZQ" then pointer 5.
      *>   G the same for a dynamic-length national sender: "UVWQ", 05.
      *>   H dynamic-length national receiver "AB": "CD" fills 1-2 -> "CD";
      *>     pointer 3 "EF" fills 3-4 over the widened image -> "CDEF".
      *>   I a USAGE DISPLAY boolean receiver (SR1 admits usage display) is
      *>     a string of '0'/'1' characters: B"101" -> "10100000" (GR7).
      *>   J a USAGE NATIONAL boolean receiver (SR1 admits usage national)
      *>     likewise: B"10" then B"1" -> "101" over "000000" -> "101000".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1179B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NS PIC N(4) VALUE N"AB-C".
       01 NOUT PIC N(3) VALUE N"...".
       01 NP PIC 99 VALUE 1.
       01 NQ PIC N(5) VALUE N"12345".
       01 D1 PIC X DYNAMIC LENGTH LIMIT 8.
       01 N1 PIC N DYNAMIC LENGTH LIMIT 8.
       01 NR PIC N DYNAMIC LENGTH LIMIT 6.
       01 AR PIC X(12) VALUE ALL ".".
       01 NA PIC N(12) VALUE ALL N".".
       01 BR PIC 1(8) VALUE B"00000000".
       01 BN PIC 1(6) USAGE NATIONAL VALUE B"000000".
       PROCEDURE DIVISION.
           STRING NS DELIMITED BY SIZE INTO NOUT WITH POINTER NP
               ON OVERFLOW DISPLAY "A-OV[" NOUT "] " NP
               NOT ON OVERFLOW DISPLAY "A-NOV"
           END-STRING
           MOVE N"..." TO NOUT
           MOVE 0 TO NP
           STRING NS DELIMITED BY SIZE INTO NOUT WITH POINTER NP
               ON OVERFLOW DISPLAY "B-OV[" NOUT "] " NP
               NOT ON OVERFLOW DISPLAY "B-NOV"
           END-STRING
           MOVE 2 TO NP
           STRING N"XY" DELIMITED BY SIZE INTO NOUT WITH POINTER NP
               ON OVERFLOW DISPLAY "C-OV[" NOUT "] " NP
               NOT ON OVERFLOW DISPLAY "C-NOV[" NOUT "] " NP
           END-STRING
           MOVE 4 TO NP
           STRING N"X" DELIMITED BY SIZE INTO NOUT WITH POINTER NP
               ON OVERFLOW DISPLAY "D-OV[" NOUT "] " NP
               NOT ON OVERFLOW DISPLAY "D-NOV"
           END-STRING
           STRING NS DELIMITED BY N"B" INTO NQ
               NOT ON OVERFLOW DISPLAY "E[" NQ "]"
           END-STRING
           MOVE "XYZ" TO D1
           MOVE 1 TO NP
           STRING D1 DELIMITED BY SIZE "Q" DELIMITED BY SIZE
               INTO AR WITH POINTER NP
           DISPLAY "F[" AR "] " NP
           MOVE N"UVW" TO N1
           MOVE 1 TO NP
           STRING N1 DELIMITED BY SIZE N"Q" DELIMITED BY SIZE
               INTO NA WITH POINTER NP
           DISPLAY "G[" NA "] " NP
           MOVE N"AB" TO NR
           STRING N"CD" DELIMITED BY SIZE INTO NR
           DISPLAY "H1[" NR "]"
           MOVE 3 TO NP
           STRING N"EF" DELIMITED BY SIZE INTO NR WITH POINTER NP
           DISPLAY "H2[" NR "]"
           STRING B"101" DELIMITED BY SIZE INTO BR
           DISPLAY "I[" BR "]"
           STRING B"10" B"1" DELIMITED BY SIZE INTO BN
           DISPLAY "J[" BN "]"
           STOP RUN.

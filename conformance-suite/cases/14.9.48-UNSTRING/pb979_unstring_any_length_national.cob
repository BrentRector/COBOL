*>--- conformance-case
*> id: pb979_unstring_any_length_national
*> rules: [SR-14.9.48.3-3, GR-14.9.48.4-4, GR-14.9.48.4-8, GR-14.9.48.4-11]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB979 + PB980 - UNSTRING into the receivers whose size exists only at run time, and the
      *> all-national rule's conforming shape. ISO 14.9.48.4 GR11 b): without DELIMITED BY "the number
      *> of characters examined is equal to the size of the current receiving area"; an ANY LENGTH item
      *> is n repetitions of its PICTURE symbol, "where n is the length of the corresponding argument"
      *> (13.18.2.4 GR1 b). Before the fix 14.9.48.4 GR11 b) examined ONE character here and
      *> `UNSTRING S2 INTO L` over "WXYZ" stored "W". Every value is derived from the rules:
      *>
      *> NAT  NS = N"AB,12,,XYZ" DELIMITED BY N"," INTO NR1 NN NN2 NR2 DELIMITER IN ND. Every operand
      *>      SR3 names is national - the two numeric receivers are USAGE NATIONAL, which SR4 pairs with
      *>      the national operands (DETERMINATION D-UN3). NR1 "AB ", NN "12" -> 012, NN2 meets two
      *>      contiguous delimiters: GR8 zero-fills it -> 000, NR2 "XYZ ", and ND's delimiting condition
      *>      is the end of the sender, so GR11 d) space-fills it.
      *> DLM  in the contained program L is a 6-character argument and L2 a 4-character one; S =
      *>      "PQ,RSTUVW" DELIMITED BY "," INTO L2 DELIMITER IN L COUNT IN C -> L2 "PQ  ", L ",     ",
      *>      C 02 (GR11 e counts the examined characters, excluding the delimiter).
      *> SIZE UNSTRING S INTO L2 L - L2 examines its 4 positions ("PQ,R"), L its 6 of the 5 left
      *>      ("STUVW") -> L2 "PQ,R", L "STUVW ". The caller's arguments show the same content.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB979ANYLEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NS  PIC N(10) VALUE N"AB,12,,XYZ".
       01 NR1 PIC N(3).
       01 NN  PIC 9(3) USAGE NATIONAL.
       01 NN2 PIC 9(3) USAGE NATIONAL VALUE 999.
       01 NR2 PIC N(4).
       01 ND  PIC N(1) VALUE N"*".
       01 X6  PIC X(6) VALUE "ABCDEF".
       01 X4  PIC X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
           UNSTRING NS DELIMITED BY N"," INTO NR1 NN NN2
               NR2 DELIMITER IN ND
           DISPLAY "NAT  [" NR1 "] " NN " " NN2 " [" NR2 "] [" ND "]"
           CALL "PB979ANYLENT" USING X6 X4
           DISPLAY "ARGS [" X6 "] [" X4 "]"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB979ANYLENT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S  PIC X(9) VALUE "PQ,RSTUVW".
       01 C  PIC 99.
       LINKAGE SECTION.
       01 L  PIC X ANY LENGTH.
       01 L2 PIC X ANY LENGTH.
       PROCEDURE DIVISION USING L L2.
           UNSTRING S DELIMITED BY "," INTO L2 DELIMITER IN L
               COUNT IN C
           DISPLAY "DLM  [" L2 "] [" L "] " C
           UNSTRING S INTO L2 L
           DISPLAY "SIZE [" L2 "] [" L "]"
           GOBACK.
       END PROGRAM PB979ANYLENT.
       END PROGRAM PB979ANYLEN.

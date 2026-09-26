      *> kb/Work PB1166 - "the same PICTURE clause" across source
      *> elements, and the bit / national group pairing, the ACCEPT half.
      *>   cite.py --check 14.8.2.3.2 "Currency symbols match if and only
      *>     if the corresponding currency strings are the same"  OK 2) a)
      *>   cite.py --check 14.8.2.3.2 "Comma picture symbols match if and
      *>     only if the DECIMAL-POINT IS COMMA clause is in effect for both
      *>     the activating and the activated runtime elements or for
      *>     neither of them"                                      OK 2) b)
      *>   cite.py --check 14.8.2.3.2 "A bit group item matches an
      *>     elementary bit data item described with the same number of
      *>     boolean positions"                                    OK 2) b)
      *>   cite.py --check 14.8.3.3 "A national group item matches an
      *>     elementary data item of usage national described with the
      *>     same number of national character positions"          OK 3)
      *>   cite.py --check 14.8.2.1 "A bit group or national group is
      *>     treated as an elementary item"                        OK NOTE
      *> Legs, each derived from the rule, each able to fail:
      *>  CUR  the program's symbol U and the class's symbol $ both stand
      *>       for "USD" - different SYMBOLS, the same currency STRING, so
      *>       rule 2 a) makes the pictures match: the RETURNING compiles
      *>       and delivers the class's 12,34 edited as " USD12,34" under
      *>       the receiver's own U9.99 mask (a refusal, or a mask read in
      *>       the other alphabet, fails the leg).
      *>  DPC  DECIMAL-POINT IS COMMA is in effect for BOTH elements, so
      *>       rule 2 b) makes Z9,99 match Z9,99: 12,34 arrives as 12,34.
      *>  NG   a national group BY REFERENCE into a PIC N(5) formal and a
      *>       PIC N(5) argument into a national-group formal: 5 national
      *>       positions each side, so "Additionally" c) makes them match.
      *>       The callee sees the caller's five characters in order and
      *>       its store comes home in order - the elementary alphabet on
      *>       both ends (the storage image would interleave NUL bytes).
      *>  BG   the bit twin: GROUP-USAGE BIT of 8 boolean positions against
      *>       PIC 1(8) USAGE BIT, "Additionally" b).
      *>  RN   a national group RETURNING into a PIC N(5) receiver and the
      *>       reverse, §14.8.3.3 "Additionally" 3), CALL ... AS NESTED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W61BPB1166.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "U"
           DECIMAL-POINT IS COMMA.
       REPOSITORY.
           CLASS W61BPB1166C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE W61BPB1166C.
       01 RCUR PIC UUU9,99.
       01 RDPC PIC Z9,99.
       01 GN GROUP-USAGE NATIONAL.
          05 GN1 PIC N(2).
          05 GN2 PIC N(3).
       01 EN PIC N(5).
       01 GB GROUP-USAGE BIT.
          05 GB1 PIC 1(4).
          05 GB2 PIC 1(4).
       01 EB PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE W61BPB1166C "NEW" RETURNING O
           INVOKE O "GETC" RETURNING RCUR
           DISPLAY "CUR [" RCUR "]"
           INVOKE O "GETD" RETURNING RDPC
           DISPLAY "DPC [" RDPC "]"
           MOVE N"VWXYZ" TO GN
           INVOKE O "TAKEN" USING GN
           DISPLAY "NG1 [" GN1 "][" GN2 "]"
           MOVE N"PQRST" TO EN
           INVOKE O "TAKEG" USING EN
           DISPLAY "NG2 [" EN "]"
           MOVE B"11000011" TO GB
           INVOKE O "TAKEB" USING GB
           DISPLAY "BG1 [" GB1 "][" GB2 "]"
           MOVE B"10100101" TO EB
           INVOKE O "TAKEBG" USING EB
           DISPLAY "BG2 [" EB "]"
           CALL "W61BRN" AS NESTED RETURNING GN
           DISPLAY "RN1 [" GN1 "][" GN2 "]"
           CALL "W61BRG" AS NESTED RETURNING EN
           DISPLAY "RN2 [" EN "]"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W61BRN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC N(5).
       PROCEDURE DIVISION RETURNING LN.
       P.
           MOVE N"ABCDE" TO LN
           GOBACK.
       END PROGRAM W61BRN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W61BRG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG GROUP-USAGE NATIONAL.
          05 LG1 PIC N(2).
          05 LG2 PIC N(3).
       PROCEDURE DIVISION RETURNING LG.
       P.
           MOVE N"FG" TO LG1
           MOVE N"HIJ" TO LG2
           GOBACK.
       END PROGRAM W61BRG.
       END PROGRAM W61BPB1166.
       IDENTIFICATION DIVISION.
       CLASS-ID. W61BPB1166C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "$"
           DECIMAL-POINT IS COMMA.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC PIC $$$9,99.
       PROCEDURE DIVISION RETURNING LC.
           MOVE 12,34 TO LC.
       END METHOD GETC.
       METHOD-ID. GETD.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LD PIC Z9,99.
       PROCEDURE DIVISION RETURNING LD.
           MOVE 12,34 TO LD.
       END METHOD GETD.
       METHOD-ID. TAKEN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PN PIC N(5).
       PROCEDURE DIVISION USING PN.
           DISPLAY "TAKEN sees [" PN "]"
           MOVE N"KLMNO" TO PN.
       END METHOD TAKEN.
       METHOD-ID. TAKEG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PG GROUP-USAGE NATIONAL.
          05 PG1 PIC N(2).
          05 PG2 PIC N(3).
       PROCEDURE DIVISION USING PG.
           DISPLAY "TAKEG sees [" PG1 "][" PG2 "]"
           MOVE N"UV" TO PG1.
       END METHOD TAKEG.
       METHOD-ID. TAKEB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PE PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION USING PE.
           DISPLAY "TAKEB sees [" PE "]"
           MOVE B"11110000" TO PE.
       END METHOD TAKEB.
       METHOD-ID. TAKEBG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PB GROUP-USAGE BIT.
          05 PB1 PIC 1(4).
          05 PB2 PIC 1(4).
       PROCEDURE DIVISION USING PB.
           DISPLAY "TAKEBG sees [" PB1 "][" PB2 "]"
           MOVE B"0000" TO PB1.
       END METHOD TAKEBG.
       END OBJECT.
       END CLASS W61BPB1166C.

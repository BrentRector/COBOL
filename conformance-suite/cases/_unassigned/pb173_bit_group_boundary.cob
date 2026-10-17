*>--- conformance-case
*> id: pb173_bit_group_boundary
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB173 - a GROUP-USAGE BIT group crossing the CALL boundary. 14.9.4.3 SR6 admits it: "If the BY
      *> REFERENCE phrase is specified or implied for an identifier-2 that is a bit data item, identifier-2 shall
      *> be described such that it is aligned on a byte boundary and that subscripting and the leftmost position
      *> in a reference modification of identifier-2 consist of only fixed-point numeric literals ..." - and
      *> 13.18.29.4 GR1a makes a bit group "a bit data item", while a level-01 entry is byte-aligned by
      *> construction. 14.2.3 GR8 then makes the formal occupy "the same storage area as the argument", so the
      *> carrier across the boundary is the group's STORAGE IMAGE and the callee's store shows through.
      *> ⛔ THE DEFECT THIS PINS: the read half rendered the group's OPERAND value (GR1b's m boolean positions,
      *> AsBits) while the write half distributed it through FromImage, which reads ceil(m/8) PACKED characters -
      *> two alphabets on one carrier. Measured before the fix: A0 crossed as 11001010 and the callee saw
      *> 00110001 (the eight bits of the character '1'), and the caller got 00110000 back.
      *> GB is TWELVE bits, so its packed image is two characters with four pad bits - the leg that catches a
      *> round trip that drops the partial trailing byte.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB173BG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GA GROUP-USAGE BIT.
          05 GA1 PIC 1(4).
          05 GA2 PIC 1(4).
       01 GB GROUP-USAGE BIT.
          05 GB1 PIC 1(5).
          05 GB2 PIC 1(7).
      *> The SAME carrier pair, on its other broken arm: a REFERENCE-MODIFIED group argument. 8.4.3.3.4 GR6
      *> makes the ref-mod result "an elementary data item" over the slice, so the boundary carries the SLICE
      *> and splices it back - it must take neither the group-image arm nor the numeric decode arm, both of
      *> whose predicates read through to the INNER item. Measured before the fix: a BACKEND CS1061
      *> (`CobolStr.RefMod(RG.AsImage(),1,3).FromImage(...)` - FromImage on a string).
       01 RG.
          05 RG1 PIC X(3).
          05 RG2 PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE B"1100" TO GA1
           MOVE B"1010" TO GA2
           MOVE B"11111" TO GB1
           MOVE B"0000000" TO GB2
           DISPLAY "A0=" GA
           DISPLAY "B0=" GB
           CALL "PB173BS" AS NESTED USING GA
           DISPLAY "A1=" GA
           CALL "PB173BT" AS NESTED USING GB
           DISPLAY "B1=" GB
      *> BY CONTENT (14.9.4.4 GR5 - the callee operates on a COPY): the callee sees the current value and its
      *> store is NOT visible to the caller.
           CALL "PB173BT" AS NESTED USING BY CONTENT GB
           DISPLAY "B2=" GB
           MOVE "ABC" TO RG1
           MOVE "DEF" TO RG2
           CALL "PB173BR" AS NESTED USING RG(1:3)
           DISPLAY "R1=" RG
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB173BS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LGA GROUP-USAGE BIT.
          05 LA1 PIC 1(4).
          05 LA2 PIC 1(4).
       PROCEDURE DIVISION USING LGA.
       P.
           DISPLAY "S-IN=" LGA
           MOVE B"0000" TO LA1
           GOBACK.
       END PROGRAM PB173BS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB173BT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LGB GROUP-USAGE BIT.
          05 LB1 PIC 1(5).
          05 LB2 PIC 1(7).
       PROCEDURE DIVISION USING LGB.
       P.
           DISPLAY "T-IN=" LGB
           MOVE B"1010101" TO LB2
           GOBACK.
       END PROGRAM PB173BT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB173BR.
       DATA DIVISION.
       LINKAGE SECTION.
      *> The slice has no PICTURE clause, so BY REFERENCE through the NESTED phrase it conforms only to an
      *> alphanumeric group formal of no more bytes (14.8.2.3.2 rule 2, 14.8.2.2 rule 1; docs/CONFORMANCE.md
      *> D-RM1, kb/Work PB3101) - never to an elementary PIC X(3) formal.
       01 LR.
          05 LR1 PIC X(3).
       PROCEDURE DIVISION USING LR.
       P.
           DISPLAY "R-IN=" LR
           MOVE "ZZZ" TO LR
           GOBACK.
       END PROGRAM PB173BR.
       END PROGRAM PB173BG.

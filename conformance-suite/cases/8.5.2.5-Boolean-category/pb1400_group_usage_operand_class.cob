*>--- conformance-case
*> id: pb1400_group_usage_operand_class
*> rules: [GR-8.5.2.5-3, GR-8.5.2.10-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1400 — a GROUP-USAGE BIT group is an operand of class boolean and a GROUP-USAGE NATIONAL group
      *> an operand of class national, the subordinate group of each included.
      *>
      *> THE RULE. ISO §8.5.2.1: "a bit group item has class and category boolean" and "a national group item has
      *> class and category national". §8.5.2.5 3) puts in category boolean "A group item explicitly or implicitly
      *> described with a GROUP-USAGE clause with the BIT phrase", and §8.5.2.10 3) the same with the NATIONAL
      *> phrase in category national, so the subordinate groups GBS and GNS (implicitly described, §13.18.29.3
      *> SR2/SR3) are boolean and national operands exactly as their records are. The negative half, the same
      *> groups refused as ARITHMETIC operands (§8.8.1.1 admits only a numeric data item there), is
      *> negative/pb1400-group-usage-arithmetic-operand.
      *>
      *> EXPECTED VALUES, DERIVED — never measured.
      *>   GB = B"110011" (GB1 1100, GB2 11). B-XOR B"101010" (§8.7.2) = 011001.
      *>   GBS = B"11". B-AND B"01" = 01.
      *>   MOVE N"PQR" TO GN: §13.18.29.4 GR2 b) treats a national group as an elementary national item of
      *>   PICTURE N(3), so the move is national to national: GN1 = PQ, GN2 = R.
      *>   GN = N"PQR" compares two national operands of equal length: equal.
      *>   MOVE GNS TO NX (PIC N(3)): GNS is a national operand of one position, R, aligned left with space
      *>   fill (§14.6.8.5), so NX = "R  "; NX = N"R" is true because the shorter national operand is
      *>   extended with national spaces (§8.8.4.2.10 2)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1400GU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GB GROUP-USAGE BIT.
          05 GB1 PIC 1(4) VALUE B"1100".
          05 GBS.
             10 GB2 PIC 1(2) VALUE B"11".
       01 R6 PIC 1(6).
       01 R2 PIC 1(2).
       01 GN GROUP-USAGE NATIONAL.
          05 GN1 PIC N(2).
          05 GNS.
             10 GN2 PIC N(1).
       01 NX PIC N(3).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R6 = GB B-XOR B"101010".
           DISPLAY "GB-XOR=" R6.
           COMPUTE R2 = GBS B-AND B"01".
           DISPLAY "GBS-AND=" R2.
           MOVE N"PQR" TO GN.
           IF GN1 = N"PQ" AND GN2 = N"R"
               DISPLAY "GN-MOVE=OK"
           ELSE
               DISPLAY "GN-MOVE=BAD"
           END-IF.
           IF GN = N"PQR"
               DISPLAY "GN-EQ=OK"
           ELSE
               DISPLAY "GN-EQ=BAD"
           END-IF.
           MOVE GNS TO NX.
           IF NX = N"R"
               DISPLAY "GNS-MOVE=OK"
           ELSE
               DISPLAY "GNS-MOVE=BAD"
           END-IF.
           STOP RUN.

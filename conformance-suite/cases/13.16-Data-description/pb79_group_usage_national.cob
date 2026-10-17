*>--- conformance-case
*> id: pb79_group_usage_national
*> rules: [GR-13.16.4-2, SR-13.18.29.3-3, GR-13.18.29.4-2, AR-15.3-9, RV-15.50.4-2, FMT-13.18.29.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB79 - GROUP-USAGE NATIONAL (ISO 13.18.29): the group is a national group, class and category national,
      *> "treated as though it were an elementary data item of usage national ... described with PICTURE N(m),
      *> where m is the length of the group" (GR2 b) - so it is Table 16's NATIONAL row on both sides of a MOVE
      *> (padded / truncated in national positions), compares as class national, is INSPECTed in national
      *> positions, is reference-modifiable as an elementary item (8.4.3.3.3 SR1 last sentence), and FUNCTION
      *> LENGTH returns its national positions (15.50.4 r2) while BYTE-LENGTH returns its bytes (2 per position).
      *> Data-model design D20.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB79NAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NG GROUP-USAGE NATIONAL.
          05 N1 PIC N(2) VALUE N"ab".
          05 N2 PIC N(3) VALUE N"cde".
       01 NR PIC N(7) VALUE N"zzzzzzz".
       01 NS PIC N(3).
       01 CNT PIC 99 VALUE 0.
       01 NEST GROUP-USAGE NATIONAL.
          05 M1 PIC N VALUE N"1".
          05 M2.
             10 M3 PIC N(2) VALUE N"23".
       PROCEDURE DIVISION.
           DISPLAY "LEN=" FUNCTION LENGTH(NG) " BYTES=" FUNCTION BYTE-LENGTH(NG).
           MOVE NG TO NR.
           DISPLAY "NR=[" NR "]".
           MOVE NG TO NS.
           DISPLAY "NS=[" NS "]".
           MOVE N"xyz" TO NG.
           DISPLAY "N1=[" N1 "] N2=[" N2 "]".
           IF NG = N"xyz" DISPLAY "EQ" ELSE DISPLAY "NE" END-IF.
           MOVE N"aaaa" TO NG.
           INSPECT NG TALLYING CNT FOR ALL N"a".
           DISPLAY "CNT=" CNT.
           MOVE N"pqrst" TO NG.
           DISPLAY "RM=[" NG(2:3) "]".
           MOVE N"UV" TO NG(4:2).
           DISPLAY "NG=[" NG "]".
           DISPLAY "NEST=[" NEST "] LEN=" FUNCTION LENGTH(NEST) " M2-LEN=" FUNCTION LENGTH(M2).
           MOVE N"789" TO NEST.
           DISPLAY "M1=[" M1 "] M3=[" M3 "]".
           STOP RUN.

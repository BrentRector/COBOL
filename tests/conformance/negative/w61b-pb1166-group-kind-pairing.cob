      *> reject-at: 2002 2014 2023
      *> kb/Work PB1166 - the group-kind half of the CALL AS NESTED lane
      *> (§14.8.2.2 / §14.8.3.2 through §14.9.4.3 SR25), the REJECT half.
      *>   cite.py --check 14.8.2.1 "A bit group or national group is
      *>     treated as an elementary item"                        OK NOTE
      *>   cite.py --check 14.8.2.2 "shall be an alphanumeric group item or
      *>     an elementary item of category alphanumeric"           OK 1)
      *> A national group is treated as an ELEMENTARY national item, so it
      *> is not the alphanumeric group rule 1 pairs with an alphanumeric
      *> group of equal width (NG); and the elementary side of an
      *> alphanumeric-group pairing must be of CATEGORY alphanumeric, which
      *> PIC A(4) (alphabetic) is not (AL). Both are COBOLNET1736.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W61BNEGGK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GN GROUP-USAGE NATIONAL.
          05 GN1 PIC N(2).
       01 GA.
          05 GA1 PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           CALL "W61BNGA" AS NESTED USING GN
           CALL "W61BNAL" AS NESTED USING GA
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W61BNGA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PG.
          05 PG1 PIC X(4).
       PROCEDURE DIVISION USING PG.
       P.
           GOBACK.
       END PROGRAM W61BNGA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W61BNAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PA PIC A(4).
       PROCEDURE DIVISION USING PA.
       P.
           GOBACK.
       END PROGRAM W61BNAL.
       END PROGRAM W61BNEGGK.

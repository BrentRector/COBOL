*>--- conformance-case
*> id: pb3101_prototype_as_if_elementary_group
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3101 - the prototype signature comparison asks the ONE
      *> pair-mode relation (ActivationRelations.SignatureDifference).
      *> 10.6.2 SR2: a program prototype and a program definition of one
      *> externalized name "shall" have the same signature; 13.7.3 SR2:
      *> the prototype's formals "shall match the description" of the
      *> definition's. The prototype's first formal is a BIT GROUP of 8
      *> boolean positions, its second a NATIONAL GROUP of 3 national
      *> character positions; the definition's are PIC 1(8) USAGE BIT
      *> and PIC N(3) USAGE NATIONAL. 13.18.29.4 GR1 b) / GR2 b): unless
      *> stated otherwise a bit group "is treated as though it were an
      *> elementary data item of usage bit ... described with PICTURE
      *> 1(m)", a national group likewise with PICTURE N(m) - so the two
      *> signatures are the same (the comparator this replaced refused
      *> the pair, COBOLNET1513, by asking whether both sides were
      *> groups). The CALL through the prototype passes W-B (PIC 1(8)
      *> BIT) for the bit group - 14.8.2.3.2 rule 2 "Additionally" b):
      *> a bit group matches an elementary bit item of the same number
      *> of boolean positions - and W-N (PIC N(3)) for the national
      *> group (c)). The definition stores B"11110000" and N"XYZ":
      *> B=11110000 and N=XYZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3101G AS "P3101GX" IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-G GROUP-USAGE BIT.
          05 L-G1 PIC 1(4).
          05 L-G2 PIC 1(4).
       01 L-N GROUP-USAGE NATIONAL.
          05 L-N1 PIC N(2).
          05 L-N2 PIC N(1).
       PROCEDURE DIVISION USING L-G L-N.
       END PROGRAM P3101G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3101M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P3101G AS "P3101GX".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-B PIC 1(8) USAGE BIT VALUE B"10100101".
       01 W-N PIC N(3) USAGE NATIONAL VALUE N"ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL P3101G USING W-B W-N
           DISPLAY "B=" W-B
           DISPLAY "N=" W-N
           STOP RUN.
       END PROGRAM P3101M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3101D AS "P3101GX".
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-B PIC 1(8) USAGE BIT.
       01 L-N PIC N(3) USAGE NATIONAL.
       PROCEDURE DIVISION USING L-B L-N.
       P-MAIN.
           MOVE B"11110000" TO L-B
           MOVE N"XYZ" TO L-N
           GOBACK.
       END PROGRAM P3101D.

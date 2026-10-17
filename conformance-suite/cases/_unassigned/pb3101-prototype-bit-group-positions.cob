      *>--- conformance-case
      *> id: pb3101-prototype-bit-group-positions
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3101 - the converse of 2002/pb3101_prototype_as_if_
      *> elementary_group: the prototype's formal is a bit group of 8
      *> boolean positions, the definition's PIC 1(4) USAGE BIT. A bit
      *> group is the elementary PIC 1(8) item 13.18.29.4 GR1 b) treats
      *> it as, which is not the PIC 1(4) item, so the two signatures
      *> differ and 10.6.2 SR2 refuses the pair (COBOLNET1513).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101G AS "N3101GX" IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-G GROUP-USAGE BIT.
          05 L-G1 PIC 1(4).
          05 L-G2 PIC 1(4).
       PROCEDURE DIVISION USING L-G.
       END PROGRAM N3101G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3101D AS "N3101GX".
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-B PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION USING L-B.
       P-MAIN.
           GOBACK.
       END PROGRAM N3101D.

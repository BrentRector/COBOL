*>--- conformance-case
*> id: pb3101-override-group-for-elementary-formal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3101 - pair mode asks the ONE description relation, the OVERRIDE lane (11.7.3 SR9 through
      *> 9.3.8.2.3). The base method's formal is PIC X(4); the override's is a 4-byte alphanumeric GROUP. A group
      *> carries none of rule 3's clauses and the elementary item carries its PICTURE clause, so the two are not
      *> described the same however equal their lengths. The comparator this replaced answered "same" at equal
      *> width (14.8.2.2 rule 1's activation reading), so the override compiled.
      *> cite.py --check 9.3.8.2.3 "the corresponding formal parameter in interface-1 has the same ALIGNED,
      *>   ANY LENGTH, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses" -> OK 3)
      *> COBOLNET0829 on the overriding method.
       IDENTIFICATION DIVISION.
       CLASS-ID. K3101R.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 PIC X(4).
       PROCEDURE DIVISION USING L1.
           CONTINUE.
       END METHOD MG.
       END OBJECT.
       END CLASS K3101R.
       IDENTIFICATION DIVISION.
       CLASS-ID. K3101S INHERITS FROM K3101R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3101R.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MG OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1.
          05 L1A PIC X(2).
          05 L1B PIC X(2).
       PROCEDURE DIVISION USING L1.
           CONTINUE.
       END METHOD MG.
       END OBJECT.
       END CLASS K3101S.

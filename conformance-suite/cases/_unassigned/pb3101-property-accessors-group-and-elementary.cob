*>--- conformance-case
*> id: pb3101-property-accessors-group-and-elementary
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3101 - pair mode asks the ONE description relation, the PROPERTY lane (8.4.3.9.3 SR7). The get
      *> property method RETURNS a 4-byte alphanumeric GROUP and the set property method takes a PIC X(4) USING
      *> parameter: a group entry and an elementary entry are not the same data description, whatever their
      *> lengths. The comparator this replaced answered "same" at equal width, so the class compiled.
      *> cite.py --check 8.4.3.9.3 "The data description of the item specified in the RETURNING phrase of the
      *>   get property method shall be the same as the data description of the item specified as the USING
      *>   parameter of the set property method" -> OK 8.4.3.9.3 7)
      *> COBOLNET2727 on the set property method.
       IDENTIFICATION DIVISION.
       CLASS-ID. K3101T.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC X(4).
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY PV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R.
          05 RA PIC X(4).
       PROCEDURE DIVISION RETURNING R.
           MOVE V TO R.
       END METHOD.
       METHOD-ID. SET PROPERTY PV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 S PIC X(4).
       PROCEDURE DIVISION USING S.
           MOVE S TO V.
       END METHOD.
       END OBJECT.
       END CLASS K3101T.

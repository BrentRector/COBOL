*>--- conformance-case
*> id: pb3101_pair_mode_one_relation
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3101 - pair mode (an OVERRIDE, a property's two accessors) asks the ONE description
      *> relation the method match and the activation conformance read (ActivationRelations.SignatureDifference
      *> over ActivationDescriptions). Each pair below is the SAME description, so the source compiles; each
      *> value is derived from the rule, not measured:
      *>  O1 a bit group of 4 bit positions overridden by PIC 1(4) USAGE BIT: 13.18.29.4 GR1 b) treats the bit
      *>     group "as though it were an elementary data item of usage bit and class and category boolean
      *>     described with PICTURE 1(m)", so the two formals have the same PICTURE and USAGE clauses
      *>     (9.3.8.2.3 rule 3). Invoked through the base class, the override runs (9.3.6) and shows "1010".
      *>  O2 a national group of 2 + 2 national positions overridden by PIC N(4) USAGE NATIONAL: GR2 b), the
      *>     national twin; the override shows "WXYZ".
      *>  O3 a PIC X ANY LENGTH formal overridden by PIC X ANY LENGTH: the same clauses, ANY LENGTH included;
      *>     the length is the argument's (13.18.2.4 GR1 b)), "ANYLEN".
      *>  P1 a property whose get method RETURNS a PIC 9(4) and whose set method takes a PIC 9(4) USING
      *>     parameter: the same data description (8.4.3.9.3 SR7); SET then GET round-trips "0042".
      *> cite.py --check 9.3.8.2.3 "the corresponding formal parameter in interface-1 has the same ALIGNED,
      *>   ANY LENGTH, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses" -> OK 3)
      *> cite.py --check 13.18.29.4 "described with PICTURE 1(m)" -> OK 13.18.29.4 1) b)
      *> cite.py --check 8.4.3.9.3 "The data description of the item specified in the RETURNING phrase of the
      *>   get property method shall be the same as the data description of the item specified as the USING
      *>   parameter of the set property method" -> OK 8.4.3.9.3 7)
       IDENTIFICATION DIVISION.
       CLASS-ID. K3101P INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 9(4).
       PROCEDURE DIVISION.
       METHOD-ID. MB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 GROUP-USAGE BIT.
          05 L1A PIC 1(2) USAGE BIT.
          05 L1B PIC 1(2) USAGE BIT.
       PROCEDURE DIVISION USING L1.
           DISPLAY "BASE MB"
           GOBACK.
       END METHOD MB.
       METHOD-ID. MN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L2 GROUP-USAGE NATIONAL.
          05 L2A PIC N(2).
          05 L2B PIC N(2).
       PROCEDURE DIVISION USING L2.
           DISPLAY "BASE MN"
           GOBACK.
       END METHOD MN.
       METHOD-ID. MA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L3 PIC X ANY LENGTH.
       PROCEDURE DIVISION USING L3.
           DISPLAY "BASE MA"
           GOBACK.
       END METHOD MA.
       METHOD-ID. GET PROPERTY PV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9(4).
       PROCEDURE DIVISION RETURNING R.
           MOVE V TO R
           GOBACK.
       END METHOD.
       METHOD-ID. SET PROPERTY PV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 S PIC 9(4).
       PROCEDURE DIVISION USING S.
           MOVE S TO V
           GOBACK.
       END METHOD.
       END OBJECT.
       END CLASS K3101P.

       IDENTIFICATION DIVISION.
       CLASS-ID. K3101Q INHERITS FROM K3101P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3101P.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MB OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1 PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION USING L1.
           DISPLAY "O1 L1=" L1
           GOBACK.
       END METHOD MB.
       METHOD-ID. MN OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L2 PIC N(4) USAGE NATIONAL.
       PROCEDURE DIVISION USING L2.
           DISPLAY "O2 L2=" L2
           GOBACK.
       END METHOD MN.
       METHOD-ID. MA OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L3 PIC X ANY LENGTH.
       PROCEDURE DIVISION USING L3.
           DISPLAY "O3 L3=" L3
           GOBACK.
       END METHOD MA.
       END OBJECT.
       END CLASS K3101Q.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3101Q.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3101P
           CLASS K3101Q
           PROPERTY PV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE K3101P.
       01 WB GROUP-USAGE BIT.
          05 WBA PIC 1(2) USAGE BIT VALUE B"10".
          05 WBB PIC 1(2) USAGE BIT VALUE B"10".
       01 WN GROUP-USAGE NATIONAL.
          05 WNA PIC N(2) VALUE N"WX".
          05 WNB PIC N(2) VALUE N"YZ".
       01 WA PIC X(6) VALUE "ANYLEN".
       01 WV PIC 9(4).
       PROCEDURE DIVISION.
           INVOKE K3101Q "NEW" RETURNING OBJ
           INVOKE OBJ "MB" USING WB
           INVOKE OBJ "MN" USING WN
           INVOKE OBJ "MA" USING WA
           MOVE 42 TO PV OF OBJ
           MOVE PV OF OBJ TO WV
           DISPLAY "P1 PV=" WV
           STOP RUN.
       END PROGRAM P3101Q.

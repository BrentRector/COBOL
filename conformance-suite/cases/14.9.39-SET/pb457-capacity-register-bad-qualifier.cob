*>--- conformance-case
*> id: pb457-capacity-register-bad-qualifier
*> rules: [SR-14.9.39.3-29]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB457 - ISO 13.18.38.3 SR30 with ISO 8.4.2.2.3 SR4. Qualification of a CAPACITY register is
      *> PERMITTED (8.4.2.2.3 SR2 - "A name may be qualified even though it does not need qualification") and
      *> SR30 says where the register sits, so OTHER-G - which is not in WS-CAP's hierarchy - is not a legal
      *> qualifier: SR4 requires qualifiers "in the order of successively more inclusive levels in the
      *> hierarchy". Contrast tests/conformance/2014/pb457_capacity_register_qualified.cob, the positive half.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB457-CAP-QBAD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OTHER-G.
          05 OG-E  PIC X(3).
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 10.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-CAP OF OTHER-G TO 7.
           STOP RUN.

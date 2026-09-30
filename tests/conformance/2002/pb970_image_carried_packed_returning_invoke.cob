      *> kb/Work PB970 - the RETURNING and INVOKE arms of the one rule
      *> the 85 golden pb970_image_carried_binary_packed_argument pins
      *> for CALL USING: a PACKED-DECIMAL item seen through a REDEFINES
      *> (image-carried) crosses an activation boundary as its STORAGE.
      *> ISO 14.2.3 GR8: "If the argument is passed by reference, the
      *> activated runtime element operates as if the formal parameter
      *> occupies the same storage area as the argument."  14.6.5: "The
      *> result of the execution of a program, function, or method that
      *> specifies a RETURNING phrase in its procedure division header,
      *> is the content of the data item referenced by that RETURNING
      *> phrase."  14.8.2.3.3 rule 2a: for a method formal "the
      *> conformance rules are the same as for a COMPUTE statement".
      *> Derivation (no measured value is used):
      *>   RETURNING: the callee's returning item holds -123 (resp. the
      *>   method's -77 / -88); its content arrives in the receiver,
      *>   native or REDEFINED alike - RET-OK each time.
      *>   INVOKE BY REFERENCE: the method's formal occupies the
      *>   argument's storage, so its value is -42 and SUBTRACT 1 leaves
      *>   the caller's item at -43, from a native and a REDEFINED
      *>   argument, into a plain and a REDEFINED formal.  The REDEFINED
      *>   formal's bytes equal the method's LOCAL-STORAGE image of
      *>   -42 (same description, VALUE -42) - BYTES-OK.
      *>   INVOKE BY CONTENT: the formal holds the COMPUTE of the
      *>   argument (-42) into its description - BYTES-OK, VALUE-OK -
      *>   and the caller's item is untouched: -42.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB970MAIN02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB970C02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O   USAGE OBJECT REFERENCE PB970C02.
       01 NP  PIC S9(5) COMP-3 VALUE -42.
       01 RP  PIC S9(5) COMP-3 VALUE -42.
       01 RPX REDEFINES RP PIC X(3).
       01 RR  PIC S9(5) COMP-3.
       01 RRX REDEFINES RR PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB970RT02" RETURNING NP.
           IF NP = -123 DISPLAY "CALL-RET-NAT-OK"
                   ELSE DISPLAY "CALL-RET-NAT-BAD".
           CALL "PB970RT02" RETURNING RR.
           IF RR = -123 DISPLAY "CALL-RET-RED-OK"
                   ELSE DISPLAY "CALL-RET-RED-BAD".
           INVOKE PB970C02 "NEW" RETURNING O.
           MOVE -42 TO NP.
           INVOKE O "MN" USING BY REFERENCE NP RETURNING RR.
           IF NP = -43 DISPLAY "INV-NAT-NAT-OK"
                   ELSE DISPLAY "INV-NAT-NAT-BAD".
           IF RR = -77 DISPLAY "INV-RET-RED-OK"
                   ELSE DISPLAY "INV-RET-RED-BAD".
           INVOKE O "MN" USING BY REFERENCE RP RETURNING RR.
           IF RP = -43 DISPLAY "INV-RED-NAT-OK"
                   ELSE DISPLAY "INV-RED-NAT-BAD".
           MOVE -42 TO NP RP.
           INVOKE O "MR" USING BY REFERENCE NP.
           IF NP = -43 DISPLAY "INV-NAT-RED-OK"
                   ELSE DISPLAY "INV-NAT-RED-BAD".
           INVOKE O "MR" USING BY REFERENCE RP.
           IF RP = -43 DISPLAY "INV-RED-RED-OK"
                   ELSE DISPLAY "INV-RED-RED-BAD".
           MOVE -42 TO RP.
           INVOKE O "MR" USING BY CONTENT RP.
           IF RP = -42 DISPLAY "INV-CONTENT-OK"
                   ELSE DISPLAY "INV-CONTENT-BAD".
           INVOKE O "MQ" RETURNING NP.
           IF NP = -88 DISPLAY "INV-RET-NAT-OK"
                   ELSE DISPLAY "INV-RET-NAT-BAD".
           STOP RUN.
       END PROGRAM PB970MAIN02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB970RT02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R   PIC S9(5) COMP-3.
       01 RX  REDEFINES R PIC X(3).
       PROCEDURE DIVISION RETURNING R.
       RT-PARA.
           MOVE -123 TO R.
           GOBACK.
       END PROGRAM PB970RT02.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB970C02 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L   PIC S9(5) COMP-3.
       01 R   PIC S9(5) COMP-3.
       PROCEDURE DIVISION USING L RETURNING R.
       MN-PARA.
           SUBTRACT 1 FROM L.
           MOVE -77 TO R.
       END METHOD MN.
       METHOD-ID. MR.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 E   PIC S9(5) COMP-3 VALUE -42.
       01 EX  REDEFINES E PIC X(3).
       LINKAGE SECTION.
       01 L   PIC S9(5) COMP-3.
       01 LX  REDEFINES L PIC X(3).
       PROCEDURE DIVISION USING L.
       MR-PARA.
           IF LX = EX DISPLAY "MR-BYTES-OK" ELSE DISPLAY "MR-BYTES-BAD".
           IF L = -42 DISPLAY "MR-VALUE-OK" ELSE DISPLAY "MR-VALUE-BAD".
           SUBTRACT 1 FROM L.
       END METHOD MR.
       METHOD-ID. MQ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R   PIC S9(5) COMP-3.
       01 RX  REDEFINES R PIC X(3).
       PROCEDURE DIVISION RETURNING R.
       MQ-PARA.
           MOVE -88 TO R.
       END METHOD MQ.
       END OBJECT.
       END CLASS PB970C02.

      *>--- conformance-case
      *> id: pb970_image_carried_binary_packed_argument
      *> rules: [GR-14.2.3-8, GR-14.2.3-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * kb/Work PB970 - a BINARY / PACKED-DECIMAL argument crossing the
      * CALL boundary where EITHER side sees it through a REDEFINES
      * (an image-carried item).  ISO 14.2.3 GR8: "If the argument is
      * passed by reference, the activated runtime element operates as
      * if the formal parameter occupies the same storage area as the
      * argument."  GR9 (first branch - no program-specifier, no NESTED
      * phrase): the BY CONTENT record is "of the same length as the
      * argument" and the argument is moved into it "without
      * conversion".  So the formal sees the argument's STORAGE BYTES,
      * whatever the byte form, and a BY REFERENCE store is the
      * caller's storage.
      * Derivation (no measured value is used):
      *   The callee's own WORKING-STORAGE item E has the SAME
      *   description as the argument and VALUE -42 (resp. -7), so its
      *   REDEFINES view EX is the storage image of -42 (resp. -7).
      *   GR8/GR9 => the formal's view LX holds exactly those bytes:
      *   BYTES-OK, and the formal's value is -42 (resp. -7): VALUE-OK.
      *   BY REFERENCE, SUBTRACT 1 => the caller's item is -43 (-8).
      *   BY CONTENT, the store never reaches the caller: still -43.
      *   Each arm is run from a NATIVE argument and from a REDEFINED
      *   (image-carried) argument, into a REDEFINED formal and into a
      *   plain one - the four pairings of the one rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB970MAIN85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NP  PIC S9(5) COMP-3 VALUE -42.
       01 RP  PIC S9(5) COMP-3 VALUE -42.
       01 RPX REDEFINES RP PIC X(3).
       01 NB  PIC S9(4) COMP VALUE -7.
       01 RB  PIC S9(4) COMP VALUE -7.
       01 RBX REDEFINES RB PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB970PK85" USING BY REFERENCE NP.
           IF NP = -43 DISPLAY "NP-REF-OK" ELSE DISPLAY "NP-REF-BAD".
           CALL "PB970PK85" USING BY REFERENCE RP.
           IF RP = -43 DISPLAY "RP-REF-OK" ELSE DISPLAY "RP-REF-BAD".
           MOVE -42 TO RP.
           CALL "PB970PN85" USING BY REFERENCE RP.
           IF RP = -43 DISPLAY "RP-NAT-OK" ELSE DISPLAY "RP-NAT-BAD".
           MOVE -42 TO RP.
           CALL "PB970PK85" USING BY CONTENT RP.
           IF RP = -42 DISPLAY "RP-CON-OK" ELSE DISPLAY "RP-CON-BAD".
           CALL "PB970BK85" USING BY REFERENCE NB.
           IF NB = -8 DISPLAY "NB-REF-OK" ELSE DISPLAY "NB-REF-BAD".
           CALL "PB970BK85" USING BY REFERENCE RB.
           IF RB = -8 DISPLAY "RB-REF-OK" ELSE DISPLAY "RB-REF-BAD".
           STOP RUN.
       END PROGRAM PB970MAIN85.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB970PK85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E   PIC S9(5) COMP-3 VALUE -42.
       01 EX  REDEFINES E PIC X(3).
       LINKAGE SECTION.
       01 L   PIC S9(5) COMP-3.
       01 LX  REDEFINES L PIC X(3).
       PROCEDURE DIVISION USING L.
       PK-PARA.
           IF LX = EX DISPLAY "PK-BYTES-OK" ELSE DISPLAY "PK-BYTES-BAD".
           IF L = -42 DISPLAY "PK-VALUE-OK" ELSE DISPLAY "PK-VALUE-BAD".
           SUBTRACT 1 FROM L.
           EXIT PROGRAM.
       END PROGRAM PB970PK85.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB970PN85.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L   PIC S9(5) COMP-3.
       PROCEDURE DIVISION USING L.
       PN-PARA.
           IF L = -42 DISPLAY "PN-VALUE-OK" ELSE DISPLAY "PN-VALUE-BAD".
           SUBTRACT 1 FROM L.
           EXIT PROGRAM.
       END PROGRAM PB970PN85.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB970BK85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E   PIC S9(4) COMP VALUE -7.
       01 EX  REDEFINES E PIC X(2).
       LINKAGE SECTION.
       01 L   PIC S9(4) COMP.
       01 LX  REDEFINES L PIC X(2).
       PROCEDURE DIVISION USING L.
       BK-PARA.
           IF LX = EX DISPLAY "BK-BYTES-OK" ELSE DISPLAY "BK-BYTES-BAD".
           IF L = -7 DISPLAY "BK-VALUE-OK" ELSE DISPLAY "BK-VALUE-BAD".
           SUBTRACT 1 FROM L.
           EXIT PROGRAM.
       END PROGRAM PB970BK85.

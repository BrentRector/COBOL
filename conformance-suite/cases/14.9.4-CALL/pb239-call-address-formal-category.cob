*>--- conformance-case
*> id: pb239-call-address-formal-category
*> rules: [SR-14.9.4.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB239 - an address-identifier argument meets its formal
      *> under ISO 14.8.2 (14.9.4.3 SR25 imports it into Format 2).
      *> 14.8.2.3.2: "If either the argument or the formal parameter is of
      *> class pointer, the corresponding formal parameter or argument shall
      *> be of class pointer and the corresponding items shall be of the same
      *> category." ADDRESS OF R is a data item of category data-pointer
      *> (8.4.3.11.4 GR1); the AS NESTED callee's formal is PIC X(5) - a
      *> non-conforming pairing the activating element can see at compile
      *> time (COBOLNET1688).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB239NCI" AS NESTED USING ADDRESS OF R
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239NCI.
       DATA DIVISION.
       LINKAGE SECTION.
       01 F PIC X(5).
       PROCEDURE DIVISION USING F.
       MAIN.
           DISPLAY F
           GOBACK.
       END PROGRAM PB239NCI.
       END PROGRAM PB239NC.

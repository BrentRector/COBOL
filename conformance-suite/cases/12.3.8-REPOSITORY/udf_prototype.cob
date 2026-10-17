*>--- conformance-case
*> id: udf_prototype
*> rules: [GR-12.3.8.4-11, GR-13.7.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §11.5 Format 2 / §10.6 / §12.3.8 — a FUNCTION-ID … IS PROTOTYPE signature-only unit
      *> (LINKAGE-only data + a header-only PROCEDURE DIVISION, §10.6.2 SR4) precedes the caller (SR1); the
      *> in-group DEFINITION that follows is the activation target (§12.3.8 GR11a). Proves the IS PROTOTYPE
      *> parse, the no-body emit (the prototype registers no runtime module), and forward resolution through
      *> the REPOSITORY FUNCTION specifier. WS-N=7 ⇒ SQUARER(7)=49 ⇒ P=000049.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. SQUARER IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       END FUNCTION SQUARER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UPROTO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION SQUARER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4) VALUE 0007.
       01 WS-R PIC 9(6).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-R = FUNCTION SQUARER(WS-N).
           DISPLAY "P=" WS-R.
           STOP RUN.
       END PROGRAM UPROTO.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. SQUARER.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       COMPUTE-IT.
           COMPUTE L-R = L-X * L-X.
           GOBACK.
       END FUNCTION SQUARER.

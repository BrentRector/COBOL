*>--- conformance-case
*> id: pb239-call-address-identifier-at-85
*> rules: [FMT-14.9.4.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB239 - the address-identifier (ISO 8.4.3.1.2 identifier
      *> Format 9 - ADDRESS OF identifier-1 / ADDRESS OF PROGRAM) arrived in
      *> COBOL-2002 with the pointer classes, so the CALL argument 14.9.4.3
      *> SR3 names is an introduction gate below 2002 (COBOLNET0900). This
      *> program uses nothing else past COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB239N85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB239N8S" USING BY CONTENT ADDRESS OF X
           STOP RUN.

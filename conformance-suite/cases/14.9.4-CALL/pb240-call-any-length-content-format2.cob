*>--- conformance-case
*> id: pb240-call-any-length-content-format2
*> rules: [SR-14.9.4.3-11]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB240 - ISO 14.9.4.3 SR18 (FORMAT 2): "Identifier-4 shall not
      *> be described with the ANY LENGTH clause." Identifier-4 is the BY
      *> CONTENT / BY VALUE operand of the program-prototype CALL, so the
      *> contained program's ANY LENGTH formal L may cross BY REFERENCE
      *> (14.8.2.3.2 rules d/e) but not BY CONTENT - COBOLNET1542.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "ABCDEF".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB240NCI" AS NESTED USING X
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240NCI.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING BY REFERENCE L.
       MAIN.
           CALL "PB240NCL" AS NESTED USING BY CONTENT L
           GOBACK.
       END PROGRAM PB240NCI.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240NCL COMMON.
       DATA DIVISION.
       LINKAGE SECTION.
       01 M PIC X(6).
       PROCEDURE DIVISION USING BY REFERENCE M.
       MAIN.
           GOBACK.
       END PROGRAM PB240NCL.
       END PROGRAM PB240NC.

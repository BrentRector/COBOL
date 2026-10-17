*>--- conformance-case
*> id: pb240-call-any-length-formal-mismatch
*> rules: [SR-14.9.4.3-11]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB240 - ISO 14.8.2.3.2 rule e), imported into a Format-2
      *> CALL by 14.9.4.3 SR25: "If the argument is described with the ANY
      *> LENGTH clause, the corresponding formal parameter shall be described
      *> with the ANY LENGTH clause." L (ANY LENGTH) crosses BY REFERENCE to
      *> a fixed PIC X(6) formal - COBOLNET1688. (The converse - a fixed
      *> argument meeting an ANY LENGTH formal - is rule d)'s legal case.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240NM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "ABCDEF".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB240NMI" AS NESTED USING X
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240NMI.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING BY REFERENCE L.
       MAIN.
           CALL "PB240NML" AS NESTED USING BY REFERENCE L
           GOBACK.
       END PROGRAM PB240NMI.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB240NML COMMON.
       DATA DIVISION.
       LINKAGE SECTION.
       01 M PIC X(6).
       PROCEDURE DIVISION USING BY REFERENCE M.
       MAIN.
           GOBACK.
       END PROGRAM PB240NML.
       END PROGRAM PB240NM.

*>--- conformance-case
*> id: pb971-arg-omitted-below-2002
*> rules: [GR-14.9.4.4-12]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB971 -- the *-ARG-OMITTED conditions (ISO 14.9.4.4 GR12, 8.4.3.2.4 GR8, 14.9.23.4 GR10)
      *> need an omittable formal, and the OPTIONAL phrase of the procedure division header and the OMITTED
      *> argument are COBOL-2002 introductions: at COBOL-85 the program draws the omitted-arguments
      *> edition diagnostic rather than binding a reference that could raise the condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P971NG.
       PROCEDURE DIVISION.
           CALL "P971NS" USING OMITTED
           STOP RUN.
       END PROGRAM P971NG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P971NS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC X(4).
       PROCEDURE DIVISION USING OPTIONAL X.
           DISPLAY "X=[" X "]"
           EXIT PROGRAM.
       END PROGRAM P971NS.

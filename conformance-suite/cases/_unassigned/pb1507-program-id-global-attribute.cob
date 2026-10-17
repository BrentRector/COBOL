*>--- conformance-case
*> id: pb1507-program-id-global-attribute
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1507 - 11.10.2 Format 1: the PROGRAM-ID attribute group admits COMMON, INITIAL and RECURSIVE and
      *>   nothing else. GLOBAL is a data description clause (13.18.27), not a program attribute in any edition: it is
      *>   a syntax error here, not a word the binder drops.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GLB1507 IS GLOBAL.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
       END PROGRAM GLB1507.

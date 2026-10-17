*>--- conformance-case
*> id: pb507-blank-when-zero-binary
*> rules: [SR-13.18.8.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.8.3 SR2: "The subject of the entry shall be implicitly or explicitly described as usage
      *> display or usage national." WS-COMP is USAGE COMP (binary). Before the screen the clause was silently
      *> ignored - a zero store displayed 00000, not spaces (kb/Work PB507). 85-era; all four editions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB507BWB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-COMP PIC 9(5) USAGE COMP BLANK WHEN ZERO.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 0 TO WS-COMP
           DISPLAY "[" WS-COMP "]"
           STOP RUN.

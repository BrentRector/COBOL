*>--- conformance-case
*> id: pb204-nested-vlg-element-width
*> rules: [GR-8.5.1.12.1-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 8.5.1.12.3, MATCHING - the second half of the compatibility relation (kb/Work PB204). Corresponding
*> tables "match when the byte length of their elements is equal and their elements are compatible". Both
*> dynamic-capacity tables here occupy relative byte position 2, so 8.5.1.12.2 makes them CORRESPOND; their
*> elements are 3 and 4 bytes, so they do not MATCH, and 8.5.1.12.1 rule 2 fails. 14.8.2.2 therefore refuses
*> the crossing. (Element byte length is exactly what the receiving side divides by to recover the capacity,
*> so an unequal pair is not a technicality - it is the fact that makes the crossing recoverable.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VG.
          05 VF PIC X(2).
          05 VT OCCURS DYNAMIC CAPACITY IN VC FROM 1.
             10 VT-N PIC 9(2).
             10 VT-A PIC X.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB204N2S" AS NESTED USING VG
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204N2S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LVG.
          05 LF PIC X(2).
          05 LT OCCURS DYNAMIC CAPACITY IN LC FROM 1.
             10 LT-N PIC 9(2).
             10 LT-A PIC X(2).
       PROCEDURE DIVISION USING LVG.
       S1.
           CONTINUE.
       END PROGRAM PB204N2S.
       END PROGRAM PB204N2.

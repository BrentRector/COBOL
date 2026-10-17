*>--- conformance-case
*> id: pb332-start-length-word-required
*> rules: [FMT-14.9.41.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.41.2 prints `[ WITH LENGTH arithmetic-expression-1 ]` with LENGTH UNDERLINED.
      *> 5.2.2: an underlined uppercase word is a KEYWORD and is "required in order to select the
      *> functionality associated with that keyword". PB332 made WITH optional because it is NOT
      *> underlined; LENGTH is, so dropping LENGTH is not conforming source. 4.2.2 requires a
      *> mechanism "to indicate violations of the general formats and the explicit syntax rules of
      *> standard COBOL", and strict mode is where this compiler provides it. This file is the
      *> guard on that asymmetry: it writes the optional word and omits the required one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB332N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb332n1.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY PIC X(4).
          05 IX-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT IXF
           MOVE "AA" TO IX-KEY
           START IXF KEY IS >= IX-KEY WITH 2
               INVALID KEY DISPLAY "INVALID"
           END-START
           CLOSE IXF
           STOP RUN.

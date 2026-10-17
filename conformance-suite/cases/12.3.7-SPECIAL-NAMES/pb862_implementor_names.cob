      *>--- conformance-case
      *> id: pb862_implementor_names
      *> rules: [SR-12.3.7.3-5, SR-12.3.7.3-8, GR-12.3.7.4-2,
      *>    GR-12.3.7.4-3, DOC-A.1-189, DOC-A.1-191]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB862 + PB716 - what a SPECIAL-NAMES switch-name / feature-name / device-name entry may
      *> consist of (ISO 12.3.7.2, rendered from the printed page, PDF p320 / folio 290), at COBOL-85 where the
      *> entry was introduced.  12.3.7.3 SR8: "The implementor shall specify the names that are available for
      *> switch-name-1, feature-name-1, and device-name-1."  COBOL.NET's names are ONE table
      *> (Binding/ImplementorNames.cs; docs/CONFORMANCE.md section 7, A.1 items 189/190/191), and every entry
      *> below names a row of it in the arm its type allows:
      *>   SWITCH-3 - a switch-name with its mnemonic AND both status phrases, written OFF-first.  The status
      *>              groups sit inside CHOICE INDICATORS (5.2.6.4 - any order, each at most once); the old
      *>              grammar spelled them ON-then-OFF only and refused this line with COBOL0001.
      *>   UPSI-7   - a switch-name with a status phrase and no mnemonic (the braced second alternative).
      *>   SYSOUT / CONSOLE - device-names, used by DISPLAY UPON (12.3.7.3 SR7).
      *>   CSP / C01 - feature-names; declared and not used here (their WRITE ADVANCING rules are pinned by
      *>              FileIoDifferentialTests), because a declaration must be ACCEPTED for every row.
      *> Expected values, from the rules: the switches' initial status comes from the external facility
      *> (12.3.7.4 GR4 - COBOL.NET's is the COBOL_<SWITCH-NAME> environment variable, absent = OFF), so SW3-OFF
      *> and U7-OFF hold at entry; SET SW3 TO ON makes SW3-ON true (14.9.39.4 GR5); the CLASS test is the
      *> control that the paragraph still binds its other clauses.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB862NAMES.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-3 IS SW3 OFF STATUS IS SW3-OFF ON STATUS IS SW3-ON
           UPSI-7 ON STATUS IS U7-ON
           SYSOUT IS OUT-DEV
           CONSOLE IS CON-DEV
           CSP IS NO-SPACE
           C01 IS TOP-PAGE
           CLASS HEXDIG IS "0" THRU "9" "A" THRU "F".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC X VALUE "C".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF SW3-OFF DISPLAY "SW3=OFF" UPON OUT-DEV.
           SET SW3 TO ON.
           IF SW3-ON DISPLAY "SW3=ON" UPON CON-DEV.
           IF U7-ON DISPLAY "U7=ON" ELSE DISPLAY "U7=OFF".
           IF D IS HEXDIG DISPLAY "HEX=YES" ELSE DISPLAY "HEX=NO".
           STOP RUN.

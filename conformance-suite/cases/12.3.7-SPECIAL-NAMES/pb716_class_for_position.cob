*>--- conformance-case
*> id: pb716_class_for_position
*> rules: [FMT-12.3.7.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB716 - the CLASS clause's FOR phrase at its PRINTED position.  ISO 12.3.7.2 (rendered from the
      *> printed page, PDF p320 / folio 290) prints
      *>     CLASS class-name-1 [ FOR { ALPHANUMERIC | NATIONAL } ]
      *>         IS { literal-5 [ { THROUGH | THRU } literal-6 ] } ... [ IN alphabet-name-4 ]
      *> - the FOR group is on the CLASS line, BETWEEN class-name-1 and IS.  The grammar used to spell it AFTER
      *> the literals, so the printed form below was refused (COBOL0305 at FOR) and the unprinted
      *> `CLASS X IS "0" NATIONAL` compiled.  FOR is not underlined (8.3.2.4.3 - an optional word), so HEXU
      *> omits it.  The FOR phrase is a COBOL-2002 introduction (the national class entered with 2002), so
      *> this is the introducing edition; the negative below it is the version matrix's.
      *> Expected: "C" is in 0-9/A-F; "g" is not (12.3.7.4 GR12 - the class is exactly the listed characters).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB716CLASSFOR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS HEXA FOR ALPHANUMERIC IS "0" THRU "9" "A" THRU "F"
           CLASS HEXU ALPHANUMERIC IS "0" THRU "9" "A" THRU "F".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D1 PIC X VALUE "C".
       01 D2 PIC X VALUE "g".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF D1 IS HEXA DISPLAY "D1-HEXA=YES" ELSE DISPLAY "D1-HEXA=NO".
           IF D2 IS HEXA DISPLAY "D2-HEXA=YES" ELSE DISPLAY "D2-HEXA=NO".
           IF D1 IS HEXU DISPLAY "D1-HEXU=YES" ELSE DISPLAY "D1-HEXU=NO".
           STOP RUN.

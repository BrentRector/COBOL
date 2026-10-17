*>--- conformance-case
*> id: pb546_group_into_national_dynamic
*> rules: [GR-13.18.60.4-8]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB546 / design D-N8 - a GROUP sender into a national DYNAMIC LENGTH
      *> receiver. ISO 14.9.25.4 GR4: the move is alphanumeric to alphanumeric with
      *> no conversion, so DN's new STORAGE is G3's bytes; 8.5.1.10.4: the new length
      *> is the content's. Three bytes cannot be whole national content, so the last
      *> position is completed with the alphanumeric space (the fill the fixed
      *> national receiver FN takes): DN is two positions, LENGTH and BYTE-LENGTH 4
      *> (15.50.4 r6 and 15.14.4 r1 count bytes). Moved back into a 4-byte group
      *> (GR4 again) the bytes are ABC and a space; four bytes are two positions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB546D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G3.
          05 G3A PIC X(3) VALUE "ABC".
       01 G4.
          05 G4A PIC X(4) VALUE "WXYZ".
       01 GX.
          05 GXA PIC X(4).
       01 DN PIC N DYNAMIC LENGTH.
       01 FN PIC N(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE G3 TO DN
           DISPLAY "LENGTH-DN=" FUNCTION LENGTH(DN)
           DISPLAY "BYTE-LENGTH-DN=" FUNCTION BYTE-LENGTH(DN)
           MOVE DN TO GX
           DISPLAY "GX=[" GX "]"
           MOVE G4 TO DN
           DISPLAY "LENGTH-DN=" FUNCTION LENGTH(DN)
           MOVE DN TO GX
           DISPLAY "GX=[" GX "]"
           MOVE G3 TO FN
           MOVE FN TO GX
           DISPLAY "FN-GX=[" GX "]"
           STOP RUN.

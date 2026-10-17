*>--- conformance-case
*> id: pb133_omitted_args
*> rules: [GR-14.2.3-3, SR-14.9.4.3-24, GR-14.9.4.4-11, GR-14.9.4.4-12,
*>    FMT-8.8.4.8.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB133 wave C - the OMITTED-argument facility whole (ISO 14.9.4.4 GR11, 8.8.4.8, 14.2.2
      *> OPTIONAL, 14.8.2.1 trailing omission): the written OMITTED (B-OMITTED), the TRAILING omission
      *> (C-OMITTED - two arguments against three formals, the third OPTIONAL), and the present argument's
      *> NOT OMITTED reading its value (A=0005). All three presence answers ride the ONE null-carrier law.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OM1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC 9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "S1" AS NESTED USING W-A OMITTED
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC 9(4).
       01 L-B PIC 9(4).
       01 L-C PIC 9(4).
       PROCEDURE DIVISION USING L-A OPTIONAL L-B OPTIONAL L-C.
       P.
           IF L-B IS OMITTED DISPLAY "B-OMITTED" END-IF
           IF L-C IS OMITTED DISPLAY "C-OMITTED" END-IF
           IF L-A IS NOT OMITTED DISPLAY "A=" L-A END-IF
           GOBACK.
       END PROGRAM S1.
       END PROGRAM OM1.

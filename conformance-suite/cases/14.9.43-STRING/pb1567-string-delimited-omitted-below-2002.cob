*>--- conformance-case
*> id: pb1567-string-delimited-omitted-below-2002
*> rules: [SR-14.9.43.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1567 - COBOL-85's STRING statement closes every sending group with its DELIMITED phrase;
      *> the omission ISO §14.9.43.3 SR9 permits ("The DELIMITED phrase may be omitted only immediately
      *> preceding the INTO phrase") is a later form. VCR row 7.31 states the derived edge. The first group is
      *> delimited and only the last omits its phrase, which is where the gate reports.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1567N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(3) VALUE "ABC".
       01 C PIC X(8) VALUE SPACES.
       PROCEDURE DIVISION.
           STRING A DELIMITED BY SIZE A INTO C
           DISPLAY C
           STOP RUN.

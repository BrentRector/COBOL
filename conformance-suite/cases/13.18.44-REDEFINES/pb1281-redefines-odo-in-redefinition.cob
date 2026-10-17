*>--- conformance-case
*> id: pb1281-redefines-odo-in-redefinition
*> rules: [SR-13.18.44.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1281 (SR5's last sentence, the row's remaining unwitnessed arm) - ISO 13.18.44.3 SR5: "neither
      *> the original definition nor the redefinition shall include an occurs-depending table."
      *> cite.py: OK 13.18.44.3 5). Here the REDEFINITION (group B) includes the occurs-depending table B-T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1020HPB1281ODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2.
       01 G.
          05 A PIC X(6).
          05 B REDEFINES A.
             10 B-T PIC X OCCURS 1 TO 6 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
           DISPLAY A.
           STOP RUN.

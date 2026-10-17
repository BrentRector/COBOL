*>--- conformance-case
*> id: pb301-column-reserved-at-every-edition
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB301 - the COMPLEMENT that keeps the screen-word rule from degenerating into "admit every word
      *> the screen grammar tokenizes". COLUMN is a screen-surface token like the other twenty-one (the
      *> 13.17.2 COLUMN clause and the ACCEPT/DISPLAY positioning phrase both spell it), but ISO 8.9 reserves it
      *> at EVERY edition - it is the report-writer COLUMN clause's word long before the screen module existed -
      *> so 8.3.2.1 rule 1 bars it from a user-defined-word slot at 85 as well as at 2023. A gate derived from
      *> 8.10 membership alone, or a blanket nameSlot admission, would accept this program and silently break
      *> the COLUMN clause; this case is what fails first if that happens.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB301COLUMNRES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COLUMN PIC X(3) VALUE "W22".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY COLUMN.
           STOP RUN.

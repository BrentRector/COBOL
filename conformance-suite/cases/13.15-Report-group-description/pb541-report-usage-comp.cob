*>--- conformance-case
*> id: pb541-report-usage-comp
*> rules: [GR-13.15.4-2, SR-13.18.60.3-7]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ THE OTHER HALF OF §13.18.60.3 SR7, AND THE HALF THAT WAS SILENT (kb/Work PB541). The rule reads
*> "Only the DISPLAY or NATIONAL phrase may be specified in any USAGE clause associated with a report group
*> item." conformance:2002/pb541_report_national_item is the admitted side; this is the excluded side, and
*> it is written on a GROUP entry on purpose.
*>
*> A USAGE clause on a report group entry is "associated with" the report group items under it — that is
*> what §13.18.60.4 GR1 says a group-level USAGE clause IS: "If the USAGE clause is specified or implied at
*> a group level, it applies only to each elementary item in the group." Before this, the report binder
*> captured such a clause and handed it only to the printable LEAF, so a group entry's USAGE clause was
*> silently DISCARDED: this program compiled and RAN, printing 1234 from an item the source said was COMP.
*> A rejection is the only answer the rule allows, and it is edition-invariant — SR7 is in the '85 standard
*> unchanged, and neither COMP nor the report group description entry moved since.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB541RUC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb541ruc.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  W PIC 9(4) VALUE 1234.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           03  USAGE COMP.
               05  COLUMN 1 PIC 9(4) SOURCE W.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.

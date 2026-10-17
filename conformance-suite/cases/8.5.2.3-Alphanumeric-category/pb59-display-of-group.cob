*>--- conformance-case
*> id: pb59-display-of-group
*> rules: [GR-8.5.2.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.26.3 r1: "Argument-1 shall be of class national" - and per
      *> 8.5.2.1 "an alphanumeric group item has class and category alphanumeric"
      *> (8.5.2.10 item 3 makes a group national only under GROUP-USAGE NATIONAL,
      *> which this group does not carry), so a plain group is not an admissible
      *> argument-1. Before PB59 family 7b the classifier answered null for every
      *> group and the class screen FAILED OPEN - this program compiled and
      *> printed the group's image (AR-15.26.3-1's own repro). 2002+ because
      *> class national and DISPLAY-OF were introduced by ISO 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NEGDG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A-GRP.
          05 A-1 PIC X(3) VALUE "ABC".
          05 A-2 PIC X(3) VALUE "DEF".
       01 A-DEST PIC X(6).
       PROCEDURE DIVISION.
           MOVE FUNCTION DISPLAY-OF(A-GRP) TO A-DEST
           STOP RUN.

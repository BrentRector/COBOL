*>--- conformance-case
*> id: pb59-display-of-all-literal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.26.3 r1: "Argument-1 shall be of class national". An ALL "A"
      *> operand carries its literal's category (8.3.3.6.4 GR9 - an alphanumeric
      *> literal repeated), i.e. class ALPHANUMERIC - not an admissible
      *> argument-1. Before PB59 family 7b the classifier answered null for
      *> every ALL literal and the screen failed open (AR-15.26.3-1's ALL leg:
      *> "if legal it must convert, if illegal it must diagnose - it does
      *> neither"; now it diagnoses). ALL N"..." - class national - remains
      *> admissible.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NEGDA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A-DEST PIC X(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION DISPLAY-OF(ALL "A") TO A-DEST
           STOP RUN.

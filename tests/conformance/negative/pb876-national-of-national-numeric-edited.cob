      *> reject-at: 2002 2014 2023
      *> kb/Work PB876 - 15.66.3 rule 1: NATIONAL-OF's "Argument-1 shall be of class alphabetic or class alphanumeric."
      *> 8.5.2.1 Table 2 puts a numeric-edited item whose usage is national in class NATIONAL (and one whose usage is
      *> display in class alphanumeric), so a PIC ZZZ9 USAGE NATIONAL item is not a legal NATIONAL-OF argument-1: the
      *> screen read the CATEGORY (numeric-edited, which Table 2 folds into alphanumeric) and admitted it, while the
      *> twin DISPLAY-OF screen refused the same item as "not national". Both now read the Table-2 class.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB876NOF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NE PIC ZZZ9 USAGE NATIONAL VALUE N"  12".
       01 R PIC N(4) USAGE NATIONAL.
       PROCEDURE DIVISION.
           MOVE FUNCTION NATIONAL-OF(NE) TO R.
           STOP RUN.

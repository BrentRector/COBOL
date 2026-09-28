      *> kb/Work PB1273 — the PROPERTY clause's subject is the entry that carries it, and §13.18.42.3's syntax
      *> rules admit exactly these shapes:
      *>   N  — an elementary item SUBORDINATE to a group, uniquely named, not subject to OCCURS: SR2 ("shall not be
      *>        specified for data items subject to an OCCURS clause") and SR3 ("only for an elementary item whose
      *>        name does not require qualification for uniqueness of reference") are both met. GET and SET.
      *>   K  — an item in a CONSTANT RECORD WITH NO SET: SR5 ("If the PROPERTY clause is specified in a data item
      *>        described with the CONSTANT RECORD clause, or in any data item subordinate to a data item described
      *>        with the CONSTANT RECORD clause, the SET phrase shall be specified") — a GET accessor only.
      *>   M  — R1's other member, no clause: no accessor; N2 — a method's LOCAL-STORAGE item, which SR1 ("only in
      *>        the working-storage section of a factory definition or an instance definition") keeps out of the
      *>        object's property name space.
      *> Hand-derived stdout (§13.18.42.4 GR1/GR2 — the implicit GET returns the item, the implicit SET moves to it):
      *>   N=7 K=4
      *>   N=3 K=4
      *>   SHOW N=3 N2=9
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1273PM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1273PC
           PROPERTY N
           PROPERTY K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB1273PC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1273PC "NEW" RETURNING A.
           DISPLAY "N=" N OF A " K=" K OF A.
           MOVE 3 TO N OF A.
           DISPLAY "N=" N OF A " K=" K OF A.
           INVOKE A "SHOW".
           STOP RUN.
       END PROGRAM PB1273PM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1273PC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1.
          05 M PIC X VALUE "A".
          05 N PIC 9 VALUE 7 PROPERTY.
       01 CR CONSTANT RECORD.
          05 K PIC 9 VALUE 4 PROPERTY WITH NO SET.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 N2 PIC 9 VALUE 9.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SHOW N=" N " N2=" N2.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1273PC.

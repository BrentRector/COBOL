*>--- conformance-case
*> id: w59h_vlg_override_signature
*> rules: [GR-9.3.5.3-7]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1519 / R43 item 2 - the LIVE half of GR-9.3.5.3-7.
      *>   cite.py --check 9.3.5.3 "If the parameter is a variable-length
      *>     group, sufficient information to determine if this group would
      *>     match the group is specified in the invoke statement." -> OK
      *>     9.3.5.3 7)
      *> Parametric polymorphism (the half that SELECTS among same-named
      *> methods by that signature) is not claimed - A.4.10 item 3,
      *> COBOLNET0822 - but the signature is still consumed by a MANDATORY
      *> rule:
      *>   cite.py --check 11.7.3 "If the OVERRIDE phrase is specified,
      *>     there shall be a method with the same method resolution
      *>     signature" -> OK 11.7.3 3)
      *> THIS PROGRAM: the overriding TAKE declares the SAME variable-length
      *> group formal as the method it overrides (a dynamic-capacity table
      *> of 2-byte elements after a 2-byte prefix), so the signatures are
      *> the same and the override is accepted. The negative twin
      *> negative/w59h-vlg-override-fixed-group changes ONLY that table to a
      *> fixed OCCURS 1 table - compatible under 8.5.1.12, but not the same
      *> signature - and is refused with COBOLNET0829.
      *> DERIVATION of the .out line:
      *>   MOVE "bb" TO T(2) - T is a receiving item whose subscript exceeds
      *>   the capacity, so "the capacity of the table is increased to the
      *>   value given by the subscript" (cite.py --check 8.5.1.9.3 -> OK):
      *>   C = 2. O is a W59HDS object, so its overriding TAKE runs (the
      *>   base TAKE's "BASE" line never prints) and sees the caller's group
      *>   by reference: P = PP, T(1) = aa, T(2) = bb.
      *>   -> SUB P=PP T1=aa T2=bb
      *> 2014 dir: OCCURS DYNAMIC (13.18.38) is a COBOL-2014 construct.
       IDENTIFICATION DIVISION.
       CLASS-ID. W59HDB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-G.
          05 LK-P PIC X(2).
          05 LK-T PIC X(2) OCCURS DYNAMIC CAPACITY IN LK-C FROM 1.
       PROCEDURE DIVISION USING LK-G.
       MAIN.
           DISPLAY "BASE C=" LK-C.
       END METHOD TAKE.
       END OBJECT.
       END CLASS W59HDB.

       IDENTIFICATION DIVISION.
       CLASS-ID. W59HDS INHERITS FROM W59HDB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W59HDB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE OVERRIDE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-G.
          05 LK-P PIC X(2).
          05 LK-T PIC X(2) OCCURS DYNAMIC CAPACITY IN LK-C FROM 1.
       PROCEDURE DIVISION USING LK-G.
       MAIN.
           DISPLAY "SUB P=" LK-P " T1=" LK-T(1) " T2=" LK-T(2).
       END METHOD TAKE.
       END OBJECT.
       END CLASS W59HDS.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59HDM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS W59HDB
           CLASS W59HDS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE W59HDB.
       01 G.
          05 P PIC X(2).
          05 T PIC X(2) OCCURS DYNAMIC CAPACITY IN C FROM 1.
       PROCEDURE DIVISION.
           MOVE "PP" TO P
           MOVE "aa" TO T(1)
           MOVE "bb" TO T(2)
           INVOKE W59HDS "NEW" RETURNING O
           INVOKE O "TAKE" USING G
           STOP RUN.
       END PROGRAM W59HDM.

*>--- conformance-case
*> id: pb62-all-subscript-annuity
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.3: "When the definition of a function permits an argument to be repeated a variable number of
      *> times, a table may be referenced by specifying the data-name and any qualifiers that identify the table,
      *> followed immediately by subscripting where one or more of the subscripts is the word ALL." ANNUITY's format (15.9.2) is `( argument-1 argument-2 )` - fixed at two, no repetition (the arity error follows; this one is the cause).
      *> Before PB62 the ALL was expanded at bind time for EVERY function, so this bound (and evaluated) over a
      *> one-occurrence table and was rejected over a three-occurrence one only by the arity count of the
      *> expansion - "takes N argument(s); M given" about arguments the user never wrote. Diagnosed as the
      *> inadmissible ALL it is, at ANY cardinality.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB62ANNALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC S9(4) OCCURS 1 TIMES VALUE 11.
       01 B PIC 9 VALUE 5.
       01 R PIC S9(4)V9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION ANNUITY(E(ALL))
           STOP RUN.

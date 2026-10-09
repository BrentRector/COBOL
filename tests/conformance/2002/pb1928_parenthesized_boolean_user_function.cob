      *> kb/Work PB1928 - a boolean user-defined function enclosed in
      *> parentheses is the boolean operand it encloses. The routing
      *> predicate read the operand without looking through the
      *> parentheses, so IF (FUNCTION UBIT(3)) was refused as "a
      *> function reference" and IF (FUNCTION UBIT(3)) = WB as "not a
      *> numeric operand".
      *>
      *> 8.8.2: a boolean expression may be "a boolean expression
      *>   enclosed in parentheses".  (cite.py: OK 8.8.2)
      *> 8.4.3.2.4 1): the function's temporary takes "the description,
      *>   class, and category" of the RETURNING item, PIC 1 here, so
      *>   the reference is a boolean operand.  (cite.py: OK)
      *> 8.8.4.3: a simple boolean condition over a boolean operand of
      *>   length 1 is true when its value is B"1".
      *>
      *>   B1  IF FUNCTION UBIT(3)            T (the reference form)
      *>   B2  IF (FUNCTION UBIT(3))          T
      *>   B3  IF (FUNCTION UBIT(3)) = WB     T (boolean relation)
      *>   B4  IF NOT (FUNCTION UZERO(3))     T (UZERO returns B"0")
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UBIT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9.
       01 L-R PIC 1.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE B"1" TO L-R.
           GOBACK.
       END FUNCTION UBIT.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UZERO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9.
       01 L-R PIC 1.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE B"0" TO L-R.
           GOBACK.
       END FUNCTION UZERO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1928PB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION UBIT
           FUNCTION UZERO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WB PIC 1 VALUE B"1".
       PROCEDURE DIVISION.
       MAIN.
           IF FUNCTION UBIT(3) DISPLAY "B1 T" ELSE DISPLAY "B1 F"
           END-IF.
           IF (FUNCTION UBIT(3)) DISPLAY "B2 T" ELSE DISPLAY "B2 F"
           END-IF.
           IF (FUNCTION UBIT(3)) = WB DISPLAY "B3 T"
           ELSE DISPLAY "B3 F" END-IF.
           IF NOT (FUNCTION UZERO(3)) DISPLAY "B4 T"
           ELSE DISPLAY "B4 F" END-IF.
           STOP RUN.
       END PROGRAM PB1928PB.

*>--- conformance-case
*> id: pb68-boolean-function-vs-alphanumeric-literal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.2.2 (Format 2) / 8.8.4.2.1 F1 SR2/SR3: a boolean operand may be compared only with another
      *> boolean operand or the figurative constant ZERO. FUNCTION BOOLEAN-OF-INTEGER's result is class boolean
      *> (15.13.1 "The function type is boolean"; 15.2 item 2), so comparing it with an ALPHANUMERIC literal is
      *> a class mix - COBOLNET0844. Before PB68 this MIRROR of the legal boolean comparison was ACCEPTED and
      *> evaluated TRUE (the relation checkpoint's local class switch had no arm for a computed boolean
      *> operand, so neither side registered as boolean); the legal `= B"100000"` was the one rejected.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB68MIRROR.
       PROCEDURE DIVISION.
           IF FUNCTION BOOLEAN-OF-INTEGER(544, 6) = "100000"
               DISPLAY "TRUE" END-IF
           STOP RUN.

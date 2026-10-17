*>--- conformance-case
*> id: pb590-boolean-class-numeric-operand
*> rules: [SR-8.8.4.4.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.4.3 SR5: "BOOLEAN shall not be specified if the category of the data item referenced by
      *> identifier-1 is numeric or numeric-edited." One category short of SR4's list, which also names
      *> boolean - a BOOLEAN class test over a category-boolean item is the ordinary case, and the
      *> 2002/pb590_boolean_class_condition golden pins it.
      *>
      *> Below 2002 the word BOOLEAN is not reserved and this spelling is governed by
      *> pb590-boolean-class-condition-below-2002 instead, which is why this case rejects at 2002 and later.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB590NG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N9 PIC 9(4) VALUE 12.
       PROCEDURE DIVISION.
       MAIN.
           IF N9 IS BOOLEAN
               DISPLAY "BOOL"
           END-IF
           STOP RUN.

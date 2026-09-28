      *> kb/Work PB1427 - the predefined NULL is an IDENTIFIER, never a
      *> figurative constant, and it binds only where 8.4.3.10.3 SR1
      *> admits it. This golden runs the admitted INITIALIZE, SET and
      *> relation slots (the CALL / function / INVOKE argument slots are
      *> pb1630_null_argument_crossing); the refused slots are the
      *> negatives pb1427-null-*.
      *> RULE (8.4.3.1.2): "Format 8 (predefined-address)".
      *> RULE (8.4.3.10.3 SR1a): "If the class of the associated data
      *> item is pointer, it may be used only as a sending operand in an
      *> INITIALIZE or a SET statement ... or in a pointer-or-object-
      *> reference relation condition".
      *> RULE (14.9.20.3 SR3): "For each DATA-POINTER, FUNCTION-POINTER,
      *> MESSAGE-TAG, OBJECT-REFERENCE, or PROGRAM-POINTER phrase
      *> specified as the category-name in the REPLACING phrase,
      *> identifier-2 shall be specified." - NULL IS identifier-2.
      *> RULE (14.9.20.4 GR4): "If the category of a receiving-operand
      *> is data-pointer, function-pointer, message-tag, object-
      *> reference, or program-pointer, the implicit statement is:" a
      *> SET receiving-operand TO NULL here.
      *> RULE (8.4.3.10.4 GR1/GR3): NULL "references a data item of
      *> category data-pointer that contains the null address" (GR3:
      *> program-pointer), so each item compares equal to NULL after.
      *> cite.py --check 8.4.3.10.3 "it may be used only as a sending
      *>   operand in an INITIALIZE or a SET statement" -> OK 1) a)
      *> cite.py --check 14.9.20.3 "For each DATA-POINTER" -> OK 3)
      *> cite.py --check 14.9.20.4 "If the category of a receiving-
      *>   operand is data-pointer" -> OK 4)
      *> Before the fix INITIALIZE ... REPLACING ... BY NULL was refused
      *> COBOLNET1982 (NULL read as literal-1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1427M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WX PIC X VALUE "A".
       01 WP USAGE POINTER.
       01 WQ USAGE PROGRAM-POINTER.
       01 WO USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
           SET WP TO ADDRESS OF WX
           SET WQ TO ENTRY "PB1427M"
           IF WP = NULL DISPLAY "P0 NULL" ELSE DISPLAY "P0 ADDR".
           IF WQ = NULL DISPLAY "Q0 NULL" ELSE DISPLAY "Q0 ADDR".
           INITIALIZE WP REPLACING DATA-POINTER DATA BY NULL
           INITIALIZE WQ REPLACING PROGRAM-POINTER DATA BY NULL
           INITIALIZE WO REPLACING OBJECT-REFERENCE DATA BY NULL
           IF WP = NULL DISPLAY "P1 NULL" ELSE DISPLAY "P1 ADDR".
           IF NULL = WQ DISPLAY "Q1 NULL" ELSE DISPLAY "Q1 ADDR".
           IF WO = NULL DISPLAY "O1 NULL" ELSE DISPLAY "O1 SET".
           SET WP TO ADDRESS OF WX
           IF WP NOT = NULL DISPLAY "P2 ADDR" ELSE DISPLAY "P2 NULL".
           EVALUATE WP
               WHEN NULL DISPLAY "P3 NULL"
               WHEN OTHER DISPLAY "P3 ADDR"
           END-EVALUATE
           SET WP TO NULL
           EVALUATE WP
               WHEN NULL DISPLAY "P4 NULL"
               WHEN OTHER DISPLAY "P4 ADDR"
           END-EVALUATE
           STOP RUN.

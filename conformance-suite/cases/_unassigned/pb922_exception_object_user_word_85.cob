      *>--- conformance-case
      *> id: pb922_exception_object_user_word_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB922 - THE EDITION IS PART OF THE QUESTION. Below COBOL-2002 there is no object-orientation
      *> facility, ISO 8.9 does not reserve the word, and EXCEPTION-OBJECT is an ORDINARY user-defined word:
      *> this program is conforming COBOL-85 source and every line of it must run.
      *>
      *> It is the drift net for a TWO-ARM defect measured on this tree: the resolver arm that intercepts the
      *> predefined object reference (ISO 8.4.3.6.3 SR2) carried the 2002 gate while the receiving-operand screen
      *> for SR1 - "EXCEPTION-OBJECT shall not be specified as a receiving operand" - compared the spelling on
      *> its own, so `MOVE "ABCD" TO EXCEPTION-OBJECT` here drew COBOLNET2196: SR1 quoted at a program that
      *> references no register at all. The positive at the introducing edition is
      *> conformance-suite/cases/8.4.3.1-Identifier/pb922_exception_object_reference.cob.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *>
      *> W85-RECV=ABCD     The item is an ordinary alphanumeric data item, so 14.9.25.4 GR1's alphanumeric
      *>                   elementary move stores the literal in it - a RECEIVING use, which is exactly the
      *>                   position SR1 forbids at 2002 and later and permits here, because at COBOL-85 the name
      *>                   denotes this program's own item and not the standard's register.
      *> W85-SEND=ABCD     And a sending use of the same ordinary item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB922EXOBJ85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EXCEPTION-OBJECT  PIC X(4).
       01 W-COPY            PIC X(4).
       PROCEDURE DIVISION.
           MOVE "ABCD" TO EXCEPTION-OBJECT
           DISPLAY "W85-RECV=" EXCEPTION-OBJECT
           MOVE EXCEPTION-OBJECT TO W-COPY
           DISPLAY "W85-SEND=" W-COPY
           STOP RUN.

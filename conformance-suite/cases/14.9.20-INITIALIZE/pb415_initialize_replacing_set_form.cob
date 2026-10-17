*>--- conformance-case
*> id: pb415_initialize_replacing_set_form
*> rules: [SR-14.9.20.3-3, SR-14.9.20.3-8, GR-14.9.20.4-2, GR-14.9.20.4-5, GR-14.9.20.4-6, FMT-14.9.20.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB415SETF.
      *> kb/Work PB415 — ISO §14.9.20.4 GR4 + GR6b: the implicit SET whose sending-operand is identifier-2.
      *>
      *> GR4: "If the category of a receiving-operand is data-pointer, function-pointer, message-tag,
      *> object-reference, or program-pointer, the implicit statement is: SET receiving-operand TO
      *> sending-operand". GR6b: "If the data item does not qualify as a receiving-operand because of the VALUE
      *> phrase, but does qualify because of the REPLACING phrase, the sending-operand is the literal-1 or
      *> identifier-2 associated with the category specified in the REPLACING phrase."
      *>
      *> ⛔ THIS ARM WAS STAGED LOUD BY kb/Work PB418 AND IS LIVE HERE. GR6a1/GR6a2 and every pointer row of
      *> GR6c's fill table give the PREDEFINED NULL; GR6b does not, and writing NULL under REPLACING would be a
      *> WRONG answer rather than a missing one. It was unreachable because `initializeCategory` could not spell
      *> DATA-POINTER / PROGRAM-POINTER / OBJECT-REFERENCE at all — PB415's grammar row.
      *> §14.9.20.3 SR3 is what makes identifier-2 the only sender here: "For each DATA-POINTER,
      *> FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE, or PROGRAM-POINTER phrase specified as the
      *> category-name in the REPLACING phrase, identifier-2 shall be specified" (the negative witness for
      *> literal-1 is tests/conformance/negative/pb415-initialize-replacing-pointer-literal).
      *>
      *> EXPECTED, DERIVED FROM THE RULES ABOVE AND WRITTEN DOWN BEFORE THE RUN:
      *>   1[SET]   P1 is nulled, then REPLACING DATA-POINTER … BY SRC qualifies it through GR5c2 and GR6b
      *>            gives it SRC's address — NOT NULL. Before PB415 this statement did not parse.
      *>   1B[SAME] the same store, compared against the sender: GR6b's sending-operand IS identifier-2, so the
      *>            receiver holds exactly SRC (§8.4.3.10.4 makes two pointers equal when they address the same
      *>            item).
      *>   2[SET]   the same for USAGE PROGRAM-POINTER, whose sender PPSRC was SET TO ENTRY "PB415SUB".
      *>   3[SET]   the same for USAGE OBJECT REFERENCE (GR4's SET form covers object-reference too).
      *>   4[NULL]  CONTROL — ALL TO VALUE over the same receiver takes GR5c1a/GR6a1 instead and nulls it, so
      *>            line 1 is evidence about GR6b and not about the SET machinery in general.
      *>   5[SET|QQQ] a group receiver: the STRONG-typedef pointer member takes SRC through GR5c2/GR6b, and the
      *>            alphanumeric member names no category in the phrase, so GR5c leaves it unchanged.
      *>   6[NULL]  CONTROL IN THE OPPOSITE DIRECTION — REPLACING ALPHANUMERIC names a category the pointer is
      *>            not, so the pointer is not a receiving-operand at all and keeps the NULL it was given.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB415CL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TGT-A PIC X(4) VALUE "ABCD".
       01 SRC   USAGE POINTER.
       01 P1    USAGE POINTER.
       01 PPSRC USAGE PROGRAM-POINTER.
       01 PP    USAGE PROGRAM-POINTER.
       01 OSRC  USAGE OBJECT REFERENCE PB415CL.
       01 O1    USAGE OBJECT REFERENCE PB415CL.
      *> §13.18.60.3 SR14 lets a USAGE POINTER item stand below level 01 only under a STRONG type declaration.
       01 PB415T IS TYPEDEF STRONG.
           05 TP-P USAGE POINTER.
           05 TP-X PIC X(3).
       01 GV TYPE PB415T.
       PROCEDURE DIVISION.
       MAIN.
           SET SRC TO ADDRESS OF TGT-A
           SET P1 TO NULL
           INITIALIZE P1 REPLACING DATA-POINTER DATA BY SRC
           IF P1 = NULL THEN DISPLAY "1[NULL]" ELSE DISPLAY "1[SET]" END-IF
           IF P1 = SRC THEN DISPLAY "1B[SAME]" ELSE DISPLAY "1B[OTHER]" END-IF

           SET PPSRC TO ENTRY "PB415SUB"
           SET PP TO NULL
           INITIALIZE PP REPLACING PROGRAM-POINTER DATA BY PPSRC
           IF PP = NULL THEN DISPLAY "2[NULL]" ELSE DISPLAY "2[SET]" END-IF

           INVOKE PB415CL "NEW" RETURNING OSRC
           SET O1 TO NULL
           INITIALIZE O1 REPLACING OBJECT-REFERENCE DATA BY OSRC
           IF O1 = NULL THEN DISPLAY "3[NULL]" ELSE DISPLAY "3[SET]" END-IF

           INITIALIZE P1 ALL TO VALUE
           IF P1 = NULL THEN DISPLAY "4[NULL]" ELSE DISPLAY "4[SET]" END-IF

           SET TP-P OF GV TO NULL
           MOVE "QQQ" TO TP-X OF GV
           INITIALIZE GV REPLACING DATA-POINTER DATA BY SRC
           IF TP-P OF GV = NULL
               THEN DISPLAY "5[NULL|" TP-X OF GV "]"
               ELSE DISPLAY "5[SET|" TP-X OF GV "]"
           END-IF

           SET P1 TO NULL
           INITIALIZE P1 REPLACING ALPHANUMERIC DATA BY "ZZZZ"
           IF P1 = NULL THEN DISPLAY "6[NULL]" ELSE DISPLAY "6[SET]" END-IF
           STOP RUN.
       END PROGRAM PB415SETF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB415SUB.
       PROCEDURE DIVISION.
       S-MAIN.
           CONTINUE.
       END PROGRAM PB415SUB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB415CL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       M-MAIN.
           DISPLAY "PINGED".
       END METHOD PING.
       END OBJECT.
       END CLASS PB415CL.

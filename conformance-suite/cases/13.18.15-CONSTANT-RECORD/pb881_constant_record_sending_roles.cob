*>--- conformance-case
*> id: pb881_constant_record_sending_roles
*> rules: [SR-13.18.15.3-2, SR-14.9.22.3-8]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB881 — the POSITIVE half of ISO 13.18.15.3 SR2: the rule bars a CONSTANT RECORD only as a
      *> RECEIVING data item, so every SENDING use stays legal. The fix routes each verb's operand by its ROLE,
      *> and this program pins the roles that must NOT be refused:
      *>   * INSPECT TALLYING alone — 14.9.22.3 SR8: "Identifier-1 is a sending operand." (Format 1)
      *>     "aabaa" holds four "a" (14.9.22.4 GR12 a), and the constant is unchanged.
      *>   * MOVE CORRESPONDING with the constant as identifier-1 (the SENDING group): F1 OF S gets "old".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB881POS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CA CONSTANT RECORD PIC X(5) VALUE "aabaa".
       01 CG CONSTANT RECORD.
          05 F1 PIC X(3) VALUE "old".
       01 S.
          05 F1 PIC X(3) VALUE "new".
       01 CN PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INSPECT CA TALLYING CN FOR ALL "a"
           DISPLAY "CN=" CN " CA=[" CA "]"
           MOVE CORRESPONDING CG TO S
           DISPLAY "F1=[" F1 OF S "] CG=[" F1 OF CG "]"
           STOP RUN.

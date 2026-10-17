      *>--- conformance-case
      *> id: l1c20-capacity-register-perform-varying
      *> rules: [SR-13.18.38.3-32, GR-13.18.38.4-15]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.38.3 SR32 / §13.18.38.4 GR15 — the CAPACITY
      *> register data-name-3 used as a PERFORM VARYING identifier.
      *> "32) Data-name-3 shall not be referenced as a receiving item,
      *>   except as the operand of a variable-table format SET
      *>   statement."
      *>   OK  §13.18.38.3 32)  (Syntax rules)
      *> "15) ... Data-name-3 shall not be referenced as a receiving
      *>   operand."
      *>   OK  §13.18.38.4 15)  (General rules)
      *> PERFORM VARYING T-CAP ... stores into T-CAP (the VARYING
      *> identifier is set and augmented, §14.9.27.4).
      *> The statement is not a SET statement, so the exception does
      *> not apply and the source shall be rejected: COBOLNET1523,
      *> whose meaning is exactly this violation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C20E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 T-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN T-CAP
                 FROM 2 TO 9.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING T-CAP FROM 1 BY 1 UNTIL T-CAP > 3
               CONTINUE
           END-PERFORM.
           STOP RUN.

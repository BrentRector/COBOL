*>--- conformance-case
*> id: ref-mod-zero-length-malformed
*> rules: [FMT-7.3.23.2]
*> source-format: free
*> portable: unverified
*>---
*> VCR 30 (ISO §7.3.23.2): >>REF-MOD-ZERO-LENGTH's operand is ON or OFF (ON is not
*> underlined in the printed format, so a bare directive selects it) — any other operand
*> is COBOLNET1911, the ONE malformed-operand producer §7.3.3 SR6 earns for the whole
*> §7.3 family (kb/Work PB794, which retired this directive's own COBOLNET1576).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RMZLMAL.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(10) VALUE "ABCDEFGHIJ".
       PROCEDURE DIVISION.
       MAIN.
      *> the malformed operand: neither ON nor OFF
       >>REF-MOD-ZERO-LENGTH MAYBE
           DISPLAY W(2:3)
           STOP RUN.

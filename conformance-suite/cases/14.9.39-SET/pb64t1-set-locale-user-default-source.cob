*>--- conformance-case
*> id: pb64t1-set-locale-user-default-source
*> rules: [SR-14.9.39.3-25]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR25: "If USER-DEFAULT is specified as the first operand, identifier-10 or locale-name-1 shall be
      *> specified in the TO phrase" - the user default is set FROM a named or saved locale (14.9.39.4 GR22), never from
      *> SYSTEM-DEFAULT or from itself. COBOLNET1667 (kb/Work PB64 T1; DESIGN-locale-facility 7 rule d).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1SR25.
       PROCEDURE DIVISION.
           SET LOCALE USER-DEFAULT TO SYSTEM-DEFAULT.
           STOP RUN.

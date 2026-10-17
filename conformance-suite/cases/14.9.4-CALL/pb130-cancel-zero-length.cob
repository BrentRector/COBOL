*>--- conformance-case
*> id: pb130-cancel-zero-length
*> rules: [SR-14.9.4.3-2, SR-14.9.5.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.5.3 SR2: the CANCEL literal shall not be zero-length. BindCall had the screen and
      *> BindCancel did not - the empty name was silently swallowed by the GR12 guard at run time
      *> (kb/Work PB130; the reader also fixes the old miscited 14.9.5.2 SR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB130NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN.
           CANCEL ""
           STOP RUN.

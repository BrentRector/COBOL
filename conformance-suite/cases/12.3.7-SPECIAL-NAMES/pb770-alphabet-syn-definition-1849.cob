*>--- conformance-case
*> id: pb770-alphabet-syn-definition-1849
*> rules: [SR-12.3.7.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> The GnuCOBOL testsuite's syn_definition.at:1849 ("ALPHABET definition"), which expects a rejection
      *> naming the duplicate characters x'00', A and B. It is genuinely illegal under ISO 12.3.7.3 SR14 a:
      *> in TESTME, x'00' is specified by `x'00' thru x'05'` and again by `ALSO x'00'`, and x'41'/x'42' are
      *> 'A'/'B', already inside `'A' THROUGH 'Z'`.
      *>
      *> ⛔ This case is here because a REJECTION IS NOT EVIDENCE OF THE RIGHT REJECTION. The differential
      *> baseline read AGREE_REJECT for years while we were rejecting it for the WRONG reason - the missing
      *> optional word IS on the second clause (12.3.7.2 rules neither IS nor FOR; 5.2.3). When d09374e2
      *> correctly restored that optional word the row flipped to WE_ACCEPT_THEY_REJECT and exposed the
      *> under-rejection the coincidental agreement had been hiding. The second ALPHABET clause below keeps
      *> the IS omitted deliberately, so this golden re-pins BOTH facts at once.
     
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB770ALPHABETSYNDEFI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET TESTME IS
                    "A" THROUGH "Z", X"00" THRU X"05";
                    X"41" ALSO X"42", ALSO X"00", X"C1" ALSO X"C2"
           ALPHABET FINE
                    "A" ALSO "B" ALSO "C" ALSO "d" ALSO "e" ALSO "f",
                    "g" ALSO "G", "1" THRU "9", X"00".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER PIC X.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.

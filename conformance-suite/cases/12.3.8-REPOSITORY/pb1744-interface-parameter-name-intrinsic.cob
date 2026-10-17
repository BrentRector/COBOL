*>--- conformance-case
*> id: pb1744-interface-parameter-name-intrinsic
*> rules: [SR-12.3.8.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1744 - ISO 12.3.8.3 SR12 (cite.py OK): "Intrinsic-function-name-1
      *> shall not be specified as a user-defined word within the scope of this
      *> REPOSITORY paragraph." The parameterized INTERFACE's parameter-name MOD is
      *> the intrinsic-function-name its own REPOSITORY identifies.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB1744IPN USING MOD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE MOD
           FUNCTION MOD INTRINSIC.
       PROCEDURE DIVISION.
       END INTERFACE PB1744IPN.

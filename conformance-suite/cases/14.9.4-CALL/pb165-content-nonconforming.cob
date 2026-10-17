*>--- conformance-case
*> id: pb165-content-nonconforming
*> rules: [SR-14.9.4.3-25, GR-14.9.4.4-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB165 - ISO 14.8.2.3.3 rule 2a, the BY CONTENT arm of the Format-2 CALL conformance dispatch,
*> which had no arm at all. 14.9.4.3 SR25 imports "the rules for conformance specified in 14.8.2,
*> Parameters"; 14.8.2.3.3 rule 2a says that for a NUMERIC formal "the conformance rules are the same as
*> for a COMPUTE statement with the argument as the sending operand and the corresponding formal parameter
*> as the receiving operand", and a COMPUTE's sending operand is an arithmetic expression - a category
*> ALPHANUMERIC argument satisfies no such rule. Measured on the pre-fix tree: this program compiled clean
*> and printed LA=0000, a wrong answer with no diagnostic, because CobolArgAdapt's converting view read
*> "ABCD" through the formal's zoned numeric profile. The BY REFERENCE sibling of this dispatch had been
*> checked since PB133; this is the other arm.
 IDENTIFICATION DIVISION.
 PROGRAM-ID. PB165N.
 DATA DIVISION.
 WORKING-STORAGE SECTION.
 01 A PIC X(4) VALUE "ABCD".
 PROCEDURE DIVISION.
 MAIN.
     CALL "PB165NS" AS NESTED USING BY CONTENT A
     STOP RUN.
 IDENTIFICATION DIVISION.
 PROGRAM-ID. PB165NS.
 DATA DIVISION.
 LINKAGE SECTION.
 01 LA PIC 9(4).
 PROCEDURE DIVISION USING LA.
 M1.
     DISPLAY "LA=" LA
     GOBACK.
 END PROGRAM PB165NS.
 END PROGRAM PB165N.

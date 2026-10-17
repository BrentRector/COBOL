*>--- conformance-case
*> id: pb2740-omitted-branch-turn-name-below-2023
*> rules: [SR-7.3.25.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 7.2.1 (cite.py --check 7.2.1 "syntactically correct in the initial source text and library text" -> OK)
*> with 7.3.25.3 SR2 (cite.py --check 7.3.25.3 "Exception-name-1 shall be one of the exception names listed in 14.6.13.1" -> OK):
*> the EC-EXTERNAL family is listed by the 2023 edition's 14.6.13.1 and not by the 2002/2014 editions' (ExceptionCatalog
*> introducedIn 2023, VERSION_CHANGE_REFERENCE rows 40/61), so a TURN naming it is malformed at 2002/2014 inside an omitted
*> >>IF branch as in a compiled one (COBOLNET0878 - the code the compiled twin, ec-external-turn-below-2023, pins).
*> At --std 85 the TURN directive itself is refused first (COBOLNET0900). kb/Work PB2740.
       >>IF 1 = 0
       >>TURN EC-EXTERNAL-FORMAT-CONFLICT CHECKING ON
       >>END-IF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2740N02.
       PROCEDURE DIVISION.
           DISPLAY "A".
           STOP RUN.

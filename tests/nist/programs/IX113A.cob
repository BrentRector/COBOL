000100 IDENTIFICATION DIVISION.                                         IX1134.2
000200 PROGRAM-ID.                                                      IX1134.2
000300     IX113A.                                                      IX1134.2
000400****************************************************************  IX1134.2
000500*                                                              *  IX1134.2
000600*    VALIDATION FOR:-                                          *  IX1134.2
000700*                                                              *  IX1134.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1134.2
000900*                                                              *  IX1134.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1134.2
001100*                                                              *  IX1134.2
001200****************************************************************  IX1134.2
001300*                                                                 IX1134.2
001400*    1.  THE  ROUTINE  CREATES  THE  MASS  STORAGE  FILE  IX-FS3  IX1134.2
001500*        CONTAINING  50  RECORDS. EACH BLOCK CONTAINS 2 RECORDS,  IX1134.2
001600*        EACH  RECORD  CONTAINS  240 CHARACTERS, ORGANIZATION IS  IX1134.2
001700*        INDEXED,  ACCESS  IS SEQUENTIAL.                         IX1134.2
001800*                                                                 IX1134.2
001900*    2. THE ROUTINE READS THE CREATED FILE,VERIFIES IT AND       *IX1134.2
002000*       CHECKS THE FILE STATUS CODES:                             IX1134.2
002100*           00  -  AFTER OPEN OUTPUT                              IX1134.2
002200*           00  -  AFTER WRITE                                    IX1134.2
002300*           00  -  AFTER CLOSE OUTPUT                             IX1134.2
002400*           42  -  AFTER CLOSE OUTPUT                             IX1134.2
002500*                                                                 IX1134.2
002600*    4. X-CARDS USED IN THIS PROGRAM:                             IX1134.2
002700*                                                                 IX1134.2
002800*                 XXXXX024                                        IX1134.2
002900*                 XXXXX055.                                       IX1134.2
003000*         P       XXXXX062.                                       IX1134.2
003100*                 XXXXX082.                                       IX1134.2
003200*                 XXXXX083.                                       IX1134.2
003300*         C       XXXXX084                                        IX1134.2
003400*                                                                 IX1134.2
003500*                                                                 IX1134.2
003600 ENVIRONMENT DIVISION.                                            IX1134.2
003700 CONFIGURATION SECTION.                                           IX1134.2
003800 SOURCE-COMPUTER.                                                 IX1134.2
003900     XXXXX082.                                                    IX1134.2
004000 OBJECT-COMPUTER.                                                 IX1134.2
004100     XXXXX083.                                                    IX1134.2
004200 INPUT-OUTPUT SECTION.                                            IX1134.2
004300 FILE-CONTROL.                                                    IX1134.2
004400P    SELECT RAW-DATA   ASSIGN TO                                  IX1134.2
004500P    XXXXX062                                                     IX1134.2
004600P           ORGANIZATION IS INDEXED                               IX1134.2
004700P           ACCESS MODE IS RANDOM                                 IX1134.2
004800P           RECORD KEY IS RAW-DATA-KEY.                           IX1134.2
004900*                                                                 IX1134.2
005000     SELECT PRINT-FILE ASSIGN TO                                  IX1134.2
005100     XXXXX055.                                                    IX1134.2
005200*                                                                 IX1134.2
005300     SELECT IX-FS3 ASSIGN                                         IX1134.2
005400     XXXXX024                                                     IX1134.2
005500     ORGANIZATION IS INDEXED                                      IX1134.2
005600     ACCESS MODE IS SEQUENTIAL                                    IX1134.2
005700     RECORD KEY IS IX-FS3-KEY                                     IX1134.2
005800     FILE STATUS IS IX-FS3-STATUS.                                IX1134.2
005900                                                                  IX1134.2
006000 DATA DIVISION.                                                   IX1134.2
006100                                                                  IX1134.2
006200 FILE SECTION.                                                    IX1134.2
006300P                                                                 IX1134.2
006400PFD  RAW-DATA.                                                    IX1134.2
006500P                                                                 IX1134.2
006600P01  RAW-DATA-SATZ.                                               IX1134.2
006700P    05  RAW-DATA-KEY        PIC X(6).                            IX1134.2
006800P    05  C-DATE              PIC 9(6).                            IX1134.2
006900P    05  C-TIME              PIC 9(8).                            IX1134.2
007000P    05  C-NO-OF-TESTS       PIC 99.                              IX1134.2
007100P    05  C-OK                PIC 999.                             IX1134.2
007200P    05  C-ALL               PIC 999.                             IX1134.2
007300P    05  C-FAIL              PIC 999.                             IX1134.2
007400P    05  C-DELETED           PIC 999.                             IX1134.2
007500P    05  C-INSPECT           PIC 999.                             IX1134.2
007600P    05  C-NOTE              PIC X(13).                           IX1134.2
007700P    05  C-INDENT            PIC X.                               IX1134.2
007800P    05  C-ABORT             PIC X(8).                            IX1134.2
007900                                                                  IX1134.2
008000 FD  PRINT-FILE.                                                  IX1134.2
008100                                                                  IX1134.2
008200 01  PRINT-REC               PIC X(120).                          IX1134.2
008300                                                                  IX1134.2
008400 01  DUMMY-RECORD            PIC X(120).                          IX1134.2
008500                                                                  IX1134.2
008600 FD  IX-FS3                                                       IX1134.2
008700C       DATA RECORDS IX-FS3R1-F-G-240                             IX1134.2
008800C       LABEL RECORD STANDARD                                     IX1134.2
008900        RECORD 240                                                IX1134.2
009000        BLOCK CONTAINS 2 RECORDS.                                 IX1134.2
009100                                                                  IX1134.2
009200 01  IX-FS3R1-F-G-240.                                            IX1134.2
009300     05  IX-FS3-REC-120      PIC X(120).                          IX1134.2
009400     05  IX-FS3-REC-120-240.                                      IX1134.2
009500         10  FILLER          PIC X(8).                            IX1134.2
009600         10  IX-FS3-KEY      PIC X(29).                           IX1134.2
009700         10  FILLER          PIC X(9).                            IX1134.2
009800         10  IX-FS3-ALTER-KEY  PIC X(29).                         IX1134.2
009900         10  FILLER            PIC X(45).                         IX1134.2
010000                                                                  IX1134.2
010100                                                                  IX1134.2
010200 WORKING-STORAGE SECTION.                                         IX1134.2
010300                                                                  IX1134.2
010400 01  GRP-0101.                                                    IX1134.2
010500     05  FILLER              PIC X(10) VALUE "RECORD-KEY".        IX1134.2
010600     05  GRP-0101-KEY        PIC 9(9)  VALUE ZERO.                IX1134.2
010700     05  FILLER              PIC X(10) VALUE "END-OF-KEY".        IX1134.2
010800                                                                  IX1134.2
010900 01  GRP-0102.                                                    IX1134.2
011000     05  FILLER              PIC X(10) VALUE "ALTERN-KEY".        IX1134.2
011100     05  GRP-0102-KEY        PIC 9(9)  VALUE ZERO.                IX1134.2
011200     05  FILLER              PIC X(10) VALUE "END-AL-KEY".        IX1134.2
011300                                                                  IX1134.2
011400 01  WRK-CS-09V00            PIC S9(9) COMP VALUE ZERO.           IX1134.2
011500                                                                  IX1134.2
011600 01  EOF-FLAG                PIC 9 VALUE ZERO.                    IX1134.2
011700                                                                  IX1134.2
011800 01  RECORDS-IN-ERROR        PIC S9(5) COMP VALUE ZERO.           IX1134.2
011900                                                                  IX1134.2
012000 01  ERROR-FLAG              PIC 9 VALUE ZERO.                    IX1134.2
012100                                                                  IX1134.2
012200 01  PERM-ERRORS             PIC S9(5) COMP VALUE ZERO.           IX1134.2
012300                                                                  IX1134.2
012400 01  STATUS-TEST-00          PIC 9 VALUE ZERO.                    IX1134.2
012500                                                                  IX1134.2
012600 01  STATUS-TEST-10          PIC 9 VALUE ZERO.                    IX1134.2
012700 01  STATUS-TEST-READ        PIC 9 VALUE ZERO.                    IX1134.2
012800                                                                  IX1134.2
012900 01  IX-FS3-STATUS.                                               IX1134.2
013000     05  IX-FS3-STAT1        PIC X.                               IX1134.2
013100     05  IX-FS3-STAT2        PIC X.                               IX1134.2
013200                                                                  IX1134.2
013300 01  COUNT-OF-RECS           PIC 9(5).                            IX1134.2
013400                                                                  IX1134.2
013500 01  COUNT-OF-RECORDS REDEFINES COUNT-OF-RECS  PIC 9(5).          IX1134.2
013600                                                                  IX1134.2
013700 01  FILE-RECORD-INFORMATION-REC.                                 IX1134.2
013800     05  FILE-RECORD-INFO-SKELETON.                               IX1134.2
013900         10  FILLER          PIC X(48) VALUE                      IX1134.2
014000              "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00". IX1134.2
014100         10  FILLER          PIC X(46) VALUE                      IX1134.2
014200                ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000". IX1134.2
014300         10  FILLER          PIC X(26) VALUE                      IX1134.2
014400                                    ",LFIL=000000,ORG=  ,LBLR= ". IX1134.2
014500         10  FILLER          PIC X(37) VALUE                      IX1134.2
014600                         ",RECKEY=                             ". IX1134.2
014700         10  FILLER          PIC X(38) VALUE                      IX1134.2
014800                        ",ALTKEY1=                             ". IX1134.2
014900         10  FILLER          PIC X(38) VALUE                      IX1134.2
015000                        ",ALTKEY2=                             ". IX1134.2
015100         10  FILLER          PIC X(7) VALUE SPACE.                IX1134.2
015200     05  FILE-RECORD-INFO             OCCURS 10.                  IX1134.2
015300         10  FILE-RECORD-INFO-P1-120.                             IX1134.2
015400             15  FILLER      PIC X(5).                            IX1134.2
015500             15  XFILE-NAME  PIC X(6).                            IX1134.2
015600             15  FILLER      PIC X(8).                            IX1134.2
015700             15  XRECORD-NAME  PIC X(6).                          IX1134.2
015800             15  FILLER        PIC X(1).                          IX1134.2
015900             15  REELUNIT-NUMBER  PIC 9(1).                       IX1134.2
016000             15  FILLER           PIC X(7).                       IX1134.2
016100             15  XRECORD-NUMBER   PIC 9(6).                       IX1134.2
016200             15  FILLER           PIC X(6).                       IX1134.2
016300             15  UPDATE-NUMBER    PIC 9(2).                       IX1134.2
016400             15  FILLER           PIC X(5).                       IX1134.2
016500             15  ODO-NUMBER       PIC 9(4).                       IX1134.2
016600             15  FILLER           PIC X(5).                       IX1134.2
016700             15  XPROGRAM-NAME    PIC X(5).                       IX1134.2
016800             15  FILLER           PIC X(7).                       IX1134.2
016900             15  XRECORD-LENGTH   PIC 9(6).                       IX1134.2
017000             15  FILLER           PIC X(7).                       IX1134.2
017100             15  CHARS-OR-RECORDS  PIC X(2).                      IX1134.2
017200             15  FILLER            PIC X(1).                      IX1134.2
017300             15  XBLOCK-SIZE       PIC 9(4).                      IX1134.2
017400             15  FILLER            PIC X(6).                      IX1134.2
017500             15  RECORDS-IN-FILE   PIC 9(6).                      IX1134.2
017600             15  FILLER            PIC X(5).                      IX1134.2
017700             15  XFILE-ORGANIZATION  PIC X(2).                    IX1134.2
017800             15  FILLER              PIC X(6).                    IX1134.2
017900             15  XLABEL-TYPE         PIC X(1).                    IX1134.2
018000         10  FILE-RECORD-INFO-P121-240.                           IX1134.2
018100             15  FILLER              PIC X(8).                    IX1134.2
018200             15  XRECORD-KEY         PIC X(29).                   IX1134.2
018300             15  FILLER              PIC X(9).                    IX1134.2
018400             15  ALTERNATE-KEY1      PIC X(29).                   IX1134.2
018500             15  FILLER              PIC X(9).                    IX1134.2
018600             15  ALTERNATE-KEY2      PIC X(29).                   IX1134.2
018700             15  FILLER              PIC X(7).                    IX1134.2
018800                                                                  IX1134.2
018900 01  TEST-RESULTS.                                                IX1134.2
019000     02 FILLER                   PIC X      VALUE SPACE.          IX1134.2
019100     02 FEATURE                  PIC X(20)  VALUE SPACE.          IX1134.2
019200     02 FILLER                   PIC X      VALUE SPACE.          IX1134.2
019300     02 P-OR-F                   PIC X(5)   VALUE SPACE.          IX1134.2
019400     02 FILLER                   PIC X      VALUE SPACE.          IX1134.2
019500     02  PAR-NAME.                                                IX1134.2
019600       03 FILLER                 PIC X(19)  VALUE SPACE.          IX1134.2
019700       03  PARDOT-X              PIC X      VALUE SPACE.          IX1134.2
019800       03 DOTVALUE               PIC 99     VALUE ZERO.           IX1134.2
019900     02 FILLER                   PIC X(8)   VALUE SPACE.          IX1134.2
020000     02 RE-MARK                  PIC X(61).                       IX1134.2
020100 01  TEST-COMPUTED.                                               IX1134.2
020200     02 FILLER                   PIC X(30)  VALUE SPACE.          IX1134.2
020300     02 FILLER                   PIC X(17)  VALUE                 IX1134.2
020400            "       COMPUTED=".                                   IX1134.2
020500     02 COMPUTED-X.                                               IX1134.2
020600     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          IX1134.2
020700     03 COMPUTED-N               REDEFINES COMPUTED-A             IX1134.2
020800                                 PIC -9(9).9(9).                  IX1134.2
020900     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         IX1134.2
021000     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     IX1134.2
021100     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     IX1134.2
021200     03       CM-18V0 REDEFINES COMPUTED-A.                       IX1134.2
021300         04 COMPUTED-18V0                    PIC -9(18).          IX1134.2
021400         04 FILLER                           PIC X.               IX1134.2
021500     03 FILLER PIC X(50) VALUE SPACE.                             IX1134.2
021600 01  TEST-CORRECT.                                                IX1134.2
021700     02 FILLER PIC X(30) VALUE SPACE.                             IX1134.2
021800     02 FILLER PIC X(17) VALUE "       CORRECT =".                IX1134.2
021900     02 CORRECT-X.                                                IX1134.2
022000     03 CORRECT-A                  PIC X(20) VALUE SPACE.         IX1134.2
022100     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      IX1134.2
022200     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         IX1134.2
022300     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     IX1134.2
022400     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     IX1134.2
022500     03      CR-18V0 REDEFINES CORRECT-A.                         IX1134.2
022600         04 CORRECT-18V0                     PIC -9(18).          IX1134.2
022700         04 FILLER                           PIC X.               IX1134.2
022800     03 FILLER PIC X(2) VALUE SPACE.                              IX1134.2
022900     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     IX1134.2
023000 01  CCVS-C-1.                                                    IX1134.2
023100     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PAIX1134.2
023200-    "SS  PARAGRAPH-NAME                                          IX1134.2
023300-    "       REMARKS".                                            IX1134.2
023400     02 FILLER                     PIC X(20)    VALUE SPACE.      IX1134.2
023500 01  CCVS-C-2.                                                    IX1134.2
023600     02 FILLER                     PIC X        VALUE SPACE.      IX1134.2
023700     02 FILLER                     PIC X(6)     VALUE "TESTED".   IX1134.2
023800     02 FILLER                     PIC X(15)    VALUE SPACE.      IX1134.2
023900     02 FILLER                     PIC X(4)     VALUE "FAIL".     IX1134.2
024000     02 FILLER                     PIC X(94)    VALUE SPACE.      IX1134.2
024100 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       IX1134.2
024200 01  REC-CT                        PIC 99       VALUE ZERO.       IX1134.2
024300 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       IX1134.2
024400 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       IX1134.2
024500 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       IX1134.2
024600 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       IX1134.2
024700 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       IX1134.2
024800 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       IX1134.2
024900 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      IX1134.2
025000 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       IX1134.2
025100 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     IX1134.2
025200 01  CCVS-H-1.                                                    IX1134.2
025300     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1134.2
025400     02  FILLER                    PIC X(42)    VALUE             IX1134.2
025500     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 IX1134.2
025600     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1134.2
025700 01  CCVS-H-2A.                                                   IX1134.2
025800   02  FILLER                        PIC X(40)  VALUE SPACE.      IX1134.2
025900   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  IX1134.2
026000   02  FILLER                        PIC XXXX   VALUE             IX1134.2
026100     "4.2 ".                                                      IX1134.2
026200   02  FILLER                        PIC X(28)  VALUE             IX1134.2
026300            " COPY - NOT FOR DISTRIBUTION".                       IX1134.2
026400   02  FILLER                        PIC X(41)  VALUE SPACE.      IX1134.2
026500                                                                  IX1134.2
026600 01  CCVS-H-2B.                                                   IX1134.2
026700   02  FILLER                        PIC X(15)  VALUE             IX1134.2
026800            "TEST RESULT OF ".                                    IX1134.2
026900   02  TEST-ID                       PIC X(9).                    IX1134.2
027000   02  FILLER                        PIC X(4)   VALUE             IX1134.2
027100            " IN ".                                               IX1134.2
027200   02  FILLER                        PIC X(12)  VALUE             IX1134.2
027300     " HIGH       ".                                              IX1134.2
027400   02  FILLER                        PIC X(22)  VALUE             IX1134.2
027500            " LEVEL VALIDATION FOR ".                             IX1134.2
027600   02  FILLER                        PIC X(58)  VALUE             IX1134.2
027700     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1134.2
027800 01  CCVS-H-3.                                                    IX1134.2
027900     02  FILLER                      PIC X(34)  VALUE             IX1134.2
028000            " FOR OFFICIAL USE ONLY    ".                         IX1134.2
028100     02  FILLER                      PIC X(58)  VALUE             IX1134.2
028200     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1134.2
028300     02  FILLER                      PIC X(28)  VALUE             IX1134.2
028400            "  COPYRIGHT   1985 ".                                IX1134.2
028500 01  CCVS-E-1.                                                    IX1134.2
028600     02 FILLER                       PIC X(52)  VALUE SPACE.      IX1134.2
028700     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              IX1134.2
028800     02 ID-AGAIN                     PIC X(9).                    IX1134.2
028900     02 FILLER                       PIC X(45)  VALUE SPACES.     IX1134.2
029000 01  CCVS-E-2.                                                    IX1134.2
029100     02  FILLER                      PIC X(31)  VALUE SPACE.      IX1134.2
029200     02  FILLER                      PIC X(21)  VALUE SPACE.      IX1134.2
029300     02 CCVS-E-2-2.                                               IX1134.2
029400         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      IX1134.2
029500         03 FILLER                   PIC X      VALUE SPACE.      IX1134.2
029600         03 ENDER-DESC               PIC X(44)  VALUE             IX1134.2
029700            "ERRORS ENCOUNTERED".                                 IX1134.2
029800 01  CCVS-E-3.                                                    IX1134.2
029900     02  FILLER                      PIC X(22)  VALUE             IX1134.2
030000            " FOR OFFICIAL USE ONLY".                             IX1134.2
030100     02  FILLER                      PIC X(12)  VALUE SPACE.      IX1134.2
030200     02  FILLER                      PIC X(58)  VALUE             IX1134.2
030300     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1134.2
030400     02  FILLER                      PIC X(13)  VALUE SPACE.      IX1134.2
030500     02 FILLER                       PIC X(15)  VALUE             IX1134.2
030600             " COPYRIGHT 1985".                                   IX1134.2
030700 01  CCVS-E-4.                                                    IX1134.2
030800     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      IX1134.2
030900     02 FILLER                       PIC X(4)   VALUE " OF ".     IX1134.2
031000     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      IX1134.2
031100     02 FILLER                       PIC X(40)  VALUE             IX1134.2
031200      "  TESTS WERE EXECUTED SUCCESSFULLY".                       IX1134.2
031300 01  XXINFO.                                                      IX1134.2
031400     02 FILLER                       PIC X(19)  VALUE             IX1134.2
031500            "*** INFORMATION ***".                                IX1134.2
031600     02 INFO-TEXT.                                                IX1134.2
031700       04 FILLER                     PIC X(8)   VALUE SPACE.      IX1134.2
031800       04 XXCOMPUTED                 PIC X(20).                   IX1134.2
031900       04 FILLER                     PIC X(5)   VALUE SPACE.      IX1134.2
032000       04 XXCORRECT                  PIC X(20).                   IX1134.2
032100     02 INF-ANSI-REFERENCE           PIC X(48).                   IX1134.2
032200 01  HYPHEN-LINE.                                                 IX1134.2
032300     02 FILLER  PIC IS X VALUE IS SPACE.                          IX1134.2
032400     02 FILLER  PIC IS X(65)    VALUE IS "************************IX1134.2
032500-    "*****************************************".                 IX1134.2
032600     02 FILLER  PIC IS X(54)    VALUE IS "************************IX1134.2
032700-    "******************************".                            IX1134.2
032800 01  TEST-NO                         PIC 99.                      IX1134.2
032900 01  CCVS-PGM-ID                     PIC X(9)   VALUE             IX1134.2
033000     "IX113A".                                                    IX1134.2
033100 PROCEDURE DIVISION.                                              IX1134.2
033200 DECLARATIVES.                                                    IX1134.2
033300                                                                  IX1134.2
033400 SECT-IX105-0002 SECTION.                                         IX1134.2
033500     USE AFTER EXCEPTION PROCEDURE ON IX-FS3.                     IX1134.2
033600 INPUT-PROCESS.                                                   IX1134.2
033700     IF TEST-NO = 5                                               IX1134.2
033800        GO TO D-C-TEST-GF-02-1.                                   IX1134.2
033900     IF STATUS-TEST-10 EQUAL TO 1                                 IX1134.2
034000        IF  IX-FS3-STAT1 EQUAL TO "1"                             IX1134.2
034100            MOVE 1 TO EOF-FLAG                                    IX1134.2
034200        ELSE                                                      IX1134.2
034300           IF  IX-FS3-STAT1 GREATER THAN "1"                      IX1134.2
034400           MOVE 1 TO PERM-ERRORS.                                 IX1134.2
034500     GO TO DECL-EXIT.                                             IX1134.2
034600 D-C-TEST-GF-02-1.                                                IX1134.2
034700     IF IX-FS3-STATUS EQUAL TO "42"                               IX1134.2
034800         GO TO D-C-PASS-GF-02-0.                                  IX1134.2
034900 D-C-FAIL-GF-02-0.                                                IX1134.2
035000     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1134.2
035100     MOVE "42" TO CORRECT-X.                                      IX1134.2
035200     MOVE "IX-5, 1.3.4, (5) B" TO RE-MARK.                        IX1134.2
035300     PERFORM D-FAIL.                                              IX1134.2
035400     GO TO D-C-WRITE-GF-02-0.                                     IX1134.2
035500 D-C-PASS-GF-02-0.                                                IX1134.2
035600     PERFORM D-PASS.                                              IX1134.2
035700 D-C-WRITE-GF-02-0.                                               IX1134.2
035800     PERFORM D-PRINT-DETAIL.                                      IX1134.2
035900 D-CLOSE-FILES.                                                   IX1134.2
036000P    OPEN I-O RAW-DATA.                                           IX1134.2
036100P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1134.2
036200P    READ RAW-DATA INVALID KEY GO TO D-END-E-2.                   IX1134.2
036300P    MOVE "OK.     " TO C-ABORT.                                  IX1134.2
036400P    MOVE PASS-COUNTER TO C-OK.                                   IX1134.2
036500P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1134.2
036600P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1134.2
036700P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1134.2
036800P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1134.2
036900P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO D-END-E-2.           IX1134.2
037000PD-END-E-2.                                                       IX1134.2
037100P    CLOSE RAW-DATA.                                              IX1134.2
037200     PERFORM D-END-ROUTINE THRU D-END-ROUTINE-13.                 IX1134.2
037300     CLOSE PRINT-FILE.                                            IX1134.2
037400 D-TERMINATE-CCVS.                                                IX1134.2
037500S    EXIT PROGRAM.                                                IX1134.2
037600SD-TERMINATE-CALL.                                                IX1134.2
037700     STOP     RUN.                                                IX1134.2
037800 D-PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.         IX1134.2
037900 D-FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.        IX1134.2
038000 D-PRINT-DETAIL.                                                  IX1134.2
038100     IF   REC-CT NOT EQUAL TO ZERO                                IX1134.2
038200          MOVE "." TO PARDOT-X                                    IX1134.2
038300          MOVE REC-CT TO DOTVALUE.                                IX1134.2
038400     MOVE TEST-RESULTS TO PRINT-REC.                              IX1134.2
038500     PERFORM D-WRITE-LINE.                                        IX1134.2
038600     IF   P-OR-F EQUAL TO "FAIL*"                                 IX1134.2
038700          PERFORM D-WRITE-LINE                                    IX1134.2
038800          PERFORM D-FAIL-ROUTINE THRU D-FAIL-ROUTINE-EX           IX1134.2
038900     ELSE                                                         IX1134.2
039000          PERFORM D-BAIL-OUT THRU D-BAIL-OUT-EX.                  IX1134.2
039100     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1134.2
039200     MOVE SPACE TO CORRECT-X.                                     IX1134.2
039300     IF   REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.           IX1134.2
039400     MOVE SPACE TO RE-MARK.                                       IX1134.2
039500 D-END-ROUTINE.                                                   IX1134.2
039600     MOVE HYPHEN-LINE TO DUMMY-RECORD.                            IX1134.2
039700     PERFORM D-WRITE-LINE 5 TIMES.                                IX1134.2
039800 D-END-RTN-EXIT.                                                  IX1134.2
039900     MOVE CCVS-E-1 TO DUMMY-RECORD.                               IX1134.2
040000     PERFORM D-WRITE-LINE 2 TIMES.                                IX1134.2
040100 D-END-ROUTINE-1.                                                 IX1134.2
040200     ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO       IX1134.2
040300     ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.                IX1134.2
040400     ADD PASS-COUNTER TO ERROR-HOLD.                              IX1134.2
040500     MOVE PASS-COUNTER TO CCVS-E-4-1.                             IX1134.2
040600     MOVE ERROR-HOLD TO CCVS-E-4-2.                               IX1134.2
040700     MOVE CCVS-E-4 TO CCVS-E-2-2.                                 IX1134.2
040800     MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM D-WRITE-LINE.          IX1134.2
040900  D-END-ROUTINE-12.                                               IX1134.2
041000     MOVE "TEST(S) FAILED" TO ENDER-DESC.                         IX1134.2
041100     IF  ERROR-COUNTER IS EQUAL TO ZERO                           IX1134.2
041200         MOVE "NO " TO ERROR-TOTAL                                IX1134.2
041300     ELSE                                                         IX1134.2
041400         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1134.2
041500     MOVE    CCVS-E-2 TO DUMMY-RECORD.                            IX1134.2
041600     PERFORM D-WRITE-LINE.                                        IX1134.2
041700 D-END-ROUTINE-13.                                                IX1134.2
041800     IF  DELETE-COUNTER IS EQUAL TO ZERO                          IX1134.2
041900         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1134.2
042000         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1134.2
042100     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1134.2
042200     MOVE CCVS-E-2 TO DUMMY-RECORD.                               IX1134.2
042300     PERFORM D-WRITE-LINE.                                        IX1134.2
042400     IF   INSPECT-COUNTER EQUAL TO ZERO                           IX1134.2
042500          MOVE "NO " TO ERROR-TOTAL                               IX1134.2
042600     ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                    IX1134.2
042700     MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.             IX1134.2
042800     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1134.2
042900     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1134.2
043000 D-WRITE-LINE.                                                    IX1134.2
043100     ADD 1 TO RECORD-COUNT.                                       IX1134.2
043200Y    IF RECORD-COUNT GREATER 42                                   IX1134.2
043300Y       MOVE DUMMY-RECORD TO DUMMY-HOLD                           IX1134.2
043400Y       MOVE SPACE TO DUMMY-RECORD                                IX1134.2
043500Y       WRITE DUMMY-RECORD AFTER ADVANCING PAGE                   IX1134.2
043600Y       MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1134.2
043700Y       MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1134.2
043800Y       MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1134.2
043900Y       MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1134.2
044000Y       MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1134.2
044100Y       MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1134.2
044200Y       MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM D-WRT-LN         IX1134.2
044300Y       MOVE DUMMY-HOLD TO DUMMY-RECORD                           IX1134.2
044400Y       MOVE ZERO TO RECORD-COUNT.                                IX1134.2
044500     PERFORM D-WRT-LN.                                            IX1134.2
044600 D-WRT-LN.                                                        IX1134.2
044700     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1134.2
044800     MOVE SPACE TO DUMMY-RECORD.                                  IX1134.2
044900 D-FAIL-ROUTINE.                                                  IX1134.2
045000     IF   COMPUTED-X NOT EQUAL TO SPACE                           IX1134.2
045100          GO TO D-FAIL-ROUTINE-WRITE.                             IX1134.2
045200     IF   CORRECT-X NOT EQUAL TO SPACE GO TO D-FAIL-ROUTINE-WRITE.IX1134.2
045300     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1134.2
045400     MOVE "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.    IX1134.2
045500     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1134.2
045600     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1134.2
045700     GO TO D-FAIL-ROUTINE-EX.                                     IX1134.2
045800 D-FAIL-ROUTINE-WRITE.                                            IX1134.2
045900     MOVE TEST-COMPUTED TO PRINT-REC PERFORM D-WRITE-LINE         IX1134.2
046000     MOVE ANSI-REFERENCE TO COR-ANSI-REFERENCE.                   IX1134.2
046100     MOVE TEST-CORRECT TO PRINT-REC PERFORM D-WRITE-LINE 2 TIMES. IX1134.2
046200     MOVE SPACES TO COR-ANSI-REFERENCE.                           IX1134.2
046300 D-FAIL-ROUTINE-EX. EXIT.                                         IX1134.2
046400 D-BAIL-OUT.                                                      IX1134.2
046500     IF  COMPUTED-A NOT EQUAL TO SPACE GO TO D-BAIL-OUT-WRITE.    IX1134.2
046600     IF  CORRECT-A EQUAL TO SPACE GO TO D-BAIL-OUT-EX.            IX1134.2
046700 D-BAIL-OUT-WRITE.                                                IX1134.2
046800     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1134.2
046900     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1134.2
047000     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1134.2
047100     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1134.2
047200 D-BAIL-OUT-EX. EXIT.                                             IX1134.2
047300 DECL-EXIT.  EXIT.                                                IX1134.2
047400 END DECLARATIVES.                                                IX1134.2
047500                                                                  IX1134.2
047600                                                                  IX1134.2
047700 CCVS1 SECTION.                                                   IX1134.2
047800 OPEN-FILES.                                                      IX1134.2
047900P    OPEN I-O RAW-DATA.                                           IX1134.2
048000P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1134.2
048100P    READ RAW-DATA INVALID KEY GO TO END-E-1.                     IX1134.2
048200P    MOVE "ABORTED " TO C-ABORT.                                  IX1134.2
048300P    ADD 1 TO C-NO-OF-TESTS.                                      IX1134.2
048400P    ACCEPT C-DATE  FROM DATE.                                    IX1134.2
048500P    ACCEPT C-TIME  FROM TIME.                                    IX1134.2
048600P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-1.             IX1134.2
048700PEND-E-1.                                                         IX1134.2
048800P    CLOSE RAW-DATA.                                              IX1134.2
048900     OPEN    OUTPUT PRINT-FILE.                                   IX1134.2
049000     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  IX1134.2
049100     MOVE    SPACE TO TEST-RESULTS.                               IX1134.2
049200     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              IX1134.2
049300     MOVE    ZERO TO REC-SKL-SUB.                                 IX1134.2
049400     PERFORM CCVS-INIT-FILE 9 TIMES.                              IX1134.2
049500 CCVS-INIT-FILE.                                                  IX1134.2
049600     ADD     1 TO REC-SKL-SUB.                                    IX1134.2
049700     MOVE    FILE-RECORD-INFO-SKELETON                            IX1134.2
049800          TO FILE-RECORD-INFO (REC-SKL-SUB).                      IX1134.2
049900 CCVS-INIT-EXIT.                                                  IX1134.2
050000     GO TO CCVS1-EXIT.                                            IX1134.2
050100 CLOSE-FILES.                                                     IX1134.2
050200P    OPEN I-O RAW-DATA.                                           IX1134.2
050300P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1134.2
050400P    READ RAW-DATA INVALID KEY GO TO END-E-2.                     IX1134.2
050500P    MOVE "OK.     " TO C-ABORT.                                  IX1134.2
050600P    MOVE PASS-COUNTER TO C-OK.                                   IX1134.2
050700P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1134.2
050800P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1134.2
050900P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1134.2
051000P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1134.2
051100P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-2.             IX1134.2
051200PEND-E-2.                                                         IX1134.2
051300P    CLOSE RAW-DATA.                                              IX1134.2
051400     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   IX1134.2
051500 TERMINATE-CCVS.                                                  IX1134.2
051600S    EXIT PROGRAM.                                                IX1134.2
051700STERMINATE-CALL.                                                  IX1134.2
051800     STOP     RUN.                                                IX1134.2
051900 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         IX1134.2
052000 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           IX1134.2
052100 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          IX1134.2
052200 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      IX1134.2
052300     MOVE "****TEST DELETED****" TO RE-MARK.                      IX1134.2
052400 PRINT-DETAIL.                                                    IX1134.2
052500     IF REC-CT NOT EQUAL TO ZERO                                  IX1134.2
052600             MOVE "." TO PARDOT-X                                 IX1134.2
052700             MOVE REC-CT TO DOTVALUE.                             IX1134.2
052800     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      IX1134.2
052900     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               IX1134.2
053000        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 IX1134.2
053100          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 IX1134.2
053200     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1134.2
053300     MOVE SPACE TO CORRECT-X.                                     IX1134.2
053400     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         IX1134.2
053500     MOVE     SPACE TO RE-MARK.                                   IX1134.2
053600 HEAD-ROUTINE.                                                    IX1134.2
053700     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1134.2
053800     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1134.2
053900     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1134.2
054000     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1134.2
054100 COLUMN-NAMES-ROUTINE.                                            IX1134.2
054200     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1134.2
054300     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1134.2
054400     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        IX1134.2
054500 END-ROUTINE.                                                     IX1134.2
054600     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.IX1134.2
054700 END-RTN-EXIT.                                                    IX1134.2
054800     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1134.2
054900 END-ROUTINE-1.                                                   IX1134.2
055000      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      IX1134.2
055100      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               IX1134.2
055200      ADD PASS-COUNTER TO ERROR-HOLD.                             IX1134.2
055300*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   IX1134.2
055400      MOVE PASS-COUNTER TO CCVS-E-4-1.                            IX1134.2
055500      MOVE ERROR-HOLD TO CCVS-E-4-2.                              IX1134.2
055600      MOVE CCVS-E-4 TO CCVS-E-2-2.                                IX1134.2
055700      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           IX1134.2
055800  END-ROUTINE-12.                                                 IX1134.2
055900      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        IX1134.2
056000     IF       ERROR-COUNTER IS EQUAL TO ZERO                      IX1134.2
056100         MOVE "NO " TO ERROR-TOTAL                                IX1134.2
056200         ELSE                                                     IX1134.2
056300         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1134.2
056400     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           IX1134.2
056500     PERFORM WRITE-LINE.                                          IX1134.2
056600 END-ROUTINE-13.                                                  IX1134.2
056700     IF DELETE-COUNTER IS EQUAL TO ZERO                           IX1134.2
056800         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1134.2
056900         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1134.2
057000     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1134.2
057100     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1134.2
057200      IF   INSPECT-COUNTER EQUAL TO ZERO                          IX1134.2
057300          MOVE "NO " TO ERROR-TOTAL                               IX1134.2
057400      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   IX1134.2
057500      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            IX1134.2
057600      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          IX1134.2
057700     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1134.2
057800 WRITE-LINE.                                                      IX1134.2
057900     ADD 1 TO RECORD-COUNT.                                       IX1134.2
058000Y    IF RECORD-COUNT GREATER 42                                   IX1134.2
058100Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          IX1134.2
058200Y        MOVE SPACE TO DUMMY-RECORD                               IX1134.2
058300Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  IX1134.2
058400Y        MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1134.2
058500Y        MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1134.2
058600Y        MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1134.2
058700Y        MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1134.2
058800Y        MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM WRT-LN            IX1134.2
058900Y        MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM WRT-LN            IX1134.2
059000Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          IX1134.2
059100Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          IX1134.2
059200Y        MOVE ZERO TO RECORD-COUNT.                               IX1134.2
059300     PERFORM WRT-LN.                                              IX1134.2
059400 WRT-LN.                                                          IX1134.2
059500     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1134.2
059600     MOVE SPACE TO DUMMY-RECORD.                                  IX1134.2
059700 BLANK-LINE-PRINT.                                                IX1134.2
059800     PERFORM WRT-LN.                                              IX1134.2
059900 FAIL-ROUTINE.                                                    IX1134.2
060000     IF     COMPUTED-X NOT EQUAL TO SPACE                         IX1134.2
060100            GO TO   FAIL-ROUTINE-WRITE.                           IX1134.2
060200     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.IX1134.2
060300     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1134.2
060400     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   IX1134.2
060500     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1134.2
060600     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1134.2
060700     GO TO  FAIL-ROUTINE-EX.                                      IX1134.2
060800 FAIL-ROUTINE-WRITE.                                              IX1134.2
060900     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         IX1134.2
061000     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 IX1134.2
061100     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. IX1134.2
061200     MOVE   SPACES TO COR-ANSI-REFERENCE.                         IX1134.2
061300 FAIL-ROUTINE-EX. EXIT.                                           IX1134.2
061400 BAIL-OUT.                                                        IX1134.2
061500     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   IX1134.2
061600     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           IX1134.2
061700 BAIL-OUT-WRITE.                                                  IX1134.2
061800     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1134.2
061900     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1134.2
062000     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1134.2
062100     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1134.2
062200 BAIL-OUT-EX. EXIT.                                               IX1134.2
062300 CCVS1-EXIT.                                                      IX1134.2
062400     EXIT.                                                        IX1134.2
062500                                                                  IX1134.2
062600 SECT-IX113A-0003 SECTION.                                        IX1134.2
062700 SEQ-INIT-010.                                                    IX1134.2
062800     MOVE ZERO TO TEST-NO.                                        IX1134.2
062900     MOVE "IX-FS3" TO XFILE-NAME (1).                             IX1134.2
063000     MOVE "R1-F-G" TO XRECORD-NAME (1).                           IX1134.2
063100     MOVE CCVS-PGM-ID TO XPROGRAM-NAME (1).                       IX1134.2
063200     MOVE 000240 TO XRECORD-LENGTH (1).                           IX1134.2
063300     MOVE "RC" TO CHARS-OR-RECORDS (1).                           IX1134.2
063400     MOVE 0002 TO XBLOCK-SIZE (1).                                IX1134.2
063500     MOVE 000050 TO RECORDS-IN-FILE (1).                          IX1134.2
063600     MOVE "IX" TO XFILE-ORGANIZATION (1).                         IX1134.2
063700     MOVE "S" TO XLABEL-TYPE (1).                                 IX1134.2
063800     MOVE 000001 TO XRECORD-NUMBER (1).                           IX1134.2
063900     MOVE 0 TO COUNT-OF-RECS.                                     IX1134.2
064000                                                                  IX1134.2
064100******************************************************************IX1134.2
064200*   TEST  1                                                      *IX1134.2
064300*         OPEN OUTPUT ...                 00 EXPECTED            *IX1134.2
064400*         IX-3, 1.3.4 (1) A                                      *IX1134.2
064500*    STATUS 00 CHECK ON OUTPUT FILE IX-FS3                       *IX1134.2
064600*    THE OUTPUT STATEMENT IS SUCCESSFULLY EXECUTED               *IX1134.2
064700******************************************************************IX1134.2
064800 OPN-INIT-GF-01-0.                                                IX1134.2
064900     MOVE 1 TO STATUS-TEST-00.                                    IX1134.2
065000     MOVE SPACES TO IX-FS3-STATUS.                                IX1134.2
065100     MOVE "OPEN OUTPUT: 00 EXP." TO FEATURE.                      IX1134.2
065200     MOVE "OPN-TEST-GF-01-0" TO PAR-NAME.                         IX1134.2
065300     OPEN                                                         IX1134.2
065400        OUTPUT IX-FS3.                                            IX1134.2
065500     IF IX-FS3-STATUS EQUAL TO "00"                               IX1134.2
065600         GO TO OPN-PASS-GF-01-0.                                  IX1134.2
065700 OPN-FAIL-GF-01-0.                                                IX1134.2
065800     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1134.2
065900     PERFORM FAIL.                                                IX1134.2
066000     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1134.2
066100     MOVE "00" TO CORRECT-X.                                      IX1134.2
066200     GO TO OPN-WRITE-GF-01-0.                                     IX1134.2
066300 OPN-PASS-GF-01-0.                                                IX1134.2
066400     PERFORM PASS.                                                IX1134.2
066500 OPN-WRITE-GF-01-0.                                               IX1134.2
066600     PERFORM PRINT-DETAIL.                                        IX1134.2
066700******************************************************************IX1134.2
066800*   TEST  2                                                      *IX1134.2
066900*         WRITE                           00 EXPECTED            *IX1134.2
067000*         IX-3, 1.3.4 (1) A                                      *IX1134.2
067100*    CREATING A INDEXED FILE WITH 50 RECORDS                     *IX1134.2
067200*    KEY:  FROM  000000001 TO 000000050                          *IX1134.2
067300******************************************************************IX1134.2
067400 WRI-INIT-GF-01-0.                                                IX1134.2
067500     MOVE SPACES TO IX-FS3-STATUS.                                IX1134.2
067600     MOVE 0 TO STATUS-TEST-00.                                    IX1134.2
067700     MOVE "WRITE: 00 EXPECTED" TO FEATURE.                        IX1134.2
067800     MOVE "WRI-TEST-GF-01-0" TO PAR-NAME.                         IX1134.2
067900 WRI-TEST-GF-01-0.                                                IX1134.2
068000     MOVE XRECORD-NUMBER (1) TO GRP-0101-KEY, COUNT-OF-RECS.      IX1134.2
068100     MOVE GRP-0101 TO XRECORD-KEY (1).                            IX1134.2
068200     MOVE GRP-0102 TO ALTERNATE-KEY1 (1).                         IX1134.2
068300*    THE VALUE OF THE ALTERNATE KEY IS 50 TIMES UNCHANGED        *IX1134.2
068400     MOVE FILE-RECORD-INFO (1) TO IX-FS3R1-F-G-240.               IX1134.2
068500     WRITE IX-FS3R1-F-G-240.                                      IX1134.2
068600     IF IX-FS3-STATUS  NOT = "00"                                 IX1134.2
068700         MOVE 1 TO STATUS-TEST-00                                 IX1134.2
068800         GO TO WRI-FAIL-GF-01-0.                                  IX1134.2
068900     IF XRECORD-NUMBER (1) EQUAL TO 50                            IX1134.2
069000         GO TO WRI-TEST-GF-01-1.                                  IX1134.2
069100     ADD 1 TO XRECORD-NUMBER (1).                                 IX1134.2
069200     GO TO WRI-TEST-GF-01-0.                                      IX1134.2
069300 WRI-TEST-GF-01-1.                                                IX1134.2
069400     IF RECORDS-IN-ERROR EQUAL TO ZERO                            IX1134.2
069500         GO TO WRI-PASS-GF-01-0.                                  IX1134.2
069600     MOVE "ERROR IN CREATING FILE" TO RE-MARK.                    IX1134.2
069700 WRI-FAIL-GF-01-0.                                                IX1134.2
069800     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1134.2
069900     PERFORM FAIL.                                                IX1134.2
070000     MOVE "RECORDS WRITTEN =" TO COMPUTED-A.                      IX1134.2
070100     GO TO WRI-WRITE-GF-01-0.                                     IX1134.2
070200 WRI-PASS-GF-01-0.                                                IX1134.2
070300     PERFORM PASS.                                                IX1134.2
070400 WRI-WRITE-GF-01-0.                                               IX1134.2
070500     PERFORM PRINT-DETAIL.                                        IX1134.2
070600     MOVE "FILE CREATED, RECS =" TO COMPUTED-A.                   IX1134.2
070700     MOVE "CREATE FILE IX-FS3" TO FEATURE.                        IX1134.2
070800     MOVE "WRI-TEST-GF-01-1" TO PAR-NAME.                         IX1134.2
070900     MOVE COUNT-OF-RECS TO CORRECT-18V0.                          IX1134.2
071000     PERFORM PRINT-DETAIL.                                        IX1134.2
071100                                                                  IX1134.2
071200******************************************************************IX1134.2
071300*   TEST  4                                                      *IX1134.2
071400*         CLOSE OUTPUT                    00 EXPECTED            *IX1134.2
071500*         IX-3, 1.3.4 (1) A                                      *IX1134.2
071600******************************************************************IX1134.2
071700 CLO-INIT-GF-01-0.                                                IX1134.2
071800     MOVE SPACES TO IX-FS3-STATUS.                                IX1134.2
071900     MOVE "CLOSE OUTPUT:00 EXP." TO FEATURE.                      IX1134.2
072000     MOVE "CLO-TEST-GF-01-0" TO PAR-NAME.                         IX1134.2
072100 CLO-TEST-GF-01-0.                                                IX1134.2
072200     CLOSE IX-FS3.                                                IX1134.2
072300     IF IX-FS3-STATUS = "00"                                      IX1134.2
072400         GO TO CLO-PASS-GF-01-0.                                  IX1134.2
072500 CLO-FAIL-GF-01-0.                                                IX1134.2
072600     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1134.2
072700     PERFORM FAIL.                                                IX1134.2
072800     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1134.2
072900     MOVE "00" TO CORRECT-X.                                      IX1134.2
073000     GO TO CLO-WRITE-GF-01-0.                                     IX1134.2
073100 CLO-PASS-GF-01-0.                                                IX1134.2
073200     PERFORM PASS.                                                IX1134.2
073300 CLO-WRITE-GF-01-0.                                               IX1134.2
073400     PERFORM PRINT-DETAIL.                                        IX1134.2
073500                                                                  IX1134.2
073600******************************************************************IX1134.2
073700*    A INDEXED FILE WITH 50 RECORDS HAS BEEN CREATED.            *IX1134.2
073800******************************************************************IX1134.2
073900                                                                  IX1134.2
074000******************************************************************IX1134.2
074100*   TEST  5                                                      *IX1134.2
074200*         CLOSE FOR A FILE NOT IN THE OPEN MODE                  *IX1134.2
074300*         FILE STATUS 42 EXPECTED IX-5, 1.3.4 (5) B              *IX1134.2
074400******************************************************************IX1134.2
074500 CLO-TEST-GF-02-0.                                                IX1134.2
074600     MOVE  5 TO TEST-NO.                                          IX1134.2
074700     MOVE SPACES TO IX-FS3-STATUS.                                IX1134.2
074800     MOVE "CLOSE-INPUT: 42 EXP." TO FEATURE                       IX1134.2
074900     MOVE "CLO-TEST-GF-02-0" TO PAR-NAME.                         IX1134.2
075000     CLOSE IX-FS3.                                                IX1134.2
075100 CLO-TEST-GF-02-1.                                                IX1134.2
075200     IF IX-FS3-STATUS EQUAL TO "42"                               IX1134.2
075300        MOVE "SHOULD HAVE EXECUTED DECLARATIVES IX-3,1.3.4(4)"    IX1134.2
075400          TO RE-MARK                                              IX1134.2
075500        GO TO CLO-WRITE-GF-02-0.                                  IX1134.2
075600 CLO-FAIL-GF-02-0.                                                IX1134.2
075700     MOVE "IX-5, 1.3.4, (5) B" TO RE-MARK.                        IX1134.2
075800 CLO-WRITE-GF-02-0.                                               IX1134.2
075900     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1134.2
076000     MOVE "42" TO CORRECT-X.                                      IX1134.2
076100     PERFORM FAIL.                                                IX1134.2
076200     PERFORM PRINT-DETAIL.                                        IX1134.2
076300                                                                  IX1134.2
076400 TERMINATE-ROUTINE.                                               IX1134.2
076500     EXIT.                                                        IX1134.2
076600                                                                  IX1134.2
076700 CCVS-EXIT SECTION.                                               IX1134.2
076800 CCVS-999999.                                                     IX1134.2
076900     GO TO CLOSE-FILES.                                           IX1134.2
       END PROGRAM IX113A.
000100 IDENTIFICATION DIVISION.                                         IX1144.2
000200 PROGRAM-ID.                                                      IX1144.2
000300     IX114A.                                                      IX1144.2
000400****************************************************************  IX1144.2
000500*                                                              *  IX1144.2
000600*    VALIDATION FOR:-                                          *  IX1144.2
000700*                                                              *  IX1144.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1144.2
000900*                                                              *  IX1144.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1144.2
001100*                                                              *  IX1144.2
001200****************************************************************  IX1144.2
001300*                                                                 IX1144.2
001400*    THIS ROUTINE USES THE MASS STORAGE FILE IX-FS3 CREATED IN    IX1144.2
001500*    IX113A.                                                      IX1144.2
001600*    THE FILE IS OPENED I-O AND THE STATUS CHECKED (00 EXPECTED)  IX1144.2
001700*    THEN CLOSED AND THE STATUS CHECKED (00 EXPECTED).  AN        IX1144.2
001800*    ATTEMPT IS THEN MADE TO READ A RECORD AT WHICH POINT THE     IX1144.2
001900*    DECLARATIVES SHOULD BE ACTIONED AND THE STATUS SHOULD BE    *IX1144.2
002000*    47 (IX-5, 1.3.4 (5) F).                                      IX1144.2
002100*                                                                 IX1144.2
002200*    4. X-CARDS USED IN THIS PROGRAM:                             IX1144.2
002300*                                                                 IX1144.2
002400*                 XXXXX024                                        IX1144.2
002500*                 XXXXX055.                                       IX1144.2
002600*         P       XXXXX062.                                       IX1144.2
002700*                 XXXXX082.                                       IX1144.2
002800*                 XXXXX083.                                       IX1144.2
002900*         C       XXXXX084                                        IX1144.2
003000*                                                                 IX1144.2
003100*                                                                 IX1144.2
003200 ENVIRONMENT DIVISION.                                            IX1144.2
003300 CONFIGURATION SECTION.                                           IX1144.2
003400 SOURCE-COMPUTER.                                                 IX1144.2
003500     XXXXX082.                                                    IX1144.2
003600 OBJECT-COMPUTER.                                                 IX1144.2
003700     XXXXX083.                                                    IX1144.2
003800 INPUT-OUTPUT SECTION.                                            IX1144.2
003900 FILE-CONTROL.                                                    IX1144.2
004000P    SELECT RAW-DATA   ASSIGN TO                                  IX1144.2
004100P    XXXXX062                                                     IX1144.2
004200P           ORGANIZATION IS INDEXED                               IX1144.2
004300P           ACCESS MODE IS RANDOM                                 IX1144.2
004400P           RECORD KEY IS RAW-DATA-KEY.                           IX1144.2
004500*                                                                 IX1144.2
004600     SELECT PRINT-FILE ASSIGN TO                                  IX1144.2
004700     XXXXX055.                                                    IX1144.2
004800*                                                                 IX1144.2
004900     SELECT IX-FS3 ASSIGN                                         IX1144.2
005000     XXXXX024                                                     IX1144.2
005100     ORGANIZATION IS INDEXED                                      IX1144.2
005200     ACCESS MODE IS SEQUENTIAL                                    IX1144.2
005300     RECORD KEY IS IX-FS3-KEY                                     IX1144.2
005400     FILE STATUS IS IX-FS3-STATUS.                                IX1144.2
005500                                                                  IX1144.2
005600 DATA DIVISION.                                                   IX1144.2
005700                                                                  IX1144.2
005800 FILE SECTION.                                                    IX1144.2
005900P                                                                 IX1144.2
006000PFD  RAW-DATA.                                                    IX1144.2
006100P                                                                 IX1144.2
006200P01  RAW-DATA-SATZ.                                               IX1144.2
006300P    05  RAW-DATA-KEY        PIC X(6).                            IX1144.2
006400P    05  C-DATE              PIC 9(6).                            IX1144.2
006500P    05  C-TIME              PIC 9(8).                            IX1144.2
006600P    05  C-NO-OF-TESTS       PIC 99.                              IX1144.2
006700P    05  C-OK                PIC 999.                             IX1144.2
006800P    05  C-ALL               PIC 999.                             IX1144.2
006900P    05  C-FAIL              PIC 999.                             IX1144.2
007000P    05  C-DELETED           PIC 999.                             IX1144.2
007100P    05  C-INSPECT           PIC 999.                             IX1144.2
007200P    05  C-NOTE              PIC X(13).                           IX1144.2
007300P    05  C-INDENT            PIC X.                               IX1144.2
007400P    05  C-ABORT             PIC X(8).                            IX1144.2
007500                                                                  IX1144.2
007600 FD  PRINT-FILE.                                                  IX1144.2
007700                                                                  IX1144.2
007800 01  PRINT-REC               PIC X(120).                          IX1144.2
007900                                                                  IX1144.2
008000 01  DUMMY-RECORD            PIC X(120).                          IX1144.2
008100                                                                  IX1144.2
008200 FD  IX-FS3                                                       IX1144.2
008300C       DATA RECORDS IX-FS3R1-F-G-240                             IX1144.2
008400C       LABEL RECORD STANDARD                                     IX1144.2
008500        RECORD 240                                                IX1144.2
008600        BLOCK CONTAINS 2 RECORDS.                                 IX1144.2
008700                                                                  IX1144.2
008800 01  IX-FS3R1-F-G-240.                                            IX1144.2
008900     05  IX-FS3-REC-120      PIC X(120).                          IX1144.2
009000     05  IX-FS3-REC-120-240.                                      IX1144.2
009100         10  FILLER          PIC X(8).                            IX1144.2
009200         10  IX-FS3-KEY      PIC X(29).                           IX1144.2
009300         10  FILLER          PIC X(9).                            IX1144.2
009400         10  IX-FS3-ALTER-KEY  PIC X(29).                         IX1144.2
009500         10  FILLER            PIC X(45).                         IX1144.2
009600                                                                  IX1144.2
009700                                                                  IX1144.2
009800 WORKING-STORAGE SECTION.                                         IX1144.2
009900                                                                  IX1144.2
010000 01  GRP-0101.                                                    IX1144.2
010100     05  FILLER              PIC X(10) VALUE "RECORD-KEY".        IX1144.2
010200     05  GRP-0101-KEY        PIC 9(9)  VALUE ZERO.                IX1144.2
010300     05  FILLER              PIC X(10) VALUE "END-OF-KEY".        IX1144.2
010400                                                                  IX1144.2
010500 01  GRP-0102.                                                    IX1144.2
010600     05  FILLER              PIC X(10) VALUE "ALTERN-KEY".        IX1144.2
010700     05  GRP-0102-KEY        PIC 9(9)  VALUE ZERO.                IX1144.2
010800     05  FILLER              PIC X(10) VALUE "END-AL-KEY".        IX1144.2
010900                                                                  IX1144.2
011000 01  WRK-CS-09V00            PIC S9(9) COMP VALUE ZERO.           IX1144.2
011100                                                                  IX1144.2
011200 01  EOF-FLAG                PIC 9 VALUE ZERO.                    IX1144.2
011300                                                                  IX1144.2
011400 01  RECORDS-IN-ERROR        PIC S9(5) COMP VALUE ZERO.           IX1144.2
011500                                                                  IX1144.2
011600 01  ERROR-FLAG              PIC 9 VALUE ZERO.                    IX1144.2
011700                                                                  IX1144.2
011800 01  PERM-ERRORS             PIC S9(5) COMP VALUE ZERO.           IX1144.2
011900                                                                  IX1144.2
012000 01  STATUS-TEST-00          PIC 9 VALUE ZERO.                    IX1144.2
012100                                                                  IX1144.2
012200 01  STATUS-TEST-10          PIC 9 VALUE ZERO.                    IX1144.2
012300 01  STATUS-TEST-READ        PIC 9 VALUE ZERO.                    IX1144.2
012400                                                                  IX1144.2
012500 01  IX-FS3-STATUS.                                               IX1144.2
012600     05  IX-FS3-STAT1        PIC X.                               IX1144.2
012700     05  IX-FS3-STAT2        PIC X.                               IX1144.2
012800                                                                  IX1144.2
012900 01  COUNT-OF-RECS           PIC 9(5).                            IX1144.2
013000                                                                  IX1144.2
013100 01  COUNT-OF-RECORDS REDEFINES COUNT-OF-RECS  PIC 9(5).          IX1144.2
013200                                                                  IX1144.2
013300 01  FILE-RECORD-INFORMATION-REC.                                 IX1144.2
013400     05  FILE-RECORD-INFO-SKELETON.                               IX1144.2
013500         10  FILLER          PIC X(48) VALUE                      IX1144.2
013600              "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00". IX1144.2
013700         10  FILLER          PIC X(46) VALUE                      IX1144.2
013800                ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000". IX1144.2
013900         10  FILLER          PIC X(26) VALUE                      IX1144.2
014000                                    ",LFIL=000000,ORG=  ,LBLR= ". IX1144.2
014100         10  FILLER          PIC X(37) VALUE                      IX1144.2
014200                         ",RECKEY=                             ". IX1144.2
014300         10  FILLER          PIC X(38) VALUE                      IX1144.2
014400                        ",ALTKEY1=                             ". IX1144.2
014500         10  FILLER          PIC X(38) VALUE                      IX1144.2
014600                        ",ALTKEY2=                             ". IX1144.2
014700         10  FILLER          PIC X(7) VALUE SPACE.                IX1144.2
014800     05  FILE-RECORD-INFO             OCCURS 10.                  IX1144.2
014900         10  FILE-RECORD-INFO-P1-120.                             IX1144.2
015000             15  FILLER      PIC X(5).                            IX1144.2
015100             15  XFILE-NAME  PIC X(6).                            IX1144.2
015200             15  FILLER      PIC X(8).                            IX1144.2
015300             15  XRECORD-NAME  PIC X(6).                          IX1144.2
015400             15  FILLER        PIC X(1).                          IX1144.2
015500             15  REELUNIT-NUMBER  PIC 9(1).                       IX1144.2
015600             15  FILLER           PIC X(7).                       IX1144.2
015700             15  XRECORD-NUMBER   PIC 9(6).                       IX1144.2
015800             15  FILLER           PIC X(6).                       IX1144.2
015900             15  UPDATE-NUMBER    PIC 9(2).                       IX1144.2
016000             15  FILLER           PIC X(5).                       IX1144.2
016100             15  ODO-NUMBER       PIC 9(4).                       IX1144.2
016200             15  FILLER           PIC X(5).                       IX1144.2
016300             15  XPROGRAM-NAME    PIC X(5).                       IX1144.2
016400             15  FILLER           PIC X(7).                       IX1144.2
016500             15  XRECORD-LENGTH   PIC 9(6).                       IX1144.2
016600             15  FILLER           PIC X(7).                       IX1144.2
016700             15  CHARS-OR-RECORDS  PIC X(2).                      IX1144.2
016800             15  FILLER            PIC X(1).                      IX1144.2
016900             15  XBLOCK-SIZE       PIC 9(4).                      IX1144.2
017000             15  FILLER            PIC X(6).                      IX1144.2
017100             15  RECORDS-IN-FILE   PIC 9(6).                      IX1144.2
017200             15  FILLER            PIC X(5).                      IX1144.2
017300             15  XFILE-ORGANIZATION  PIC X(2).                    IX1144.2
017400             15  FILLER              PIC X(6).                    IX1144.2
017500             15  XLABEL-TYPE         PIC X(1).                    IX1144.2
017600         10  FILE-RECORD-INFO-P121-240.                           IX1144.2
017700             15  FILLER              PIC X(8).                    IX1144.2
017800             15  XRECORD-KEY         PIC X(29).                   IX1144.2
017900             15  FILLER              PIC X(9).                    IX1144.2
018000             15  ALTERNATE-KEY1      PIC X(29).                   IX1144.2
018100             15  FILLER              PIC X(9).                    IX1144.2
018200             15  ALTERNATE-KEY2      PIC X(29).                   IX1144.2
018300             15  FILLER              PIC X(7).                    IX1144.2
018400                                                                  IX1144.2
018500 01  TEST-RESULTS.                                                IX1144.2
018600     02 FILLER                   PIC X      VALUE SPACE.          IX1144.2
018700     02 FEATURE                  PIC X(20)  VALUE SPACE.          IX1144.2
018800     02 FILLER                   PIC X      VALUE SPACE.          IX1144.2
018900     02 P-OR-F                   PIC X(5)   VALUE SPACE.          IX1144.2
019000     02 FILLER                   PIC X      VALUE SPACE.          IX1144.2
019100     02  PAR-NAME.                                                IX1144.2
019200       03 FILLER                 PIC X(19)  VALUE SPACE.          IX1144.2
019300       03  PARDOT-X              PIC X      VALUE SPACE.          IX1144.2
019400       03 DOTVALUE               PIC 99     VALUE ZERO.           IX1144.2
019500     02 FILLER                   PIC X(8)   VALUE SPACE.          IX1144.2
019600     02 RE-MARK                  PIC X(61).                       IX1144.2
019700 01  TEST-COMPUTED.                                               IX1144.2
019800     02 FILLER                   PIC X(30)  VALUE SPACE.          IX1144.2
019900     02 FILLER                   PIC X(17)  VALUE                 IX1144.2
020000            "       COMPUTED=".                                   IX1144.2
020100     02 COMPUTED-X.                                               IX1144.2
020200     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          IX1144.2
020300     03 COMPUTED-N               REDEFINES COMPUTED-A             IX1144.2
020400                                 PIC -9(9).9(9).                  IX1144.2
020500     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         IX1144.2
020600     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     IX1144.2
020700     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     IX1144.2
020800     03       CM-18V0 REDEFINES COMPUTED-A.                       IX1144.2
020900         04 COMPUTED-18V0                    PIC -9(18).          IX1144.2
021000         04 FILLER                           PIC X.               IX1144.2
021100     03 FILLER PIC X(50) VALUE SPACE.                             IX1144.2
021200 01  TEST-CORRECT.                                                IX1144.2
021300     02 FILLER PIC X(30) VALUE SPACE.                             IX1144.2
021400     02 FILLER PIC X(17) VALUE "       CORRECT =".                IX1144.2
021500     02 CORRECT-X.                                                IX1144.2
021600     03 CORRECT-A                  PIC X(20) VALUE SPACE.         IX1144.2
021700     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      IX1144.2
021800     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         IX1144.2
021900     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     IX1144.2
022000     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     IX1144.2
022100     03      CR-18V0 REDEFINES CORRECT-A.                         IX1144.2
022200         04 CORRECT-18V0                     PIC -9(18).          IX1144.2
022300         04 FILLER                           PIC X.               IX1144.2
022400     03 FILLER PIC X(2) VALUE SPACE.                              IX1144.2
022500     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     IX1144.2
022600 01  CCVS-C-1.                                                    IX1144.2
022700     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PAIX1144.2
022800-    "SS  PARAGRAPH-NAME                                          IX1144.2
022900-    "       REMARKS".                                            IX1144.2
023000     02 FILLER                     PIC X(20)    VALUE SPACE.      IX1144.2
023100 01  CCVS-C-2.                                                    IX1144.2
023200     02 FILLER                     PIC X        VALUE SPACE.      IX1144.2
023300     02 FILLER                     PIC X(6)     VALUE "TESTED".   IX1144.2
023400     02 FILLER                     PIC X(15)    VALUE SPACE.      IX1144.2
023500     02 FILLER                     PIC X(4)     VALUE "FAIL".     IX1144.2
023600     02 FILLER                     PIC X(94)    VALUE SPACE.      IX1144.2
023700 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       IX1144.2
023800 01  REC-CT                        PIC 99       VALUE ZERO.       IX1144.2
023900 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       IX1144.2
024000 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       IX1144.2
024100 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       IX1144.2
024200 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       IX1144.2
024300 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       IX1144.2
024400 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       IX1144.2
024500 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      IX1144.2
024600 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       IX1144.2
024700 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     IX1144.2
024800 01  CCVS-H-1.                                                    IX1144.2
024900     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1144.2
025000     02  FILLER                    PIC X(42)    VALUE             IX1144.2
025100     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 IX1144.2
025200     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1144.2
025300 01  CCVS-H-2A.                                                   IX1144.2
025400   02  FILLER                        PIC X(40)  VALUE SPACE.      IX1144.2
025500   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  IX1144.2
025600   02  FILLER                        PIC XXXX   VALUE             IX1144.2
025700     "4.2 ".                                                      IX1144.2
025800   02  FILLER                        PIC X(28)  VALUE             IX1144.2
025900            " COPY - NOT FOR DISTRIBUTION".                       IX1144.2
026000   02  FILLER                        PIC X(41)  VALUE SPACE.      IX1144.2
026100                                                                  IX1144.2
026200 01  CCVS-H-2B.                                                   IX1144.2
026300   02  FILLER                        PIC X(15)  VALUE             IX1144.2
026400            "TEST RESULT OF ".                                    IX1144.2
026500   02  TEST-ID                       PIC X(9).                    IX1144.2
026600   02  FILLER                        PIC X(4)   VALUE             IX1144.2
026700            " IN ".                                               IX1144.2
026800   02  FILLER                        PIC X(12)  VALUE             IX1144.2
026900     " HIGH       ".                                              IX1144.2
027000   02  FILLER                        PIC X(22)  VALUE             IX1144.2
027100            " LEVEL VALIDATION FOR ".                             IX1144.2
027200   02  FILLER                        PIC X(58)  VALUE             IX1144.2
027300     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1144.2
027400 01  CCVS-H-3.                                                    IX1144.2
027500     02  FILLER                      PIC X(34)  VALUE             IX1144.2
027600            " FOR OFFICIAL USE ONLY    ".                         IX1144.2
027700     02  FILLER                      PIC X(58)  VALUE             IX1144.2
027800     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1144.2
027900     02  FILLER                      PIC X(28)  VALUE             IX1144.2
028000            "  COPYRIGHT   1985 ".                                IX1144.2
028100 01  CCVS-E-1.                                                    IX1144.2
028200     02 FILLER                       PIC X(52)  VALUE SPACE.      IX1144.2
028300     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              IX1144.2
028400     02 ID-AGAIN                     PIC X(9).                    IX1144.2
028500     02 FILLER                       PIC X(45)  VALUE SPACES.     IX1144.2
028600 01  CCVS-E-2.                                                    IX1144.2
028700     02  FILLER                      PIC X(31)  VALUE SPACE.      IX1144.2
028800     02  FILLER                      PIC X(21)  VALUE SPACE.      IX1144.2
028900     02 CCVS-E-2-2.                                               IX1144.2
029000         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      IX1144.2
029100         03 FILLER                   PIC X      VALUE SPACE.      IX1144.2
029200         03 ENDER-DESC               PIC X(44)  VALUE             IX1144.2
029300            "ERRORS ENCOUNTERED".                                 IX1144.2
029400 01  CCVS-E-3.                                                    IX1144.2
029500     02  FILLER                      PIC X(22)  VALUE             IX1144.2
029600            " FOR OFFICIAL USE ONLY".                             IX1144.2
029700     02  FILLER                      PIC X(12)  VALUE SPACE.      IX1144.2
029800     02  FILLER                      PIC X(58)  VALUE             IX1144.2
029900     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1144.2
030000     02  FILLER                      PIC X(13)  VALUE SPACE.      IX1144.2
030100     02 FILLER                       PIC X(15)  VALUE             IX1144.2
030200             " COPYRIGHT 1985".                                   IX1144.2
030300 01  CCVS-E-4.                                                    IX1144.2
030400     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      IX1144.2
030500     02 FILLER                       PIC X(4)   VALUE " OF ".     IX1144.2
030600     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      IX1144.2
030700     02 FILLER                       PIC X(40)  VALUE             IX1144.2
030800      "  TESTS WERE EXECUTED SUCCESSFULLY".                       IX1144.2
030900 01  XXINFO.                                                      IX1144.2
031000     02 FILLER                       PIC X(19)  VALUE             IX1144.2
031100            "*** INFORMATION ***".                                IX1144.2
031200     02 INFO-TEXT.                                                IX1144.2
031300       04 FILLER                     PIC X(8)   VALUE SPACE.      IX1144.2
031400       04 XXCOMPUTED                 PIC X(20).                   IX1144.2
031500       04 FILLER                     PIC X(5)   VALUE SPACE.      IX1144.2
031600       04 XXCORRECT                  PIC X(20).                   IX1144.2
031700     02 INF-ANSI-REFERENCE           PIC X(48).                   IX1144.2
031800 01  HYPHEN-LINE.                                                 IX1144.2
031900     02 FILLER  PIC IS X VALUE IS SPACE.                          IX1144.2
032000     02 FILLER  PIC IS X(65)    VALUE IS "************************IX1144.2
032100-    "*****************************************".                 IX1144.2
032200     02 FILLER  PIC IS X(54)    VALUE IS "************************IX1144.2
032300-    "******************************".                            IX1144.2
032400 01  TEST-NO                         PIC 99.                      IX1144.2
032500 01  CCVS-PGM-ID                     PIC X(9)   VALUE             IX1144.2
032600     "IX114A".                                                    IX1144.2
032700 PROCEDURE DIVISION.                                              IX1144.2
032800 DECLARATIVES.                                                    IX1144.2
032900                                                                  IX1144.2
033000 SECT-IX105-0002 SECTION.                                         IX1144.2
033100     USE AFTER EXCEPTION PROCEDURE ON IX-FS3.                     IX1144.2
033200 INPUT-PROCESS.                                                   IX1144.2
033300     IF TEST-NO = 5                                               IX1144.2
033400        GO TO D-C-TEST-GF-01-1.                                   IX1144.2
033500     IF STATUS-TEST-10 EQUAL TO 1                                 IX1144.2
033600        IF  IX-FS3-STAT1 EQUAL TO "1"                             IX1144.2
033700            MOVE 1 TO EOF-FLAG                                    IX1144.2
033800        ELSE                                                      IX1144.2
033900           IF  IX-FS3-STAT1 GREATER THAN "1"                      IX1144.2
034000           MOVE 1 TO PERM-ERRORS.                                 IX1144.2
034100     GO TO DECL-EXIT.                                             IX1144.2
034200 D-C-TEST-GF-01-1.                                                IX1144.2
034300     IF IX-FS3-STATUS EQUAL TO "47"                               IX1144.2
034400         GO TO D-C-PASS-GF-01-0.                                  IX1144.2
034500 D-C-FAIL-GF-01-0.                                                IX1144.2
034600     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1144.2
034700     MOVE "47" TO CORRECT-X.                                      IX1144.2
034800     MOVE "IX-5, 1.3.4, (5) F" TO RE-MARK.                        IX1144.2
034900     PERFORM D-FAIL.                                              IX1144.2
035000     GO TO D-C-WRITE-GF-01-0.                                     IX1144.2
035100 D-C-PASS-GF-01-0.                                                IX1144.2
035200     PERFORM D-PASS.                                              IX1144.2
035300 D-C-WRITE-GF-01-0.                                               IX1144.2
035400     PERFORM D-PRINT-DETAIL.                                      IX1144.2
035500 D-CLOSE-FILES.                                                   IX1144.2
035600P    OPEN I-O RAW-DATA.                                           IX1144.2
035700P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1144.2
035800P    READ RAW-DATA INVALID KEY GO TO D-END-E-2.                   IX1144.2
035900P    MOVE "OK.     " TO C-ABORT.                                  IX1144.2
036000P    MOVE PASS-COUNTER TO C-OK.                                   IX1144.2
036100P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1144.2
036200P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1144.2
036300P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1144.2
036400P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1144.2
036500P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO D-END-E-2.           IX1144.2
036600PD-END-E-2.                                                       IX1144.2
036700P    CLOSE RAW-DATA.                                              IX1144.2
036800     PERFORM D-END-ROUTINE THRU D-END-ROUTINE-13.                 IX1144.2
036900     CLOSE PRINT-FILE.                                            IX1144.2
037000 D-TERMINATE-CCVS.                                                IX1144.2
037100S    EXIT PROGRAM.                                                IX1144.2
037200SD-TERMINATE-CALL.                                                IX1144.2
037300     STOP     RUN.                                                IX1144.2
037400 D-PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.         IX1144.2
037500 D-FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.        IX1144.2
037600 D-PRINT-DETAIL.                                                  IX1144.2
037700     IF   REC-CT NOT EQUAL TO ZERO                                IX1144.2
037800          MOVE "." TO PARDOT-X                                    IX1144.2
037900          MOVE REC-CT TO DOTVALUE.                                IX1144.2
038000     MOVE TEST-RESULTS TO PRINT-REC.                              IX1144.2
038100     PERFORM D-WRITE-LINE.                                        IX1144.2
038200     IF   P-OR-F EQUAL TO "FAIL*"                                 IX1144.2
038300          PERFORM D-WRITE-LINE                                    IX1144.2
038400          PERFORM D-FAIL-ROUTINE THRU D-FAIL-ROUTINE-EX           IX1144.2
038500     ELSE                                                         IX1144.2
038600          PERFORM D-BAIL-OUT THRU D-BAIL-OUT-EX.                  IX1144.2
038700     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1144.2
038800     MOVE SPACE TO CORRECT-X.                                     IX1144.2
038900     IF   REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.           IX1144.2
039000     MOVE SPACE TO RE-MARK.                                       IX1144.2
039100 D-END-ROUTINE.                                                   IX1144.2
039200     MOVE HYPHEN-LINE TO DUMMY-RECORD.                            IX1144.2
039300     PERFORM D-WRITE-LINE 5 TIMES.                                IX1144.2
039400 D-END-RTN-EXIT.                                                  IX1144.2
039500     MOVE CCVS-E-1 TO DUMMY-RECORD.                               IX1144.2
039600     PERFORM D-WRITE-LINE 2 TIMES.                                IX1144.2
039700 D-END-ROUTINE-1.                                                 IX1144.2
039800     ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO       IX1144.2
039900     ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.                IX1144.2
040000     ADD PASS-COUNTER TO ERROR-HOLD.                              IX1144.2
040100     MOVE PASS-COUNTER TO CCVS-E-4-1.                             IX1144.2
040200     MOVE ERROR-HOLD TO CCVS-E-4-2.                               IX1144.2
040300     MOVE CCVS-E-4 TO CCVS-E-2-2.                                 IX1144.2
040400     MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM D-WRITE-LINE.          IX1144.2
040500  D-END-ROUTINE-12.                                               IX1144.2
040600     MOVE "TEST(S) FAILED" TO ENDER-DESC.                         IX1144.2
040700     IF  ERROR-COUNTER IS EQUAL TO ZERO                           IX1144.2
040800         MOVE "NO " TO ERROR-TOTAL                                IX1144.2
040900     ELSE                                                         IX1144.2
041000         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1144.2
041100     MOVE    CCVS-E-2 TO DUMMY-RECORD.                            IX1144.2
041200     PERFORM D-WRITE-LINE.                                        IX1144.2
041300 D-END-ROUTINE-13.                                                IX1144.2
041400     IF  DELETE-COUNTER IS EQUAL TO ZERO                          IX1144.2
041500         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1144.2
041600         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1144.2
041700     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1144.2
041800     MOVE CCVS-E-2 TO DUMMY-RECORD.                               IX1144.2
041900     PERFORM D-WRITE-LINE.                                        IX1144.2
042000     IF   INSPECT-COUNTER EQUAL TO ZERO                           IX1144.2
042100          MOVE "NO " TO ERROR-TOTAL                               IX1144.2
042200     ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                    IX1144.2
042300     MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.             IX1144.2
042400     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1144.2
042500     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1144.2
042600 D-WRITE-LINE.                                                    IX1144.2
042700     ADD 1 TO RECORD-COUNT.                                       IX1144.2
042800Y    IF RECORD-COUNT GREATER 42                                   IX1144.2
042900Y       MOVE DUMMY-RECORD TO DUMMY-HOLD                           IX1144.2
043000Y       MOVE SPACE TO DUMMY-RECORD                                IX1144.2
043100Y       WRITE DUMMY-RECORD AFTER ADVANCING PAGE                   IX1144.2
043200Y       MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1144.2
043300Y       MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1144.2
043400Y       MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1144.2
043500Y       MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1144.2
043600Y       MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1144.2
043700Y       MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1144.2
043800Y       MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM D-WRT-LN         IX1144.2
043900Y       MOVE DUMMY-HOLD TO DUMMY-RECORD                           IX1144.2
044000Y       MOVE ZERO TO RECORD-COUNT.                                IX1144.2
044100     PERFORM D-WRT-LN.                                            IX1144.2
044200 D-WRT-LN.                                                        IX1144.2
044300     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1144.2
044400     MOVE SPACE TO DUMMY-RECORD.                                  IX1144.2
044500 D-FAIL-ROUTINE.                                                  IX1144.2
044600     IF   COMPUTED-X NOT EQUAL TO SPACE                           IX1144.2
044700          GO TO D-FAIL-ROUTINE-WRITE.                             IX1144.2
044800     IF   CORRECT-X NOT EQUAL TO SPACE GO TO D-FAIL-ROUTINE-WRITE.IX1144.2
044900     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1144.2
045000     MOVE "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.    IX1144.2
045100     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1144.2
045200     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1144.2
045300     GO TO D-FAIL-ROUTINE-EX.                                     IX1144.2
045400 D-FAIL-ROUTINE-WRITE.                                            IX1144.2
045500     MOVE TEST-COMPUTED TO PRINT-REC PERFORM D-WRITE-LINE         IX1144.2
045600     MOVE ANSI-REFERENCE TO COR-ANSI-REFERENCE.                   IX1144.2
045700     MOVE TEST-CORRECT TO PRINT-REC PERFORM D-WRITE-LINE 2 TIMES. IX1144.2
045800     MOVE SPACES TO COR-ANSI-REFERENCE.                           IX1144.2
045900 D-FAIL-ROUTINE-EX. EXIT.                                         IX1144.2
046000 D-BAIL-OUT.                                                      IX1144.2
046100     IF  COMPUTED-A NOT EQUAL TO SPACE GO TO D-BAIL-OUT-WRITE.    IX1144.2
046200     IF  CORRECT-A EQUAL TO SPACE GO TO D-BAIL-OUT-EX.            IX1144.2
046300 D-BAIL-OUT-WRITE.                                                IX1144.2
046400     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1144.2
046500     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1144.2
046600     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1144.2
046700     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1144.2
046800 D-BAIL-OUT-EX. EXIT.                                             IX1144.2
046900 DECL-EXIT.  EXIT.                                                IX1144.2
047000 END DECLARATIVES.                                                IX1144.2
047100                                                                  IX1144.2
047200                                                                  IX1144.2
047300 CCVS1 SECTION.                                                   IX1144.2
047400 OPEN-FILES.                                                      IX1144.2
047500P    OPEN I-O RAW-DATA.                                           IX1144.2
047600P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1144.2
047700P    READ RAW-DATA INVALID KEY GO TO END-E-1.                     IX1144.2
047800P    MOVE "ABORTED " TO C-ABORT.                                  IX1144.2
047900P    ADD 1 TO C-NO-OF-TESTS.                                      IX1144.2
048000P    ACCEPT C-DATE  FROM DATE.                                    IX1144.2
048100P    ACCEPT C-TIME  FROM TIME.                                    IX1144.2
048200P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-1.             IX1144.2
048300PEND-E-1.                                                         IX1144.2
048400P    CLOSE RAW-DATA.                                              IX1144.2
048500     OPEN    OUTPUT PRINT-FILE.                                   IX1144.2
048600     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  IX1144.2
048700     MOVE    SPACE TO TEST-RESULTS.                               IX1144.2
048800     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              IX1144.2
048900     MOVE    ZERO TO REC-SKL-SUB.                                 IX1144.2
049000     PERFORM CCVS-INIT-FILE 9 TIMES.                              IX1144.2
049100 CCVS-INIT-FILE.                                                  IX1144.2
049200     ADD     1 TO REC-SKL-SUB.                                    IX1144.2
049300     MOVE    FILE-RECORD-INFO-SKELETON                            IX1144.2
049400          TO FILE-RECORD-INFO (REC-SKL-SUB).                      IX1144.2
049500 CCVS-INIT-EXIT.                                                  IX1144.2
049600     GO TO CCVS1-EXIT.                                            IX1144.2
049700 CLOSE-FILES.                                                     IX1144.2
049800P    OPEN I-O RAW-DATA.                                           IX1144.2
049900P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1144.2
050000P    READ RAW-DATA INVALID KEY GO TO END-E-2.                     IX1144.2
050100P    MOVE "OK.     " TO C-ABORT.                                  IX1144.2
050200P    MOVE PASS-COUNTER TO C-OK.                                   IX1144.2
050300P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1144.2
050400P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1144.2
050500P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1144.2
050600P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1144.2
050700P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-2.             IX1144.2
050800PEND-E-2.                                                         IX1144.2
050900P    CLOSE RAW-DATA.                                              IX1144.2
051000     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   IX1144.2
051100 TERMINATE-CCVS.                                                  IX1144.2
051200S    EXIT PROGRAM.                                                IX1144.2
051300STERMINATE-CALL.                                                  IX1144.2
051400     STOP     RUN.                                                IX1144.2
051500 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         IX1144.2
051600 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           IX1144.2
051700 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          IX1144.2
051800 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      IX1144.2
051900     MOVE "****TEST DELETED****" TO RE-MARK.                      IX1144.2
052000 PRINT-DETAIL.                                                    IX1144.2
052100     IF REC-CT NOT EQUAL TO ZERO                                  IX1144.2
052200             MOVE "." TO PARDOT-X                                 IX1144.2
052300             MOVE REC-CT TO DOTVALUE.                             IX1144.2
052400     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      IX1144.2
052500     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               IX1144.2
052600        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 IX1144.2
052700          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 IX1144.2
052800     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1144.2
052900     MOVE SPACE TO CORRECT-X.                                     IX1144.2
053000     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         IX1144.2
053100     MOVE     SPACE TO RE-MARK.                                   IX1144.2
053200 HEAD-ROUTINE.                                                    IX1144.2
053300     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1144.2
053400     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1144.2
053500     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1144.2
053600     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1144.2
053700 COLUMN-NAMES-ROUTINE.                                            IX1144.2
053800     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1144.2
053900     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1144.2
054000     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        IX1144.2
054100 END-ROUTINE.                                                     IX1144.2
054200     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.IX1144.2
054300 END-RTN-EXIT.                                                    IX1144.2
054400     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1144.2
054500 END-ROUTINE-1.                                                   IX1144.2
054600      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      IX1144.2
054700      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               IX1144.2
054800      ADD PASS-COUNTER TO ERROR-HOLD.                             IX1144.2
054900*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   IX1144.2
055000      MOVE PASS-COUNTER TO CCVS-E-4-1.                            IX1144.2
055100      MOVE ERROR-HOLD TO CCVS-E-4-2.                              IX1144.2
055200      MOVE CCVS-E-4 TO CCVS-E-2-2.                                IX1144.2
055300      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           IX1144.2
055400  END-ROUTINE-12.                                                 IX1144.2
055500      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        IX1144.2
055600     IF       ERROR-COUNTER IS EQUAL TO ZERO                      IX1144.2
055700         MOVE "NO " TO ERROR-TOTAL                                IX1144.2
055800         ELSE                                                     IX1144.2
055900         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1144.2
056000     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           IX1144.2
056100     PERFORM WRITE-LINE.                                          IX1144.2
056200 END-ROUTINE-13.                                                  IX1144.2
056300     IF DELETE-COUNTER IS EQUAL TO ZERO                           IX1144.2
056400         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1144.2
056500         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1144.2
056600     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1144.2
056700     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1144.2
056800      IF   INSPECT-COUNTER EQUAL TO ZERO                          IX1144.2
056900          MOVE "NO " TO ERROR-TOTAL                               IX1144.2
057000      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   IX1144.2
057100      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            IX1144.2
057200      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          IX1144.2
057300     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1144.2
057400 WRITE-LINE.                                                      IX1144.2
057500     ADD 1 TO RECORD-COUNT.                                       IX1144.2
057600Y    IF RECORD-COUNT GREATER 42                                   IX1144.2
057700Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          IX1144.2
057800Y        MOVE SPACE TO DUMMY-RECORD                               IX1144.2
057900Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  IX1144.2
058000Y        MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1144.2
058100Y        MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1144.2
058200Y        MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1144.2
058300Y        MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1144.2
058400Y        MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM WRT-LN            IX1144.2
058500Y        MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM WRT-LN            IX1144.2
058600Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          IX1144.2
058700Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          IX1144.2
058800Y        MOVE ZERO TO RECORD-COUNT.                               IX1144.2
058900     PERFORM WRT-LN.                                              IX1144.2
059000 WRT-LN.                                                          IX1144.2
059100     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1144.2
059200     MOVE SPACE TO DUMMY-RECORD.                                  IX1144.2
059300 BLANK-LINE-PRINT.                                                IX1144.2
059400     PERFORM WRT-LN.                                              IX1144.2
059500 FAIL-ROUTINE.                                                    IX1144.2
059600     IF     COMPUTED-X NOT EQUAL TO SPACE                         IX1144.2
059700            GO TO   FAIL-ROUTINE-WRITE.                           IX1144.2
059800     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.IX1144.2
059900     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1144.2
060000     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   IX1144.2
060100     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1144.2
060200     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1144.2
060300     GO TO  FAIL-ROUTINE-EX.                                      IX1144.2
060400 FAIL-ROUTINE-WRITE.                                              IX1144.2
060500     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         IX1144.2
060600     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 IX1144.2
060700     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. IX1144.2
060800     MOVE   SPACES TO COR-ANSI-REFERENCE.                         IX1144.2
060900 FAIL-ROUTINE-EX. EXIT.                                           IX1144.2
061000 BAIL-OUT.                                                        IX1144.2
061100     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   IX1144.2
061200     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           IX1144.2
061300 BAIL-OUT-WRITE.                                                  IX1144.2
061400     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1144.2
061500     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1144.2
061600     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1144.2
061700     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1144.2
061800 BAIL-OUT-EX. EXIT.                                               IX1144.2
061900 CCVS1-EXIT.                                                      IX1144.2
062000     EXIT.                                                        IX1144.2
062100                                                                  IX1144.2
062200 SECT-IX114A-0003 SECTION.                                        IX1144.2
062300 SEQ-INIT-010.                                                    IX1144.2
062400     MOVE ZERO TO TEST-NO.                                        IX1144.2
062500     MOVE "IX-FS3" TO XFILE-NAME (1).                             IX1144.2
062600     MOVE "R1-F-G" TO XRECORD-NAME (1).                           IX1144.2
062700     MOVE CCVS-PGM-ID TO XPROGRAM-NAME (1).                       IX1144.2
062800     MOVE 000240 TO XRECORD-LENGTH (1).                           IX1144.2
062900     MOVE "RC" TO CHARS-OR-RECORDS (1).                           IX1144.2
063000     MOVE 0002 TO XBLOCK-SIZE (1).                                IX1144.2
063100     MOVE 000050 TO RECORDS-IN-FILE (1).                          IX1144.2
063200     MOVE "IX" TO XFILE-ORGANIZATION (1).                         IX1144.2
063300     MOVE "S" TO XLABEL-TYPE (1).                                 IX1144.2
063400     MOVE 000001 TO XRECORD-NUMBER (1).                           IX1144.2
063500     MOVE 0 TO COUNT-OF-RECS.                                     IX1144.2
063600                                                                  IX1144.2
063700******************************************************************IX1144.2
063800*   TEST  1                                                      *IX1144.2
063900*         OPEN OUTPUT ...                 00 EXPECTED            *IX1144.2
064000*         IX-3, 1.3.4 (1) A                                      *IX1144.2
064100*    STATUS 00 CHECK ON OUTPUT FILE IX-FS3                       *IX1144.2
064200*    THE OUTPUT STATEMENT IS SUCCESSFULLY EXECUTED               *IX1144.2
064300******************************************************************IX1144.2
064400 OPN-INIT-GF-01-0.                                                IX1144.2
064500     MOVE 1 TO STATUS-TEST-00.                                    IX1144.2
064600     MOVE SPACES TO IX-FS3-STATUS.                                IX1144.2
064700     MOVE "OPEN I-O   : 00 EXP." TO FEATURE.                      IX1144.2
064800     MOVE "OPN-TEST-GF-01-0" TO PAR-NAME.                         IX1144.2
064900     OPEN                                                         IX1144.2
065000        I-O    IX-FS3.                                            IX1144.2
065100     IF IX-FS3-STATUS EQUAL TO "00"                               IX1144.2
065200         GO TO OPN-PASS-GF-01-0.                                  IX1144.2
065300 OPN-FAIL-GF-01-0.                                                IX1144.2
065400     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1144.2
065500     PERFORM FAIL.                                                IX1144.2
065600     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1144.2
065700     MOVE "00" TO CORRECT-X.                                      IX1144.2
065800     GO TO OPN-WRITE-GF-01-0.                                     IX1144.2
065900 OPN-PASS-GF-01-0.                                                IX1144.2
066000     PERFORM PASS.                                                IX1144.2
066100 OPN-WRITE-GF-01-0.                                               IX1144.2
066200     PERFORM PRINT-DETAIL.                                        IX1144.2
066300******************************************************************IX1144.2
066400*   TEST  4                                                      *IX1144.2
066500*         CLOSE I-O                       00 EXPECTED            *IX1144.2
066600*         IX-3, 1.3.4 (1) A                                      *IX1144.2
066700******************************************************************IX1144.2
066800 CLO-INIT-GF-01-0.                                                IX1144.2
066900     MOVE SPACES TO IX-FS3-STATUS.                                IX1144.2
067000     MOVE "CLOSE I-O   :00 EXP." TO FEATURE.                      IX1144.2
067100     MOVE "CLO-TEST-GF-01-0" TO PAR-NAME.                         IX1144.2
067200 CLO-TEST-GF-01-0.                                                IX1144.2
067300     CLOSE IX-FS3.                                                IX1144.2
067400     IF IX-FS3-STATUS = "00"                                      IX1144.2
067500         GO TO CLO-PASS-GF-01-0.                                  IX1144.2
067600 CLO-FAIL-GF-01-0.                                                IX1144.2
067700     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1144.2
067800     PERFORM FAIL.                                                IX1144.2
067900     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1144.2
068000     MOVE "00" TO CORRECT-X.                                      IX1144.2
068100     GO TO CLO-WRITE-GF-01-0.                                     IX1144.2
068200 CLO-PASS-GF-01-0.                                                IX1144.2
068300     PERFORM PASS.                                                IX1144.2
068400 CLO-WRITE-GF-01-0.                                               IX1144.2
068500     PERFORM PRINT-DETAIL.                                        IX1144.2
068600                                                                  IX1144.2
068700******************************************************************IX1144.2
068800*    A INDEXED FILE WITH 50 RECORDS HAS BEEN CREATED.            *IX1144.2
068900******************************************************************IX1144.2
069000                                                                  IX1144.2
069100******************************************************************IX1144.2
069200*   TEST  5                                                      *IX1144.2
069300*         READ ...  A FILE NOT IN THE OPEN MODE                  *IX1144.2
069400*         FILE STATUS 47 EXPECTED IX-5, 1.3.4 (5) F              *IX1144.2
069500******************************************************************IX1144.2
069600 REA-TEST-GF-01-0.                                                IX1144.2
069700     MOVE  5 TO TEST-NO.                                          IX1144.2
069800     MOVE SPACES TO IX-FS3-STATUS.                                IX1144.2
069900     MOVE "READ.        47 EXP." TO FEATURE                       IX1144.2
070000     MOVE "REA-TEST-GF-01-0" TO PAR-NAME.                         IX1144.2
070100     READ IX-FS3 AT END GO TO REA-TEST-GF-01-1.                   IX1144.2
070200 REA-TEST-GF-01-1.                                                IX1144.2
070300     IF IX-FS3-STATUS EQUAL TO "47"                               IX1144.2
070400        MOVE "SHOULD HAVE EXECUTED DECLARATIVES IX-3,1.3.4(4)"    IX1144.2
070500          TO RE-MARK                                              IX1144.2
070600        GO TO REA-WRITE-GF-01-0.                                  IX1144.2
070700 REA-FAIL-GF-01-0.                                                IX1144.2
070800     MOVE "IX-5, 1.3.4, (5) F" TO RE-MARK.                        IX1144.2
070900 REA-WRITE-GF-01-0.                                               IX1144.2
071000     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1144.2
071100     MOVE "47" TO CORRECT-X.                                      IX1144.2
071200     PERFORM FAIL.                                                IX1144.2
071300     PERFORM PRINT-DETAIL.                                        IX1144.2
071400                                                                  IX1144.2
071500 TERMINATE-ROUTINE.                                               IX1144.2
071600     EXIT.                                                        IX1144.2
071700                                                                  IX1144.2
071800 CCVS-EXIT SECTION.                                               IX1144.2
071900 CCVS-999999.                                                     IX1144.2
072000     GO TO CLOSE-FILES.                                           IX1144.2
       END PROGRAM IX114A.
000100 IDENTIFICATION DIVISION.                                         IX1154.2
000200 PROGRAM-ID.                                                      IX1154.2
000300     IX115A.                                                      IX1154.2
000400****************************************************************  IX1154.2
000500*                                                              *  IX1154.2
000600*    VALIDATION FOR:-                                          *  IX1154.2
000700*                                                              *  IX1154.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1154.2
000900*                                                              *  IX1154.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1154.2
001100*                                                              *  IX1154.2
001200****************************************************************  IX1154.2
001300*                                                                 IX1154.2
001400*    THIS ROUTINE USES THE MASS STORAGE FILE IX-FS3 CREATED IN    IX1154.2
001500*    IX113A.                                                      IX1154.2
001600*    THE FILE IS OPENED I-O AND THE STATUS CHECKED (00 EXPECTED)  IX1154.2
001700*    THEN CLOSED AND THE STATUS CHECKED AGAIN (00 EXPECTED).  AN  IX1154.2
001800*    ATTEMPT IS THEN MADE TO WRITE A RECORD TO THE CLOSED FILE    IX1154.2
001900*    AT WHICH POINT THE USE AFTER STANDARD EXCEPTION PROCEDURE   *IX1154.2
002000*    STATEMENTS IN THE DECLARATIVES SHOULD BE EXECUTED AND THE    IX1154.2
002100*    FILE STATUS SHOULD BE 48 (IX-5, 1.3.4 (5) G.                 IX1154.2
002200*                                                                 IX1154.2
002300*    4. X-CARDS USED IN THIS PROGRAM:                             IX1154.2
002400*                                                                 IX1154.2
002500*                 XXXXX024                                        IX1154.2
002600*                 XXXXX055.                                       IX1154.2
002700*         P       XXXXX062.                                       IX1154.2
002800*                 XXXXX082.                                       IX1154.2
002900*                 XXXXX083.                                       IX1154.2
003000*         C       XXXXX084                                        IX1154.2
003100*                                                                 IX1154.2
003200*                                                                 IX1154.2
003300 ENVIRONMENT DIVISION.                                            IX1154.2
003400 CONFIGURATION SECTION.                                           IX1154.2
003500 SOURCE-COMPUTER.                                                 IX1154.2
003600     XXXXX082.                                                    IX1154.2
003700 OBJECT-COMPUTER.                                                 IX1154.2
003800     XXXXX083.                                                    IX1154.2
003900 INPUT-OUTPUT SECTION.                                            IX1154.2
004000 FILE-CONTROL.                                                    IX1154.2
004100P    SELECT RAW-DATA   ASSIGN TO                                  IX1154.2
004200P    XXXXX062                                                     IX1154.2
004300P           ORGANIZATION IS INDEXED                               IX1154.2
004400P           ACCESS MODE IS RANDOM                                 IX1154.2
004500P           RECORD KEY IS RAW-DATA-KEY.                           IX1154.2
004600*                                                                 IX1154.2
004700     SELECT PRINT-FILE ASSIGN TO                                  IX1154.2
004800     XXXXX055.                                                    IX1154.2
004900*                                                                 IX1154.2
005000     SELECT IX-FS3 ASSIGN                                         IX1154.2
005100     XXXXX024                                                     IX1154.2
005200     ORGANIZATION IS INDEXED                                      IX1154.2
005300     ACCESS MODE IS SEQUENTIAL                                    IX1154.2
005400     RECORD KEY IS IX-FS3-KEY                                     IX1154.2
005500     FILE STATUS IS IX-FS3-STATUS.                                IX1154.2
005600                                                                  IX1154.2
005700 DATA DIVISION.                                                   IX1154.2
005800                                                                  IX1154.2
005900 FILE SECTION.                                                    IX1154.2
006000P                                                                 IX1154.2
006100PFD  RAW-DATA.                                                    IX1154.2
006200P                                                                 IX1154.2
006300P01  RAW-DATA-SATZ.                                               IX1154.2
006400P    05  RAW-DATA-KEY        PIC X(6).                            IX1154.2
006500P    05  C-DATE              PIC 9(6).                            IX1154.2
006600P    05  C-TIME              PIC 9(8).                            IX1154.2
006700P    05  C-NO-OF-TESTS       PIC 99.                              IX1154.2
006800P    05  C-OK                PIC 999.                             IX1154.2
006900P    05  C-ALL               PIC 999.                             IX1154.2
007000P    05  C-FAIL              PIC 999.                             IX1154.2
007100P    05  C-DELETED           PIC 999.                             IX1154.2
007200P    05  C-INSPECT           PIC 999.                             IX1154.2
007300P    05  C-NOTE              PIC X(13).                           IX1154.2
007400P    05  C-INDENT            PIC X.                               IX1154.2
007500P    05  C-ABORT             PIC X(8).                            IX1154.2
007600                                                                  IX1154.2
007700 FD  PRINT-FILE.                                                  IX1154.2
007800                                                                  IX1154.2
007900 01  PRINT-REC               PIC X(120).                          IX1154.2
008000                                                                  IX1154.2
008100 01  DUMMY-RECORD            PIC X(120).                          IX1154.2
008200                                                                  IX1154.2
008300 FD  IX-FS3                                                       IX1154.2
008400C       DATA RECORDS IX-FS3R1-F-G-240                             IX1154.2
008500C       LABEL RECORD STANDARD                                     IX1154.2
008600        RECORD 240                                                IX1154.2
008700        BLOCK CONTAINS 2 RECORDS.                                 IX1154.2
008800                                                                  IX1154.2
008900 01  IX-FS3R1-F-G-240.                                            IX1154.2
009000     05  IX-FS3-REC-120      PIC X(120).                          IX1154.2
009100     05  IX-FS3-REC-120-240.                                      IX1154.2
009200         10  FILLER          PIC X(8).                            IX1154.2
009300         10  IX-FS3-KEY      PIC X(29).                           IX1154.2
009400         10  FILLER          PIC X(9).                            IX1154.2
009500         10  IX-FS3-ALTER-KEY  PIC X(29).                         IX1154.2
009600         10  FILLER            PIC X(45).                         IX1154.2
009700                                                                  IX1154.2
009800                                                                  IX1154.2
009900 WORKING-STORAGE SECTION.                                         IX1154.2
010000                                                                  IX1154.2
010100 01  GRP-0101.                                                    IX1154.2
010200     05  FILLER              PIC X(10) VALUE "RECORD-KEY".        IX1154.2
010300     05  GRP-0101-KEY        PIC 9(9)  VALUE ZERO.                IX1154.2
010400     05  FILLER              PIC X(10) VALUE "END-OF-KEY".        IX1154.2
010500                                                                  IX1154.2
010600 01  GRP-0102.                                                    IX1154.2
010700     05  FILLER              PIC X(10) VALUE "ALTERN-KEY".        IX1154.2
010800     05  GRP-0102-KEY        PIC 9(9)  VALUE ZERO.                IX1154.2
010900     05  FILLER              PIC X(10) VALUE "END-AL-KEY".        IX1154.2
011000                                                                  IX1154.2
011100 01  WRK-CS-09V00            PIC S9(9) COMP VALUE ZERO.           IX1154.2
011200                                                                  IX1154.2
011300 01  EOF-FLAG                PIC 9 VALUE ZERO.                    IX1154.2
011400                                                                  IX1154.2
011500 01  RECORDS-IN-ERROR        PIC S9(5) COMP VALUE ZERO.           IX1154.2
011600                                                                  IX1154.2
011700 01  ERROR-FLAG              PIC 9 VALUE ZERO.                    IX1154.2
011800                                                                  IX1154.2
011900 01  PERM-ERRORS             PIC S9(5) COMP VALUE ZERO.           IX1154.2
012000                                                                  IX1154.2
012100 01  STATUS-TEST-00          PIC 9 VALUE ZERO.                    IX1154.2
012200                                                                  IX1154.2
012300 01  STATUS-TEST-10          PIC 9 VALUE ZERO.                    IX1154.2
012400 01  STATUS-TEST-READ        PIC 9 VALUE ZERO.                    IX1154.2
012500                                                                  IX1154.2
012600 01  IX-FS3-STATUS.                                               IX1154.2
012700     05  IX-FS3-STAT1        PIC X.                               IX1154.2
012800     05  IX-FS3-STAT2        PIC X.                               IX1154.2
012900                                                                  IX1154.2
013000 01  COUNT-OF-RECS           PIC 9(5).                            IX1154.2
013100                                                                  IX1154.2
013200 01  COUNT-OF-RECORDS REDEFINES COUNT-OF-RECS  PIC 9(5).          IX1154.2
013300                                                                  IX1154.2
013400 01  FILE-RECORD-INFORMATION-REC.                                 IX1154.2
013500     05  FILE-RECORD-INFO-SKELETON.                               IX1154.2
013600         10  FILLER          PIC X(48) VALUE                      IX1154.2
013700              "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00". IX1154.2
013800         10  FILLER          PIC X(46) VALUE                      IX1154.2
013900                ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000". IX1154.2
014000         10  FILLER          PIC X(26) VALUE                      IX1154.2
014100                                    ",LFIL=000000,ORG=  ,LBLR= ". IX1154.2
014200         10  FILLER          PIC X(37) VALUE                      IX1154.2
014300                         ",RECKEY=                             ". IX1154.2
014400         10  FILLER          PIC X(38) VALUE                      IX1154.2
014500                        ",ALTKEY1=                             ". IX1154.2
014600         10  FILLER          PIC X(38) VALUE                      IX1154.2
014700                        ",ALTKEY2=                             ". IX1154.2
014800         10  FILLER          PIC X(7) VALUE SPACE.                IX1154.2
014900     05  FILE-RECORD-INFO             OCCURS 10.                  IX1154.2
015000         10  FILE-RECORD-INFO-P1-120.                             IX1154.2
015100             15  FILLER      PIC X(5).                            IX1154.2
015200             15  XFILE-NAME  PIC X(6).                            IX1154.2
015300             15  FILLER      PIC X(8).                            IX1154.2
015400             15  XRECORD-NAME  PIC X(6).                          IX1154.2
015500             15  FILLER        PIC X(1).                          IX1154.2
015600             15  REELUNIT-NUMBER  PIC 9(1).                       IX1154.2
015700             15  FILLER           PIC X(7).                       IX1154.2
015800             15  XRECORD-NUMBER   PIC 9(6).                       IX1154.2
015900             15  FILLER           PIC X(6).                       IX1154.2
016000             15  UPDATE-NUMBER    PIC 9(2).                       IX1154.2
016100             15  FILLER           PIC X(5).                       IX1154.2
016200             15  ODO-NUMBER       PIC 9(4).                       IX1154.2
016300             15  FILLER           PIC X(5).                       IX1154.2
016400             15  XPROGRAM-NAME    PIC X(5).                       IX1154.2
016500             15  FILLER           PIC X(7).                       IX1154.2
016600             15  XRECORD-LENGTH   PIC 9(6).                       IX1154.2
016700             15  FILLER           PIC X(7).                       IX1154.2
016800             15  CHARS-OR-RECORDS  PIC X(2).                      IX1154.2
016900             15  FILLER            PIC X(1).                      IX1154.2
017000             15  XBLOCK-SIZE       PIC 9(4).                      IX1154.2
017100             15  FILLER            PIC X(6).                      IX1154.2
017200             15  RECORDS-IN-FILE   PIC 9(6).                      IX1154.2
017300             15  FILLER            PIC X(5).                      IX1154.2
017400             15  XFILE-ORGANIZATION  PIC X(2).                    IX1154.2
017500             15  FILLER              PIC X(6).                    IX1154.2
017600             15  XLABEL-TYPE         PIC X(1).                    IX1154.2
017700         10  FILE-RECORD-INFO-P121-240.                           IX1154.2
017800             15  FILLER              PIC X(8).                    IX1154.2
017900             15  XRECORD-KEY         PIC X(29).                   IX1154.2
018000             15  FILLER              PIC X(9).                    IX1154.2
018100             15  ALTERNATE-KEY1      PIC X(29).                   IX1154.2
018200             15  FILLER              PIC X(9).                    IX1154.2
018300             15  ALTERNATE-KEY2      PIC X(29).                   IX1154.2
018400             15  FILLER              PIC X(7).                    IX1154.2
018500                                                                  IX1154.2
018600 01  TEST-RESULTS.                                                IX1154.2
018700     02 FILLER                   PIC X      VALUE SPACE.          IX1154.2
018800     02 FEATURE                  PIC X(20)  VALUE SPACE.          IX1154.2
018900     02 FILLER                   PIC X      VALUE SPACE.          IX1154.2
019000     02 P-OR-F                   PIC X(5)   VALUE SPACE.          IX1154.2
019100     02 FILLER                   PIC X      VALUE SPACE.          IX1154.2
019200     02  PAR-NAME.                                                IX1154.2
019300       03 FILLER                 PIC X(19)  VALUE SPACE.          IX1154.2
019400       03  PARDOT-X              PIC X      VALUE SPACE.          IX1154.2
019500       03 DOTVALUE               PIC 99     VALUE ZERO.           IX1154.2
019600     02 FILLER                   PIC X(8)   VALUE SPACE.          IX1154.2
019700     02 RE-MARK                  PIC X(61).                       IX1154.2
019800 01  TEST-COMPUTED.                                               IX1154.2
019900     02 FILLER                   PIC X(30)  VALUE SPACE.          IX1154.2
020000     02 FILLER                   PIC X(17)  VALUE                 IX1154.2
020100            "       COMPUTED=".                                   IX1154.2
020200     02 COMPUTED-X.                                               IX1154.2
020300     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          IX1154.2
020400     03 COMPUTED-N               REDEFINES COMPUTED-A             IX1154.2
020500                                 PIC -9(9).9(9).                  IX1154.2
020600     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         IX1154.2
020700     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     IX1154.2
020800     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     IX1154.2
020900     03       CM-18V0 REDEFINES COMPUTED-A.                       IX1154.2
021000         04 COMPUTED-18V0                    PIC -9(18).          IX1154.2
021100         04 FILLER                           PIC X.               IX1154.2
021200     03 FILLER PIC X(50) VALUE SPACE.                             IX1154.2
021300 01  TEST-CORRECT.                                                IX1154.2
021400     02 FILLER PIC X(30) VALUE SPACE.                             IX1154.2
021500     02 FILLER PIC X(17) VALUE "       CORRECT =".                IX1154.2
021600     02 CORRECT-X.                                                IX1154.2
021700     03 CORRECT-A                  PIC X(20) VALUE SPACE.         IX1154.2
021800     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      IX1154.2
021900     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         IX1154.2
022000     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     IX1154.2
022100     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     IX1154.2
022200     03      CR-18V0 REDEFINES CORRECT-A.                         IX1154.2
022300         04 CORRECT-18V0                     PIC -9(18).          IX1154.2
022400         04 FILLER                           PIC X.               IX1154.2
022500     03 FILLER PIC X(2) VALUE SPACE.                              IX1154.2
022600     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     IX1154.2
022700 01  CCVS-C-1.                                                    IX1154.2
022800     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PAIX1154.2
022900-    "SS  PARAGRAPH-NAME                                          IX1154.2
023000-    "       REMARKS".                                            IX1154.2
023100     02 FILLER                     PIC X(20)    VALUE SPACE.      IX1154.2
023200 01  CCVS-C-2.                                                    IX1154.2
023300     02 FILLER                     PIC X        VALUE SPACE.      IX1154.2
023400     02 FILLER                     PIC X(6)     VALUE "TESTED".   IX1154.2
023500     02 FILLER                     PIC X(15)    VALUE SPACE.      IX1154.2
023600     02 FILLER                     PIC X(4)     VALUE "FAIL".     IX1154.2
023700     02 FILLER                     PIC X(94)    VALUE SPACE.      IX1154.2
023800 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       IX1154.2
023900 01  REC-CT                        PIC 99       VALUE ZERO.       IX1154.2
024000 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       IX1154.2
024100 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       IX1154.2
024200 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       IX1154.2
024300 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       IX1154.2
024400 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       IX1154.2
024500 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       IX1154.2
024600 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      IX1154.2
024700 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       IX1154.2
024800 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     IX1154.2
024900 01  CCVS-H-1.                                                    IX1154.2
025000     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1154.2
025100     02  FILLER                    PIC X(42)    VALUE             IX1154.2
025200     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 IX1154.2
025300     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1154.2
025400 01  CCVS-H-2A.                                                   IX1154.2
025500   02  FILLER                        PIC X(40)  VALUE SPACE.      IX1154.2
025600   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  IX1154.2
025700   02  FILLER                        PIC XXXX   VALUE             IX1154.2
025800     "4.2 ".                                                      IX1154.2
025900   02  FILLER                        PIC X(28)  VALUE             IX1154.2
026000            " COPY - NOT FOR DISTRIBUTION".                       IX1154.2
026100   02  FILLER                        PIC X(41)  VALUE SPACE.      IX1154.2
026200                                                                  IX1154.2
026300 01  CCVS-H-2B.                                                   IX1154.2
026400   02  FILLER                        PIC X(15)  VALUE             IX1154.2
026500            "TEST RESULT OF ".                                    IX1154.2
026600   02  TEST-ID                       PIC X(9).                    IX1154.2
026700   02  FILLER                        PIC X(4)   VALUE             IX1154.2
026800            " IN ".                                               IX1154.2
026900   02  FILLER                        PIC X(12)  VALUE             IX1154.2
027000     " HIGH       ".                                              IX1154.2
027100   02  FILLER                        PIC X(22)  VALUE             IX1154.2
027200            " LEVEL VALIDATION FOR ".                             IX1154.2
027300   02  FILLER                        PIC X(58)  VALUE             IX1154.2
027400     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1154.2
027500 01  CCVS-H-3.                                                    IX1154.2
027600     02  FILLER                      PIC X(34)  VALUE             IX1154.2
027700            " FOR OFFICIAL USE ONLY    ".                         IX1154.2
027800     02  FILLER                      PIC X(58)  VALUE             IX1154.2
027900     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1154.2
028000     02  FILLER                      PIC X(28)  VALUE             IX1154.2
028100            "  COPYRIGHT   1985 ".                                IX1154.2
028200 01  CCVS-E-1.                                                    IX1154.2
028300     02 FILLER                       PIC X(52)  VALUE SPACE.      IX1154.2
028400     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              IX1154.2
028500     02 ID-AGAIN                     PIC X(9).                    IX1154.2
028600     02 FILLER                       PIC X(45)  VALUE SPACES.     IX1154.2
028700 01  CCVS-E-2.                                                    IX1154.2
028800     02  FILLER                      PIC X(31)  VALUE SPACE.      IX1154.2
028900     02  FILLER                      PIC X(21)  VALUE SPACE.      IX1154.2
029000     02 CCVS-E-2-2.                                               IX1154.2
029100         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      IX1154.2
029200         03 FILLER                   PIC X      VALUE SPACE.      IX1154.2
029300         03 ENDER-DESC               PIC X(44)  VALUE             IX1154.2
029400            "ERRORS ENCOUNTERED".                                 IX1154.2
029500 01  CCVS-E-3.                                                    IX1154.2
029600     02  FILLER                      PIC X(22)  VALUE             IX1154.2
029700            " FOR OFFICIAL USE ONLY".                             IX1154.2
029800     02  FILLER                      PIC X(12)  VALUE SPACE.      IX1154.2
029900     02  FILLER                      PIC X(58)  VALUE             IX1154.2
030000     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1154.2
030100     02  FILLER                      PIC X(13)  VALUE SPACE.      IX1154.2
030200     02 FILLER                       PIC X(15)  VALUE             IX1154.2
030300             " COPYRIGHT 1985".                                   IX1154.2
030400 01  CCVS-E-4.                                                    IX1154.2
030500     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      IX1154.2
030600     02 FILLER                       PIC X(4)   VALUE " OF ".     IX1154.2
030700     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      IX1154.2
030800     02 FILLER                       PIC X(40)  VALUE             IX1154.2
030900      "  TESTS WERE EXECUTED SUCCESSFULLY".                       IX1154.2
031000 01  XXINFO.                                                      IX1154.2
031100     02 FILLER                       PIC X(19)  VALUE             IX1154.2
031200            "*** INFORMATION ***".                                IX1154.2
031300     02 INFO-TEXT.                                                IX1154.2
031400       04 FILLER                     PIC X(8)   VALUE SPACE.      IX1154.2
031500       04 XXCOMPUTED                 PIC X(20).                   IX1154.2
031600       04 FILLER                     PIC X(5)   VALUE SPACE.      IX1154.2
031700       04 XXCORRECT                  PIC X(20).                   IX1154.2
031800     02 INF-ANSI-REFERENCE           PIC X(48).                   IX1154.2
031900 01  HYPHEN-LINE.                                                 IX1154.2
032000     02 FILLER  PIC IS X VALUE IS SPACE.                          IX1154.2
032100     02 FILLER  PIC IS X(65)    VALUE IS "************************IX1154.2
032200-    "*****************************************".                 IX1154.2
032300     02 FILLER  PIC IS X(54)    VALUE IS "************************IX1154.2
032400-    "******************************".                            IX1154.2
032500 01  TEST-NO                         PIC 99.                      IX1154.2
032600 01  CCVS-PGM-ID                     PIC X(9)   VALUE             IX1154.2
032700     "IX115A".                                                    IX1154.2
032800 PROCEDURE DIVISION.                                              IX1154.2
032900 DECLARATIVES.                                                    IX1154.2
033000                                                                  IX1154.2
033100 SECT-IX105-0002 SECTION.                                         IX1154.2
033200     USE AFTER EXCEPTION PROCEDURE ON IX-FS3.                     IX1154.2
033300 INPUT-PROCESS.                                                   IX1154.2
033400     IF TEST-NO = 5                                               IX1154.2
033500        GO TO D-C-TEST-GF-01-1.                                   IX1154.2
033600     IF STATUS-TEST-10 EQUAL TO 1                                 IX1154.2
033700        IF  IX-FS3-STAT1 EQUAL TO "1"                             IX1154.2
033800            MOVE 1 TO EOF-FLAG                                    IX1154.2
033900        ELSE                                                      IX1154.2
034000           IF  IX-FS3-STAT1 GREATER THAN "1"                      IX1154.2
034100           MOVE 1 TO PERM-ERRORS.                                 IX1154.2
034200     GO TO DECL-EXIT.                                             IX1154.2
034300 D-C-TEST-GF-01-1.                                                IX1154.2
034400     IF IX-FS3-STATUS EQUAL TO "48"                               IX1154.2
034500         GO TO D-C-PASS-GF-01-0.                                  IX1154.2
034600 D-C-FAIL-GF-01-0.                                                IX1154.2
034700     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1154.2
034800     MOVE "48" TO CORRECT-X.                                      IX1154.2
034900     MOVE "IX-5, 1.3.4, (5) G" TO RE-MARK.                        IX1154.2
035000     PERFORM D-FAIL.                                              IX1154.2
035100     GO TO D-C-WRITE-GF-01-0.                                     IX1154.2
035200 D-C-PASS-GF-01-0.                                                IX1154.2
035300     PERFORM D-PASS.                                              IX1154.2
035400 D-C-WRITE-GF-01-0.                                               IX1154.2
035500     PERFORM D-PRINT-DETAIL.                                      IX1154.2
035600 D-CLOSE-FILES.                                                   IX1154.2
035700P    OPEN I-O RAW-DATA.                                           IX1154.2
035800P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1154.2
035900P    READ RAW-DATA INVALID KEY GO TO D-END-E-2.                   IX1154.2
036000P    MOVE "OK.     " TO C-ABORT.                                  IX1154.2
036100P    MOVE PASS-COUNTER TO C-OK.                                   IX1154.2
036200P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1154.2
036300P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1154.2
036400P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1154.2
036500P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1154.2
036600P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO D-END-E-2.           IX1154.2
036700PD-END-E-2.                                                       IX1154.2
036800P    CLOSE RAW-DATA.                                              IX1154.2
036900     PERFORM D-END-ROUTINE THRU D-END-ROUTINE-13.                 IX1154.2
037000     CLOSE PRINT-FILE.                                            IX1154.2
037100 D-TERMINATE-CCVS.                                                IX1154.2
037200S    EXIT PROGRAM.                                                IX1154.2
037300SD-TERMINATE-CALL.                                                IX1154.2
037400     STOP     RUN.                                                IX1154.2
037500 D-PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.         IX1154.2
037600 D-FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.        IX1154.2
037700 D-PRINT-DETAIL.                                                  IX1154.2
037800     IF   REC-CT NOT EQUAL TO ZERO                                IX1154.2
037900          MOVE "." TO PARDOT-X                                    IX1154.2
038000          MOVE REC-CT TO DOTVALUE.                                IX1154.2
038100     MOVE TEST-RESULTS TO PRINT-REC.                              IX1154.2
038200     PERFORM D-WRITE-LINE.                                        IX1154.2
038300     IF   P-OR-F EQUAL TO "FAIL*"                                 IX1154.2
038400          PERFORM D-WRITE-LINE                                    IX1154.2
038500          PERFORM D-FAIL-ROUTINE THRU D-FAIL-ROUTINE-EX           IX1154.2
038600     ELSE                                                         IX1154.2
038700          PERFORM D-BAIL-OUT THRU D-BAIL-OUT-EX.                  IX1154.2
038800     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1154.2
038900     MOVE SPACE TO CORRECT-X.                                     IX1154.2
039000     IF   REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.           IX1154.2
039100     MOVE SPACE TO RE-MARK.                                       IX1154.2
039200 D-END-ROUTINE.                                                   IX1154.2
039300     MOVE HYPHEN-LINE TO DUMMY-RECORD.                            IX1154.2
039400     PERFORM D-WRITE-LINE 5 TIMES.                                IX1154.2
039500 D-END-RTN-EXIT.                                                  IX1154.2
039600     MOVE CCVS-E-1 TO DUMMY-RECORD.                               IX1154.2
039700     PERFORM D-WRITE-LINE 2 TIMES.                                IX1154.2
039800 D-END-ROUTINE-1.                                                 IX1154.2
039900     ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO       IX1154.2
040000     ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.                IX1154.2
040100     ADD PASS-COUNTER TO ERROR-HOLD.                              IX1154.2
040200     MOVE PASS-COUNTER TO CCVS-E-4-1.                             IX1154.2
040300     MOVE ERROR-HOLD TO CCVS-E-4-2.                               IX1154.2
040400     MOVE CCVS-E-4 TO CCVS-E-2-2.                                 IX1154.2
040500     MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM D-WRITE-LINE.          IX1154.2
040600  D-END-ROUTINE-12.                                               IX1154.2
040700     MOVE "TEST(S) FAILED" TO ENDER-DESC.                         IX1154.2
040800     IF  ERROR-COUNTER IS EQUAL TO ZERO                           IX1154.2
040900         MOVE "NO " TO ERROR-TOTAL                                IX1154.2
041000     ELSE                                                         IX1154.2
041100         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1154.2
041200     MOVE    CCVS-E-2 TO DUMMY-RECORD.                            IX1154.2
041300     PERFORM D-WRITE-LINE.                                        IX1154.2
041400 D-END-ROUTINE-13.                                                IX1154.2
041500     IF  DELETE-COUNTER IS EQUAL TO ZERO                          IX1154.2
041600         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1154.2
041700         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1154.2
041800     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1154.2
041900     MOVE CCVS-E-2 TO DUMMY-RECORD.                               IX1154.2
042000     PERFORM D-WRITE-LINE.                                        IX1154.2
042100     IF   INSPECT-COUNTER EQUAL TO ZERO                           IX1154.2
042200          MOVE "NO " TO ERROR-TOTAL                               IX1154.2
042300     ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                    IX1154.2
042400     MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.             IX1154.2
042500     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1154.2
042600     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1154.2
042700 D-WRITE-LINE.                                                    IX1154.2
042800     ADD 1 TO RECORD-COUNT.                                       IX1154.2
042900Y    IF RECORD-COUNT GREATER 42                                   IX1154.2
043000Y       MOVE DUMMY-RECORD TO DUMMY-HOLD                           IX1154.2
043100Y       MOVE SPACE TO DUMMY-RECORD                                IX1154.2
043200Y       WRITE DUMMY-RECORD AFTER ADVANCING PAGE                   IX1154.2
043300Y       MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1154.2
043400Y       MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1154.2
043500Y       MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1154.2
043600Y       MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1154.2
043700Y       MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1154.2
043800Y       MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1154.2
043900Y       MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM D-WRT-LN         IX1154.2
044000Y       MOVE DUMMY-HOLD TO DUMMY-RECORD                           IX1154.2
044100Y       MOVE ZERO TO RECORD-COUNT.                                IX1154.2
044200     PERFORM D-WRT-LN.                                            IX1154.2
044300 D-WRT-LN.                                                        IX1154.2
044400     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1154.2
044500     MOVE SPACE TO DUMMY-RECORD.                                  IX1154.2
044600 D-FAIL-ROUTINE.                                                  IX1154.2
044700     IF   COMPUTED-X NOT EQUAL TO SPACE                           IX1154.2
044800          GO TO D-FAIL-ROUTINE-WRITE.                             IX1154.2
044900     IF   CORRECT-X NOT EQUAL TO SPACE GO TO D-FAIL-ROUTINE-WRITE.IX1154.2
045000     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1154.2
045100     MOVE "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.    IX1154.2
045200     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1154.2
045300     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1154.2
045400     GO TO D-FAIL-ROUTINE-EX.                                     IX1154.2
045500 D-FAIL-ROUTINE-WRITE.                                            IX1154.2
045600     MOVE TEST-COMPUTED TO PRINT-REC PERFORM D-WRITE-LINE         IX1154.2
045700     MOVE ANSI-REFERENCE TO COR-ANSI-REFERENCE.                   IX1154.2
045800     MOVE TEST-CORRECT TO PRINT-REC PERFORM D-WRITE-LINE 2 TIMES. IX1154.2
045900     MOVE SPACES TO COR-ANSI-REFERENCE.                           IX1154.2
046000 D-FAIL-ROUTINE-EX. EXIT.                                         IX1154.2
046100 D-BAIL-OUT.                                                      IX1154.2
046200     IF  COMPUTED-A NOT EQUAL TO SPACE GO TO D-BAIL-OUT-WRITE.    IX1154.2
046300     IF  CORRECT-A EQUAL TO SPACE GO TO D-BAIL-OUT-EX.            IX1154.2
046400 D-BAIL-OUT-WRITE.                                                IX1154.2
046500     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1154.2
046600     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1154.2
046700     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1154.2
046800     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1154.2
046900 D-BAIL-OUT-EX. EXIT.                                             IX1154.2
047000 DECL-EXIT.  EXIT.                                                IX1154.2
047100 END DECLARATIVES.                                                IX1154.2
047200                                                                  IX1154.2
047300                                                                  IX1154.2
047400 CCVS1 SECTION.                                                   IX1154.2
047500 OPEN-FILES.                                                      IX1154.2
047600P    OPEN I-O RAW-DATA.                                           IX1154.2
047700P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1154.2
047800P    READ RAW-DATA INVALID KEY GO TO END-E-1.                     IX1154.2
047900P    MOVE "ABORTED " TO C-ABORT.                                  IX1154.2
048000P    ADD 1 TO C-NO-OF-TESTS.                                      IX1154.2
048100P    ACCEPT C-DATE  FROM DATE.                                    IX1154.2
048200P    ACCEPT C-TIME  FROM TIME.                                    IX1154.2
048300P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-1.             IX1154.2
048400PEND-E-1.                                                         IX1154.2
048500P    CLOSE RAW-DATA.                                              IX1154.2
048600     OPEN    OUTPUT PRINT-FILE.                                   IX1154.2
048700     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  IX1154.2
048800     MOVE    SPACE TO TEST-RESULTS.                               IX1154.2
048900     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              IX1154.2
049000     MOVE    ZERO TO REC-SKL-SUB.                                 IX1154.2
049100     PERFORM CCVS-INIT-FILE 9 TIMES.                              IX1154.2
049200 CCVS-INIT-FILE.                                                  IX1154.2
049300     ADD     1 TO REC-SKL-SUB.                                    IX1154.2
049400     MOVE    FILE-RECORD-INFO-SKELETON                            IX1154.2
049500          TO FILE-RECORD-INFO (REC-SKL-SUB).                      IX1154.2
049600 CCVS-INIT-EXIT.                                                  IX1154.2
049700     GO TO CCVS1-EXIT.                                            IX1154.2
049800 CLOSE-FILES.                                                     IX1154.2
049900P    OPEN I-O RAW-DATA.                                           IX1154.2
050000P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1154.2
050100P    READ RAW-DATA INVALID KEY GO TO END-E-2.                     IX1154.2
050200P    MOVE "OK.     " TO C-ABORT.                                  IX1154.2
050300P    MOVE PASS-COUNTER TO C-OK.                                   IX1154.2
050400P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1154.2
050500P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1154.2
050600P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1154.2
050700P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1154.2
050800P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-2.             IX1154.2
050900PEND-E-2.                                                         IX1154.2
051000P    CLOSE RAW-DATA.                                              IX1154.2
051100     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   IX1154.2
051200 TERMINATE-CCVS.                                                  IX1154.2
051300S    EXIT PROGRAM.                                                IX1154.2
051400STERMINATE-CALL.                                                  IX1154.2
051500     STOP     RUN.                                                IX1154.2
051600 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         IX1154.2
051700 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           IX1154.2
051800 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          IX1154.2
051900 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      IX1154.2
052000     MOVE "****TEST DELETED****" TO RE-MARK.                      IX1154.2
052100 PRINT-DETAIL.                                                    IX1154.2
052200     IF REC-CT NOT EQUAL TO ZERO                                  IX1154.2
052300             MOVE "." TO PARDOT-X                                 IX1154.2
052400             MOVE REC-CT TO DOTVALUE.                             IX1154.2
052500     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      IX1154.2
052600     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               IX1154.2
052700        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 IX1154.2
052800          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 IX1154.2
052900     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1154.2
053000     MOVE SPACE TO CORRECT-X.                                     IX1154.2
053100     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         IX1154.2
053200     MOVE     SPACE TO RE-MARK.                                   IX1154.2
053300 HEAD-ROUTINE.                                                    IX1154.2
053400     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1154.2
053500     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1154.2
053600     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1154.2
053700     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1154.2
053800 COLUMN-NAMES-ROUTINE.                                            IX1154.2
053900     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1154.2
054000     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1154.2
054100     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        IX1154.2
054200 END-ROUTINE.                                                     IX1154.2
054300     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.IX1154.2
054400 END-RTN-EXIT.                                                    IX1154.2
054500     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1154.2
054600 END-ROUTINE-1.                                                   IX1154.2
054700      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      IX1154.2
054800      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               IX1154.2
054900      ADD PASS-COUNTER TO ERROR-HOLD.                             IX1154.2
055000*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   IX1154.2
055100      MOVE PASS-COUNTER TO CCVS-E-4-1.                            IX1154.2
055200      MOVE ERROR-HOLD TO CCVS-E-4-2.                              IX1154.2
055300      MOVE CCVS-E-4 TO CCVS-E-2-2.                                IX1154.2
055400      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           IX1154.2
055500  END-ROUTINE-12.                                                 IX1154.2
055600      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        IX1154.2
055700     IF       ERROR-COUNTER IS EQUAL TO ZERO                      IX1154.2
055800         MOVE "NO " TO ERROR-TOTAL                                IX1154.2
055900         ELSE                                                     IX1154.2
056000         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1154.2
056100     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           IX1154.2
056200     PERFORM WRITE-LINE.                                          IX1154.2
056300 END-ROUTINE-13.                                                  IX1154.2
056400     IF DELETE-COUNTER IS EQUAL TO ZERO                           IX1154.2
056500         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1154.2
056600         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1154.2
056700     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1154.2
056800     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1154.2
056900      IF   INSPECT-COUNTER EQUAL TO ZERO                          IX1154.2
057000          MOVE "NO " TO ERROR-TOTAL                               IX1154.2
057100      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   IX1154.2
057200      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            IX1154.2
057300      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          IX1154.2
057400     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1154.2
057500 WRITE-LINE.                                                      IX1154.2
057600     ADD 1 TO RECORD-COUNT.                                       IX1154.2
057700Y    IF RECORD-COUNT GREATER 42                                   IX1154.2
057800Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          IX1154.2
057900Y        MOVE SPACE TO DUMMY-RECORD                               IX1154.2
058000Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  IX1154.2
058100Y        MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1154.2
058200Y        MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1154.2
058300Y        MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1154.2
058400Y        MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1154.2
058500Y        MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM WRT-LN            IX1154.2
058600Y        MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM WRT-LN            IX1154.2
058700Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          IX1154.2
058800Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          IX1154.2
058900Y        MOVE ZERO TO RECORD-COUNT.                               IX1154.2
059000     PERFORM WRT-LN.                                              IX1154.2
059100 WRT-LN.                                                          IX1154.2
059200     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1154.2
059300     MOVE SPACE TO DUMMY-RECORD.                                  IX1154.2
059400 BLANK-LINE-PRINT.                                                IX1154.2
059500     PERFORM WRT-LN.                                              IX1154.2
059600 FAIL-ROUTINE.                                                    IX1154.2
059700     IF     COMPUTED-X NOT EQUAL TO SPACE                         IX1154.2
059800            GO TO   FAIL-ROUTINE-WRITE.                           IX1154.2
059900     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.IX1154.2
060000     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1154.2
060100     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   IX1154.2
060200     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1154.2
060300     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1154.2
060400     GO TO  FAIL-ROUTINE-EX.                                      IX1154.2
060500 FAIL-ROUTINE-WRITE.                                              IX1154.2
060600     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         IX1154.2
060700     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 IX1154.2
060800     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. IX1154.2
060900     MOVE   SPACES TO COR-ANSI-REFERENCE.                         IX1154.2
061000 FAIL-ROUTINE-EX. EXIT.                                           IX1154.2
061100 BAIL-OUT.                                                        IX1154.2
061200     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   IX1154.2
061300     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           IX1154.2
061400 BAIL-OUT-WRITE.                                                  IX1154.2
061500     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1154.2
061600     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1154.2
061700     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1154.2
061800     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1154.2
061900 BAIL-OUT-EX. EXIT.                                               IX1154.2
062000 CCVS1-EXIT.                                                      IX1154.2
062100     EXIT.                                                        IX1154.2
062200                                                                  IX1154.2
062300 SECT-IX115A-0003 SECTION.                                        IX1154.2
062400 SEQ-INIT-010.                                                    IX1154.2
062500     MOVE ZERO TO TEST-NO.                                        IX1154.2
062600     MOVE "IX-FS3" TO XFILE-NAME (1).                             IX1154.2
062700     MOVE "R1-F-G" TO XRECORD-NAME (1).                           IX1154.2
062800     MOVE CCVS-PGM-ID TO XPROGRAM-NAME (1).                       IX1154.2
062900     MOVE 000240 TO XRECORD-LENGTH (1).                           IX1154.2
063000     MOVE "RC" TO CHARS-OR-RECORDS (1).                           IX1154.2
063100     MOVE 0002 TO XBLOCK-SIZE (1).                                IX1154.2
063200     MOVE 000050 TO RECORDS-IN-FILE (1).                          IX1154.2
063300     MOVE "IX" TO XFILE-ORGANIZATION (1).                         IX1154.2
063400     MOVE "S" TO XLABEL-TYPE (1).                                 IX1154.2
063500     MOVE 000001 TO XRECORD-NUMBER (1).                           IX1154.2
063600     MOVE 0 TO COUNT-OF-RECS.                                     IX1154.2
063700                                                                  IX1154.2
063800******************************************************************IX1154.2
063900*   TEST  1                                                      *IX1154.2
064000*         OPEN I-O                                                IX1154.2
064100*         IX-3, 1.3.4 (1) A                                      *IX1154.2
064200*    STATUS 00 CHECK ON OUTPUT FILE IX-FS3                       *IX1154.2
064300*    THE OPEN   STATEMENT IS SUCCESSFULLY EXECUTED               *IX1154.2
064400******************************************************************IX1154.2
064500 OPN-INIT-GF-01-0.                                                IX1154.2
064600     MOVE 1 TO STATUS-TEST-00.                                    IX1154.2
064700     MOVE SPACES TO IX-FS3-STATUS.                                IX1154.2
064800     MOVE "OPEN I-O   : 00 EXP." TO FEATURE.                      IX1154.2
064900     MOVE "OPN-TEST-GF-01-0" TO PAR-NAME.                         IX1154.2
065000     OPEN                                                         IX1154.2
065100        I-O    IX-FS3.                                            IX1154.2
065200     IF IX-FS3-STATUS EQUAL TO "00"                               IX1154.2
065300         GO TO OPN-PASS-GF-01-0.                                  IX1154.2
065400 OPN-FAIL-GF-01-0.                                                IX1154.2
065500     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1154.2
065600     PERFORM FAIL.                                                IX1154.2
065700     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1154.2
065800     MOVE "00" TO CORRECT-X.                                      IX1154.2
065900     GO TO OPN-WRITE-GF-01-0.                                     IX1154.2
066000 OPN-PASS-GF-01-0.                                                IX1154.2
066100     PERFORM PASS.                                                IX1154.2
066200 OPN-WRITE-GF-01-0.                                               IX1154.2
066300     PERFORM PRINT-DETAIL.                                        IX1154.2
066400******************************************************************IX1154.2
066500*   TEST  4                                                      *IX1154.2
066600*         CLOSE I-O                       00 EXPECTED            *IX1154.2
066700*         IX-3, 1.3.4 (1) A                                      *IX1154.2
066800******************************************************************IX1154.2
066900 CLO-INIT-GF-01-0.                                                IX1154.2
067000     MOVE SPACES TO IX-FS3-STATUS.                                IX1154.2
067100     MOVE "CLOSE I-O   :00 EXP." TO FEATURE.                      IX1154.2
067200     MOVE "CLO-TEST-GF-01-0" TO PAR-NAME.                         IX1154.2
067300 CLO-TEST-GF-01-0.                                                IX1154.2
067400     CLOSE IX-FS3.                                                IX1154.2
067500     IF IX-FS3-STATUS = "00"                                      IX1154.2
067600         GO TO CLO-PASS-GF-01-0.                                  IX1154.2
067700 CLO-FAIL-GF-01-0.                                                IX1154.2
067800     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1154.2
067900     PERFORM FAIL.                                                IX1154.2
068000     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1154.2
068100     MOVE "00" TO CORRECT-X.                                      IX1154.2
068200     GO TO CLO-WRITE-GF-01-0.                                     IX1154.2
068300 CLO-PASS-GF-01-0.                                                IX1154.2
068400     PERFORM PASS.                                                IX1154.2
068500 CLO-WRITE-GF-01-0.                                               IX1154.2
068600     PERFORM PRINT-DETAIL.                                        IX1154.2
068700                                                                  IX1154.2
068800******************************************************************IX1154.2
068900*    A INDEXED FILE WITH 50 RECORDS HAS BEEN CREATED.            *IX1154.2
069000******************************************************************IX1154.2
069100                                                                  IX1154.2
069200******************************************************************IX1154.2
069300*   TEST  5                                                      *IX1154.2
069400*         WRITE...  A FILE NOT IN THE OPEN MODE                  *IX1154.2
069500*         FILE STATUS 48 EXPECTED IX-5, 1.3.4 (5) G              *IX1154.2
069600******************************************************************IX1154.2
069700 WRI-TEST-GF-01-0.                                                IX1154.2
069800     MOVE  5 TO TEST-NO.                                          IX1154.2
069900     MOVE SPACES TO IX-FS3-STATUS.                                IX1154.2
070000     MOVE "WRITE.       48 EXP." TO FEATURE                       IX1154.2
070100     MOVE "WRI-TEST-GF-01-0" TO PAR-NAME.                         IX1154.2
070200     WRITE IX-FS3R1-F-G-240.                                      IX1154.2
070300 WRI-TEST-GF-01-1.                                                IX1154.2
070400     IF IX-FS3-STATUS EQUAL TO "48"                               IX1154.2
070500        MOVE "SHOULD HAVE EXECUTED DECLARATIVES IX-3,1.3.4(4)"    IX1154.2
070600          TO RE-MARK                                              IX1154.2
070700        GO TO WRI-WRITE-GF-01-0.                                  IX1154.2
070800 WRI-FAIL-GF-01-0.                                                IX1154.2
070900     MOVE "IX-5, 1.3.4, (5) G" TO RE-MARK.                        IX1154.2
071000 WRI-WRITE-GF-01-0.                                               IX1154.2
071100     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1154.2
071200     MOVE "48" TO CORRECT-X.                                      IX1154.2
071300     PERFORM FAIL.                                                IX1154.2
071400     PERFORM PRINT-DETAIL.                                        IX1154.2
071500                                                                  IX1154.2
071600 TERMINATE-ROUTINE.                                               IX1154.2
071700     EXIT.                                                        IX1154.2
071800                                                                  IX1154.2
071900 CCVS-EXIT SECTION.                                               IX1154.2
072000 CCVS-999999.                                                     IX1154.2
072100     GO TO CLOSE-FILES.                                           IX1154.2
       END PROGRAM IX115A.
000100 IDENTIFICATION DIVISION.                                         IX1164.2
000200 PROGRAM-ID.                                                      IX1164.2
000300     IX116A.                                                      IX1164.2
000400****************************************************************  IX1164.2
000500*                                                              *  IX1164.2
000600*    VALIDATION FOR:-                                          *  IX1164.2
000700*                                                              *  IX1164.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1164.2
000900*                                                              *  IX1164.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1164.2
001100*                                                              *  IX1164.2
001200****************************************************************  IX1164.2
001300*                                                                 IX1164.2
001400*    THIS ROUTINE USES THE MASS STORAGE FILE IX-FS3 CREATED IN    IX1164.2
001500*    IX113A.                                                      IX1164.2
001600*    THE FILE IS OPENED I-O AND THE STATUS CHECKED (00 EXPECTED), IX1164.2
001700*    CLOSED AND THE STATUS CHECKED (00 EXPECTED) THEN AN ATTEMPT  IX1164.2
001800*    IS MADE TO DELETE A RECORD, AT WHICH POINT THE DECLARATIVES  IX1164.2
001900*    SHOULD BE ACTIONED AND THE FILE STATUS SHOULD BE 49 .        IX1164.2
002000*                                                                 IX1164.2
002100*    STANDARD REFERENCE IX-5, 1.3.4 (5) H                         IX1164.2
002200*                                                                 IX1164.2
002300*    X-CARDS USED IN THIS PROGRAM:                                IX1164.2
002400*                                                                 IX1164.2
002500*                 XXXXX024                                        IX1164.2
002600*                 XXXXX055.                                       IX1164.2
002700*         P       XXXXX062.                                       IX1164.2
002800*                 XXXXX082.                                       IX1164.2
002900*                 XXXXX083.                                       IX1164.2
003000*         C       XXXXX084                                        IX1164.2
003100*                                                                 IX1164.2
003200*                                                                 IX1164.2
003300 ENVIRONMENT DIVISION.                                            IX1164.2
003400 CONFIGURATION SECTION.                                           IX1164.2
003500 SOURCE-COMPUTER.                                                 IX1164.2
003600     XXXXX082.                                                    IX1164.2
003700 OBJECT-COMPUTER.                                                 IX1164.2
003800     XXXXX083.                                                    IX1164.2
003900 INPUT-OUTPUT SECTION.                                            IX1164.2
004000 FILE-CONTROL.                                                    IX1164.2
004100P    SELECT RAW-DATA   ASSIGN TO                                  IX1164.2
004200P    XXXXX062                                                     IX1164.2
004300P           ORGANIZATION IS INDEXED                               IX1164.2
004400P           ACCESS MODE IS RANDOM                                 IX1164.2
004500P           RECORD KEY IS RAW-DATA-KEY.                           IX1164.2
004600*                                                                 IX1164.2
004700     SELECT PRINT-FILE ASSIGN TO                                  IX1164.2
004800     XXXXX055.                                                    IX1164.2
004900*                                                                 IX1164.2
005000     SELECT IX-FS3 ASSIGN                                         IX1164.2
005100     XXXXX024                                                     IX1164.2
005200     ORGANIZATION IS INDEXED                                      IX1164.2
005300     ACCESS MODE IS SEQUENTIAL                                    IX1164.2
005400     RECORD KEY IS IX-FS3-KEY                                     IX1164.2
005500     FILE STATUS IS IX-FS3-STATUS.                                IX1164.2
005600                                                                  IX1164.2
005700 DATA DIVISION.                                                   IX1164.2
005800                                                                  IX1164.2
005900 FILE SECTION.                                                    IX1164.2
006000P                                                                 IX1164.2
006100PFD  RAW-DATA.                                                    IX1164.2
006200P                                                                 IX1164.2
006300P01  RAW-DATA-SATZ.                                               IX1164.2
006400P    05  RAW-DATA-KEY        PIC X(6).                            IX1164.2
006500P    05  C-DATE              PIC 9(6).                            IX1164.2
006600P    05  C-TIME              PIC 9(8).                            IX1164.2
006700P    05  C-NO-OF-TESTS       PIC 99.                              IX1164.2
006800P    05  C-OK                PIC 999.                             IX1164.2
006900P    05  C-ALL               PIC 999.                             IX1164.2
007000P    05  C-FAIL              PIC 999.                             IX1164.2
007100P    05  C-DELETED           PIC 999.                             IX1164.2
007200P    05  C-INSPECT           PIC 999.                             IX1164.2
007300P    05  C-NOTE              PIC X(13).                           IX1164.2
007400P    05  C-INDENT            PIC X.                               IX1164.2
007500P    05  C-ABORT             PIC X(8).                            IX1164.2
007600                                                                  IX1164.2
007700 FD  PRINT-FILE.                                                  IX1164.2
007800                                                                  IX1164.2
007900 01  PRINT-REC               PIC X(120).                          IX1164.2
008000                                                                  IX1164.2
008100 01  DUMMY-RECORD            PIC X(120).                          IX1164.2
008200                                                                  IX1164.2
008300 FD  IX-FS3                                                       IX1164.2
008400C       DATA RECORDS IX-FS3R1-F-G-240                             IX1164.2
008500C       LABEL RECORD STANDARD                                     IX1164.2
008600        RECORD 240                                                IX1164.2
008700        BLOCK CONTAINS 2 RECORDS.                                 IX1164.2
008800                                                                  IX1164.2
008900 01  IX-FS3R1-F-G-240.                                            IX1164.2
009000     05  IX-FS3-REC-120      PIC X(120).                          IX1164.2
009100     05  IX-FS3-REC-120-240.                                      IX1164.2
009200         10  FILLER          PIC X(8).                            IX1164.2
009300         10  IX-FS3-KEY      PIC X(29).                           IX1164.2
009400         10  FILLER          PIC X(9).                            IX1164.2
009500         10  IX-FS3-ALTER-KEY  PIC X(29).                         IX1164.2
009600         10  FILLER            PIC X(45).                         IX1164.2
009700                                                                  IX1164.2
009800                                                                  IX1164.2
009900 WORKING-STORAGE SECTION.                                         IX1164.2
010000                                                                  IX1164.2
010100 01  GRP-0101.                                                    IX1164.2
010200     05  FILLER              PIC X(10) VALUE "RECORD-KEY".        IX1164.2
010300     05  GRP-0101-KEY        PIC 9(9)  VALUE ZERO.                IX1164.2
010400     05  FILLER              PIC X(10) VALUE "END-OF-KEY".        IX1164.2
010500                                                                  IX1164.2
010600 01  GRP-0102.                                                    IX1164.2
010700     05  FILLER              PIC X(10) VALUE "ALTERN-KEY".        IX1164.2
010800     05  GRP-0102-KEY        PIC 9(9)  VALUE ZERO.                IX1164.2
010900     05  FILLER              PIC X(10) VALUE "END-AL-KEY".        IX1164.2
011000                                                                  IX1164.2
011100 01  WRK-CS-09V00            PIC S9(9) COMP VALUE ZERO.           IX1164.2
011200                                                                  IX1164.2
011300 01  EOF-FLAG                PIC 9 VALUE ZERO.                    IX1164.2
011400                                                                  IX1164.2
011500 01  RECORDS-IN-ERROR        PIC S9(5) COMP VALUE ZERO.           IX1164.2
011600                                                                  IX1164.2
011700 01  ERROR-FLAG              PIC 9 VALUE ZERO.                    IX1164.2
011800                                                                  IX1164.2
011900 01  PERM-ERRORS             PIC S9(5) COMP VALUE ZERO.           IX1164.2
012000                                                                  IX1164.2
012100 01  STATUS-TEST-00          PIC 9 VALUE ZERO.                    IX1164.2
012200                                                                  IX1164.2
012300 01  STATUS-TEST-10          PIC 9 VALUE ZERO.                    IX1164.2
012400 01  STATUS-TEST-READ        PIC 9 VALUE ZERO.                    IX1164.2
012500                                                                  IX1164.2
012600 01  IX-FS3-STATUS.                                               IX1164.2
012700     05  IX-FS3-STAT1        PIC X.                               IX1164.2
012800     05  IX-FS3-STAT2        PIC X.                               IX1164.2
012900                                                                  IX1164.2
013000 01  COUNT-OF-RECS           PIC 9(5).                            IX1164.2
013100                                                                  IX1164.2
013200 01  COUNT-OF-RECORDS REDEFINES COUNT-OF-RECS  PIC 9(5).          IX1164.2
013300                                                                  IX1164.2
013400 01  FILE-RECORD-INFORMATION-REC.                                 IX1164.2
013500     05  FILE-RECORD-INFO-SKELETON.                               IX1164.2
013600         10  FILLER          PIC X(48) VALUE                      IX1164.2
013700              "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00". IX1164.2
013800         10  FILLER          PIC X(46) VALUE                      IX1164.2
013900                ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000". IX1164.2
014000         10  FILLER          PIC X(26) VALUE                      IX1164.2
014100                                    ",LFIL=000000,ORG=  ,LBLR= ". IX1164.2
014200         10  FILLER          PIC X(37) VALUE                      IX1164.2
014300                         ",RECKEY=                             ". IX1164.2
014400         10  FILLER          PIC X(38) VALUE                      IX1164.2
014500                        ",ALTKEY1=                             ". IX1164.2
014600         10  FILLER          PIC X(38) VALUE                      IX1164.2
014700                        ",ALTKEY2=                             ". IX1164.2
014800         10  FILLER          PIC X(7) VALUE SPACE.                IX1164.2
014900     05  FILE-RECORD-INFO             OCCURS 10.                  IX1164.2
015000         10  FILE-RECORD-INFO-P1-120.                             IX1164.2
015100             15  FILLER      PIC X(5).                            IX1164.2
015200             15  XFILE-NAME  PIC X(6).                            IX1164.2
015300             15  FILLER      PIC X(8).                            IX1164.2
015400             15  XRECORD-NAME  PIC X(6).                          IX1164.2
015500             15  FILLER        PIC X(1).                          IX1164.2
015600             15  REELUNIT-NUMBER  PIC 9(1).                       IX1164.2
015700             15  FILLER           PIC X(7).                       IX1164.2
015800             15  XRECORD-NUMBER   PIC 9(6).                       IX1164.2
015900             15  FILLER           PIC X(6).                       IX1164.2
016000             15  UPDATE-NUMBER    PIC 9(2).                       IX1164.2
016100             15  FILLER           PIC X(5).                       IX1164.2
016200             15  ODO-NUMBER       PIC 9(4).                       IX1164.2
016300             15  FILLER           PIC X(5).                       IX1164.2
016400             15  XPROGRAM-NAME    PIC X(5).                       IX1164.2
016500             15  FILLER           PIC X(7).                       IX1164.2
016600             15  XRECORD-LENGTH   PIC 9(6).                       IX1164.2
016700             15  FILLER           PIC X(7).                       IX1164.2
016800             15  CHARS-OR-RECORDS  PIC X(2).                      IX1164.2
016900             15  FILLER            PIC X(1).                      IX1164.2
017000             15  XBLOCK-SIZE       PIC 9(4).                      IX1164.2
017100             15  FILLER            PIC X(6).                      IX1164.2
017200             15  RECORDS-IN-FILE   PIC 9(6).                      IX1164.2
017300             15  FILLER            PIC X(5).                      IX1164.2
017400             15  XFILE-ORGANIZATION  PIC X(2).                    IX1164.2
017500             15  FILLER              PIC X(6).                    IX1164.2
017600             15  XLABEL-TYPE         PIC X(1).                    IX1164.2
017700         10  FILE-RECORD-INFO-P121-240.                           IX1164.2
017800             15  FILLER              PIC X(8).                    IX1164.2
017900             15  XRECORD-KEY         PIC X(29).                   IX1164.2
018000             15  FILLER              PIC X(9).                    IX1164.2
018100             15  ALTERNATE-KEY1      PIC X(29).                   IX1164.2
018200             15  FILLER              PIC X(9).                    IX1164.2
018300             15  ALTERNATE-KEY2      PIC X(29).                   IX1164.2
018400             15  FILLER              PIC X(7).                    IX1164.2
018500                                                                  IX1164.2
018600 01  TEST-RESULTS.                                                IX1164.2
018700     02 FILLER                   PIC X      VALUE SPACE.          IX1164.2
018800     02 FEATURE                  PIC X(20)  VALUE SPACE.          IX1164.2
018900     02 FILLER                   PIC X      VALUE SPACE.          IX1164.2
019000     02 P-OR-F                   PIC X(5)   VALUE SPACE.          IX1164.2
019100     02 FILLER                   PIC X      VALUE SPACE.          IX1164.2
019200     02  PAR-NAME.                                                IX1164.2
019300       03 FILLER                 PIC X(19)  VALUE SPACE.          IX1164.2
019400       03  PARDOT-X              PIC X      VALUE SPACE.          IX1164.2
019500       03 DOTVALUE               PIC 99     VALUE ZERO.           IX1164.2
019600     02 FILLER                   PIC X(8)   VALUE SPACE.          IX1164.2
019700     02 RE-MARK                  PIC X(61).                       IX1164.2
019800 01  TEST-COMPUTED.                                               IX1164.2
019900     02 FILLER                   PIC X(30)  VALUE SPACE.          IX1164.2
020000     02 FILLER                   PIC X(17)  VALUE                 IX1164.2
020100            "       COMPUTED=".                                   IX1164.2
020200     02 COMPUTED-X.                                               IX1164.2
020300     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          IX1164.2
020400     03 COMPUTED-N               REDEFINES COMPUTED-A             IX1164.2
020500                                 PIC -9(9).9(9).                  IX1164.2
020600     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         IX1164.2
020700     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     IX1164.2
020800     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     IX1164.2
020900     03       CM-18V0 REDEFINES COMPUTED-A.                       IX1164.2
021000         04 COMPUTED-18V0                    PIC -9(18).          IX1164.2
021100         04 FILLER                           PIC X.               IX1164.2
021200     03 FILLER PIC X(50) VALUE SPACE.                             IX1164.2
021300 01  TEST-CORRECT.                                                IX1164.2
021400     02 FILLER PIC X(30) VALUE SPACE.                             IX1164.2
021500     02 FILLER PIC X(17) VALUE "       CORRECT =".                IX1164.2
021600     02 CORRECT-X.                                                IX1164.2
021700     03 CORRECT-A                  PIC X(20) VALUE SPACE.         IX1164.2
021800     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      IX1164.2
021900     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         IX1164.2
022000     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     IX1164.2
022100     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     IX1164.2
022200     03      CR-18V0 REDEFINES CORRECT-A.                         IX1164.2
022300         04 CORRECT-18V0                     PIC -9(18).          IX1164.2
022400         04 FILLER                           PIC X.               IX1164.2
022500     03 FILLER PIC X(2) VALUE SPACE.                              IX1164.2
022600     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     IX1164.2
022700 01  CCVS-C-1.                                                    IX1164.2
022800     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PAIX1164.2
022900-    "SS  PARAGRAPH-NAME                                          IX1164.2
023000-    "       REMARKS".                                            IX1164.2
023100     02 FILLER                     PIC X(20)    VALUE SPACE.      IX1164.2
023200 01  CCVS-C-2.                                                    IX1164.2
023300     02 FILLER                     PIC X        VALUE SPACE.      IX1164.2
023400     02 FILLER                     PIC X(6)     VALUE "TESTED".   IX1164.2
023500     02 FILLER                     PIC X(15)    VALUE SPACE.      IX1164.2
023600     02 FILLER                     PIC X(4)     VALUE "FAIL".     IX1164.2
023700     02 FILLER                     PIC X(94)    VALUE SPACE.      IX1164.2
023800 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       IX1164.2
023900 01  REC-CT                        PIC 99       VALUE ZERO.       IX1164.2
024000 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       IX1164.2
024100 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       IX1164.2
024200 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       IX1164.2
024300 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       IX1164.2
024400 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       IX1164.2
024500 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       IX1164.2
024600 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      IX1164.2
024700 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       IX1164.2
024800 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     IX1164.2
024900 01  CCVS-H-1.                                                    IX1164.2
025000     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1164.2
025100     02  FILLER                    PIC X(42)    VALUE             IX1164.2
025200     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 IX1164.2
025300     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1164.2
025400 01  CCVS-H-2A.                                                   IX1164.2
025500   02  FILLER                        PIC X(40)  VALUE SPACE.      IX1164.2
025600   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  IX1164.2
025700   02  FILLER                        PIC XXXX   VALUE             IX1164.2
025800     "4.2 ".                                                      IX1164.2
025900   02  FILLER                        PIC X(28)  VALUE             IX1164.2
026000            " COPY - NOT FOR DISTRIBUTION".                       IX1164.2
026100   02  FILLER                        PIC X(41)  VALUE SPACE.      IX1164.2
026200                                                                  IX1164.2
026300 01  CCVS-H-2B.                                                   IX1164.2
026400   02  FILLER                        PIC X(15)  VALUE             IX1164.2
026500            "TEST RESULT OF ".                                    IX1164.2
026600   02  TEST-ID                       PIC X(9).                    IX1164.2
026700   02  FILLER                        PIC X(4)   VALUE             IX1164.2
026800            " IN ".                                               IX1164.2
026900   02  FILLER                        PIC X(12)  VALUE             IX1164.2
027000     " HIGH       ".                                              IX1164.2
027100   02  FILLER                        PIC X(22)  VALUE             IX1164.2
027200            " LEVEL VALIDATION FOR ".                             IX1164.2
027300   02  FILLER                        PIC X(58)  VALUE             IX1164.2
027400     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1164.2
027500 01  CCVS-H-3.                                                    IX1164.2
027600     02  FILLER                      PIC X(34)  VALUE             IX1164.2
027700            " FOR OFFICIAL USE ONLY    ".                         IX1164.2
027800     02  FILLER                      PIC X(58)  VALUE             IX1164.2
027900     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1164.2
028000     02  FILLER                      PIC X(28)  VALUE             IX1164.2
028100            "  COPYRIGHT   1985 ".                                IX1164.2
028200 01  CCVS-E-1.                                                    IX1164.2
028300     02 FILLER                       PIC X(52)  VALUE SPACE.      IX1164.2
028400     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              IX1164.2
028500     02 ID-AGAIN                     PIC X(9).                    IX1164.2
028600     02 FILLER                       PIC X(45)  VALUE SPACES.     IX1164.2
028700 01  CCVS-E-2.                                                    IX1164.2
028800     02  FILLER                      PIC X(31)  VALUE SPACE.      IX1164.2
028900     02  FILLER                      PIC X(21)  VALUE SPACE.      IX1164.2
029000     02 CCVS-E-2-2.                                               IX1164.2
029100         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      IX1164.2
029200         03 FILLER                   PIC X      VALUE SPACE.      IX1164.2
029300         03 ENDER-DESC               PIC X(44)  VALUE             IX1164.2
029400            "ERRORS ENCOUNTERED".                                 IX1164.2
029500 01  CCVS-E-3.                                                    IX1164.2
029600     02  FILLER                      PIC X(22)  VALUE             IX1164.2
029700            " FOR OFFICIAL USE ONLY".                             IX1164.2
029800     02  FILLER                      PIC X(12)  VALUE SPACE.      IX1164.2
029900     02  FILLER                      PIC X(58)  VALUE             IX1164.2
030000     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1164.2
030100     02  FILLER                      PIC X(13)  VALUE SPACE.      IX1164.2
030200     02 FILLER                       PIC X(15)  VALUE             IX1164.2
030300             " COPYRIGHT 1985".                                   IX1164.2
030400 01  CCVS-E-4.                                                    IX1164.2
030500     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      IX1164.2
030600     02 FILLER                       PIC X(4)   VALUE " OF ".     IX1164.2
030700     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      IX1164.2
030800     02 FILLER                       PIC X(40)  VALUE             IX1164.2
030900      "  TESTS WERE EXECUTED SUCCESSFULLY".                       IX1164.2
031000 01  XXINFO.                                                      IX1164.2
031100     02 FILLER                       PIC X(19)  VALUE             IX1164.2
031200            "*** INFORMATION ***".                                IX1164.2
031300     02 INFO-TEXT.                                                IX1164.2
031400       04 FILLER                     PIC X(8)   VALUE SPACE.      IX1164.2
031500       04 XXCOMPUTED                 PIC X(20).                   IX1164.2
031600       04 FILLER                     PIC X(5)   VALUE SPACE.      IX1164.2
031700       04 XXCORRECT                  PIC X(20).                   IX1164.2
031800     02 INF-ANSI-REFERENCE           PIC X(48).                   IX1164.2
031900 01  HYPHEN-LINE.                                                 IX1164.2
032000     02 FILLER  PIC IS X VALUE IS SPACE.                          IX1164.2
032100     02 FILLER  PIC IS X(65)    VALUE IS "************************IX1164.2
032200-    "*****************************************".                 IX1164.2
032300     02 FILLER  PIC IS X(54)    VALUE IS "************************IX1164.2
032400-    "******************************".                            IX1164.2
032500 01  TEST-NO                         PIC 99.                      IX1164.2
032600 01  CCVS-PGM-ID                     PIC X(9)   VALUE             IX1164.2
032700     "IX116A".                                                    IX1164.2
032800 PROCEDURE DIVISION.                                              IX1164.2
032900 DECLARATIVES.                                                    IX1164.2
033000                                                                  IX1164.2
033100 SECT-IX105-0002 SECTION.                                         IX1164.2
033200     USE AFTER EXCEPTION PROCEDURE ON IX-FS3.                     IX1164.2
033300 INPUT-PROCESS.                                                   IX1164.2
033400     IF TEST-NO = 5                                               IX1164.2
033500        GO TO D-C-TEST-GF-01-1.                                   IX1164.2
033600     IF STATUS-TEST-10 EQUAL TO 1                                 IX1164.2
033700        IF  IX-FS3-STAT1 EQUAL TO "1"                             IX1164.2
033800            MOVE 1 TO EOF-FLAG                                    IX1164.2
033900        ELSE                                                      IX1164.2
034000           IF  IX-FS3-STAT1 GREATER THAN "1"                      IX1164.2
034100           MOVE 1 TO PERM-ERRORS.                                 IX1164.2
034200     GO TO DECL-EXIT.                                             IX1164.2
034300 D-C-TEST-GF-01-1.                                                IX1164.2
034400     IF IX-FS3-STATUS EQUAL TO "49"                               IX1164.2
034500         GO TO D-C-PASS-GF-01-0.                                  IX1164.2
034600 D-C-FAIL-GF-01-0.                                                IX1164.2
034700     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1164.2
034800     MOVE "49" TO CORRECT-X.                                      IX1164.2
034900     MOVE "IX-5, 1.3.4, (5) H" TO RE-MARK.                        IX1164.2
035000     PERFORM D-FAIL.                                              IX1164.2
035100     GO TO D-C-WRITE-GF-01-0.                                     IX1164.2
035200 D-C-PASS-GF-01-0.                                                IX1164.2
035300     PERFORM D-PASS.                                              IX1164.2
035400 D-C-WRITE-GF-01-0.                                               IX1164.2
035500     PERFORM D-PRINT-DETAIL.                                      IX1164.2
035600 D-CLOSE-FILES.                                                   IX1164.2
035700P    OPEN I-O RAW-DATA.                                           IX1164.2
035800P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1164.2
035900P    READ RAW-DATA INVALID KEY GO TO D-END-E-2.                   IX1164.2
036000P    MOVE "OK.     " TO C-ABORT.                                  IX1164.2
036100P    MOVE PASS-COUNTER TO C-OK.                                   IX1164.2
036200P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1164.2
036300P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1164.2
036400P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1164.2
036500P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1164.2
036600P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO D-END-E-2.           IX1164.2
036700PD-END-E-2.                                                       IX1164.2
036800P    CLOSE RAW-DATA.                                              IX1164.2
036900     PERFORM D-END-ROUTINE THRU D-END-ROUTINE-13.                 IX1164.2
037000     CLOSE PRINT-FILE.                                            IX1164.2
037100 D-TERMINATE-CCVS.                                                IX1164.2
037200S    EXIT PROGRAM.                                                IX1164.2
037300SD-TERMINATE-CALL.                                                IX1164.2
037400     STOP     RUN.                                                IX1164.2
037500 D-PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.         IX1164.2
037600 D-FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.        IX1164.2
037700 D-PRINT-DETAIL.                                                  IX1164.2
037800     IF   REC-CT NOT EQUAL TO ZERO                                IX1164.2
037900          MOVE "." TO PARDOT-X                                    IX1164.2
038000          MOVE REC-CT TO DOTVALUE.                                IX1164.2
038100     MOVE TEST-RESULTS TO PRINT-REC.                              IX1164.2
038200     PERFORM D-WRITE-LINE.                                        IX1164.2
038300     IF   P-OR-F EQUAL TO "FAIL*"                                 IX1164.2
038400          PERFORM D-WRITE-LINE                                    IX1164.2
038500          PERFORM D-FAIL-ROUTINE THRU D-FAIL-ROUTINE-EX           IX1164.2
038600     ELSE                                                         IX1164.2
038700          PERFORM D-BAIL-OUT THRU D-BAIL-OUT-EX.                  IX1164.2
038800     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1164.2
038900     MOVE SPACE TO CORRECT-X.                                     IX1164.2
039000     IF   REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.           IX1164.2
039100     MOVE SPACE TO RE-MARK.                                       IX1164.2
039200 D-END-ROUTINE.                                                   IX1164.2
039300     MOVE HYPHEN-LINE TO DUMMY-RECORD.                            IX1164.2
039400     PERFORM D-WRITE-LINE 5 TIMES.                                IX1164.2
039500 D-END-RTN-EXIT.                                                  IX1164.2
039600     MOVE CCVS-E-1 TO DUMMY-RECORD.                               IX1164.2
039700     PERFORM D-WRITE-LINE 2 TIMES.                                IX1164.2
039800 D-END-ROUTINE-1.                                                 IX1164.2
039900     ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO       IX1164.2
040000     ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.                IX1164.2
040100     ADD PASS-COUNTER TO ERROR-HOLD.                              IX1164.2
040200     MOVE PASS-COUNTER TO CCVS-E-4-1.                             IX1164.2
040300     MOVE ERROR-HOLD TO CCVS-E-4-2.                               IX1164.2
040400     MOVE CCVS-E-4 TO CCVS-E-2-2.                                 IX1164.2
040500     MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM D-WRITE-LINE.          IX1164.2
040600  D-END-ROUTINE-12.                                               IX1164.2
040700     MOVE "TEST(S) FAILED" TO ENDER-DESC.                         IX1164.2
040800     IF  ERROR-COUNTER IS EQUAL TO ZERO                           IX1164.2
040900         MOVE "NO " TO ERROR-TOTAL                                IX1164.2
041000     ELSE                                                         IX1164.2
041100         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1164.2
041200     MOVE    CCVS-E-2 TO DUMMY-RECORD.                            IX1164.2
041300     PERFORM D-WRITE-LINE.                                        IX1164.2
041400 D-END-ROUTINE-13.                                                IX1164.2
041500     IF  DELETE-COUNTER IS EQUAL TO ZERO                          IX1164.2
041600         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1164.2
041700         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1164.2
041800     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1164.2
041900     MOVE CCVS-E-2 TO DUMMY-RECORD.                               IX1164.2
042000     PERFORM D-WRITE-LINE.                                        IX1164.2
042100     IF   INSPECT-COUNTER EQUAL TO ZERO                           IX1164.2
042200          MOVE "NO " TO ERROR-TOTAL                               IX1164.2
042300     ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                    IX1164.2
042400     MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.             IX1164.2
042500     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1164.2
042600     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1164.2
042700 D-WRITE-LINE.                                                    IX1164.2
042800     ADD 1 TO RECORD-COUNT.                                       IX1164.2
042900Y    IF RECORD-COUNT GREATER 42                                   IX1164.2
043000Y       MOVE DUMMY-RECORD TO DUMMY-HOLD                           IX1164.2
043100Y       MOVE SPACE TO DUMMY-RECORD                                IX1164.2
043200Y       WRITE DUMMY-RECORD AFTER ADVANCING PAGE                   IX1164.2
043300Y       MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1164.2
043400Y       MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1164.2
043500Y       MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1164.2
043600Y       MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1164.2
043700Y       MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1164.2
043800Y       MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1164.2
043900Y       MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM D-WRT-LN         IX1164.2
044000Y       MOVE DUMMY-HOLD TO DUMMY-RECORD                           IX1164.2
044100Y       MOVE ZERO TO RECORD-COUNT.                                IX1164.2
044200     PERFORM D-WRT-LN.                                            IX1164.2
044300 D-WRT-LN.                                                        IX1164.2
044400     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1164.2
044500     MOVE SPACE TO DUMMY-RECORD.                                  IX1164.2
044600 D-FAIL-ROUTINE.                                                  IX1164.2
044700     IF   COMPUTED-X NOT EQUAL TO SPACE                           IX1164.2
044800          GO TO D-FAIL-ROUTINE-WRITE.                             IX1164.2
044900     IF   CORRECT-X NOT EQUAL TO SPACE GO TO D-FAIL-ROUTINE-WRITE.IX1164.2
045000     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1164.2
045100     MOVE "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.    IX1164.2
045200     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1164.2
045300     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1164.2
045400     GO TO D-FAIL-ROUTINE-EX.                                     IX1164.2
045500 D-FAIL-ROUTINE-WRITE.                                            IX1164.2
045600     MOVE TEST-COMPUTED TO PRINT-REC PERFORM D-WRITE-LINE         IX1164.2
045700     MOVE ANSI-REFERENCE TO COR-ANSI-REFERENCE.                   IX1164.2
045800     MOVE TEST-CORRECT TO PRINT-REC PERFORM D-WRITE-LINE 2 TIMES. IX1164.2
045900     MOVE SPACES TO COR-ANSI-REFERENCE.                           IX1164.2
046000 D-FAIL-ROUTINE-EX. EXIT.                                         IX1164.2
046100 D-BAIL-OUT.                                                      IX1164.2
046200     IF  COMPUTED-A NOT EQUAL TO SPACE GO TO D-BAIL-OUT-WRITE.    IX1164.2
046300     IF  CORRECT-A EQUAL TO SPACE GO TO D-BAIL-OUT-EX.            IX1164.2
046400 D-BAIL-OUT-WRITE.                                                IX1164.2
046500     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1164.2
046600     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1164.2
046700     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1164.2
046800     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1164.2
046900 D-BAIL-OUT-EX. EXIT.                                             IX1164.2
047000 DECL-EXIT.  EXIT.                                                IX1164.2
047100 END DECLARATIVES.                                                IX1164.2
047200                                                                  IX1164.2
047300                                                                  IX1164.2
047400 CCVS1 SECTION.                                                   IX1164.2
047500 OPEN-FILES.                                                      IX1164.2
047600P    OPEN I-O RAW-DATA.                                           IX1164.2
047700P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1164.2
047800P    READ RAW-DATA INVALID KEY GO TO END-E-1.                     IX1164.2
047900P    MOVE "ABORTED " TO C-ABORT.                                  IX1164.2
048000P    ADD 1 TO C-NO-OF-TESTS.                                      IX1164.2
048100P    ACCEPT C-DATE  FROM DATE.                                    IX1164.2
048200P    ACCEPT C-TIME  FROM TIME.                                    IX1164.2
048300P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-1.             IX1164.2
048400PEND-E-1.                                                         IX1164.2
048500P    CLOSE RAW-DATA.                                              IX1164.2
048600     OPEN    OUTPUT PRINT-FILE.                                   IX1164.2
048700     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  IX1164.2
048800     MOVE    SPACE TO TEST-RESULTS.                               IX1164.2
048900     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              IX1164.2
049000     MOVE    ZERO TO REC-SKL-SUB.                                 IX1164.2
049100     PERFORM CCVS-INIT-FILE 9 TIMES.                              IX1164.2
049200 CCVS-INIT-FILE.                                                  IX1164.2
049300     ADD     1 TO REC-SKL-SUB.                                    IX1164.2
049400     MOVE    FILE-RECORD-INFO-SKELETON                            IX1164.2
049500          TO FILE-RECORD-INFO (REC-SKL-SUB).                      IX1164.2
049600 CCVS-INIT-EXIT.                                                  IX1164.2
049700     GO TO CCVS1-EXIT.                                            IX1164.2
049800 CLOSE-FILES.                                                     IX1164.2
049900P    OPEN I-O RAW-DATA.                                           IX1164.2
050000P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1164.2
050100P    READ RAW-DATA INVALID KEY GO TO END-E-2.                     IX1164.2
050200P    MOVE "OK.     " TO C-ABORT.                                  IX1164.2
050300P    MOVE PASS-COUNTER TO C-OK.                                   IX1164.2
050400P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1164.2
050500P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1164.2
050600P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1164.2
050700P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1164.2
050800P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-2.             IX1164.2
050900PEND-E-2.                                                         IX1164.2
051000P    CLOSE RAW-DATA.                                              IX1164.2
051100     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   IX1164.2
051200 TERMINATE-CCVS.                                                  IX1164.2
051300S    EXIT PROGRAM.                                                IX1164.2
051400STERMINATE-CALL.                                                  IX1164.2
051500     STOP     RUN.                                                IX1164.2
051600 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         IX1164.2
051700 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           IX1164.2
051800 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          IX1164.2
051900 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      IX1164.2
052000     MOVE "****TEST DELETED****" TO RE-MARK.                      IX1164.2
052100 PRINT-DETAIL.                                                    IX1164.2
052200     IF REC-CT NOT EQUAL TO ZERO                                  IX1164.2
052300             MOVE "." TO PARDOT-X                                 IX1164.2
052400             MOVE REC-CT TO DOTVALUE.                             IX1164.2
052500     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      IX1164.2
052600     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               IX1164.2
052700        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 IX1164.2
052800          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 IX1164.2
052900     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1164.2
053000     MOVE SPACE TO CORRECT-X.                                     IX1164.2
053100     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         IX1164.2
053200     MOVE     SPACE TO RE-MARK.                                   IX1164.2
053300 HEAD-ROUTINE.                                                    IX1164.2
053400     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1164.2
053500     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1164.2
053600     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1164.2
053700     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1164.2
053800 COLUMN-NAMES-ROUTINE.                                            IX1164.2
053900     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1164.2
054000     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1164.2
054100     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        IX1164.2
054200 END-ROUTINE.                                                     IX1164.2
054300     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.IX1164.2
054400 END-RTN-EXIT.                                                    IX1164.2
054500     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1164.2
054600 END-ROUTINE-1.                                                   IX1164.2
054700      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      IX1164.2
054800      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               IX1164.2
054900      ADD PASS-COUNTER TO ERROR-HOLD.                             IX1164.2
055000*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   IX1164.2
055100      MOVE PASS-COUNTER TO CCVS-E-4-1.                            IX1164.2
055200      MOVE ERROR-HOLD TO CCVS-E-4-2.                              IX1164.2
055300      MOVE CCVS-E-4 TO CCVS-E-2-2.                                IX1164.2
055400      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           IX1164.2
055500  END-ROUTINE-12.                                                 IX1164.2
055600      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        IX1164.2
055700     IF       ERROR-COUNTER IS EQUAL TO ZERO                      IX1164.2
055800         MOVE "NO " TO ERROR-TOTAL                                IX1164.2
055900         ELSE                                                     IX1164.2
056000         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1164.2
056100     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           IX1164.2
056200     PERFORM WRITE-LINE.                                          IX1164.2
056300 END-ROUTINE-13.                                                  IX1164.2
056400     IF DELETE-COUNTER IS EQUAL TO ZERO                           IX1164.2
056500         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1164.2
056600         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1164.2
056700     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1164.2
056800     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1164.2
056900      IF   INSPECT-COUNTER EQUAL TO ZERO                          IX1164.2
057000          MOVE "NO " TO ERROR-TOTAL                               IX1164.2
057100      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   IX1164.2
057200      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            IX1164.2
057300      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          IX1164.2
057400     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1164.2
057500 WRITE-LINE.                                                      IX1164.2
057600     ADD 1 TO RECORD-COUNT.                                       IX1164.2
057700Y    IF RECORD-COUNT GREATER 42                                   IX1164.2
057800Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          IX1164.2
057900Y        MOVE SPACE TO DUMMY-RECORD                               IX1164.2
058000Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  IX1164.2
058100Y        MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1164.2
058200Y        MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1164.2
058300Y        MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1164.2
058400Y        MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1164.2
058500Y        MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM WRT-LN            IX1164.2
058600Y        MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM WRT-LN            IX1164.2
058700Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          IX1164.2
058800Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          IX1164.2
058900Y        MOVE ZERO TO RECORD-COUNT.                               IX1164.2
059000     PERFORM WRT-LN.                                              IX1164.2
059100 WRT-LN.                                                          IX1164.2
059200     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1164.2
059300     MOVE SPACE TO DUMMY-RECORD.                                  IX1164.2
059400 BLANK-LINE-PRINT.                                                IX1164.2
059500     PERFORM WRT-LN.                                              IX1164.2
059600 FAIL-ROUTINE.                                                    IX1164.2
059700     IF     COMPUTED-X NOT EQUAL TO SPACE                         IX1164.2
059800            GO TO   FAIL-ROUTINE-WRITE.                           IX1164.2
059900     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.IX1164.2
060000     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1164.2
060100     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   IX1164.2
060200     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1164.2
060300     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1164.2
060400     GO TO  FAIL-ROUTINE-EX.                                      IX1164.2
060500 FAIL-ROUTINE-WRITE.                                              IX1164.2
060600     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         IX1164.2
060700     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 IX1164.2
060800     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. IX1164.2
060900     MOVE   SPACES TO COR-ANSI-REFERENCE.                         IX1164.2
061000 FAIL-ROUTINE-EX. EXIT.                                           IX1164.2
061100 BAIL-OUT.                                                        IX1164.2
061200     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   IX1164.2
061300     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           IX1164.2
061400 BAIL-OUT-WRITE.                                                  IX1164.2
061500     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1164.2
061600     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1164.2
061700     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1164.2
061800     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1164.2
061900 BAIL-OUT-EX. EXIT.                                               IX1164.2
062000 CCVS1-EXIT.                                                      IX1164.2
062100     EXIT.                                                        IX1164.2
062200                                                                  IX1164.2
062300 SECT-IX116A-0003 SECTION.                                        IX1164.2
062400 SEQ-INIT-010.                                                    IX1164.2
062500     MOVE ZERO TO TEST-NO.                                        IX1164.2
062600     MOVE "IX-FS3" TO XFILE-NAME (1).                             IX1164.2
062700     MOVE "R1-F-G" TO XRECORD-NAME (1).                           IX1164.2
062800     MOVE CCVS-PGM-ID TO XPROGRAM-NAME (1).                       IX1164.2
062900     MOVE 000240 TO XRECORD-LENGTH (1).                           IX1164.2
063000     MOVE "RC" TO CHARS-OR-RECORDS (1).                           IX1164.2
063100     MOVE 0002 TO XBLOCK-SIZE (1).                                IX1164.2
063200     MOVE 000050 TO RECORDS-IN-FILE (1).                          IX1164.2
063300     MOVE "IX" TO XFILE-ORGANIZATION (1).                         IX1164.2
063400     MOVE "S" TO XLABEL-TYPE (1).                                 IX1164.2
063500     MOVE 000001 TO XRECORD-NUMBER (1).                           IX1164.2
063600     MOVE 0 TO COUNT-OF-RECS.                                     IX1164.2
063700                                                                  IX1164.2
063800******************************************************************IX1164.2
063900*   TEST  1                                                      *IX1164.2
064000*         OPEN OUTPUT ...                 00 EXPECTED            *IX1164.2
064100*         IX-3, 1.3.4 (1) A                                      *IX1164.2
064200*    STATUS 00 CHECK ON OUTPUT FILE IX-FS3                       *IX1164.2
064300*    THE OUTPUT STATEMENT IS SUCCESSFULLY EXECUTED               *IX1164.2
064400******************************************************************IX1164.2
064500 OPN-INIT-GF-01-0.                                                IX1164.2
064600     MOVE 1 TO STATUS-TEST-00.                                    IX1164.2
064700     MOVE SPACES TO IX-FS3-STATUS.                                IX1164.2
064800     MOVE "OPEN I-O   : 00 EXP." TO FEATURE.                      IX1164.2
064900     MOVE "OPN-TEST-GF-01-0" TO PAR-NAME.                         IX1164.2
065000     OPEN                                                         IX1164.2
065100        I-O    IX-FS3.                                            IX1164.2
065200     IF IX-FS3-STATUS EQUAL TO "00"                               IX1164.2
065300         GO TO OPN-PASS-GF-01-0.                                  IX1164.2
065400 OPN-FAIL-GF-01-0.                                                IX1164.2
065500     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1164.2
065600     PERFORM FAIL.                                                IX1164.2
065700     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1164.2
065800     MOVE "00" TO CORRECT-X.                                      IX1164.2
065900     GO TO OPN-WRITE-GF-01-0.                                     IX1164.2
066000 OPN-PASS-GF-01-0.                                                IX1164.2
066100     PERFORM PASS.                                                IX1164.2
066200 OPN-WRITE-GF-01-0.                                               IX1164.2
066300     PERFORM PRINT-DETAIL.                                        IX1164.2
066400******************************************************************IX1164.2
066500*   TEST  4                                                      *IX1164.2
066600*         CLOSE I-O                       00 EXPECTED            *IX1164.2
066700*         IX-3, 1.3.4 (1) A                                      *IX1164.2
066800******************************************************************IX1164.2
066900 CLO-INIT-GF-01-0.                                                IX1164.2
067000     MOVE SPACES TO IX-FS3-STATUS.                                IX1164.2
067100     MOVE "CLOSE I-O   :00 EXP." TO FEATURE.                      IX1164.2
067200     MOVE "CLO-TEST-GF-01-0" TO PAR-NAME.                         IX1164.2
067300 CLO-TEST-GF-01-0.                                                IX1164.2
067400     CLOSE IX-FS3.                                                IX1164.2
067500     IF IX-FS3-STATUS = "00"                                      IX1164.2
067600         GO TO CLO-PASS-GF-01-0.                                  IX1164.2
067700 CLO-FAIL-GF-01-0.                                                IX1164.2
067800     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1164.2
067900     PERFORM FAIL.                                                IX1164.2
068000     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1164.2
068100     MOVE "00" TO CORRECT-X.                                      IX1164.2
068200     GO TO CLO-WRITE-GF-01-0.                                     IX1164.2
068300 CLO-PASS-GF-01-0.                                                IX1164.2
068400     PERFORM PASS.                                                IX1164.2
068500 CLO-WRITE-GF-01-0.                                               IX1164.2
068600     PERFORM PRINT-DETAIL.                                        IX1164.2
068700                                                                  IX1164.2
068800******************************************************************IX1164.2
068900*    A INDEXED FILE WITH 50 RECORDS HAS BEEN CREATED.            *IX1164.2
069000******************************************************************IX1164.2
069100                                                                  IX1164.2
069200******************************************************************IX1164.2
069300*   TEST  5                                                      *IX1164.2
069400*         DELETE....  FILE NOT IN THE OPEN MODE                  *IX1164.2
069500*         FILE STATUS 49 EXPECTED IX-5, 1.3.4 (5) H              *IX1164.2
069600******************************************************************IX1164.2
069700 DEL-TEST-GF-01-0.                                                IX1164.2
069800     MOVE  5 TO TEST-NO.                                          IX1164.2
069900     MOVE SPACES TO IX-FS3-STATUS.                                IX1164.2
070000     MOVE "DELETE       49 EXP." TO FEATURE                       IX1164.2
070100     MOVE "DEL-TEST-GF-01-0" TO PAR-NAME.                         IX1164.2
070200     DELETE IX-FS3 RECORD.                                        IX1164.2
070300 DEL-TEST-GF-01-1.                                                IX1164.2
070400     IF IX-FS3-STATUS EQUAL TO "49"                               IX1164.2
070500        MOVE "SHOULD HAVE EXECUTED DECLARATIVES IX-3,1.3.4(4)"    IX1164.2
070600          TO RE-MARK                                              IX1164.2
070700        GO TO DEL-WRITE-GF-01-0.                                  IX1164.2
070800 DEL-FAIL-GF-01-0.                                                IX1164.2
070900     MOVE "IX-5, 1.3.4, (5) H" TO RE-MARK.                        IX1164.2
071000 DEL-WRITE-GF-01-0.                                               IX1164.2
071100     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1164.2
071200     MOVE "49" TO CORRECT-X.                                      IX1164.2
071300     PERFORM FAIL.                                                IX1164.2
071400     PERFORM PRINT-DETAIL.                                        IX1164.2
071500                                                                  IX1164.2
071600 TERMINATE-ROUTINE.                                               IX1164.2
071700     EXIT.                                                        IX1164.2
071800                                                                  IX1164.2
071900 CCVS-EXIT SECTION.                                               IX1164.2
072000 CCVS-999999.                                                     IX1164.2
072100     GO TO CLOSE-FILES.                                           IX1164.2
       END PROGRAM IX116A.
000100 IDENTIFICATION DIVISION.                                         IX1174.2
000200 PROGRAM-ID.                                                      IX1174.2
000300     IX117A.                                                      IX1174.2
000400****************************************************************  IX1174.2
000500*                                                              *  IX1174.2
000600*    VALIDATION FOR:-                                          *  IX1174.2
000700*                                                              *  IX1174.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1174.2
000900*                                                              *  IX1174.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1174.2
001100*                                                              *  IX1174.2
001200****************************************************************  IX1174.2
001300*                                                                 IX1174.2
001400*    THIS ROUTINE USES THE MASS STORAGE FILE IX-FS3 CREATED IN    IX1174.2
001500*    IX113A.                                                      IX1174.2
001600*    THE FILE IS OPENED I-O AND THE STATUS CHECKED (00 EXPECTED), IX1174.2
001700*    CLOSED AND THE STATUS CHECKED (00 EXPECTED) THEN AN ATTEMPT  IX1174.2
001800*    IS MADE TO REWRITE A RECORD, AT WHICH POINT THE DECLARATIVES IX1174.2
001900*    SECTION SHOULD BE ACTIONED AND THE FILE STATUES SHOULD BE 49 IX1174.2
002000*                                                                 IX1174.2
002100*                                                                 IX1174.2
002200*    4. X-CARDS USED IN THIS PROGRAM:                             IX1174.2
002300*                                                                 IX1174.2
002400*                 XXXXX024                                        IX1174.2
002500*                 XXXXX055.                                       IX1174.2
002600*         P       XXXXX062.                                       IX1174.2
002700*                 XXXXX082.                                       IX1174.2
002800*                 XXXXX083.                                       IX1174.2
002900*         C       XXXXX084                                        IX1174.2
003000*                                                                 IX1174.2
003100*                                                                 IX1174.2
003200 ENVIRONMENT DIVISION.                                            IX1174.2
003300 CONFIGURATION SECTION.                                           IX1174.2
003400 SOURCE-COMPUTER.                                                 IX1174.2
003500     XXXXX082.                                                    IX1174.2
003600 OBJECT-COMPUTER.                                                 IX1174.2
003700     XXXXX083.                                                    IX1174.2
003800 INPUT-OUTPUT SECTION.                                            IX1174.2
003900 FILE-CONTROL.                                                    IX1174.2
004000P    SELECT RAW-DATA   ASSIGN TO                                  IX1174.2
004100P    XXXXX062                                                     IX1174.2
004200P           ORGANIZATION IS INDEXED                               IX1174.2
004300P           ACCESS MODE IS RANDOM                                 IX1174.2
004400P           RECORD KEY IS RAW-DATA-KEY.                           IX1174.2
004500*                                                                 IX1174.2
004600     SELECT PRINT-FILE ASSIGN TO                                  IX1174.2
004700     XXXXX055.                                                    IX1174.2
004800*                                                                 IX1174.2
004900     SELECT IX-FS3 ASSIGN                                         IX1174.2
005000     XXXXX024                                                     IX1174.2
005100     ORGANIZATION IS INDEXED                                      IX1174.2
005200     ACCESS MODE IS SEQUENTIAL                                    IX1174.2
005300     RECORD KEY IS IX-FS3-KEY                                     IX1174.2
005400     FILE STATUS IS IX-FS3-STATUS.                                IX1174.2
005500                                                                  IX1174.2
005600 DATA DIVISION.                                                   IX1174.2
005700                                                                  IX1174.2
005800 FILE SECTION.                                                    IX1174.2
005900P                                                                 IX1174.2
006000PFD  RAW-DATA.                                                    IX1174.2
006100P                                                                 IX1174.2
006200P01  RAW-DATA-SATZ.                                               IX1174.2
006300P    05  RAW-DATA-KEY        PIC X(6).                            IX1174.2
006400P    05  C-DATE              PIC 9(6).                            IX1174.2
006500P    05  C-TIME              PIC 9(8).                            IX1174.2
006600P    05  C-NO-OF-TESTS       PIC 99.                              IX1174.2
006700P    05  C-OK                PIC 999.                             IX1174.2
006800P    05  C-ALL               PIC 999.                             IX1174.2
006900P    05  C-FAIL              PIC 999.                             IX1174.2
007000P    05  C-DELETED           PIC 999.                             IX1174.2
007100P    05  C-INSPECT           PIC 999.                             IX1174.2
007200P    05  C-NOTE              PIC X(13).                           IX1174.2
007300P    05  C-INDENT            PIC X.                               IX1174.2
007400P    05  C-ABORT             PIC X(8).                            IX1174.2
007500                                                                  IX1174.2
007600 FD  PRINT-FILE.                                                  IX1174.2
007700                                                                  IX1174.2
007800 01  PRINT-REC               PIC X(120).                          IX1174.2
007900                                                                  IX1174.2
008000 01  DUMMY-RECORD            PIC X(120).                          IX1174.2
008100                                                                  IX1174.2
008200 FD  IX-FS3                                                       IX1174.2
008300C       DATA RECORDS IX-FS3R1-F-G-240                             IX1174.2
008400C       LABEL RECORD STANDARD                                     IX1174.2
008500        RECORD 240                                                IX1174.2
008600        BLOCK CONTAINS 2 RECORDS.                                 IX1174.2
008700                                                                  IX1174.2
008800 01  IX-FS3R1-F-G-240.                                            IX1174.2
008900     05  IX-FS3-REC-120      PIC X(120).                          IX1174.2
009000     05  IX-FS3-REC-120-240.                                      IX1174.2
009100         10  FILLER          PIC X(8).                            IX1174.2
009200         10  IX-FS3-KEY      PIC X(29).                           IX1174.2
009300         10  FILLER          PIC X(9).                            IX1174.2
009400         10  IX-FS3-ALTER-KEY  PIC X(29).                         IX1174.2
009500         10  FILLER            PIC X(45).                         IX1174.2
009600                                                                  IX1174.2
009700                                                                  IX1174.2
009800 WORKING-STORAGE SECTION.                                         IX1174.2
009900                                                                  IX1174.2
010000 01  GRP-0101.                                                    IX1174.2
010100     05  FILLER              PIC X(10) VALUE "RECORD-KEY".        IX1174.2
010200     05  GRP-0101-KEY        PIC 9(9)  VALUE ZERO.                IX1174.2
010300     05  FILLER              PIC X(10) VALUE "END-OF-KEY".        IX1174.2
010400                                                                  IX1174.2
010500 01  GRP-0102.                                                    IX1174.2
010600     05  FILLER              PIC X(10) VALUE "ALTERN-KEY".        IX1174.2
010700     05  GRP-0102-KEY        PIC 9(9)  VALUE ZERO.                IX1174.2
010800     05  FILLER              PIC X(10) VALUE "END-AL-KEY".        IX1174.2
010900                                                                  IX1174.2
011000 01  WRK-CS-09V00            PIC S9(9) COMP VALUE ZERO.           IX1174.2
011100                                                                  IX1174.2
011200 01  EOF-FLAG                PIC 9 VALUE ZERO.                    IX1174.2
011300                                                                  IX1174.2
011400 01  RECORDS-IN-ERROR        PIC S9(5) COMP VALUE ZERO.           IX1174.2
011500                                                                  IX1174.2
011600 01  ERROR-FLAG              PIC 9 VALUE ZERO.                    IX1174.2
011700                                                                  IX1174.2
011800 01  PERM-ERRORS             PIC S9(5) COMP VALUE ZERO.           IX1174.2
011900                                                                  IX1174.2
012000 01  STATUS-TEST-00          PIC 9 VALUE ZERO.                    IX1174.2
012100                                                                  IX1174.2
012200 01  STATUS-TEST-10          PIC 9 VALUE ZERO.                    IX1174.2
012300 01  STATUS-TEST-READ        PIC 9 VALUE ZERO.                    IX1174.2
012400                                                                  IX1174.2
012500 01  IX-FS3-STATUS.                                               IX1174.2
012600     05  IX-FS3-STAT1        PIC X.                               IX1174.2
012700     05  IX-FS3-STAT2        PIC X.                               IX1174.2
012800                                                                  IX1174.2
012900 01  COUNT-OF-RECS           PIC 9(5).                            IX1174.2
013000                                                                  IX1174.2
013100 01  COUNT-OF-RECORDS REDEFINES COUNT-OF-RECS  PIC 9(5).          IX1174.2
013200                                                                  IX1174.2
013300 01  FILE-RECORD-INFORMATION-REC.                                 IX1174.2
013400     05  FILE-RECORD-INFO-SKELETON.                               IX1174.2
013500         10  FILLER          PIC X(48) VALUE                      IX1174.2
013600              "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00". IX1174.2
013700         10  FILLER          PIC X(46) VALUE                      IX1174.2
013800                ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000". IX1174.2
013900         10  FILLER          PIC X(26) VALUE                      IX1174.2
014000                                    ",LFIL=000000,ORG=  ,LBLR= ". IX1174.2
014100         10  FILLER          PIC X(37) VALUE                      IX1174.2
014200                         ",RECKEY=                             ". IX1174.2
014300         10  FILLER          PIC X(38) VALUE                      IX1174.2
014400                        ",ALTKEY1=                             ". IX1174.2
014500         10  FILLER          PIC X(38) VALUE                      IX1174.2
014600                        ",ALTKEY2=                             ". IX1174.2
014700         10  FILLER          PIC X(7) VALUE SPACE.                IX1174.2
014800     05  FILE-RECORD-INFO             OCCURS 10.                  IX1174.2
014900         10  FILE-RECORD-INFO-P1-120.                             IX1174.2
015000             15  FILLER      PIC X(5).                            IX1174.2
015100             15  XFILE-NAME  PIC X(6).                            IX1174.2
015200             15  FILLER      PIC X(8).                            IX1174.2
015300             15  XRECORD-NAME  PIC X(6).                          IX1174.2
015400             15  FILLER        PIC X(1).                          IX1174.2
015500             15  REELUNIT-NUMBER  PIC 9(1).                       IX1174.2
015600             15  FILLER           PIC X(7).                       IX1174.2
015700             15  XRECORD-NUMBER   PIC 9(6).                       IX1174.2
015800             15  FILLER           PIC X(6).                       IX1174.2
015900             15  UPDATE-NUMBER    PIC 9(2).                       IX1174.2
016000             15  FILLER           PIC X(5).                       IX1174.2
016100             15  ODO-NUMBER       PIC 9(4).                       IX1174.2
016200             15  FILLER           PIC X(5).                       IX1174.2
016300             15  XPROGRAM-NAME    PIC X(5).                       IX1174.2
016400             15  FILLER           PIC X(7).                       IX1174.2
016500             15  XRECORD-LENGTH   PIC 9(6).                       IX1174.2
016600             15  FILLER           PIC X(7).                       IX1174.2
016700             15  CHARS-OR-RECORDS  PIC X(2).                      IX1174.2
016800             15  FILLER            PIC X(1).                      IX1174.2
016900             15  XBLOCK-SIZE       PIC 9(4).                      IX1174.2
017000             15  FILLER            PIC X(6).                      IX1174.2
017100             15  RECORDS-IN-FILE   PIC 9(6).                      IX1174.2
017200             15  FILLER            PIC X(5).                      IX1174.2
017300             15  XFILE-ORGANIZATION  PIC X(2).                    IX1174.2
017400             15  FILLER              PIC X(6).                    IX1174.2
017500             15  XLABEL-TYPE         PIC X(1).                    IX1174.2
017600         10  FILE-RECORD-INFO-P121-240.                           IX1174.2
017700             15  FILLER              PIC X(8).                    IX1174.2
017800             15  XRECORD-KEY         PIC X(29).                   IX1174.2
017900             15  FILLER              PIC X(9).                    IX1174.2
018000             15  ALTERNATE-KEY1      PIC X(29).                   IX1174.2
018100             15  FILLER              PIC X(9).                    IX1174.2
018200             15  ALTERNATE-KEY2      PIC X(29).                   IX1174.2
018300             15  FILLER              PIC X(7).                    IX1174.2
018400                                                                  IX1174.2
018500 01  TEST-RESULTS.                                                IX1174.2
018600     02 FILLER                   PIC X      VALUE SPACE.          IX1174.2
018700     02 FEATURE                  PIC X(20)  VALUE SPACE.          IX1174.2
018800     02 FILLER                   PIC X      VALUE SPACE.          IX1174.2
018900     02 P-OR-F                   PIC X(5)   VALUE SPACE.          IX1174.2
019000     02 FILLER                   PIC X      VALUE SPACE.          IX1174.2
019100     02  PAR-NAME.                                                IX1174.2
019200       03 FILLER                 PIC X(19)  VALUE SPACE.          IX1174.2
019300       03  PARDOT-X              PIC X      VALUE SPACE.          IX1174.2
019400       03 DOTVALUE               PIC 99     VALUE ZERO.           IX1174.2
019500     02 FILLER                   PIC X(8)   VALUE SPACE.          IX1174.2
019600     02 RE-MARK                  PIC X(61).                       IX1174.2
019700 01  TEST-COMPUTED.                                               IX1174.2
019800     02 FILLER                   PIC X(30)  VALUE SPACE.          IX1174.2
019900     02 FILLER                   PIC X(17)  VALUE                 IX1174.2
020000            "       COMPUTED=".                                   IX1174.2
020100     02 COMPUTED-X.                                               IX1174.2
020200     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          IX1174.2
020300     03 COMPUTED-N               REDEFINES COMPUTED-A             IX1174.2
020400                                 PIC -9(9).9(9).                  IX1174.2
020500     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         IX1174.2
020600     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     IX1174.2
020700     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     IX1174.2
020800     03       CM-18V0 REDEFINES COMPUTED-A.                       IX1174.2
020900         04 COMPUTED-18V0                    PIC -9(18).          IX1174.2
021000         04 FILLER                           PIC X.               IX1174.2
021100     03 FILLER PIC X(50) VALUE SPACE.                             IX1174.2
021200 01  TEST-CORRECT.                                                IX1174.2
021300     02 FILLER PIC X(30) VALUE SPACE.                             IX1174.2
021400     02 FILLER PIC X(17) VALUE "       CORRECT =".                IX1174.2
021500     02 CORRECT-X.                                                IX1174.2
021600     03 CORRECT-A                  PIC X(20) VALUE SPACE.         IX1174.2
021700     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      IX1174.2
021800     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         IX1174.2
021900     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     IX1174.2
022000     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     IX1174.2
022100     03      CR-18V0 REDEFINES CORRECT-A.                         IX1174.2
022200         04 CORRECT-18V0                     PIC -9(18).          IX1174.2
022300         04 FILLER                           PIC X.               IX1174.2
022400     03 FILLER PIC X(2) VALUE SPACE.                              IX1174.2
022500     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     IX1174.2
022600 01  CCVS-C-1.                                                    IX1174.2
022700     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PAIX1174.2
022800-    "SS  PARAGRAPH-NAME                                          IX1174.2
022900-    "       REMARKS".                                            IX1174.2
023000     02 FILLER                     PIC X(20)    VALUE SPACE.      IX1174.2
023100 01  CCVS-C-2.                                                    IX1174.2
023200     02 FILLER                     PIC X        VALUE SPACE.      IX1174.2
023300     02 FILLER                     PIC X(6)     VALUE "TESTED".   IX1174.2
023400     02 FILLER                     PIC X(15)    VALUE SPACE.      IX1174.2
023500     02 FILLER                     PIC X(4)     VALUE "FAIL".     IX1174.2
023600     02 FILLER                     PIC X(94)    VALUE SPACE.      IX1174.2
023700 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       IX1174.2
023800 01  REC-CT                        PIC 99       VALUE ZERO.       IX1174.2
023900 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       IX1174.2
024000 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       IX1174.2
024100 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       IX1174.2
024200 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       IX1174.2
024300 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       IX1174.2
024400 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       IX1174.2
024500 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      IX1174.2
024600 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       IX1174.2
024700 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     IX1174.2
024800 01  CCVS-H-1.                                                    IX1174.2
024900     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1174.2
025000     02  FILLER                    PIC X(42)    VALUE             IX1174.2
025100     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 IX1174.2
025200     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1174.2
025300 01  CCVS-H-2A.                                                   IX1174.2
025400   02  FILLER                        PIC X(40)  VALUE SPACE.      IX1174.2
025500   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  IX1174.2
025600   02  FILLER                        PIC XXXX   VALUE             IX1174.2
025700     "4.2 ".                                                      IX1174.2
025800   02  FILLER                        PIC X(28)  VALUE             IX1174.2
025900            " COPY - NOT FOR DISTRIBUTION".                       IX1174.2
026000   02  FILLER                        PIC X(41)  VALUE SPACE.      IX1174.2
026100                                                                  IX1174.2
026200 01  CCVS-H-2B.                                                   IX1174.2
026300   02  FILLER                        PIC X(15)  VALUE             IX1174.2
026400            "TEST RESULT OF ".                                    IX1174.2
026500   02  TEST-ID                       PIC X(9).                    IX1174.2
026600   02  FILLER                        PIC X(4)   VALUE             IX1174.2
026700            " IN ".                                               IX1174.2
026800   02  FILLER                        PIC X(12)  VALUE             IX1174.2
026900     " HIGH       ".                                              IX1174.2
027000   02  FILLER                        PIC X(22)  VALUE             IX1174.2
027100            " LEVEL VALIDATION FOR ".                             IX1174.2
027200   02  FILLER                        PIC X(58)  VALUE             IX1174.2
027300     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1174.2
027400 01  CCVS-H-3.                                                    IX1174.2
027500     02  FILLER                      PIC X(34)  VALUE             IX1174.2
027600            " FOR OFFICIAL USE ONLY    ".                         IX1174.2
027700     02  FILLER                      PIC X(58)  VALUE             IX1174.2
027800     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1174.2
027900     02  FILLER                      PIC X(28)  VALUE             IX1174.2
028000            "  COPYRIGHT   1985 ".                                IX1174.2
028100 01  CCVS-E-1.                                                    IX1174.2
028200     02 FILLER                       PIC X(52)  VALUE SPACE.      IX1174.2
028300     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              IX1174.2
028400     02 ID-AGAIN                     PIC X(9).                    IX1174.2
028500     02 FILLER                       PIC X(45)  VALUE SPACES.     IX1174.2
028600 01  CCVS-E-2.                                                    IX1174.2
028700     02  FILLER                      PIC X(31)  VALUE SPACE.      IX1174.2
028800     02  FILLER                      PIC X(21)  VALUE SPACE.      IX1174.2
028900     02 CCVS-E-2-2.                                               IX1174.2
029000         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      IX1174.2
029100         03 FILLER                   PIC X      VALUE SPACE.      IX1174.2
029200         03 ENDER-DESC               PIC X(44)  VALUE             IX1174.2
029300            "ERRORS ENCOUNTERED".                                 IX1174.2
029400 01  CCVS-E-3.                                                    IX1174.2
029500     02  FILLER                      PIC X(22)  VALUE             IX1174.2
029600            " FOR OFFICIAL USE ONLY".                             IX1174.2
029700     02  FILLER                      PIC X(12)  VALUE SPACE.      IX1174.2
029800     02  FILLER                      PIC X(58)  VALUE             IX1174.2
029900     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1174.2
030000     02  FILLER                      PIC X(13)  VALUE SPACE.      IX1174.2
030100     02 FILLER                       PIC X(15)  VALUE             IX1174.2
030200             " COPYRIGHT 1985".                                   IX1174.2
030300 01  CCVS-E-4.                                                    IX1174.2
030400     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      IX1174.2
030500     02 FILLER                       PIC X(4)   VALUE " OF ".     IX1174.2
030600     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      IX1174.2
030700     02 FILLER                       PIC X(40)  VALUE             IX1174.2
030800      "  TESTS WERE EXECUTED SUCCESSFULLY".                       IX1174.2
030900 01  XXINFO.                                                      IX1174.2
031000     02 FILLER                       PIC X(19)  VALUE             IX1174.2
031100            "*** INFORMATION ***".                                IX1174.2
031200     02 INFO-TEXT.                                                IX1174.2
031300       04 FILLER                     PIC X(8)   VALUE SPACE.      IX1174.2
031400       04 XXCOMPUTED                 PIC X(20).                   IX1174.2
031500       04 FILLER                     PIC X(5)   VALUE SPACE.      IX1174.2
031600       04 XXCORRECT                  PIC X(20).                   IX1174.2
031700     02 INF-ANSI-REFERENCE           PIC X(48).                   IX1174.2
031800 01  HYPHEN-LINE.                                                 IX1174.2
031900     02 FILLER  PIC IS X VALUE IS SPACE.                          IX1174.2
032000     02 FILLER  PIC IS X(65)    VALUE IS "************************IX1174.2
032100-    "*****************************************".                 IX1174.2
032200     02 FILLER  PIC IS X(54)    VALUE IS "************************IX1174.2
032300-    "******************************".                            IX1174.2
032400 01  TEST-NO                         PIC 99.                      IX1174.2
032500 01  CCVS-PGM-ID                     PIC X(9)   VALUE             IX1174.2
032600     "IX117A".                                                    IX1174.2
032700 PROCEDURE DIVISION.                                              IX1174.2
032800 DECLARATIVES.                                                    IX1174.2
032900                                                                  IX1174.2
033000 SECT-IX105-0002 SECTION.                                         IX1174.2
033100     USE AFTER EXCEPTION PROCEDURE ON IX-FS3.                     IX1174.2
033200 INPUT-PROCESS.                                                   IX1174.2
033300     IF TEST-NO = 5                                               IX1174.2
033400        GO TO D-C-TEST-GF-01-1.                                   IX1174.2
033500     IF STATUS-TEST-10 EQUAL TO 1                                 IX1174.2
033600        IF  IX-FS3-STAT1 EQUAL TO "1"                             IX1174.2
033700            MOVE 1 TO EOF-FLAG                                    IX1174.2
033800        ELSE                                                      IX1174.2
033900           IF  IX-FS3-STAT1 GREATER THAN "1"                      IX1174.2
034000           MOVE 1 TO PERM-ERRORS.                                 IX1174.2
034100     GO TO DECL-EXIT.                                             IX1174.2
034200 D-C-TEST-GF-01-1.                                                IX1174.2
034300     IF IX-FS3-STATUS EQUAL TO "49"                               IX1174.2
034400         GO TO D-C-PASS-GF-01-0.                                  IX1174.2
034500 D-C-FAIL-GF-01-0.                                                IX1174.2
034600     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1174.2
034700     MOVE "49" TO CORRECT-X.                                      IX1174.2
034800     MOVE "IX-5, 1.3.4, (5) H" TO RE-MARK.                        IX1174.2
034900     PERFORM D-FAIL.                                              IX1174.2
035000     GO TO D-C-WRITE-GF-01-0.                                     IX1174.2
035100 D-C-PASS-GF-01-0.                                                IX1174.2
035200     PERFORM D-PASS.                                              IX1174.2
035300 D-C-WRITE-GF-01-0.                                               IX1174.2
035400     PERFORM D-PRINT-DETAIL.                                      IX1174.2
035500 D-CLOSE-FILES.                                                   IX1174.2
035600P    OPEN I-O RAW-DATA.                                           IX1174.2
035700P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1174.2
035800P    READ RAW-DATA INVALID KEY GO TO D-END-E-2.                   IX1174.2
035900P    MOVE "OK.     " TO C-ABORT.                                  IX1174.2
036000P    MOVE PASS-COUNTER TO C-OK.                                   IX1174.2
036100P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1174.2
036200P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1174.2
036300P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1174.2
036400P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1174.2
036500P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO D-END-E-2.           IX1174.2
036600PD-END-E-2.                                                       IX1174.2
036700P    CLOSE RAW-DATA.                                              IX1174.2
036800     PERFORM D-END-ROUTINE THRU D-END-ROUTINE-13.                 IX1174.2
036900     CLOSE PRINT-FILE.                                            IX1174.2
037000 D-TERMINATE-CCVS.                                                IX1174.2
037100S    EXIT PROGRAM.                                                IX1174.2
037200SD-TERMINATE-CALL.                                                IX1174.2
037300     STOP     RUN.                                                IX1174.2
037400 D-PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.         IX1174.2
037500 D-FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.        IX1174.2
037600 D-PRINT-DETAIL.                                                  IX1174.2
037700     IF   REC-CT NOT EQUAL TO ZERO                                IX1174.2
037800          MOVE "." TO PARDOT-X                                    IX1174.2
037900          MOVE REC-CT TO DOTVALUE.                                IX1174.2
038000     MOVE TEST-RESULTS TO PRINT-REC.                              IX1174.2
038100     PERFORM D-WRITE-LINE.                                        IX1174.2
038200     IF   P-OR-F EQUAL TO "FAIL*"                                 IX1174.2
038300          PERFORM D-WRITE-LINE                                    IX1174.2
038400          PERFORM D-FAIL-ROUTINE THRU D-FAIL-ROUTINE-EX           IX1174.2
038500     ELSE                                                         IX1174.2
038600          PERFORM D-BAIL-OUT THRU D-BAIL-OUT-EX.                  IX1174.2
038700     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1174.2
038800     MOVE SPACE TO CORRECT-X.                                     IX1174.2
038900     IF   REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.           IX1174.2
039000     MOVE SPACE TO RE-MARK.                                       IX1174.2
039100 D-END-ROUTINE.                                                   IX1174.2
039200     MOVE HYPHEN-LINE TO DUMMY-RECORD.                            IX1174.2
039300     PERFORM D-WRITE-LINE 5 TIMES.                                IX1174.2
039400 D-END-RTN-EXIT.                                                  IX1174.2
039500     MOVE CCVS-E-1 TO DUMMY-RECORD.                               IX1174.2
039600     PERFORM D-WRITE-LINE 2 TIMES.                                IX1174.2
039700 D-END-ROUTINE-1.                                                 IX1174.2
039800     ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO       IX1174.2
039900     ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.                IX1174.2
040000     ADD PASS-COUNTER TO ERROR-HOLD.                              IX1174.2
040100     MOVE PASS-COUNTER TO CCVS-E-4-1.                             IX1174.2
040200     MOVE ERROR-HOLD TO CCVS-E-4-2.                               IX1174.2
040300     MOVE CCVS-E-4 TO CCVS-E-2-2.                                 IX1174.2
040400     MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM D-WRITE-LINE.          IX1174.2
040500  D-END-ROUTINE-12.                                               IX1174.2
040600     MOVE "TEST(S) FAILED" TO ENDER-DESC.                         IX1174.2
040700     IF  ERROR-COUNTER IS EQUAL TO ZERO                           IX1174.2
040800         MOVE "NO " TO ERROR-TOTAL                                IX1174.2
040900     ELSE                                                         IX1174.2
041000         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1174.2
041100     MOVE    CCVS-E-2 TO DUMMY-RECORD.                            IX1174.2
041200     PERFORM D-WRITE-LINE.                                        IX1174.2
041300 D-END-ROUTINE-13.                                                IX1174.2
041400     IF  DELETE-COUNTER IS EQUAL TO ZERO                          IX1174.2
041500         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1174.2
041600         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1174.2
041700     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1174.2
041800     MOVE CCVS-E-2 TO DUMMY-RECORD.                               IX1174.2
041900     PERFORM D-WRITE-LINE.                                        IX1174.2
042000     IF   INSPECT-COUNTER EQUAL TO ZERO                           IX1174.2
042100          MOVE "NO " TO ERROR-TOTAL                               IX1174.2
042200     ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                    IX1174.2
042300     MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.             IX1174.2
042400     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1174.2
042500     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1174.2
042600 D-WRITE-LINE.                                                    IX1174.2
042700     ADD 1 TO RECORD-COUNT.                                       IX1174.2
042800Y    IF RECORD-COUNT GREATER 42                                   IX1174.2
042900Y       MOVE DUMMY-RECORD TO DUMMY-HOLD                           IX1174.2
043000Y       MOVE SPACE TO DUMMY-RECORD                                IX1174.2
043100Y       WRITE DUMMY-RECORD AFTER ADVANCING PAGE                   IX1174.2
043200Y       MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1174.2
043300Y       MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1174.2
043400Y       MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1174.2
043500Y       MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1174.2
043600Y       MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1174.2
043700Y       MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1174.2
043800Y       MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM D-WRT-LN         IX1174.2
043900Y       MOVE DUMMY-HOLD TO DUMMY-RECORD                           IX1174.2
044000Y       MOVE ZERO TO RECORD-COUNT.                                IX1174.2
044100     PERFORM D-WRT-LN.                                            IX1174.2
044200 D-WRT-LN.                                                        IX1174.2
044300     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1174.2
044400     MOVE SPACE TO DUMMY-RECORD.                                  IX1174.2
044500 D-FAIL-ROUTINE.                                                  IX1174.2
044600     IF   COMPUTED-X NOT EQUAL TO SPACE                           IX1174.2
044700          GO TO D-FAIL-ROUTINE-WRITE.                             IX1174.2
044800     IF   CORRECT-X NOT EQUAL TO SPACE GO TO D-FAIL-ROUTINE-WRITE.IX1174.2
044900     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1174.2
045000     MOVE "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.    IX1174.2
045100     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1174.2
045200     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1174.2
045300     GO TO D-FAIL-ROUTINE-EX.                                     IX1174.2
045400 D-FAIL-ROUTINE-WRITE.                                            IX1174.2
045500     MOVE TEST-COMPUTED TO PRINT-REC PERFORM D-WRITE-LINE         IX1174.2
045600     MOVE ANSI-REFERENCE TO COR-ANSI-REFERENCE.                   IX1174.2
045700     MOVE TEST-CORRECT TO PRINT-REC PERFORM D-WRITE-LINE 2 TIMES. IX1174.2
045800     MOVE SPACES TO COR-ANSI-REFERENCE.                           IX1174.2
045900 D-FAIL-ROUTINE-EX. EXIT.                                         IX1174.2
046000 D-BAIL-OUT.                                                      IX1174.2
046100     IF  COMPUTED-A NOT EQUAL TO SPACE GO TO D-BAIL-OUT-WRITE.    IX1174.2
046200     IF  CORRECT-A EQUAL TO SPACE GO TO D-BAIL-OUT-EX.            IX1174.2
046300 D-BAIL-OUT-WRITE.                                                IX1174.2
046400     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1174.2
046500     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1174.2
046600     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1174.2
046700     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1174.2
046800 D-BAIL-OUT-EX. EXIT.                                             IX1174.2
046900 DECL-EXIT.  EXIT.                                                IX1174.2
047000 END DECLARATIVES.                                                IX1174.2
047100                                                                  IX1174.2
047200                                                                  IX1174.2
047300 CCVS1 SECTION.                                                   IX1174.2
047400 OPEN-FILES.                                                      IX1174.2
047500P    OPEN I-O RAW-DATA.                                           IX1174.2
047600P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1174.2
047700P    READ RAW-DATA INVALID KEY GO TO END-E-1.                     IX1174.2
047800P    MOVE "ABORTED " TO C-ABORT.                                  IX1174.2
047900P    ADD 1 TO C-NO-OF-TESTS.                                      IX1174.2
048000P    ACCEPT C-DATE  FROM DATE.                                    IX1174.2
048100P    ACCEPT C-TIME  FROM TIME.                                    IX1174.2
048200P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-1.             IX1174.2
048300PEND-E-1.                                                         IX1174.2
048400P    CLOSE RAW-DATA.                                              IX1174.2
048500     OPEN    OUTPUT PRINT-FILE.                                   IX1174.2
048600     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  IX1174.2
048700     MOVE    SPACE TO TEST-RESULTS.                               IX1174.2
048800     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              IX1174.2
048900     MOVE    ZERO TO REC-SKL-SUB.                                 IX1174.2
049000     PERFORM CCVS-INIT-FILE 9 TIMES.                              IX1174.2
049100 CCVS-INIT-FILE.                                                  IX1174.2
049200     ADD     1 TO REC-SKL-SUB.                                    IX1174.2
049300     MOVE    FILE-RECORD-INFO-SKELETON                            IX1174.2
049400          TO FILE-RECORD-INFO (REC-SKL-SUB).                      IX1174.2
049500 CCVS-INIT-EXIT.                                                  IX1174.2
049600     GO TO CCVS1-EXIT.                                            IX1174.2
049700 CLOSE-FILES.                                                     IX1174.2
049800P    OPEN I-O RAW-DATA.                                           IX1174.2
049900P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1174.2
050000P    READ RAW-DATA INVALID KEY GO TO END-E-2.                     IX1174.2
050100P    MOVE "OK.     " TO C-ABORT.                                  IX1174.2
050200P    MOVE PASS-COUNTER TO C-OK.                                   IX1174.2
050300P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1174.2
050400P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1174.2
050500P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1174.2
050600P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1174.2
050700P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-2.             IX1174.2
050800PEND-E-2.                                                         IX1174.2
050900P    CLOSE RAW-DATA.                                              IX1174.2
051000     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   IX1174.2
051100 TERMINATE-CCVS.                                                  IX1174.2
051200S    EXIT PROGRAM.                                                IX1174.2
051300STERMINATE-CALL.                                                  IX1174.2
051400     STOP     RUN.                                                IX1174.2
051500 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         IX1174.2
051600 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           IX1174.2
051700 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          IX1174.2
051800 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      IX1174.2
051900     MOVE "****TEST DELETED****" TO RE-MARK.                      IX1174.2
052000 PRINT-DETAIL.                                                    IX1174.2
052100     IF REC-CT NOT EQUAL TO ZERO                                  IX1174.2
052200             MOVE "." TO PARDOT-X                                 IX1174.2
052300             MOVE REC-CT TO DOTVALUE.                             IX1174.2
052400     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      IX1174.2
052500     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               IX1174.2
052600        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 IX1174.2
052700          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 IX1174.2
052800     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1174.2
052900     MOVE SPACE TO CORRECT-X.                                     IX1174.2
053000     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         IX1174.2
053100     MOVE     SPACE TO RE-MARK.                                   IX1174.2
053200 HEAD-ROUTINE.                                                    IX1174.2
053300     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1174.2
053400     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1174.2
053500     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1174.2
053600     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1174.2
053700 COLUMN-NAMES-ROUTINE.                                            IX1174.2
053800     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1174.2
053900     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1174.2
054000     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        IX1174.2
054100 END-ROUTINE.                                                     IX1174.2
054200     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.IX1174.2
054300 END-RTN-EXIT.                                                    IX1174.2
054400     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1174.2
054500 END-ROUTINE-1.                                                   IX1174.2
054600      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      IX1174.2
054700      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               IX1174.2
054800      ADD PASS-COUNTER TO ERROR-HOLD.                             IX1174.2
054900*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   IX1174.2
055000      MOVE PASS-COUNTER TO CCVS-E-4-1.                            IX1174.2
055100      MOVE ERROR-HOLD TO CCVS-E-4-2.                              IX1174.2
055200      MOVE CCVS-E-4 TO CCVS-E-2-2.                                IX1174.2
055300      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           IX1174.2
055400  END-ROUTINE-12.                                                 IX1174.2
055500      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        IX1174.2
055600     IF       ERROR-COUNTER IS EQUAL TO ZERO                      IX1174.2
055700         MOVE "NO " TO ERROR-TOTAL                                IX1174.2
055800         ELSE                                                     IX1174.2
055900         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1174.2
056000     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           IX1174.2
056100     PERFORM WRITE-LINE.                                          IX1174.2
056200 END-ROUTINE-13.                                                  IX1174.2
056300     IF DELETE-COUNTER IS EQUAL TO ZERO                           IX1174.2
056400         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1174.2
056500         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1174.2
056600     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1174.2
056700     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1174.2
056800      IF   INSPECT-COUNTER EQUAL TO ZERO                          IX1174.2
056900          MOVE "NO " TO ERROR-TOTAL                               IX1174.2
057000      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   IX1174.2
057100      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            IX1174.2
057200      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          IX1174.2
057300     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1174.2
057400 WRITE-LINE.                                                      IX1174.2
057500     ADD 1 TO RECORD-COUNT.                                       IX1174.2
057600Y    IF RECORD-COUNT GREATER 42                                   IX1174.2
057700Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          IX1174.2
057800Y        MOVE SPACE TO DUMMY-RECORD                               IX1174.2
057900Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  IX1174.2
058000Y        MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1174.2
058100Y        MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1174.2
058200Y        MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1174.2
058300Y        MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1174.2
058400Y        MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM WRT-LN            IX1174.2
058500Y        MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM WRT-LN            IX1174.2
058600Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          IX1174.2
058700Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          IX1174.2
058800Y        MOVE ZERO TO RECORD-COUNT.                               IX1174.2
058900     PERFORM WRT-LN.                                              IX1174.2
059000 WRT-LN.                                                          IX1174.2
059100     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1174.2
059200     MOVE SPACE TO DUMMY-RECORD.                                  IX1174.2
059300 BLANK-LINE-PRINT.                                                IX1174.2
059400     PERFORM WRT-LN.                                              IX1174.2
059500 FAIL-ROUTINE.                                                    IX1174.2
059600     IF     COMPUTED-X NOT EQUAL TO SPACE                         IX1174.2
059700            GO TO   FAIL-ROUTINE-WRITE.                           IX1174.2
059800     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.IX1174.2
059900     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1174.2
060000     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   IX1174.2
060100     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1174.2
060200     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1174.2
060300     GO TO  FAIL-ROUTINE-EX.                                      IX1174.2
060400 FAIL-ROUTINE-WRITE.                                              IX1174.2
060500     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         IX1174.2
060600     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 IX1174.2
060700     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. IX1174.2
060800     MOVE   SPACES TO COR-ANSI-REFERENCE.                         IX1174.2
060900 FAIL-ROUTINE-EX. EXIT.                                           IX1174.2
061000 BAIL-OUT.                                                        IX1174.2
061100     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   IX1174.2
061200     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           IX1174.2
061300 BAIL-OUT-WRITE.                                                  IX1174.2
061400     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1174.2
061500     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1174.2
061600     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1174.2
061700     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1174.2
061800 BAIL-OUT-EX. EXIT.                                               IX1174.2
061900 CCVS1-EXIT.                                                      IX1174.2
062000     EXIT.                                                        IX1174.2
062100                                                                  IX1174.2
062200 SECT-IX117A-0003 SECTION.                                        IX1174.2
062300 SEQ-INIT-010.                                                    IX1174.2
062400     MOVE ZERO TO TEST-NO.                                        IX1174.2
062500     MOVE "IX-FS3" TO XFILE-NAME (1).                             IX1174.2
062600     MOVE "R1-F-G" TO XRECORD-NAME (1).                           IX1174.2
062700     MOVE CCVS-PGM-ID TO XPROGRAM-NAME (1).                       IX1174.2
062800     MOVE 000240 TO XRECORD-LENGTH (1).                           IX1174.2
062900     MOVE "RC" TO CHARS-OR-RECORDS (1).                           IX1174.2
063000     MOVE 0002 TO XBLOCK-SIZE (1).                                IX1174.2
063100     MOVE 000050 TO RECORDS-IN-FILE (1).                          IX1174.2
063200     MOVE "IX" TO XFILE-ORGANIZATION (1).                         IX1174.2
063300     MOVE "S" TO XLABEL-TYPE (1).                                 IX1174.2
063400     MOVE 000001 TO XRECORD-NUMBER (1).                           IX1174.2
063500     MOVE 0 TO COUNT-OF-RECS.                                     IX1174.2
063600                                                                  IX1174.2
063700******************************************************************IX1174.2
063800*   TEST  1                                                      *IX1174.2
063900*         OPEN OUTPUT ...                 00 EXPECTED            *IX1174.2
064000*         IX-3, 1.3.4 (1) A                                      *IX1174.2
064100*    STATUS 00 CHECK ON OUTPUT FILE IX-FS3                       *IX1174.2
064200*    THE OUTPUT STATEMENT IS SUCCESSFULLY EXECUTED               *IX1174.2
064300******************************************************************IX1174.2
064400 OPN-INIT-GF-01-0.                                                IX1174.2
064500     MOVE 1 TO STATUS-TEST-00.                                    IX1174.2
064600     MOVE SPACES TO IX-FS3-STATUS.                                IX1174.2
064700     MOVE "OPEN I-O   : 00 EXP." TO FEATURE.                      IX1174.2
064800     MOVE "OPN-TEST-GF-01-0" TO PAR-NAME.                         IX1174.2
064900     OPEN                                                         IX1174.2
065000        I-O    IX-FS3.                                            IX1174.2
065100     IF IX-FS3-STATUS EQUAL TO "00"                               IX1174.2
065200         GO TO OPN-PASS-GF-01-0.                                  IX1174.2
065300 OPN-FAIL-GF-01-0.                                                IX1174.2
065400     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1174.2
065500     PERFORM FAIL.                                                IX1174.2
065600     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1174.2
065700     MOVE "00" TO CORRECT-X.                                      IX1174.2
065800     GO TO OPN-WRITE-GF-01-0.                                     IX1174.2
065900 OPN-PASS-GF-01-0.                                                IX1174.2
066000     PERFORM PASS.                                                IX1174.2
066100 OPN-WRITE-GF-01-0.                                               IX1174.2
066200     PERFORM PRINT-DETAIL.                                        IX1174.2
066300******************************************************************IX1174.2
066400*   TEST  4                                                      *IX1174.2
066500*         CLOSE I-O                       00 EXPECTED            *IX1174.2
066600*         IX-3, 1.3.4 (1) A                                      *IX1174.2
066700******************************************************************IX1174.2
066800 CLO-INIT-GF-01-0.                                                IX1174.2
066900     MOVE SPACES TO IX-FS3-STATUS.                                IX1174.2
067000     MOVE "CLOSE I-O   :00 EXP." TO FEATURE.                      IX1174.2
067100     MOVE "CLO-TEST-GF-01-0" TO PAR-NAME.                         IX1174.2
067200 CLO-TEST-GF-01-0.                                                IX1174.2
067300     CLOSE IX-FS3.                                                IX1174.2
067400     IF IX-FS3-STATUS = "00"                                      IX1174.2
067500         GO TO CLO-PASS-GF-01-0.                                  IX1174.2
067600 CLO-FAIL-GF-01-0.                                                IX1174.2
067700     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1174.2
067800     PERFORM FAIL.                                                IX1174.2
067900     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1174.2
068000     MOVE "00" TO CORRECT-X.                                      IX1174.2
068100     GO TO CLO-WRITE-GF-01-0.                                     IX1174.2
068200 CLO-PASS-GF-01-0.                                                IX1174.2
068300     PERFORM PASS.                                                IX1174.2
068400 CLO-WRITE-GF-01-0.                                               IX1174.2
068500     PERFORM PRINT-DETAIL.                                        IX1174.2
068600                                                                  IX1174.2
068700******************************************************************IX1174.2
068800*    A INDEXED FILE WITH 50 RECORDS HAS BEEN CREATED.            *IX1174.2
068900******************************************************************IX1174.2
069000                                                                  IX1174.2
069100******************************************************************IX1174.2
069200*   TEST  5                                                      *IX1174.2
069300*         REWRITE...  FILE NOT IN THE OPEN MODE                  *IX1174.2
069400*         FILE STATUS 49 EXPECTED IX-5, 1.3.4 (5) H              *IX1174.2
069500******************************************************************IX1174.2
069600 RWR-TEST-GF-01-0.                                                IX1174.2
069700     MOVE  5 TO TEST-NO.                                          IX1174.2
069800     MOVE SPACES TO IX-FS3-STATUS.                                IX1174.2
069900     MOVE "REWRITE      49 EXP." TO FEATURE                       IX1174.2
070000     MOVE "RWR-TEST-GF-01-0" TO PAR-NAME.                         IX1174.2
070100     REWRITE IX-FS3R1-F-G-240.                                    IX1174.2
070200 RWR-TEST-GF-01-1.                                                IX1174.2
070300     IF IX-FS3-STATUS EQUAL TO "49"                               IX1174.2
070400        MOVE "SHOULD HAVE EXECUTED DECLARATIVES IX-3,1.3.4(4)"    IX1174.2
070500          TO RE-MARK                                              IX1174.2
070600        GO TO RWR-WRITE-GF-01-0.                                  IX1174.2
070700 RWR-FAIL-GF-01-0.                                                IX1174.2
070800     MOVE "IX-5, 1.3.4, (5) H" TO RE-MARK.                        IX1174.2
070900 RWR-WRITE-GF-01-0.                                               IX1174.2
071000     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1174.2
071100     MOVE "49" TO CORRECT-X.                                      IX1174.2
071200     PERFORM FAIL.                                                IX1174.2
071300     PERFORM PRINT-DETAIL.                                        IX1174.2
071400                                                                  IX1174.2
071500 TERMINATE-ROUTINE.                                               IX1174.2
071600     EXIT.                                                        IX1174.2
071700                                                                  IX1174.2
071800 CCVS-EXIT SECTION.                                               IX1174.2
071900 CCVS-999999.                                                     IX1174.2
072000     GO TO CLOSE-FILES.                                           IX1174.2
       END PROGRAM IX117A.
000100 IDENTIFICATION DIVISION.                                         IX1184.2
000200 PROGRAM-ID.                                                      IX1184.2
000300     IX118A.                                                      IX1184.2
000400****************************************************************  IX1184.2
000500*                                                              *  IX1184.2
000600*    VALIDATION FOR:-                                          *  IX1184.2
000700*                                                              *  IX1184.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1184.2
000900*                                                              *  IX1184.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1184.2
001100*                                                              *  IX1184.2
001200****************************************************************  IX1184.2
001300*                                                                 IX1184.2
001400*    THIS ROUTINE USES THE MASS STORAGE FILE IX-FS3 CREATED IN    IX1184.2
001500*    IX113A.                                                      IX1184.2
001600*    THE FILE IS OPENED I-O AND THE STATUS CHECKED (00 EXPECTED), IX1184.2
001700*    CLOSED AND THE STATUS CHECKED (00 EXPECTED) THEN THE FILE IS IX1184.2
001800*    OPENED TWICE, AT WHICH POINT THE DECLARATIVES                IX1184.2
001900*    SECTION SHOULD BE ACTIONED AND THE FILE STATUS SHOULD BE 41  IX1184.2
002000*    STANDARD REF. IX-5 1.3.4 (5) A                               IX1184.2
002100*                                                                 IX1184.2
002200*    X-CARDS USED IN THIS PROGRAM:                                IX1184.2
002300*                                                                 IX1184.2
002400*                 XXXXX024                                        IX1184.2
002500*                 XXXXX055.                                       IX1184.2
002600*         P       XXXXX062.                                       IX1184.2
002700*                 XXXXX082.                                       IX1184.2
002800*                 XXXXX083.                                       IX1184.2
002900*         C       XXXXX084                                        IX1184.2
003000*                                                                 IX1184.2
003100*                                                                 IX1184.2
003200 ENVIRONMENT DIVISION.                                            IX1184.2
003300 CONFIGURATION SECTION.                                           IX1184.2
003400 SOURCE-COMPUTER.                                                 IX1184.2
003500     XXXXX082.                                                    IX1184.2
003600 OBJECT-COMPUTER.                                                 IX1184.2
003700     XXXXX083.                                                    IX1184.2
003800 INPUT-OUTPUT SECTION.                                            IX1184.2
003900 FILE-CONTROL.                                                    IX1184.2
004000P    SELECT RAW-DATA   ASSIGN TO                                  IX1184.2
004100P    XXXXX062                                                     IX1184.2
004200P           ORGANIZATION IS INDEXED                               IX1184.2
004300P           ACCESS MODE IS RANDOM                                 IX1184.2
004400P           RECORD KEY IS RAW-DATA-KEY.                           IX1184.2
004500*                                                                 IX1184.2
004600     SELECT PRINT-FILE ASSIGN TO                                  IX1184.2
004700     XXXXX055.                                                    IX1184.2
004800*                                                                 IX1184.2
004900     SELECT IX-FS3 ASSIGN                                         IX1184.2
005000     XXXXX024                                                     IX1184.2
005100     ORGANIZATION IS INDEXED                                      IX1184.2
005200     ACCESS MODE IS SEQUENTIAL                                    IX1184.2
005300     RECORD KEY IS IX-FS3-KEY                                     IX1184.2
005400     FILE STATUS IS IX-FS3-STATUS.                                IX1184.2
005500                                                                  IX1184.2
005600 DATA DIVISION.                                                   IX1184.2
005700                                                                  IX1184.2
005800 FILE SECTION.                                                    IX1184.2
005900P                                                                 IX1184.2
006000PFD  RAW-DATA.                                                    IX1184.2
006100P                                                                 IX1184.2
006200P01  RAW-DATA-SATZ.                                               IX1184.2
006300P    05  RAW-DATA-KEY        PIC X(6).                            IX1184.2
006400P    05  C-DATE              PIC 9(6).                            IX1184.2
006500P    05  C-TIME              PIC 9(8).                            IX1184.2
006600P    05  C-NO-OF-TESTS       PIC 99.                              IX1184.2
006700P    05  C-OK                PIC 999.                             IX1184.2
006800P    05  C-ALL               PIC 999.                             IX1184.2
006900P    05  C-FAIL              PIC 999.                             IX1184.2
007000P    05  C-DELETED           PIC 999.                             IX1184.2
007100P    05  C-INSPECT           PIC 999.                             IX1184.2
007200P    05  C-NOTE              PIC X(13).                           IX1184.2
007300P    05  C-INDENT            PIC X.                               IX1184.2
007400P    05  C-ABORT             PIC X(8).                            IX1184.2
007500                                                                  IX1184.2
007600 FD  PRINT-FILE.                                                  IX1184.2
007700                                                                  IX1184.2
007800 01  PRINT-REC               PIC X(120).                          IX1184.2
007900                                                                  IX1184.2
008000 01  DUMMY-RECORD            PIC X(120).                          IX1184.2
008100                                                                  IX1184.2
008200 FD  IX-FS3                                                       IX1184.2
008300C       DATA RECORDS IX-FS3R1-F-G-240                             IX1184.2
008400C       LABEL RECORD STANDARD                                     IX1184.2
008500        RECORD 240                                                IX1184.2
008600        BLOCK CONTAINS 2 RECORDS.                                 IX1184.2
008700                                                                  IX1184.2
008800 01  IX-FS3R1-F-G-240.                                            IX1184.2
008900     05  IX-FS3-REC-120      PIC X(120).                          IX1184.2
009000     05  IX-FS3-REC-120-240.                                      IX1184.2
009100         10  FILLER          PIC X(8).                            IX1184.2
009200         10  IX-FS3-KEY      PIC X(29).                           IX1184.2
009300         10  FILLER          PIC X(9).                            IX1184.2
009400         10  IX-FS3-ALTER-KEY  PIC X(29).                         IX1184.2
009500         10  FILLER            PIC X(45).                         IX1184.2
009600                                                                  IX1184.2
009700                                                                  IX1184.2
009800 WORKING-STORAGE SECTION.                                         IX1184.2
009900                                                                  IX1184.2
010000 01  GRP-0101.                                                    IX1184.2
010100     05  FILLER              PIC X(10) VALUE "RECORD-KEY".        IX1184.2
010200     05  GRP-0101-KEY        PIC 9(9)  VALUE ZERO.                IX1184.2
010300     05  FILLER              PIC X(10) VALUE "END-OF-KEY".        IX1184.2
010400                                                                  IX1184.2
010500 01  GRP-0102.                                                    IX1184.2
010600     05  FILLER              PIC X(10) VALUE "ALTERN-KEY".        IX1184.2
010700     05  GRP-0102-KEY        PIC 9(9)  VALUE ZERO.                IX1184.2
010800     05  FILLER              PIC X(10) VALUE "END-AL-KEY".        IX1184.2
010900                                                                  IX1184.2
011000 01  WRK-CS-09V00            PIC S9(9) COMP VALUE ZERO.           IX1184.2
011100                                                                  IX1184.2
011200 01  EOF-FLAG                PIC 9 VALUE ZERO.                    IX1184.2
011300                                                                  IX1184.2
011400 01  RECORDS-IN-ERROR        PIC S9(5) COMP VALUE ZERO.           IX1184.2
011500                                                                  IX1184.2
011600 01  ERROR-FLAG              PIC 9 VALUE ZERO.                    IX1184.2
011700                                                                  IX1184.2
011800 01  PERM-ERRORS             PIC S9(5) COMP VALUE ZERO.           IX1184.2
011900                                                                  IX1184.2
012000 01  STATUS-TEST-00          PIC 9 VALUE ZERO.                    IX1184.2
012100                                                                  IX1184.2
012200 01  STATUS-TEST-10          PIC 9 VALUE ZERO.                    IX1184.2
012300 01  STATUS-TEST-READ        PIC 9 VALUE ZERO.                    IX1184.2
012400                                                                  IX1184.2
012500 01  IX-FS3-STATUS.                                               IX1184.2
012600     05  IX-FS3-STAT1        PIC X.                               IX1184.2
012700     05  IX-FS3-STAT2        PIC X.                               IX1184.2
012800                                                                  IX1184.2
012900 01  COUNT-OF-RECS           PIC 9(5).                            IX1184.2
013000                                                                  IX1184.2
013100 01  COUNT-OF-RECORDS REDEFINES COUNT-OF-RECS  PIC 9(5).          IX1184.2
013200                                                                  IX1184.2
013300 01  FILE-RECORD-INFORMATION-REC.                                 IX1184.2
013400     05  FILE-RECORD-INFO-SKELETON.                               IX1184.2
013500         10  FILLER          PIC X(48) VALUE                      IX1184.2
013600              "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00". IX1184.2
013700         10  FILLER          PIC X(46) VALUE                      IX1184.2
013800                ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000". IX1184.2
013900         10  FILLER          PIC X(26) VALUE                      IX1184.2
014000                                    ",LFIL=000000,ORG=  ,LBLR= ". IX1184.2
014100         10  FILLER          PIC X(37) VALUE                      IX1184.2
014200                         ",RECKEY=                             ". IX1184.2
014300         10  FILLER          PIC X(38) VALUE                      IX1184.2
014400                        ",ALTKEY1=                             ". IX1184.2
014500         10  FILLER          PIC X(38) VALUE                      IX1184.2
014600                        ",ALTKEY2=                             ". IX1184.2
014700         10  FILLER          PIC X(7) VALUE SPACE.                IX1184.2
014800     05  FILE-RECORD-INFO             OCCURS 10.                  IX1184.2
014900         10  FILE-RECORD-INFO-P1-120.                             IX1184.2
015000             15  FILLER      PIC X(5).                            IX1184.2
015100             15  XFILE-NAME  PIC X(6).                            IX1184.2
015200             15  FILLER      PIC X(8).                            IX1184.2
015300             15  XRECORD-NAME  PIC X(6).                          IX1184.2
015400             15  FILLER        PIC X(1).                          IX1184.2
015500             15  REELUNIT-NUMBER  PIC 9(1).                       IX1184.2
015600             15  FILLER           PIC X(7).                       IX1184.2
015700             15  XRECORD-NUMBER   PIC 9(6).                       IX1184.2
015800             15  FILLER           PIC X(6).                       IX1184.2
015900             15  UPDATE-NUMBER    PIC 9(2).                       IX1184.2
016000             15  FILLER           PIC X(5).                       IX1184.2
016100             15  ODO-NUMBER       PIC 9(4).                       IX1184.2
016200             15  FILLER           PIC X(5).                       IX1184.2
016300             15  XPROGRAM-NAME    PIC X(5).                       IX1184.2
016400             15  FILLER           PIC X(7).                       IX1184.2
016500             15  XRECORD-LENGTH   PIC 9(6).                       IX1184.2
016600             15  FILLER           PIC X(7).                       IX1184.2
016700             15  CHARS-OR-RECORDS  PIC X(2).                      IX1184.2
016800             15  FILLER            PIC X(1).                      IX1184.2
016900             15  XBLOCK-SIZE       PIC 9(4).                      IX1184.2
017000             15  FILLER            PIC X(6).                      IX1184.2
017100             15  RECORDS-IN-FILE   PIC 9(6).                      IX1184.2
017200             15  FILLER            PIC X(5).                      IX1184.2
017300             15  XFILE-ORGANIZATION  PIC X(2).                    IX1184.2
017400             15  FILLER              PIC X(6).                    IX1184.2
017500             15  XLABEL-TYPE         PIC X(1).                    IX1184.2
017600         10  FILE-RECORD-INFO-P121-240.                           IX1184.2
017700             15  FILLER              PIC X(8).                    IX1184.2
017800             15  XRECORD-KEY         PIC X(29).                   IX1184.2
017900             15  FILLER              PIC X(9).                    IX1184.2
018000             15  ALTERNATE-KEY1      PIC X(29).                   IX1184.2
018100             15  FILLER              PIC X(9).                    IX1184.2
018200             15  ALTERNATE-KEY2      PIC X(29).                   IX1184.2
018300             15  FILLER              PIC X(7).                    IX1184.2
018400                                                                  IX1184.2
018500 01  TEST-RESULTS.                                                IX1184.2
018600     02 FILLER                   PIC X      VALUE SPACE.          IX1184.2
018700     02 FEATURE                  PIC X(20)  VALUE SPACE.          IX1184.2
018800     02 FILLER                   PIC X      VALUE SPACE.          IX1184.2
018900     02 P-OR-F                   PIC X(5)   VALUE SPACE.          IX1184.2
019000     02 FILLER                   PIC X      VALUE SPACE.          IX1184.2
019100     02  PAR-NAME.                                                IX1184.2
019200       03 FILLER                 PIC X(19)  VALUE SPACE.          IX1184.2
019300       03  PARDOT-X              PIC X      VALUE SPACE.          IX1184.2
019400       03 DOTVALUE               PIC 99     VALUE ZERO.           IX1184.2
019500     02 FILLER                   PIC X(8)   VALUE SPACE.          IX1184.2
019600     02 RE-MARK                  PIC X(61).                       IX1184.2
019700 01  TEST-COMPUTED.                                               IX1184.2
019800     02 FILLER                   PIC X(30)  VALUE SPACE.          IX1184.2
019900     02 FILLER                   PIC X(17)  VALUE                 IX1184.2
020000            "       COMPUTED=".                                   IX1184.2
020100     02 COMPUTED-X.                                               IX1184.2
020200     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          IX1184.2
020300     03 COMPUTED-N               REDEFINES COMPUTED-A             IX1184.2
020400                                 PIC -9(9).9(9).                  IX1184.2
020500     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         IX1184.2
020600     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     IX1184.2
020700     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     IX1184.2
020800     03       CM-18V0 REDEFINES COMPUTED-A.                       IX1184.2
020900         04 COMPUTED-18V0                    PIC -9(18).          IX1184.2
021000         04 FILLER                           PIC X.               IX1184.2
021100     03 FILLER PIC X(50) VALUE SPACE.                             IX1184.2
021200 01  TEST-CORRECT.                                                IX1184.2
021300     02 FILLER PIC X(30) VALUE SPACE.                             IX1184.2
021400     02 FILLER PIC X(17) VALUE "       CORRECT =".                IX1184.2
021500     02 CORRECT-X.                                                IX1184.2
021600     03 CORRECT-A                  PIC X(20) VALUE SPACE.         IX1184.2
021700     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      IX1184.2
021800     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         IX1184.2
021900     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     IX1184.2
022000     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     IX1184.2
022100     03      CR-18V0 REDEFINES CORRECT-A.                         IX1184.2
022200         04 CORRECT-18V0                     PIC -9(18).          IX1184.2
022300         04 FILLER                           PIC X.               IX1184.2
022400     03 FILLER PIC X(2) VALUE SPACE.                              IX1184.2
022500     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     IX1184.2
022600 01  CCVS-C-1.                                                    IX1184.2
022700     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PAIX1184.2
022800-    "SS  PARAGRAPH-NAME                                          IX1184.2
022900-    "       REMARKS".                                            IX1184.2
023000     02 FILLER                     PIC X(20)    VALUE SPACE.      IX1184.2
023100 01  CCVS-C-2.                                                    IX1184.2
023200     02 FILLER                     PIC X        VALUE SPACE.      IX1184.2
023300     02 FILLER                     PIC X(6)     VALUE "TESTED".   IX1184.2
023400     02 FILLER                     PIC X(15)    VALUE SPACE.      IX1184.2
023500     02 FILLER                     PIC X(4)     VALUE "FAIL".     IX1184.2
023600     02 FILLER                     PIC X(94)    VALUE SPACE.      IX1184.2
023700 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       IX1184.2
023800 01  REC-CT                        PIC 99       VALUE ZERO.       IX1184.2
023900 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       IX1184.2
024000 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       IX1184.2
024100 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       IX1184.2
024200 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       IX1184.2
024300 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       IX1184.2
024400 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       IX1184.2
024500 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      IX1184.2
024600 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       IX1184.2
024700 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     IX1184.2
024800 01  CCVS-H-1.                                                    IX1184.2
024900     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1184.2
025000     02  FILLER                    PIC X(42)    VALUE             IX1184.2
025100     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 IX1184.2
025200     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1184.2
025300 01  CCVS-H-2A.                                                   IX1184.2
025400   02  FILLER                        PIC X(40)  VALUE SPACE.      IX1184.2
025500   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  IX1184.2
025600   02  FILLER                        PIC XXXX   VALUE             IX1184.2
025700     "4.2 ".                                                      IX1184.2
025800   02  FILLER                        PIC X(28)  VALUE             IX1184.2
025900            " COPY - NOT FOR DISTRIBUTION".                       IX1184.2
026000   02  FILLER                        PIC X(41)  VALUE SPACE.      IX1184.2
026100                                                                  IX1184.2
026200 01  CCVS-H-2B.                                                   IX1184.2
026300   02  FILLER                        PIC X(15)  VALUE             IX1184.2
026400            "TEST RESULT OF ".                                    IX1184.2
026500   02  TEST-ID                       PIC X(9).                    IX1184.2
026600   02  FILLER                        PIC X(4)   VALUE             IX1184.2
026700            " IN ".                                               IX1184.2
026800   02  FILLER                        PIC X(12)  VALUE             IX1184.2
026900     " HIGH       ".                                              IX1184.2
027000   02  FILLER                        PIC X(22)  VALUE             IX1184.2
027100            " LEVEL VALIDATION FOR ".                             IX1184.2
027200   02  FILLER                        PIC X(58)  VALUE             IX1184.2
027300     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1184.2
027400 01  CCVS-H-3.                                                    IX1184.2
027500     02  FILLER                      PIC X(34)  VALUE             IX1184.2
027600            " FOR OFFICIAL USE ONLY    ".                         IX1184.2
027700     02  FILLER                      PIC X(58)  VALUE             IX1184.2
027800     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1184.2
027900     02  FILLER                      PIC X(28)  VALUE             IX1184.2
028000            "  COPYRIGHT   1985 ".                                IX1184.2
028100 01  CCVS-E-1.                                                    IX1184.2
028200     02 FILLER                       PIC X(52)  VALUE SPACE.      IX1184.2
028300     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              IX1184.2
028400     02 ID-AGAIN                     PIC X(9).                    IX1184.2
028500     02 FILLER                       PIC X(45)  VALUE SPACES.     IX1184.2
028600 01  CCVS-E-2.                                                    IX1184.2
028700     02  FILLER                      PIC X(31)  VALUE SPACE.      IX1184.2
028800     02  FILLER                      PIC X(21)  VALUE SPACE.      IX1184.2
028900     02 CCVS-E-2-2.                                               IX1184.2
029000         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      IX1184.2
029100         03 FILLER                   PIC X      VALUE SPACE.      IX1184.2
029200         03 ENDER-DESC               PIC X(44)  VALUE             IX1184.2
029300            "ERRORS ENCOUNTERED".                                 IX1184.2
029400 01  CCVS-E-3.                                                    IX1184.2
029500     02  FILLER                      PIC X(22)  VALUE             IX1184.2
029600            " FOR OFFICIAL USE ONLY".                             IX1184.2
029700     02  FILLER                      PIC X(12)  VALUE SPACE.      IX1184.2
029800     02  FILLER                      PIC X(58)  VALUE             IX1184.2
029900     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1184.2
030000     02  FILLER                      PIC X(13)  VALUE SPACE.      IX1184.2
030100     02 FILLER                       PIC X(15)  VALUE             IX1184.2
030200             " COPYRIGHT 1985".                                   IX1184.2
030300 01  CCVS-E-4.                                                    IX1184.2
030400     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      IX1184.2
030500     02 FILLER                       PIC X(4)   VALUE " OF ".     IX1184.2
030600     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      IX1184.2
030700     02 FILLER                       PIC X(40)  VALUE             IX1184.2
030800      "  TESTS WERE EXECUTED SUCCESSFULLY".                       IX1184.2
030900 01  XXINFO.                                                      IX1184.2
031000     02 FILLER                       PIC X(19)  VALUE             IX1184.2
031100            "*** INFORMATION ***".                                IX1184.2
031200     02 INFO-TEXT.                                                IX1184.2
031300       04 FILLER                     PIC X(8)   VALUE SPACE.      IX1184.2
031400       04 XXCOMPUTED                 PIC X(20).                   IX1184.2
031500       04 FILLER                     PIC X(5)   VALUE SPACE.      IX1184.2
031600       04 XXCORRECT                  PIC X(20).                   IX1184.2
031700     02 INF-ANSI-REFERENCE           PIC X(48).                   IX1184.2
031800 01  HYPHEN-LINE.                                                 IX1184.2
031900     02 FILLER  PIC IS X VALUE IS SPACE.                          IX1184.2
032000     02 FILLER  PIC IS X(65)    VALUE IS "************************IX1184.2
032100-    "*****************************************".                 IX1184.2
032200     02 FILLER  PIC IS X(54)    VALUE IS "************************IX1184.2
032300-    "******************************".                            IX1184.2
032400 01  TEST-NO                         PIC 99.                      IX1184.2
032500 01  CCVS-PGM-ID                     PIC X(9)   VALUE             IX1184.2
032600     "IX118A".                                                    IX1184.2
032700 PROCEDURE DIVISION.                                              IX1184.2
032800 DECLARATIVES.                                                    IX1184.2
032900                                                                  IX1184.2
033000 SECT-IX105-0002 SECTION.                                         IX1184.2
033100     USE AFTER EXCEPTION PROCEDURE ON IX-FS3.                     IX1184.2
033200 INPUT-PROCESS.                                                   IX1184.2
033300     IF TEST-NO = 5                                               IX1184.2
033400        GO TO D-C-TEST-GF-01-1.                                   IX1184.2
033500     IF STATUS-TEST-10 EQUAL TO 1                                 IX1184.2
033600        IF  IX-FS3-STAT1 EQUAL TO "1"                             IX1184.2
033700            MOVE 1 TO EOF-FLAG                                    IX1184.2
033800        ELSE                                                      IX1184.2
033900           IF  IX-FS3-STAT1 GREATER THAN "1"                      IX1184.2
034000           MOVE 1 TO PERM-ERRORS.                                 IX1184.2
034100     GO TO DECL-EXIT.                                             IX1184.2
034200 D-C-TEST-GF-01-1.                                                IX1184.2
034300     IF IX-FS3-STATUS EQUAL TO "41"                               IX1184.2
034400         GO TO D-C-PASS-GF-01-0.                                  IX1184.2
034500 D-C-FAIL-GF-01-0.                                                IX1184.2
034600     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1184.2
034700     MOVE "41" TO CORRECT-X.                                      IX1184.2
034800     MOVE "IX-5, 1.3.4, (5) A" TO RE-MARK.                        IX1184.2
034900     PERFORM D-FAIL.                                              IX1184.2
035000     GO TO D-C-WRITE-GF-01-0.                                     IX1184.2
035100 D-C-PASS-GF-01-0.                                                IX1184.2
035200     PERFORM D-PASS.                                              IX1184.2
035300 D-C-WRITE-GF-01-0.                                               IX1184.2
035400     PERFORM D-PRINT-DETAIL.                                      IX1184.2
035500 D-CLOSE-FILES.                                                   IX1184.2
035600P    OPEN I-O RAW-DATA.                                           IX1184.2
035700P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1184.2
035800P    READ RAW-DATA INVALID KEY GO TO D-END-E-2.                   IX1184.2
035900P    MOVE "OK.     " TO C-ABORT.                                  IX1184.2
036000P    MOVE PASS-COUNTER TO C-OK.                                   IX1184.2
036100P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1184.2
036200P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1184.2
036300P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1184.2
036400P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1184.2
036500P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO D-END-E-2.           IX1184.2
036600PD-END-E-2.                                                       IX1184.2
036700P    CLOSE RAW-DATA.                                              IX1184.2
036800     PERFORM D-END-ROUTINE THRU D-END-ROUTINE-13.                 IX1184.2
036900     CLOSE PRINT-FILE.                                            IX1184.2
037000 D-TERMINATE-CCVS.                                                IX1184.2
037100S    EXIT PROGRAM.                                                IX1184.2
037200SD-TERMINATE-CALL.                                                IX1184.2
037300     STOP     RUN.                                                IX1184.2
037400 D-PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.         IX1184.2
037500 D-FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.        IX1184.2
037600 D-PRINT-DETAIL.                                                  IX1184.2
037700     IF   REC-CT NOT EQUAL TO ZERO                                IX1184.2
037800          MOVE "." TO PARDOT-X                                    IX1184.2
037900          MOVE REC-CT TO DOTVALUE.                                IX1184.2
038000     MOVE TEST-RESULTS TO PRINT-REC.                              IX1184.2
038100     PERFORM D-WRITE-LINE.                                        IX1184.2
038200     IF   P-OR-F EQUAL TO "FAIL*"                                 IX1184.2
038300          PERFORM D-WRITE-LINE                                    IX1184.2
038400          PERFORM D-FAIL-ROUTINE THRU D-FAIL-ROUTINE-EX           IX1184.2
038500     ELSE                                                         IX1184.2
038600          PERFORM D-BAIL-OUT THRU D-BAIL-OUT-EX.                  IX1184.2
038700     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1184.2
038800     MOVE SPACE TO CORRECT-X.                                     IX1184.2
038900     IF   REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.           IX1184.2
039000     MOVE SPACE TO RE-MARK.                                       IX1184.2
039100 D-END-ROUTINE.                                                   IX1184.2
039200     MOVE HYPHEN-LINE TO DUMMY-RECORD.                            IX1184.2
039300     PERFORM D-WRITE-LINE 5 TIMES.                                IX1184.2
039400 D-END-RTN-EXIT.                                                  IX1184.2
039500     MOVE CCVS-E-1 TO DUMMY-RECORD.                               IX1184.2
039600     PERFORM D-WRITE-LINE 2 TIMES.                                IX1184.2
039700 D-END-ROUTINE-1.                                                 IX1184.2
039800     ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO       IX1184.2
039900     ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.                IX1184.2
040000     ADD PASS-COUNTER TO ERROR-HOLD.                              IX1184.2
040100     MOVE PASS-COUNTER TO CCVS-E-4-1.                             IX1184.2
040200     MOVE ERROR-HOLD TO CCVS-E-4-2.                               IX1184.2
040300     MOVE CCVS-E-4 TO CCVS-E-2-2.                                 IX1184.2
040400     MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM D-WRITE-LINE.          IX1184.2
040500  D-END-ROUTINE-12.                                               IX1184.2
040600     MOVE "TEST(S) FAILED" TO ENDER-DESC.                         IX1184.2
040700     IF  ERROR-COUNTER IS EQUAL TO ZERO                           IX1184.2
040800         MOVE "NO " TO ERROR-TOTAL                                IX1184.2
040900     ELSE                                                         IX1184.2
041000         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1184.2
041100     MOVE    CCVS-E-2 TO DUMMY-RECORD.                            IX1184.2
041200     PERFORM D-WRITE-LINE.                                        IX1184.2
041300 D-END-ROUTINE-13.                                                IX1184.2
041400     IF  DELETE-COUNTER IS EQUAL TO ZERO                          IX1184.2
041500         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1184.2
041600         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1184.2
041700     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1184.2
041800     MOVE CCVS-E-2 TO DUMMY-RECORD.                               IX1184.2
041900     PERFORM D-WRITE-LINE.                                        IX1184.2
042000     IF   INSPECT-COUNTER EQUAL TO ZERO                           IX1184.2
042100          MOVE "NO " TO ERROR-TOTAL                               IX1184.2
042200     ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                    IX1184.2
042300     MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.             IX1184.2
042400     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1184.2
042500     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1184.2
042600 D-WRITE-LINE.                                                    IX1184.2
042700     ADD 1 TO RECORD-COUNT.                                       IX1184.2
042800Y    IF RECORD-COUNT GREATER 42                                   IX1184.2
042900Y       MOVE DUMMY-RECORD TO DUMMY-HOLD                           IX1184.2
043000Y       MOVE SPACE TO DUMMY-RECORD                                IX1184.2
043100Y       WRITE DUMMY-RECORD AFTER ADVANCING PAGE                   IX1184.2
043200Y       MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1184.2
043300Y       MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1184.2
043400Y       MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1184.2
043500Y       MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1184.2
043600Y       MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1184.2
043700Y       MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1184.2
043800Y       MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM D-WRT-LN         IX1184.2
043900Y       MOVE DUMMY-HOLD TO DUMMY-RECORD                           IX1184.2
044000Y       MOVE ZERO TO RECORD-COUNT.                                IX1184.2
044100     PERFORM D-WRT-LN.                                            IX1184.2
044200 D-WRT-LN.                                                        IX1184.2
044300     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1184.2
044400     MOVE SPACE TO DUMMY-RECORD.                                  IX1184.2
044500 D-FAIL-ROUTINE.                                                  IX1184.2
044600     IF   COMPUTED-X NOT EQUAL TO SPACE                           IX1184.2
044700          GO TO D-FAIL-ROUTINE-WRITE.                             IX1184.2
044800     IF   CORRECT-X NOT EQUAL TO SPACE GO TO D-FAIL-ROUTINE-WRITE.IX1184.2
044900     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1184.2
045000     MOVE "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.    IX1184.2
045100     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1184.2
045200     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1184.2
045300     GO TO D-FAIL-ROUTINE-EX.                                     IX1184.2
045400 D-FAIL-ROUTINE-WRITE.                                            IX1184.2
045500     MOVE TEST-COMPUTED TO PRINT-REC PERFORM D-WRITE-LINE         IX1184.2
045600     MOVE ANSI-REFERENCE TO COR-ANSI-REFERENCE.                   IX1184.2
045700     MOVE TEST-CORRECT TO PRINT-REC PERFORM D-WRITE-LINE 2 TIMES. IX1184.2
045800     MOVE SPACES TO COR-ANSI-REFERENCE.                           IX1184.2
045900 D-FAIL-ROUTINE-EX. EXIT.                                         IX1184.2
046000 D-BAIL-OUT.                                                      IX1184.2
046100     IF  COMPUTED-A NOT EQUAL TO SPACE GO TO D-BAIL-OUT-WRITE.    IX1184.2
046200     IF  CORRECT-A EQUAL TO SPACE GO TO D-BAIL-OUT-EX.            IX1184.2
046300 D-BAIL-OUT-WRITE.                                                IX1184.2
046400     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1184.2
046500     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1184.2
046600     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1184.2
046700     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1184.2
046800 D-BAIL-OUT-EX. EXIT.                                             IX1184.2
046900 DECL-EXIT.  EXIT.                                                IX1184.2
047000 END DECLARATIVES.                                                IX1184.2
047100                                                                  IX1184.2
047200                                                                  IX1184.2
047300 CCVS1 SECTION.                                                   IX1184.2
047400 OPEN-FILES.                                                      IX1184.2
047500P    OPEN I-O RAW-DATA.                                           IX1184.2
047600P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1184.2
047700P    READ RAW-DATA INVALID KEY GO TO END-E-1.                     IX1184.2
047800P    MOVE "ABORTED " TO C-ABORT.                                  IX1184.2
047900P    ADD 1 TO C-NO-OF-TESTS.                                      IX1184.2
048000P    ACCEPT C-DATE  FROM DATE.                                    IX1184.2
048100P    ACCEPT C-TIME  FROM TIME.                                    IX1184.2
048200P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-1.             IX1184.2
048300PEND-E-1.                                                         IX1184.2
048400P    CLOSE RAW-DATA.                                              IX1184.2
048500     OPEN    OUTPUT PRINT-FILE.                                   IX1184.2
048600     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  IX1184.2
048700     MOVE    SPACE TO TEST-RESULTS.                               IX1184.2
048800     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              IX1184.2
048900     MOVE    ZERO TO REC-SKL-SUB.                                 IX1184.2
049000     PERFORM CCVS-INIT-FILE 9 TIMES.                              IX1184.2
049100 CCVS-INIT-FILE.                                                  IX1184.2
049200     ADD     1 TO REC-SKL-SUB.                                    IX1184.2
049300     MOVE    FILE-RECORD-INFO-SKELETON                            IX1184.2
049400          TO FILE-RECORD-INFO (REC-SKL-SUB).                      IX1184.2
049500 CCVS-INIT-EXIT.                                                  IX1184.2
049600     GO TO CCVS1-EXIT.                                            IX1184.2
049700 CLOSE-FILES.                                                     IX1184.2
049800P    OPEN I-O RAW-DATA.                                           IX1184.2
049900P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1184.2
050000P    READ RAW-DATA INVALID KEY GO TO END-E-2.                     IX1184.2
050100P    MOVE "OK.     " TO C-ABORT.                                  IX1184.2
050200P    MOVE PASS-COUNTER TO C-OK.                                   IX1184.2
050300P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1184.2
050400P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1184.2
050500P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1184.2
050600P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1184.2
050700P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-2.             IX1184.2
050800PEND-E-2.                                                         IX1184.2
050900P    CLOSE RAW-DATA.                                              IX1184.2
051000     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   IX1184.2
051100 TERMINATE-CCVS.                                                  IX1184.2
051200S    EXIT PROGRAM.                                                IX1184.2
051300STERMINATE-CALL.                                                  IX1184.2
051400     STOP     RUN.                                                IX1184.2
051500 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         IX1184.2
051600 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           IX1184.2
051700 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          IX1184.2
051800 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      IX1184.2
051900     MOVE "****TEST DELETED****" TO RE-MARK.                      IX1184.2
052000 PRINT-DETAIL.                                                    IX1184.2
052100     IF REC-CT NOT EQUAL TO ZERO                                  IX1184.2
052200             MOVE "." TO PARDOT-X                                 IX1184.2
052300             MOVE REC-CT TO DOTVALUE.                             IX1184.2
052400     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      IX1184.2
052500     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               IX1184.2
052600        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 IX1184.2
052700          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 IX1184.2
052800     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1184.2
052900     MOVE SPACE TO CORRECT-X.                                     IX1184.2
053000     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         IX1184.2
053100     MOVE     SPACE TO RE-MARK.                                   IX1184.2
053200 HEAD-ROUTINE.                                                    IX1184.2
053300     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1184.2
053400     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1184.2
053500     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1184.2
053600     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1184.2
053700 COLUMN-NAMES-ROUTINE.                                            IX1184.2
053800     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1184.2
053900     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1184.2
054000     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        IX1184.2
054100 END-ROUTINE.                                                     IX1184.2
054200     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.IX1184.2
054300 END-RTN-EXIT.                                                    IX1184.2
054400     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1184.2
054500 END-ROUTINE-1.                                                   IX1184.2
054600      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      IX1184.2
054700      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               IX1184.2
054800      ADD PASS-COUNTER TO ERROR-HOLD.                             IX1184.2
054900*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   IX1184.2
055000      MOVE PASS-COUNTER TO CCVS-E-4-1.                            IX1184.2
055100      MOVE ERROR-HOLD TO CCVS-E-4-2.                              IX1184.2
055200      MOVE CCVS-E-4 TO CCVS-E-2-2.                                IX1184.2
055300      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           IX1184.2
055400  END-ROUTINE-12.                                                 IX1184.2
055500      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        IX1184.2
055600     IF       ERROR-COUNTER IS EQUAL TO ZERO                      IX1184.2
055700         MOVE "NO " TO ERROR-TOTAL                                IX1184.2
055800         ELSE                                                     IX1184.2
055900         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1184.2
056000     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           IX1184.2
056100     PERFORM WRITE-LINE.                                          IX1184.2
056200 END-ROUTINE-13.                                                  IX1184.2
056300     IF DELETE-COUNTER IS EQUAL TO ZERO                           IX1184.2
056400         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1184.2
056500         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1184.2
056600     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1184.2
056700     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1184.2
056800      IF   INSPECT-COUNTER EQUAL TO ZERO                          IX1184.2
056900          MOVE "NO " TO ERROR-TOTAL                               IX1184.2
057000      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   IX1184.2
057100      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            IX1184.2
057200      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          IX1184.2
057300     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1184.2
057400 WRITE-LINE.                                                      IX1184.2
057500     ADD 1 TO RECORD-COUNT.                                       IX1184.2
057600Y    IF RECORD-COUNT GREATER 42                                   IX1184.2
057700Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          IX1184.2
057800Y        MOVE SPACE TO DUMMY-RECORD                               IX1184.2
057900Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  IX1184.2
058000Y        MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1184.2
058100Y        MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1184.2
058200Y        MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1184.2
058300Y        MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1184.2
058400Y        MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM WRT-LN            IX1184.2
058500Y        MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM WRT-LN            IX1184.2
058600Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          IX1184.2
058700Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          IX1184.2
058800Y        MOVE ZERO TO RECORD-COUNT.                               IX1184.2
058900     PERFORM WRT-LN.                                              IX1184.2
059000 WRT-LN.                                                          IX1184.2
059100     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1184.2
059200     MOVE SPACE TO DUMMY-RECORD.                                  IX1184.2
059300 BLANK-LINE-PRINT.                                                IX1184.2
059400     PERFORM WRT-LN.                                              IX1184.2
059500 FAIL-ROUTINE.                                                    IX1184.2
059600     IF     COMPUTED-X NOT EQUAL TO SPACE                         IX1184.2
059700            GO TO   FAIL-ROUTINE-WRITE.                           IX1184.2
059800     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.IX1184.2
059900     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1184.2
060000     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   IX1184.2
060100     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1184.2
060200     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1184.2
060300     GO TO  FAIL-ROUTINE-EX.                                      IX1184.2
060400 FAIL-ROUTINE-WRITE.                                              IX1184.2
060500     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         IX1184.2
060600     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 IX1184.2
060700     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. IX1184.2
060800     MOVE   SPACES TO COR-ANSI-REFERENCE.                         IX1184.2
060900 FAIL-ROUTINE-EX. EXIT.                                           IX1184.2
061000 BAIL-OUT.                                                        IX1184.2
061100     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   IX1184.2
061200     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           IX1184.2
061300 BAIL-OUT-WRITE.                                                  IX1184.2
061400     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1184.2
061500     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1184.2
061600     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1184.2
061700     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1184.2
061800 BAIL-OUT-EX. EXIT.                                               IX1184.2
061900 CCVS1-EXIT.                                                      IX1184.2
062000     EXIT.                                                        IX1184.2
062100                                                                  IX1184.2
062200 SECT-IX118A-0003 SECTION.                                        IX1184.2
062300 SEQ-INIT-010.                                                    IX1184.2
062400     MOVE ZERO TO TEST-NO.                                        IX1184.2
062500     MOVE "IX-FS3" TO XFILE-NAME (1).                             IX1184.2
062600     MOVE "R1-F-G" TO XRECORD-NAME (1).                           IX1184.2
062700     MOVE CCVS-PGM-ID TO XPROGRAM-NAME (1).                       IX1184.2
062800     MOVE 000240 TO XRECORD-LENGTH (1).                           IX1184.2
062900     MOVE "RC" TO CHARS-OR-RECORDS (1).                           IX1184.2
063000     MOVE 0002 TO XBLOCK-SIZE (1).                                IX1184.2
063100     MOVE 000050 TO RECORDS-IN-FILE (1).                          IX1184.2
063200     MOVE "IX" TO XFILE-ORGANIZATION (1).                         IX1184.2
063300     MOVE "S" TO XLABEL-TYPE (1).                                 IX1184.2
063400     MOVE 000001 TO XRECORD-NUMBER (1).                           IX1184.2
063500     MOVE 0 TO COUNT-OF-RECS.                                     IX1184.2
063600                                                                  IX1184.2
063700******************************************************************IX1184.2
063800*   TEST  1                                                      *IX1184.2
063900*         OPEN OUTPUT ...                 00 EXPECTED            *IX1184.2
064000*         IX-3, 1.3.4 (1) A                                      *IX1184.2
064100*    STATUS 00 CHECK ON OUTPUT FILE IX-FS3                       *IX1184.2
064200*    THE OUTPUT STATEMENT IS SUCCESSFULLY EXECUTED               *IX1184.2
064300******************************************************************IX1184.2
064400 OPN-INIT-GF-01-0.                                                IX1184.2
064500     MOVE 1 TO STATUS-TEST-00.                                    IX1184.2
064600     MOVE SPACES TO IX-FS3-STATUS.                                IX1184.2
064700     MOVE "OPEN I-O   : 00 EXP." TO FEATURE.                      IX1184.2
064800     MOVE "OPN-TEST-GF-01-0" TO PAR-NAME.                         IX1184.2
064900     OPEN                                                         IX1184.2
065000        I-O    IX-FS3.                                            IX1184.2
065100     IF IX-FS3-STATUS EQUAL TO "00"                               IX1184.2
065200         GO TO OPN-PASS-GF-01-0.                                  IX1184.2
065300 OPN-FAIL-GF-01-0.                                                IX1184.2
065400     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1184.2
065500     PERFORM FAIL.                                                IX1184.2
065600     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1184.2
065700     MOVE "00" TO CORRECT-X.                                      IX1184.2
065800     GO TO OPN-WRITE-GF-01-0.                                     IX1184.2
065900 OPN-PASS-GF-01-0.                                                IX1184.2
066000     PERFORM PASS.                                                IX1184.2
066100 OPN-WRITE-GF-01-0.                                               IX1184.2
066200     PERFORM PRINT-DETAIL.                                        IX1184.2
066300******************************************************************IX1184.2
066400*   TEST  4                                                      *IX1184.2
066500*         CLOSE I-O                       00 EXPECTED            *IX1184.2
066600*         IX-3, 1.3.4 (1) A                                      *IX1184.2
066700******************************************************************IX1184.2
066800 CLO-INIT-GF-01-0.                                                IX1184.2
066900     MOVE SPACES TO IX-FS3-STATUS.                                IX1184.2
067000     MOVE "CLOSE I-O   :00 EXP." TO FEATURE.                      IX1184.2
067100     MOVE "CLO-TEST-GF-01-0" TO PAR-NAME.                         IX1184.2
067200 CLO-TEST-GF-01-0.                                                IX1184.2
067300     CLOSE IX-FS3.                                                IX1184.2
067400     IF IX-FS3-STATUS = "00"                                      IX1184.2
067500         GO TO CLO-PASS-GF-01-0.                                  IX1184.2
067600 CLO-FAIL-GF-01-0.                                                IX1184.2
067700     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1184.2
067800     PERFORM FAIL.                                                IX1184.2
067900     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1184.2
068000     MOVE "00" TO CORRECT-X.                                      IX1184.2
068100     GO TO CLO-WRITE-GF-01-0.                                     IX1184.2
068200 CLO-PASS-GF-01-0.                                                IX1184.2
068300     PERFORM PASS.                                                IX1184.2
068400 CLO-WRITE-GF-01-0.                                               IX1184.2
068500     PERFORM PRINT-DETAIL.                                        IX1184.2
068600                                                                  IX1184.2
068700******************************************************************IX1184.2
068800*    A INDEXED FILE WITH 50 RECORDS HAS BEEN CREATED.            *IX1184.2
068900******************************************************************IX1184.2
069000                                                                  IX1184.2
069100******************************************************************IX1184.2
069200*   TEST  5                                                      *IX1184.2
069300*         OPEN FOR A FILE ALREADY IN  OPEN MODE                  *IX1184.2
069400*         FILE STATUS 41 EXPECTED IX-5, 1.3.4 (5) A              *IX1184.2
069500******************************************************************IX1184.2
069600 OPN-TEST-GF-02-0.                                                IX1184.2
069700     MOVE  5 TO TEST-NO.                                          IX1184.2
069800     MOVE SPACES TO IX-FS3-STATUS.                                IX1184.2
069900     MOVE "OPEN         41 EXP." TO FEATURE                       IX1184.2
070000     MOVE "OPN-TEST-GF-02-0" TO PAR-NAME.                         IX1184.2
070100     OPEN INPUT IX-FS3.                                           IX1184.2
070200     OPEN INPUT IX-FS3.                                           IX1184.2
070300 OPN-TEST-GF-02-1.                                                IX1184.2
070400     IF IX-FS3-STATUS EQUAL TO "41"                               IX1184.2
070500        MOVE "SHOULD HAVE EXECUTED DECLARATIVES IX-3,1.3.4(4)"    IX1184.2
070600          TO RE-MARK                                              IX1184.2
070700        GO TO OPN-WRITE-GF-02-0.                                  IX1184.2
070800 OPN-FAIL-GF-02-0.                                                IX1184.2
070900     MOVE "IX-5, 1.3.4, (5) A" TO RE-MARK.                        IX1184.2
071000 OPN-WRITE-GF-02-0.                                               IX1184.2
071100     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1184.2
071200     MOVE "41" TO CORRECT-X.                                      IX1184.2
071300     PERFORM FAIL.                                                IX1184.2
071400     PERFORM PRINT-DETAIL.                                        IX1184.2
071500     CLOSE IX-FS3.                                                IX1184.2
071600                                                                  IX1184.2
071700 TERMINATE-ROUTINE.                                               IX1184.2
071800     EXIT.                                                        IX1184.2
071900                                                                  IX1184.2
072000 CCVS-EXIT SECTION.                                               IX1184.2
072100 CCVS-999999.                                                     IX1184.2
072200     GO TO CLOSE-FILES.                                           IX1184.2
       END PROGRAM IX118A.
000100 IDENTIFICATION DIVISION.                                         IX1194.2
000200 PROGRAM-ID.                                                      IX1194.2
000300     IX119A.                                                      IX1194.2
000400****************************************************************  IX1194.2
000500*                                                              *  IX1194.2
000600*    VALIDATION FOR:-                                          *  IX1194.2
000700*                                                              *  IX1194.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1194.2
000900*                                                              *  IX1194.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1194.2
001100*                                                              *  IX1194.2
001200****************************************************************  IX1194.2
001300*                                                                 IX1194.2
001400*    THIS ROUTINE USES THE MASS STORAGE FILE IX-FS3 CREATED IN    IX1194.2
001500*    IX113A.                                                      IX1194.2
001600*    THE FILE IS OPENED I-O AND THE STATUS CHECKED (00 EXPECTED), IX1194.2
001700*    THEN AN ATTEMPT IS MADE TO REWRITE A RECORD WITH THE WRONG   IX1194.2
001800*    PRIME RECORD KEY (STATUS 21 EXPECTED).  THEN AN ATTEMPT      IX1194.2
001900*    IS MADE TO DELETE A RECORD, AT WHICH POINT THE DECLARATIVES  IX1194.2
002000*    SHOULD BE ACTIONED AND THE FILE STATUS SHOULD BE 43 .        IX1194.2
002100*                                                                 IX1194.2
002200*    STANDARD REFERENCE IX-5, 1.3.4 (3) A                         IX1194.2
002300*    STANDARD REFERENCE IX-5, 1.3.4 (5) C                         IX1194.2
002400*                                                                 IX1194.2
002500*    X-CARDS USED IN THIS PROGRAM:                                IX1194.2
002600*                                                                 IX1194.2
002700*                 XXXXX024                                        IX1194.2
002800*                 XXXXX055.                                       IX1194.2
002900*         P       XXXXX062.                                       IX1194.2
003000*                 XXXXX082.                                       IX1194.2
003100*                 XXXXX083.                                       IX1194.2
003200*         C       XXXXX084                                        IX1194.2
003300*                                                                 IX1194.2
003400*                                                                 IX1194.2
003500 ENVIRONMENT DIVISION.                                            IX1194.2
003600 CONFIGURATION SECTION.                                           IX1194.2
003700 SOURCE-COMPUTER.                                                 IX1194.2
003800     XXXXX082.                                                    IX1194.2
003900 OBJECT-COMPUTER.                                                 IX1194.2
004000     XXXXX083.                                                    IX1194.2
004100 INPUT-OUTPUT SECTION.                                            IX1194.2
004200 FILE-CONTROL.                                                    IX1194.2
004300P    SELECT RAW-DATA   ASSIGN TO                                  IX1194.2
004400P    XXXXX062                                                     IX1194.2
004500P           ORGANIZATION IS INDEXED                               IX1194.2
004600P           ACCESS MODE IS RANDOM                                 IX1194.2
004700P           RECORD KEY IS RAW-DATA-KEY.                           IX1194.2
004800*                                                                 IX1194.2
004900     SELECT PRINT-FILE ASSIGN TO                                  IX1194.2
005000     XXXXX055.                                                    IX1194.2
005100*                                                                 IX1194.2
005200     SELECT IX-FS3 ASSIGN                                         IX1194.2
005300     XXXXX024                                                     IX1194.2
005400     ORGANIZATION IS INDEXED                                      IX1194.2
005500     ACCESS MODE IS SEQUENTIAL                                    IX1194.2
005600     RECORD KEY IS IX-FS3-KEY                                     IX1194.2
005700     FILE STATUS IS IX-FS3-STATUS.                                IX1194.2
005800                                                                  IX1194.2
005900 DATA DIVISION.                                                   IX1194.2
006000                                                                  IX1194.2
006100 FILE SECTION.                                                    IX1194.2
006200P                                                                 IX1194.2
006300PFD  RAW-DATA.                                                    IX1194.2
006400P                                                                 IX1194.2
006500P01  RAW-DATA-SATZ.                                               IX1194.2
006600P    05  RAW-DATA-KEY        PIC X(6).                            IX1194.2
006700P    05  C-DATE              PIC 9(6).                            IX1194.2
006800P    05  C-TIME              PIC 9(8).                            IX1194.2
006900P    05  C-NO-OF-TESTS       PIC 99.                              IX1194.2
007000P    05  C-OK                PIC 999.                             IX1194.2
007100P    05  C-ALL               PIC 999.                             IX1194.2
007200P    05  C-FAIL              PIC 999.                             IX1194.2
007300P    05  C-DELETED           PIC 999.                             IX1194.2
007400P    05  C-INSPECT           PIC 999.                             IX1194.2
007500P    05  C-NOTE              PIC X(13).                           IX1194.2
007600P    05  C-INDENT            PIC X.                               IX1194.2
007700P    05  C-ABORT             PIC X(8).                            IX1194.2
007800                                                                  IX1194.2
007900 FD  PRINT-FILE.                                                  IX1194.2
008000                                                                  IX1194.2
008100 01  PRINT-REC               PIC X(120).                          IX1194.2
008200                                                                  IX1194.2
008300 01  DUMMY-RECORD            PIC X(120).                          IX1194.2
008400                                                                  IX1194.2
008500 FD  IX-FS3                                                       IX1194.2
008600C       DATA RECORDS IX-FS3R1-F-G-240                             IX1194.2
008700C       LABEL RECORD STANDARD                                     IX1194.2
008800        RECORD 240                                                IX1194.2
008900        BLOCK CONTAINS 2 RECORDS.                                 IX1194.2
009000                                                                  IX1194.2
009100 01  IX-FS3R1-F-G-240.                                            IX1194.2
009200     05  IX-FS3-REC-120      PIC X(120).                          IX1194.2
009300     05  IX-FS3-REC-120-240.                                      IX1194.2
009400         10  FILLER          PIC X(8).                            IX1194.2
009500         10  IX-FS3-KEY      PIC X(29).                           IX1194.2
009600         10  FILLER          PIC X(9).                            IX1194.2
009700         10  IX-FS3-ALTER-KEY  PIC X(29).                         IX1194.2
009800         10  FILLER            PIC X(45).                         IX1194.2
009900                                                                  IX1194.2
010000                                                                  IX1194.2
010100 WORKING-STORAGE SECTION.                                         IX1194.2
010200                                                                  IX1194.2
010300 01  GRP-0101.                                                    IX1194.2
010400     05  FILLER              PIC X(10) VALUE "RECORD-KEY".        IX1194.2
010500     05  GRP-0101-KEY        PIC 9(9)  VALUE ZERO.                IX1194.2
010600     05  FILLER              PIC X(10) VALUE "END-OF-KEY".        IX1194.2
010700                                                                  IX1194.2
010800 01  GRP-0102.                                                    IX1194.2
010900     05  FILLER              PIC X(10) VALUE "ALTERN-KEY".        IX1194.2
011000     05  GRP-0102-KEY        PIC 9(9)  VALUE ZERO.                IX1194.2
011100     05  FILLER              PIC X(10) VALUE "END-AL-KEY".        IX1194.2
011200                                                                  IX1194.2
011300 01  WRK-CS-09V00            PIC S9(9) COMP VALUE ZERO.           IX1194.2
011400                                                                  IX1194.2
011500 01  EOF-FLAG                PIC 9 VALUE ZERO.                    IX1194.2
011600                                                                  IX1194.2
011700 01  RECORDS-IN-ERROR        PIC S9(5) COMP VALUE ZERO.           IX1194.2
011800                                                                  IX1194.2
011900 01  ERROR-FLAG              PIC 9 VALUE ZERO.                    IX1194.2
012000                                                                  IX1194.2
012100 01  PERM-ERRORS             PIC S9(5) COMP VALUE ZERO.           IX1194.2
012200                                                                  IX1194.2
012300 01  STATUS-TEST-00          PIC 9 VALUE ZERO.                    IX1194.2
012400                                                                  IX1194.2
012500 01  STATUS-TEST-10          PIC 9 VALUE ZERO.                    IX1194.2
012600 01  STATUS-TEST-READ        PIC 9 VALUE ZERO.                    IX1194.2
012700                                                                  IX1194.2
012800 01  IX-FS3-STATUS.                                               IX1194.2
012900     05  IX-FS3-STAT1        PIC X.                               IX1194.2
013000     05  IX-FS3-STAT2        PIC X.                               IX1194.2
013100                                                                  IX1194.2
013200 01  COUNT-OF-RECS           PIC 9(5).                            IX1194.2
013300                                                                  IX1194.2
013400 01  COUNT-OF-RECORDS REDEFINES COUNT-OF-RECS  PIC 9(5).          IX1194.2
013500                                                                  IX1194.2
013600 01  FILE-RECORD-INFORMATION-REC.                                 IX1194.2
013700     05  FILE-RECORD-INFO-SKELETON.                               IX1194.2
013800         10  FILLER          PIC X(48) VALUE                      IX1194.2
013900              "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00". IX1194.2
014000         10  FILLER          PIC X(46) VALUE                      IX1194.2
014100                ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000". IX1194.2
014200         10  FILLER          PIC X(26) VALUE                      IX1194.2
014300                                    ",LFIL=000000,ORG=  ,LBLR= ". IX1194.2
014400         10  FILLER          PIC X(37) VALUE                      IX1194.2
014500                         ",RECKEY=                             ". IX1194.2
014600         10  FILLER          PIC X(38) VALUE                      IX1194.2
014700                        ",ALTKEY1=                             ". IX1194.2
014800         10  FILLER          PIC X(38) VALUE                      IX1194.2
014900                        ",ALTKEY2=                             ". IX1194.2
015000         10  FILLER          PIC X(7) VALUE SPACE.                IX1194.2
015100     05  FILE-RECORD-INFO             OCCURS 10.                  IX1194.2
015200         10  FILE-RECORD-INFO-P1-120.                             IX1194.2
015300             15  FILLER      PIC X(5).                            IX1194.2
015400             15  XFILE-NAME  PIC X(6).                            IX1194.2
015500             15  FILLER      PIC X(8).                            IX1194.2
015600             15  XRECORD-NAME  PIC X(6).                          IX1194.2
015700             15  FILLER        PIC X(1).                          IX1194.2
015800             15  REELUNIT-NUMBER  PIC 9(1).                       IX1194.2
015900             15  FILLER           PIC X(7).                       IX1194.2
016000             15  XRECORD-NUMBER   PIC 9(6).                       IX1194.2
016100             15  FILLER           PIC X(6).                       IX1194.2
016200             15  UPDATE-NUMBER    PIC 9(2).                       IX1194.2
016300             15  FILLER           PIC X(5).                       IX1194.2
016400             15  ODO-NUMBER       PIC 9(4).                       IX1194.2
016500             15  FILLER           PIC X(5).                       IX1194.2
016600             15  XPROGRAM-NAME    PIC X(5).                       IX1194.2
016700             15  FILLER           PIC X(7).                       IX1194.2
016800             15  XRECORD-LENGTH   PIC 9(6).                       IX1194.2
016900             15  FILLER           PIC X(7).                       IX1194.2
017000             15  CHARS-OR-RECORDS  PIC X(2).                      IX1194.2
017100             15  FILLER            PIC X(1).                      IX1194.2
017200             15  XBLOCK-SIZE       PIC 9(4).                      IX1194.2
017300             15  FILLER            PIC X(6).                      IX1194.2
017400             15  RECORDS-IN-FILE   PIC 9(6).                      IX1194.2
017500             15  FILLER            PIC X(5).                      IX1194.2
017600             15  XFILE-ORGANIZATION  PIC X(2).                    IX1194.2
017700             15  FILLER              PIC X(6).                    IX1194.2
017800             15  XLABEL-TYPE         PIC X(1).                    IX1194.2
017900         10  FILE-RECORD-INFO-P121-240.                           IX1194.2
018000             15  FILLER              PIC X(8).                    IX1194.2
018100             15  XRECORD-KEY         PIC X(29).                   IX1194.2
018200             15  FILLER              PIC X(9).                    IX1194.2
018300             15  ALTERNATE-KEY1      PIC X(29).                   IX1194.2
018400             15  FILLER              PIC X(9).                    IX1194.2
018500             15  ALTERNATE-KEY2      PIC X(29).                   IX1194.2
018600             15  FILLER              PIC X(7).                    IX1194.2
018700                                                                  IX1194.2
018800 01  TEST-RESULTS.                                                IX1194.2
018900     02 FILLER                   PIC X      VALUE SPACE.          IX1194.2
019000     02 FEATURE                  PIC X(20)  VALUE SPACE.          IX1194.2
019100     02 FILLER                   PIC X      VALUE SPACE.          IX1194.2
019200     02 P-OR-F                   PIC X(5)   VALUE SPACE.          IX1194.2
019300     02 FILLER                   PIC X      VALUE SPACE.          IX1194.2
019400     02  PAR-NAME.                                                IX1194.2
019500       03 FILLER                 PIC X(19)  VALUE SPACE.          IX1194.2
019600       03  PARDOT-X              PIC X      VALUE SPACE.          IX1194.2
019700       03 DOTVALUE               PIC 99     VALUE ZERO.           IX1194.2
019800     02 FILLER                   PIC X(8)   VALUE SPACE.          IX1194.2
019900     02 RE-MARK                  PIC X(61).                       IX1194.2
020000 01  TEST-COMPUTED.                                               IX1194.2
020100     02 FILLER                   PIC X(30)  VALUE SPACE.          IX1194.2
020200     02 FILLER                   PIC X(17)  VALUE                 IX1194.2
020300            "       COMPUTED=".                                   IX1194.2
020400     02 COMPUTED-X.                                               IX1194.2
020500     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          IX1194.2
020600     03 COMPUTED-N               REDEFINES COMPUTED-A             IX1194.2
020700                                 PIC -9(9).9(9).                  IX1194.2
020800     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         IX1194.2
020900     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     IX1194.2
021000     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     IX1194.2
021100     03       CM-18V0 REDEFINES COMPUTED-A.                       IX1194.2
021200         04 COMPUTED-18V0                    PIC -9(18).          IX1194.2
021300         04 FILLER                           PIC X.               IX1194.2
021400     03 FILLER PIC X(50) VALUE SPACE.                             IX1194.2
021500 01  TEST-CORRECT.                                                IX1194.2
021600     02 FILLER PIC X(30) VALUE SPACE.                             IX1194.2
021700     02 FILLER PIC X(17) VALUE "       CORRECT =".                IX1194.2
021800     02 CORRECT-X.                                                IX1194.2
021900     03 CORRECT-A                  PIC X(20) VALUE SPACE.         IX1194.2
022000     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      IX1194.2
022100     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         IX1194.2
022200     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     IX1194.2
022300     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     IX1194.2
022400     03      CR-18V0 REDEFINES CORRECT-A.                         IX1194.2
022500         04 CORRECT-18V0                     PIC -9(18).          IX1194.2
022600         04 FILLER                           PIC X.               IX1194.2
022700     03 FILLER PIC X(2) VALUE SPACE.                              IX1194.2
022800     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     IX1194.2
022900 01  CCVS-C-1.                                                    IX1194.2
023000     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PAIX1194.2
023100-    "SS  PARAGRAPH-NAME                                          IX1194.2
023200-    "       REMARKS".                                            IX1194.2
023300     02 FILLER                     PIC X(20)    VALUE SPACE.      IX1194.2
023400 01  CCVS-C-2.                                                    IX1194.2
023500     02 FILLER                     PIC X        VALUE SPACE.      IX1194.2
023600     02 FILLER                     PIC X(6)     VALUE "TESTED".   IX1194.2
023700     02 FILLER                     PIC X(15)    VALUE SPACE.      IX1194.2
023800     02 FILLER                     PIC X(4)     VALUE "FAIL".     IX1194.2
023900     02 FILLER                     PIC X(94)    VALUE SPACE.      IX1194.2
024000 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       IX1194.2
024100 01  REC-CT                        PIC 99       VALUE ZERO.       IX1194.2
024200 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       IX1194.2
024300 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       IX1194.2
024400 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       IX1194.2
024500 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       IX1194.2
024600 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       IX1194.2
024700 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       IX1194.2
024800 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      IX1194.2
024900 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       IX1194.2
025000 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     IX1194.2
025100 01  CCVS-H-1.                                                    IX1194.2
025200     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1194.2
025300     02  FILLER                    PIC X(42)    VALUE             IX1194.2
025400     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 IX1194.2
025500     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1194.2
025600 01  CCVS-H-2A.                                                   IX1194.2
025700   02  FILLER                        PIC X(40)  VALUE SPACE.      IX1194.2
025800   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  IX1194.2
025900   02  FILLER                        PIC XXXX   VALUE             IX1194.2
026000     "4.2 ".                                                      IX1194.2
026100   02  FILLER                        PIC X(28)  VALUE             IX1194.2
026200            " COPY - NOT FOR DISTRIBUTION".                       IX1194.2
026300   02  FILLER                        PIC X(41)  VALUE SPACE.      IX1194.2
026400                                                                  IX1194.2
026500 01  CCVS-H-2B.                                                   IX1194.2
026600   02  FILLER                        PIC X(15)  VALUE             IX1194.2
026700            "TEST RESULT OF ".                                    IX1194.2
026800   02  TEST-ID                       PIC X(9).                    IX1194.2
026900   02  FILLER                        PIC X(4)   VALUE             IX1194.2
027000            " IN ".                                               IX1194.2
027100   02  FILLER                        PIC X(12)  VALUE             IX1194.2
027200     " HIGH       ".                                              IX1194.2
027300   02  FILLER                        PIC X(22)  VALUE             IX1194.2
027400            " LEVEL VALIDATION FOR ".                             IX1194.2
027500   02  FILLER                        PIC X(58)  VALUE             IX1194.2
027600     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1194.2
027700 01  CCVS-H-3.                                                    IX1194.2
027800     02  FILLER                      PIC X(34)  VALUE             IX1194.2
027900            " FOR OFFICIAL USE ONLY    ".                         IX1194.2
028000     02  FILLER                      PIC X(58)  VALUE             IX1194.2
028100     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1194.2
028200     02  FILLER                      PIC X(28)  VALUE             IX1194.2
028300            "  COPYRIGHT   1985 ".                                IX1194.2
028400 01  CCVS-E-1.                                                    IX1194.2
028500     02 FILLER                       PIC X(52)  VALUE SPACE.      IX1194.2
028600     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              IX1194.2
028700     02 ID-AGAIN                     PIC X(9).                    IX1194.2
028800     02 FILLER                       PIC X(45)  VALUE SPACES.     IX1194.2
028900 01  CCVS-E-2.                                                    IX1194.2
029000     02  FILLER                      PIC X(31)  VALUE SPACE.      IX1194.2
029100     02  FILLER                      PIC X(21)  VALUE SPACE.      IX1194.2
029200     02 CCVS-E-2-2.                                               IX1194.2
029300         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      IX1194.2
029400         03 FILLER                   PIC X      VALUE SPACE.      IX1194.2
029500         03 ENDER-DESC               PIC X(44)  VALUE             IX1194.2
029600            "ERRORS ENCOUNTERED".                                 IX1194.2
029700 01  CCVS-E-3.                                                    IX1194.2
029800     02  FILLER                      PIC X(22)  VALUE             IX1194.2
029900            " FOR OFFICIAL USE ONLY".                             IX1194.2
030000     02  FILLER                      PIC X(12)  VALUE SPACE.      IX1194.2
030100     02  FILLER                      PIC X(58)  VALUE             IX1194.2
030200     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1194.2
030300     02  FILLER                      PIC X(13)  VALUE SPACE.      IX1194.2
030400     02 FILLER                       PIC X(15)  VALUE             IX1194.2
030500             " COPYRIGHT 1985".                                   IX1194.2
030600 01  CCVS-E-4.                                                    IX1194.2
030700     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      IX1194.2
030800     02 FILLER                       PIC X(4)   VALUE " OF ".     IX1194.2
030900     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      IX1194.2
031000     02 FILLER                       PIC X(40)  VALUE             IX1194.2
031100      "  TESTS WERE EXECUTED SUCCESSFULLY".                       IX1194.2
031200 01  XXINFO.                                                      IX1194.2
031300     02 FILLER                       PIC X(19)  VALUE             IX1194.2
031400            "*** INFORMATION ***".                                IX1194.2
031500     02 INFO-TEXT.                                                IX1194.2
031600       04 FILLER                     PIC X(8)   VALUE SPACE.      IX1194.2
031700       04 XXCOMPUTED                 PIC X(20).                   IX1194.2
031800       04 FILLER                     PIC X(5)   VALUE SPACE.      IX1194.2
031900       04 XXCORRECT                  PIC X(20).                   IX1194.2
032000     02 INF-ANSI-REFERENCE           PIC X(48).                   IX1194.2
032100 01  HYPHEN-LINE.                                                 IX1194.2
032200     02 FILLER  PIC IS X VALUE IS SPACE.                          IX1194.2
032300     02 FILLER  PIC IS X(65)    VALUE IS "************************IX1194.2
032400-    "*****************************************".                 IX1194.2
032500     02 FILLER  PIC IS X(54)    VALUE IS "************************IX1194.2
032600-    "******************************".                            IX1194.2
032700 01  TEST-NO                         PIC 99.                      IX1194.2
032800 01  CCVS-PGM-ID                     PIC X(9)   VALUE             IX1194.2
032900     "IX119A".                                                    IX1194.2
033000 PROCEDURE DIVISION.                                              IX1194.2
033100 DECLARATIVES.                                                    IX1194.2
033200                                                                  IX1194.2
033300 SECT-IX105-0002 SECTION.                                         IX1194.2
033400     USE AFTER EXCEPTION PROCEDURE ON IX-FS3.                     IX1194.2
033500 INPUT-PROCESS.                                                   IX1194.2
033600     IF TEST-NO = 5                                               IX1194.2
033700        GO TO D-C-TEST-GF-01-1.                                   IX1194.2
033800     IF STATUS-TEST-10 EQUAL TO 1                                 IX1194.2
033900        IF  IX-FS3-STAT1 EQUAL TO "1"                             IX1194.2
034000            MOVE 1 TO EOF-FLAG                                    IX1194.2
034100        ELSE                                                      IX1194.2
034200           IF  IX-FS3-STAT1 GREATER THAN "1"                      IX1194.2
034300           MOVE 1 TO PERM-ERRORS.                                 IX1194.2
034400     GO TO DECL-EXIT.                                             IX1194.2
034500 D-C-TEST-GF-01-1.                                                IX1194.2
034600     IF IX-FS3-STATUS EQUAL TO "43"                               IX1194.2
034700         GO TO D-C-PASS-GF-01-0.                                  IX1194.2
034800 D-C-FAIL-GF-01-0.                                                IX1194.2
034900     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1194.2
035000     MOVE "43" TO CORRECT-X.                                      IX1194.2
035100     MOVE "IX-5, 1.3.4, (5) C" TO RE-MARK.                        IX1194.2
035200     PERFORM D-FAIL.                                              IX1194.2
035300     GO TO D-C-WRITE-GF-01-0.                                     IX1194.2
035400 D-C-PASS-GF-01-0.                                                IX1194.2
035500     PERFORM D-PASS.                                              IX1194.2
035600 D-C-WRITE-GF-01-0.                                               IX1194.2
035700     PERFORM D-PRINT-DETAIL.                                      IX1194.2
035800 D-CLOSE-FILES.                                                   IX1194.2
035900     CLOSE IX-FS3.                                                IX1194.2
036000P    OPEN I-O RAW-DATA.                                           IX1194.2
036100P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1194.2
036200P    READ RAW-DATA INVALID KEY GO TO D-END-E-2.                   IX1194.2
036300P    MOVE "OK.     " TO C-ABORT.                                  IX1194.2
036400P    MOVE PASS-COUNTER TO C-OK.                                   IX1194.2
036500P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1194.2
036600P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1194.2
036700P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1194.2
036800P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1194.2
036900P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO D-END-E-2.           IX1194.2
037000PD-END-E-2.                                                       IX1194.2
037100P    CLOSE RAW-DATA.                                              IX1194.2
037200     PERFORM D-END-ROUTINE THRU D-END-ROUTINE-13.                 IX1194.2
037300     CLOSE PRINT-FILE.                                            IX1194.2
037400 D-TERMINATE-CCVS.                                                IX1194.2
037500S    EXIT PROGRAM.                                                IX1194.2
037600SD-TERMINATE-CALL.                                                IX1194.2
037700     STOP     RUN.                                                IX1194.2
037800 D-PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.         IX1194.2
037900 D-FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.        IX1194.2
038000 D-PRINT-DETAIL.                                                  IX1194.2
038100     IF   REC-CT NOT EQUAL TO ZERO                                IX1194.2
038200          MOVE "." TO PARDOT-X                                    IX1194.2
038300          MOVE REC-CT TO DOTVALUE.                                IX1194.2
038400     MOVE TEST-RESULTS TO PRINT-REC.                              IX1194.2
038500     PERFORM D-WRITE-LINE.                                        IX1194.2
038600     IF   P-OR-F EQUAL TO "FAIL*"                                 IX1194.2
038700          PERFORM D-WRITE-LINE                                    IX1194.2
038800          PERFORM D-FAIL-ROUTINE THRU D-FAIL-ROUTINE-EX           IX1194.2
038900     ELSE                                                         IX1194.2
039000          PERFORM D-BAIL-OUT THRU D-BAIL-OUT-EX.                  IX1194.2
039100     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1194.2
039200     MOVE SPACE TO CORRECT-X.                                     IX1194.2
039300     IF   REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.           IX1194.2
039400     MOVE SPACE TO RE-MARK.                                       IX1194.2
039500 D-END-ROUTINE.                                                   IX1194.2
039600     MOVE HYPHEN-LINE TO DUMMY-RECORD.                            IX1194.2
039700     PERFORM D-WRITE-LINE 5 TIMES.                                IX1194.2
039800 D-END-RTN-EXIT.                                                  IX1194.2
039900     MOVE CCVS-E-1 TO DUMMY-RECORD.                               IX1194.2
040000     PERFORM D-WRITE-LINE 2 TIMES.                                IX1194.2
040100 D-END-ROUTINE-1.                                                 IX1194.2
040200     ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO       IX1194.2
040300     ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.                IX1194.2
040400     ADD PASS-COUNTER TO ERROR-HOLD.                              IX1194.2
040500     MOVE PASS-COUNTER TO CCVS-E-4-1.                             IX1194.2
040600     MOVE ERROR-HOLD TO CCVS-E-4-2.                               IX1194.2
040700     MOVE CCVS-E-4 TO CCVS-E-2-2.                                 IX1194.2
040800     MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM D-WRITE-LINE.          IX1194.2
040900  D-END-ROUTINE-12.                                               IX1194.2
041000     MOVE "TEST(S) FAILED" TO ENDER-DESC.                         IX1194.2
041100     IF  ERROR-COUNTER IS EQUAL TO ZERO                           IX1194.2
041200         MOVE "NO " TO ERROR-TOTAL                                IX1194.2
041300     ELSE                                                         IX1194.2
041400         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1194.2
041500     MOVE    CCVS-E-2 TO DUMMY-RECORD.                            IX1194.2
041600     PERFORM D-WRITE-LINE.                                        IX1194.2
041700 D-END-ROUTINE-13.                                                IX1194.2
041800     IF  DELETE-COUNTER IS EQUAL TO ZERO                          IX1194.2
041900         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1194.2
042000         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1194.2
042100     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1194.2
042200     MOVE CCVS-E-2 TO DUMMY-RECORD.                               IX1194.2
042300     PERFORM D-WRITE-LINE.                                        IX1194.2
042400     IF   INSPECT-COUNTER EQUAL TO ZERO                           IX1194.2
042500          MOVE "NO " TO ERROR-TOTAL                               IX1194.2
042600     ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                    IX1194.2
042700     MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.             IX1194.2
042800     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1194.2
042900     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1194.2
043000 D-WRITE-LINE.                                                    IX1194.2
043100     ADD 1 TO RECORD-COUNT.                                       IX1194.2
043200Y    IF RECORD-COUNT GREATER 42                                   IX1194.2
043300Y       MOVE DUMMY-RECORD TO DUMMY-HOLD                           IX1194.2
043400Y       MOVE SPACE TO DUMMY-RECORD                                IX1194.2
043500Y       WRITE DUMMY-RECORD AFTER ADVANCING PAGE                   IX1194.2
043600Y       MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1194.2
043700Y       MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1194.2
043800Y       MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1194.2
043900Y       MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1194.2
044000Y       MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1194.2
044100Y       MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1194.2
044200Y       MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM D-WRT-LN         IX1194.2
044300Y       MOVE DUMMY-HOLD TO DUMMY-RECORD                           IX1194.2
044400Y       MOVE ZERO TO RECORD-COUNT.                                IX1194.2
044500     PERFORM D-WRT-LN.                                            IX1194.2
044600 D-WRT-LN.                                                        IX1194.2
044700     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1194.2
044800     MOVE SPACE TO DUMMY-RECORD.                                  IX1194.2
044900 D-FAIL-ROUTINE.                                                  IX1194.2
045000     IF   COMPUTED-X NOT EQUAL TO SPACE                           IX1194.2
045100          GO TO D-FAIL-ROUTINE-WRITE.                             IX1194.2
045200     IF   CORRECT-X NOT EQUAL TO SPACE GO TO D-FAIL-ROUTINE-WRITE.IX1194.2
045300     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1194.2
045400     MOVE "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.    IX1194.2
045500     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1194.2
045600     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1194.2
045700     GO TO D-FAIL-ROUTINE-EX.                                     IX1194.2
045800 D-FAIL-ROUTINE-WRITE.                                            IX1194.2
045900     MOVE TEST-COMPUTED TO PRINT-REC PERFORM D-WRITE-LINE         IX1194.2
046000     MOVE ANSI-REFERENCE TO COR-ANSI-REFERENCE.                   IX1194.2
046100     MOVE TEST-CORRECT TO PRINT-REC PERFORM D-WRITE-LINE 2 TIMES. IX1194.2
046200     MOVE SPACES TO COR-ANSI-REFERENCE.                           IX1194.2
046300 D-FAIL-ROUTINE-EX. EXIT.                                         IX1194.2
046400 D-BAIL-OUT.                                                      IX1194.2
046500     IF  COMPUTED-A NOT EQUAL TO SPACE GO TO D-BAIL-OUT-WRITE.    IX1194.2
046600     IF  CORRECT-A EQUAL TO SPACE GO TO D-BAIL-OUT-EX.            IX1194.2
046700 D-BAIL-OUT-WRITE.                                                IX1194.2
046800     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1194.2
046900     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1194.2
047000     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1194.2
047100     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1194.2
047200 D-BAIL-OUT-EX. EXIT.                                             IX1194.2
047300 DECL-EXIT.  EXIT.                                                IX1194.2
047400 END DECLARATIVES.                                                IX1194.2
047500                                                                  IX1194.2
047600                                                                  IX1194.2
047700 CCVS1 SECTION.                                                   IX1194.2
047800 OPEN-FILES.                                                      IX1194.2
047900P    OPEN I-O RAW-DATA.                                           IX1194.2
048000P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1194.2
048100P    READ RAW-DATA INVALID KEY GO TO END-E-1.                     IX1194.2
048200P    MOVE "ABORTED " TO C-ABORT.                                  IX1194.2
048300P    ADD 1 TO C-NO-OF-TESTS.                                      IX1194.2
048400P    ACCEPT C-DATE  FROM DATE.                                    IX1194.2
048500P    ACCEPT C-TIME  FROM TIME.                                    IX1194.2
048600P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-1.             IX1194.2
048700PEND-E-1.                                                         IX1194.2
048800P    CLOSE RAW-DATA.                                              IX1194.2
048900     OPEN    OUTPUT PRINT-FILE.                                   IX1194.2
049000     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  IX1194.2
049100     MOVE    SPACE TO TEST-RESULTS.                               IX1194.2
049200     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              IX1194.2
049300     MOVE    ZERO TO REC-SKL-SUB.                                 IX1194.2
049400     PERFORM CCVS-INIT-FILE 9 TIMES.                              IX1194.2
049500 CCVS-INIT-FILE.                                                  IX1194.2
049600     ADD     1 TO REC-SKL-SUB.                                    IX1194.2
049700     MOVE    FILE-RECORD-INFO-SKELETON                            IX1194.2
049800          TO FILE-RECORD-INFO (REC-SKL-SUB).                      IX1194.2
049900 CCVS-INIT-EXIT.                                                  IX1194.2
050000     GO TO CCVS1-EXIT.                                            IX1194.2
050100 CLOSE-FILES.                                                     IX1194.2
050200P    OPEN I-O RAW-DATA.                                           IX1194.2
050300P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1194.2
050400P    READ RAW-DATA INVALID KEY GO TO END-E-2.                     IX1194.2
050500P    MOVE "OK.     " TO C-ABORT.                                  IX1194.2
050600P    MOVE PASS-COUNTER TO C-OK.                                   IX1194.2
050700P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1194.2
050800P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1194.2
050900P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1194.2
051000P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1194.2
051100P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-2.             IX1194.2
051200PEND-E-2.                                                         IX1194.2
051300P    CLOSE RAW-DATA.                                              IX1194.2
051400     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   IX1194.2
051500 TERMINATE-CCVS.                                                  IX1194.2
051600S    EXIT PROGRAM.                                                IX1194.2
051700STERMINATE-CALL.                                                  IX1194.2
051800     STOP     RUN.                                                IX1194.2
051900 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         IX1194.2
052000 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           IX1194.2
052100 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          IX1194.2
052200 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      IX1194.2
052300     MOVE "****TEST DELETED****" TO RE-MARK.                      IX1194.2
052400 PRINT-DETAIL.                                                    IX1194.2
052500     IF REC-CT NOT EQUAL TO ZERO                                  IX1194.2
052600             MOVE "." TO PARDOT-X                                 IX1194.2
052700             MOVE REC-CT TO DOTVALUE.                             IX1194.2
052800     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      IX1194.2
052900     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               IX1194.2
053000        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 IX1194.2
053100          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 IX1194.2
053200     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1194.2
053300     MOVE SPACE TO CORRECT-X.                                     IX1194.2
053400     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         IX1194.2
053500     MOVE     SPACE TO RE-MARK.                                   IX1194.2
053600 HEAD-ROUTINE.                                                    IX1194.2
053700     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1194.2
053800     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1194.2
053900     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1194.2
054000     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1194.2
054100 COLUMN-NAMES-ROUTINE.                                            IX1194.2
054200     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1194.2
054300     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1194.2
054400     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        IX1194.2
054500 END-ROUTINE.                                                     IX1194.2
054600     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.IX1194.2
054700 END-RTN-EXIT.                                                    IX1194.2
054800     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1194.2
054900 END-ROUTINE-1.                                                   IX1194.2
055000      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      IX1194.2
055100      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               IX1194.2
055200      ADD PASS-COUNTER TO ERROR-HOLD.                             IX1194.2
055300*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   IX1194.2
055400      MOVE PASS-COUNTER TO CCVS-E-4-1.                            IX1194.2
055500      MOVE ERROR-HOLD TO CCVS-E-4-2.                              IX1194.2
055600      MOVE CCVS-E-4 TO CCVS-E-2-2.                                IX1194.2
055700      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           IX1194.2
055800  END-ROUTINE-12.                                                 IX1194.2
055900      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        IX1194.2
056000     IF       ERROR-COUNTER IS EQUAL TO ZERO                      IX1194.2
056100         MOVE "NO " TO ERROR-TOTAL                                IX1194.2
056200         ELSE                                                     IX1194.2
056300         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1194.2
056400     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           IX1194.2
056500     PERFORM WRITE-LINE.                                          IX1194.2
056600 END-ROUTINE-13.                                                  IX1194.2
056700     IF DELETE-COUNTER IS EQUAL TO ZERO                           IX1194.2
056800         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1194.2
056900         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1194.2
057000     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1194.2
057100     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1194.2
057200      IF   INSPECT-COUNTER EQUAL TO ZERO                          IX1194.2
057300          MOVE "NO " TO ERROR-TOTAL                               IX1194.2
057400      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   IX1194.2
057500      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            IX1194.2
057600      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          IX1194.2
057700     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1194.2
057800 WRITE-LINE.                                                      IX1194.2
057900     ADD 1 TO RECORD-COUNT.                                       IX1194.2
058000Y    IF RECORD-COUNT GREATER 42                                   IX1194.2
058100Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          IX1194.2
058200Y        MOVE SPACE TO DUMMY-RECORD                               IX1194.2
058300Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  IX1194.2
058400Y        MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1194.2
058500Y        MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1194.2
058600Y        MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1194.2
058700Y        MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1194.2
058800Y        MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM WRT-LN            IX1194.2
058900Y        MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM WRT-LN            IX1194.2
059000Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          IX1194.2
059100Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          IX1194.2
059200Y        MOVE ZERO TO RECORD-COUNT.                               IX1194.2
059300     PERFORM WRT-LN.                                              IX1194.2
059400 WRT-LN.                                                          IX1194.2
059500     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1194.2
059600     MOVE SPACE TO DUMMY-RECORD.                                  IX1194.2
059700 BLANK-LINE-PRINT.                                                IX1194.2
059800     PERFORM WRT-LN.                                              IX1194.2
059900 FAIL-ROUTINE.                                                    IX1194.2
060000     IF     COMPUTED-X NOT EQUAL TO SPACE                         IX1194.2
060100            GO TO   FAIL-ROUTINE-WRITE.                           IX1194.2
060200     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.IX1194.2
060300     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1194.2
060400     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   IX1194.2
060500     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1194.2
060600     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1194.2
060700     GO TO  FAIL-ROUTINE-EX.                                      IX1194.2
060800 FAIL-ROUTINE-WRITE.                                              IX1194.2
060900     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         IX1194.2
061000     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 IX1194.2
061100     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. IX1194.2
061200     MOVE   SPACES TO COR-ANSI-REFERENCE.                         IX1194.2
061300 FAIL-ROUTINE-EX. EXIT.                                           IX1194.2
061400 BAIL-OUT.                                                        IX1194.2
061500     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   IX1194.2
061600     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           IX1194.2
061700 BAIL-OUT-WRITE.                                                  IX1194.2
061800     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1194.2
061900     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1194.2
062000     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1194.2
062100     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1194.2
062200 BAIL-OUT-EX. EXIT.                                               IX1194.2
062300 CCVS1-EXIT.                                                      IX1194.2
062400     EXIT.                                                        IX1194.2
062500                                                                  IX1194.2
062600 SECT-IX119A-0003 SECTION.                                        IX1194.2
062700 SEQ-INIT-010.                                                    IX1194.2
062800     MOVE ZERO TO TEST-NO.                                        IX1194.2
062900     MOVE "IX-FS3" TO XFILE-NAME (1).                             IX1194.2
063000     MOVE "R1-F-G" TO XRECORD-NAME (1).                           IX1194.2
063100     MOVE CCVS-PGM-ID TO XPROGRAM-NAME (1).                       IX1194.2
063200     MOVE 000240 TO XRECORD-LENGTH (1).                           IX1194.2
063300     MOVE "RC" TO CHARS-OR-RECORDS (1).                           IX1194.2
063400     MOVE 0002 TO XBLOCK-SIZE (1).                                IX1194.2
063500     MOVE 000050 TO RECORDS-IN-FILE (1).                          IX1194.2
063600     MOVE "IX" TO XFILE-ORGANIZATION (1).                         IX1194.2
063700     MOVE "S" TO XLABEL-TYPE (1).                                 IX1194.2
063800     MOVE 000001 TO XRECORD-NUMBER (1).                           IX1194.2
063900     MOVE 0 TO COUNT-OF-RECS.                                     IX1194.2
064000                                                                  IX1194.2
064100******************************************************************IX1194.2
064200*   TEST  1                                                      *IX1194.2
064300*         OPEN OUTPUT ...                 00 EXPECTED            *IX1194.2
064400*         IX-3, 1.3.4 (1) A                                      *IX1194.2
064500*    STATUS 00 CHECK ON OUTPUT FILE IX-FS3                       *IX1194.2
064600*    THE OUTPUT STATEMENT IS SUCCESSFULLY EXECUTED               *IX1194.2
064700******************************************************************IX1194.2
064800 OPN-INIT-GF-01-0.                                                IX1194.2
064900     MOVE 1 TO STATUS-TEST-00.                                    IX1194.2
065000     MOVE SPACES TO IX-FS3-STATUS.                                IX1194.2
065100     MOVE "OPEN I-O   : 00 EXP." TO FEATURE.                      IX1194.2
065200     MOVE "OPN-TEST-GF-01-0" TO PAR-NAME.                         IX1194.2
065300     OPEN                                                         IX1194.2
065400        I-O    IX-FS3.                                            IX1194.2
065500     IF IX-FS3-STATUS EQUAL TO "00"                               IX1194.2
065600         GO TO OPN-PASS-GF-01-0.                                  IX1194.2
065700 OPN-FAIL-GF-01-0.                                                IX1194.2
065800     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1194.2
065900     PERFORM FAIL.                                                IX1194.2
066000     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1194.2
066100     MOVE "00" TO CORRECT-X.                                      IX1194.2
066200     GO TO OPN-WRITE-GF-01-0.                                     IX1194.2
066300 OPN-PASS-GF-01-0.                                                IX1194.2
066400     PERFORM PASS.                                                IX1194.2
066500 OPN-WRITE-GF-01-0.                                               IX1194.2
066600     PERFORM PRINT-DETAIL.                                        IX1194.2
066700******************************************************************IX1194.2
066800*   TEST  4                                                      *IX1194.2
066900*      REWRITE  PRIME RECORD SHOULD BE CHANGED 21 OR 22 EXPECTED  IX1194.2
067000*         IX-3, 1.3.4 (3) A                                      *IX1194.2
067100******************************************************************IX1194.2
067200 RWR-INIT-GF-01-0.                                                IX1194.2
067300     MOVE SPACES TO IX-FS3-STATUS.                                IX1194.2
067400     MOVE 0 TO STATUS-TEST-00.                                    IX1194.2
067500     MOVE "REWRITE: 21/22  EXP." TO FEATURE.                      IX1194.2
067600     MOVE "RWR-TEST-GF-01-0" TO PAR-NAME.                         IX1194.2
067700     READ IX-FS3 AT END MOVE 0 TO IX-FS3-KEY.                     IX1194.2
067800     MOVE 9 TO XRECORD-NUMBER (1).                                IX1194.2
067900 RWR-TEST-GF-01-0.                                                IX1194.2
068000     MOVE XRECORD-NUMBER (1) TO GRP-0101-KEY, COUNT-OF-RECS.      IX1194.2
068100     MOVE GRP-0101 TO XRECORD-KEY (1).                            IX1194.2
068200     MOVE GRP-0102 TO ALTERNATE-KEY1 (1).                         IX1194.2
068300     MOVE FILE-RECORD-INFO (1) TO IX-FS3R1-F-G-240.               IX1194.2
068400     REWRITE IX-FS3R1-F-G-240 INVALID KEY GO TO RWR-TEST-GF-01-1. IX1194.2
068500 RWR-TEST-GF-01-1.                                                IX1194.2
068600     IF IX-FS3-STATUS = "21"                                      IX1194.2
068700        GO TO RWR-PASS-GF-01-0.                                   IX1194.2
068800     IF IX-FS3-STATUS = "22"                                      IX1194.2
068900        GO TO RWR-PASS-GF-01-0.                                   IX1194.2
069000 RWR-FAIL-GF-01-0.                                                IX1194.2
069100     MOVE "IX-3, 1.3.4, (3) A. " TO RE-MARK.                      IX1194.2
069200     PERFORM FAIL.                                                IX1194.2
069300     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1194.2
069400     MOVE "21" TO CORRECT-X.                                      IX1194.2
069500     GO TO RWR-WRITE-GF-01-0.                                     IX1194.2
069600 RWR-PASS-GF-01-0.                                                IX1194.2
069700     PERFORM PASS.                                                IX1194.2
069800 RWR-WRITE-GF-01-0.                                               IX1194.2
069900     PERFORM PRINT-DETAIL.                                        IX1194.2
070000                                                                  IX1194.2
070100******************************************************************IX1194.2
070200*   TEST  5                                                      *IX1194.2
070300*         DELETE....  STATUS 43 EXPECTED                          IX1194.2
070400*         IX-5, 1.3.4 (5) C                                       IX1194.2
070500******************************************************************IX1194.2
070600 DEL-TEST-GF-01-0.                                                IX1194.2
070700     MOVE  5 TO TEST-NO.                                          IX1194.2
070800     MOVE SPACES TO IX-FS3-STATUS.                                IX1194.2
070900     MOVE "DELETE       43 EXP." TO FEATURE                       IX1194.2
071000     MOVE "DEL-TEST-GF-01-0" TO PAR-NAME.                         IX1194.2
071100     DELETE IX-FS3 RECORD.                                        IX1194.2
071200 DEL-TEST-GF-01-1.                                                IX1194.2
071300     IF IX-FS3-STATUS EQUAL TO "43"                               IX1194.2
071400        MOVE "SHOULD HAVE EXECUTED DECLARATIVES IX-3,1.3.4(4)"    IX1194.2
071500          TO RE-MARK                                              IX1194.2
071600        GO TO DEL-WRITE-GF-01-0.                                  IX1194.2
071700 DEL-FAIL-GF-01-0.                                                IX1194.2
071800     MOVE "IX-5, 1.3.4, (5) C" TO RE-MARK.                        IX1194.2
071900 DEL-WRITE-GF-01-0.                                               IX1194.2
072000     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1194.2
072100     MOVE "43" TO CORRECT-X.                                      IX1194.2
072200     PERFORM FAIL.                                                IX1194.2
072300     PERFORM PRINT-DETAIL.                                        IX1194.2
072400     CLOSE IX-FS3.                                                IX1194.2
072500                                                                  IX1194.2
072600 TERMINATE-ROUTINE.                                               IX1194.2
072700     EXIT.                                                        IX1194.2
072800                                                                  IX1194.2
072900 CCVS-EXIT SECTION.                                               IX1194.2
073000 CCVS-999999.                                                     IX1194.2
073100     GO TO CLOSE-FILES.                                           IX1194.2
       END PROGRAM IX119A.
000100 IDENTIFICATION DIVISION.                                         IX1204.2
000200 PROGRAM-ID.                                                      IX1204.2
000300     IX120A.                                                      IX1204.2
000400****************************************************************  IX1204.2
000500*                                                              *  IX1204.2
000600*    VALIDATION FOR:-                                          *  IX1204.2
000700*                                                              *  IX1204.2
000800*    "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1204.2
000900*                                                              *  IX1204.2
001000*    "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1204.2
001100*                                                              *  IX1204.2
001200****************************************************************  IX1204.2
001300*                                                                 IX1204.2
001400*    THIS ROUTINE USES THE MASS STORAGE FILE IX-FS3 CREATED IN    IX1204.2
001500*    IX113A.                                                      IX1204.2
001600*    THE FILE IS OPENED I-O AND THE STATUS CHECKED (00 EXPECTED), IX1204.2
001700*    THE FILE IS THEN READ UNTIL THE AT END CONDITION IS REACHED  IX1204.2
001800*    AND THEN READ ONCE MORE.  AN ATTEMPT IS THEN MADE TO REWRITE IX1204.2
001900*    A RECORD, AT WHICH POINT THE DECLARATIVES                    IX1204.2
002000*    SHOULD BE ACTIONED AND THE FILE STATUS SHOULD BE 43 .        IX1204.2
002100*                                                                 IX1204.2
002200*    STANDARD REFERENCE IX-5, 1.3.4 (5) C                         IX1204.2
002300*                                                                 IX1204.2
002400*    X-CARDS USED IN THIS PROGRAM:                                IX1204.2
002500*                                                                 IX1204.2
002600*                 XXXXX024                                        IX1204.2
002700*                 XXXXX055.                                       IX1204.2
002800*         P       XXXXX062.                                       IX1204.2
002900*                 XXXXX082.                                       IX1204.2
003000*                 XXXXX083.                                       IX1204.2
003100*         C       XXXXX084                                        IX1204.2
003200*                                                                 IX1204.2
003300*                                                                 IX1204.2
003400 ENVIRONMENT DIVISION.                                            IX1204.2
003500 CONFIGURATION SECTION.                                           IX1204.2
003600 SOURCE-COMPUTER.                                                 IX1204.2
003700     XXXXX082.                                                    IX1204.2
003800 OBJECT-COMPUTER.                                                 IX1204.2
003900     XXXXX083.                                                    IX1204.2
004000 INPUT-OUTPUT SECTION.                                            IX1204.2
004100 FILE-CONTROL.                                                    IX1204.2
004200P    SELECT RAW-DATA   ASSIGN TO                                  IX1204.2
004300P    XXXXX062                                                     IX1204.2
004400P           ORGANIZATION IS INDEXED                               IX1204.2
004500P           ACCESS MODE IS RANDOM                                 IX1204.2
004600P           RECORD KEY IS RAW-DATA-KEY.                           IX1204.2
004700*                                                                 IX1204.2
004800     SELECT PRINT-FILE ASSIGN TO                                  IX1204.2
004900     XXXXX055.                                                    IX1204.2
005000*                                                                 IX1204.2
005100     SELECT IX-FS3 ASSIGN                                         IX1204.2
005200     XXXXX024                                                     IX1204.2
005300     ORGANIZATION IS INDEXED                                      IX1204.2
005400     ACCESS MODE IS SEQUENTIAL                                    IX1204.2
005500     RECORD KEY IS IX-FS3-KEY                                     IX1204.2
005600     FILE STATUS IS IX-FS3-STATUS.                                IX1204.2
005700                                                                  IX1204.2
005800 DATA DIVISION.                                                   IX1204.2
005900                                                                  IX1204.2
006000 FILE SECTION.                                                    IX1204.2
006100P                                                                 IX1204.2
006200PFD  RAW-DATA.                                                    IX1204.2
006300P                                                                 IX1204.2
006400P01  RAW-DATA-SATZ.                                               IX1204.2
006500P    05  RAW-DATA-KEY        PIC X(6).                            IX1204.2
006600P    05  C-DATE              PIC 9(6).                            IX1204.2
006700P    05  C-TIME              PIC 9(8).                            IX1204.2
006800P    05  C-NO-OF-TESTS       PIC 99.                              IX1204.2
006900P    05  C-OK                PIC 999.                             IX1204.2
007000P    05  C-ALL               PIC 999.                             IX1204.2
007100P    05  C-FAIL              PIC 999.                             IX1204.2
007200P    05  C-DELETED           PIC 999.                             IX1204.2
007300P    05  C-INSPECT           PIC 999.                             IX1204.2
007400P    05  C-NOTE              PIC X(13).                           IX1204.2
007500P    05  C-INDENT            PIC X.                               IX1204.2
007600P    05  C-ABORT             PIC X(8).                            IX1204.2
007700                                                                  IX1204.2
007800 FD  PRINT-FILE.                                                  IX1204.2
007900                                                                  IX1204.2
008000 01  PRINT-REC               PIC X(120).                          IX1204.2
008100                                                                  IX1204.2
008200 01  DUMMY-RECORD            PIC X(120).                          IX1204.2
008300                                                                  IX1204.2
008400 FD  IX-FS3                                                       IX1204.2
008500C       DATA RECORDS IX-FS3R1-F-G-240                             IX1204.2
008600C       LABEL RECORD STANDARD                                     IX1204.2
008700        RECORD 240                                                IX1204.2
008800        BLOCK CONTAINS 2 RECORDS.                                 IX1204.2
008900                                                                  IX1204.2
009000 01  IX-FS3R1-F-G-240.                                            IX1204.2
009100     05  IX-FS3-REC-120      PIC X(120).                          IX1204.2
009200     05  IX-FS3-REC-120-240.                                      IX1204.2
009300         10  FILLER          PIC X(8).                            IX1204.2
009400         10  IX-FS3-KEY      PIC X(29).                           IX1204.2
009500         10  FILLER          PIC X(9).                            IX1204.2
009600         10  IX-FS3-ALTER-KEY  PIC X(29).                         IX1204.2
009700         10  FILLER            PIC X(45).                         IX1204.2
009800                                                                  IX1204.2
009900                                                                  IX1204.2
010000 WORKING-STORAGE SECTION.                                         IX1204.2
010100                                                                  IX1204.2
010200 01  GRP-0101.                                                    IX1204.2
010300     05  FILLER              PIC X(10) VALUE "RECORD-KEY".        IX1204.2
010400     05  GRP-0101-KEY        PIC 9(9)  VALUE ZERO.                IX1204.2
010500     05  FILLER              PIC X(10) VALUE "END-OF-KEY".        IX1204.2
010600                                                                  IX1204.2
010700 01  GRP-0102.                                                    IX1204.2
010800     05  FILLER              PIC X(10) VALUE "ALTERN-KEY".        IX1204.2
010900     05  GRP-0102-KEY        PIC 9(9)  VALUE ZERO.                IX1204.2
011000     05  FILLER              PIC X(10) VALUE "END-AL-KEY".        IX1204.2
011100                                                                  IX1204.2
011200 01  WRK-CS-09V00            PIC S9(9) COMP VALUE ZERO.           IX1204.2
011300                                                                  IX1204.2
011400 01  EOF-FLAG                PIC 9 VALUE ZERO.                    IX1204.2
011500                                                                  IX1204.2
011600 01  RECORDS-IN-ERROR        PIC S9(5) COMP VALUE ZERO.           IX1204.2
011700                                                                  IX1204.2
011800 01  ERROR-FLAG              PIC 9 VALUE ZERO.                    IX1204.2
011900                                                                  IX1204.2
012000 01  PERM-ERRORS             PIC S9(5) COMP VALUE ZERO.           IX1204.2
012100                                                                  IX1204.2
012200 01  STATUS-TEST-00          PIC 9 VALUE ZERO.                    IX1204.2
012300                                                                  IX1204.2
012400 01  STATUS-TEST-10          PIC 9 VALUE ZERO.                    IX1204.2
012500 01  STATUS-TEST-READ        PIC 9 VALUE ZERO.                    IX1204.2
012600                                                                  IX1204.2
012700 01  IX-FS3-STATUS.                                               IX1204.2
012800     05  IX-FS3-STAT1        PIC X.                               IX1204.2
012900     05  IX-FS3-STAT2        PIC X.                               IX1204.2
013000                                                                  IX1204.2
013100 01  COUNT-OF-RECS           PIC 9(5).                            IX1204.2
013200                                                                  IX1204.2
013300 01  COUNT-OF-RECORDS REDEFINES COUNT-OF-RECS  PIC 9(5).          IX1204.2
013400                                                                  IX1204.2
013500 01  FILE-RECORD-INFORMATION-REC.                                 IX1204.2
013600     05  FILE-RECORD-INFO-SKELETON.                               IX1204.2
013700         10  FILLER          PIC X(48) VALUE                      IX1204.2
013800              "FILE=      ,RECORD=      /0,RECNO=000000,UPDT=00". IX1204.2
013900         10  FILLER          PIC X(46) VALUE                      IX1204.2
014000                ",ODO=0000,PGM=     ,LRECL=000000,BLKSIZ  =0000". IX1204.2
014100         10  FILLER          PIC X(26) VALUE                      IX1204.2
014200                                    ",LFIL=000000,ORG=  ,LBLR= ". IX1204.2
014300         10  FILLER          PIC X(37) VALUE                      IX1204.2
014400                         ",RECKEY=                             ". IX1204.2
014500         10  FILLER          PIC X(38) VALUE                      IX1204.2
014600                        ",ALTKEY1=                             ". IX1204.2
014700         10  FILLER          PIC X(38) VALUE                      IX1204.2
014800                        ",ALTKEY2=                             ". IX1204.2
014900         10  FILLER          PIC X(7) VALUE SPACE.                IX1204.2
015000     05  FILE-RECORD-INFO             OCCURS 10.                  IX1204.2
015100         10  FILE-RECORD-INFO-P1-120.                             IX1204.2
015200             15  FILLER      PIC X(5).                            IX1204.2
015300             15  XFILE-NAME  PIC X(6).                            IX1204.2
015400             15  FILLER      PIC X(8).                            IX1204.2
015500             15  XRECORD-NAME  PIC X(6).                          IX1204.2
015600             15  FILLER        PIC X(1).                          IX1204.2
015700             15  REELUNIT-NUMBER  PIC 9(1).                       IX1204.2
015800             15  FILLER           PIC X(7).                       IX1204.2
015900             15  XRECORD-NUMBER   PIC 9(6).                       IX1204.2
016000             15  FILLER           PIC X(6).                       IX1204.2
016100             15  UPDATE-NUMBER    PIC 9(2).                       IX1204.2
016200             15  FILLER           PIC X(5).                       IX1204.2
016300             15  ODO-NUMBER       PIC 9(4).                       IX1204.2
016400             15  FILLER           PIC X(5).                       IX1204.2
016500             15  XPROGRAM-NAME    PIC X(5).                       IX1204.2
016600             15  FILLER           PIC X(7).                       IX1204.2
016700             15  XRECORD-LENGTH   PIC 9(6).                       IX1204.2
016800             15  FILLER           PIC X(7).                       IX1204.2
016900             15  CHARS-OR-RECORDS  PIC X(2).                      IX1204.2
017000             15  FILLER            PIC X(1).                      IX1204.2
017100             15  XBLOCK-SIZE       PIC 9(4).                      IX1204.2
017200             15  FILLER            PIC X(6).                      IX1204.2
017300             15  RECORDS-IN-FILE   PIC 9(6).                      IX1204.2
017400             15  FILLER            PIC X(5).                      IX1204.2
017500             15  XFILE-ORGANIZATION  PIC X(2).                    IX1204.2
017600             15  FILLER              PIC X(6).                    IX1204.2
017700             15  XLABEL-TYPE         PIC X(1).                    IX1204.2
017800         10  FILE-RECORD-INFO-P121-240.                           IX1204.2
017900             15  FILLER              PIC X(8).                    IX1204.2
018000             15  XRECORD-KEY         PIC X(29).                   IX1204.2
018100             15  FILLER              PIC X(9).                    IX1204.2
018200             15  ALTERNATE-KEY1      PIC X(29).                   IX1204.2
018300             15  FILLER              PIC X(9).                    IX1204.2
018400             15  ALTERNATE-KEY2      PIC X(29).                   IX1204.2
018500             15  FILLER              PIC X(7).                    IX1204.2
018600                                                                  IX1204.2
018700 01  TEST-RESULTS.                                                IX1204.2
018800     02 FILLER                   PIC X      VALUE SPACE.          IX1204.2
018900     02 FEATURE                  PIC X(20)  VALUE SPACE.          IX1204.2
019000     02 FILLER                   PIC X      VALUE SPACE.          IX1204.2
019100     02 P-OR-F                   PIC X(5)   VALUE SPACE.          IX1204.2
019200     02 FILLER                   PIC X      VALUE SPACE.          IX1204.2
019300     02  PAR-NAME.                                                IX1204.2
019400       03 FILLER                 PIC X(19)  VALUE SPACE.          IX1204.2
019500       03  PARDOT-X              PIC X      VALUE SPACE.          IX1204.2
019600       03 DOTVALUE               PIC 99     VALUE ZERO.           IX1204.2
019700     02 FILLER                   PIC X(8)   VALUE SPACE.          IX1204.2
019800     02 RE-MARK                  PIC X(61).                       IX1204.2
019900 01  TEST-COMPUTED.                                               IX1204.2
020000     02 FILLER                   PIC X(30)  VALUE SPACE.          IX1204.2
020100     02 FILLER                   PIC X(17)  VALUE                 IX1204.2
020200            "       COMPUTED=".                                   IX1204.2
020300     02 COMPUTED-X.                                               IX1204.2
020400     03 COMPUTED-A               PIC X(20)  VALUE SPACE.          IX1204.2
020500     03 COMPUTED-N               REDEFINES COMPUTED-A             IX1204.2
020600                                 PIC -9(9).9(9).                  IX1204.2
020700     03 COMPUTED-0V18 REDEFINES COMPUTED-A   PIC -.9(18).         IX1204.2
020800     03 COMPUTED-4V14 REDEFINES COMPUTED-A   PIC -9(4).9(14).     IX1204.2
020900     03 COMPUTED-14V4 REDEFINES COMPUTED-A   PIC -9(14).9(4).     IX1204.2
021000     03       CM-18V0 REDEFINES COMPUTED-A.                       IX1204.2
021100         04 COMPUTED-18V0                    PIC -9(18).          IX1204.2
021200         04 FILLER                           PIC X.               IX1204.2
021300     03 FILLER PIC X(50) VALUE SPACE.                             IX1204.2
021400 01  TEST-CORRECT.                                                IX1204.2
021500     02 FILLER PIC X(30) VALUE SPACE.                             IX1204.2
021600     02 FILLER PIC X(17) VALUE "       CORRECT =".                IX1204.2
021700     02 CORRECT-X.                                                IX1204.2
021800     03 CORRECT-A                  PIC X(20) VALUE SPACE.         IX1204.2
021900     03 CORRECT-N    REDEFINES CORRECT-A     PIC -9(9).9(9).      IX1204.2
022000     03 CORRECT-0V18 REDEFINES CORRECT-A     PIC -.9(18).         IX1204.2
022100     03 CORRECT-4V14 REDEFINES CORRECT-A     PIC -9(4).9(14).     IX1204.2
022200     03 CORRECT-14V4 REDEFINES CORRECT-A     PIC -9(14).9(4).     IX1204.2
022300     03      CR-18V0 REDEFINES CORRECT-A.                         IX1204.2
022400         04 CORRECT-18V0                     PIC -9(18).          IX1204.2
022500         04 FILLER                           PIC X.               IX1204.2
022600     03 FILLER PIC X(2) VALUE SPACE.                              IX1204.2
022700     03 COR-ANSI-REFERENCE             PIC X(48) VALUE SPACE.     IX1204.2
022800 01  CCVS-C-1.                                                    IX1204.2
022900     02 FILLER  PIC IS X(99)    VALUE IS " FEATURE              PAIX1204.2
023000-    "SS  PARAGRAPH-NAME                                          IX1204.2
023100-    "       REMARKS".                                            IX1204.2
023200     02 FILLER                     PIC X(20)    VALUE SPACE.      IX1204.2
023300 01  CCVS-C-2.                                                    IX1204.2
023400     02 FILLER                     PIC X        VALUE SPACE.      IX1204.2
023500     02 FILLER                     PIC X(6)     VALUE "TESTED".   IX1204.2
023600     02 FILLER                     PIC X(15)    VALUE SPACE.      IX1204.2
023700     02 FILLER                     PIC X(4)     VALUE "FAIL".     IX1204.2
023800     02 FILLER                     PIC X(94)    VALUE SPACE.      IX1204.2
023900 01  REC-SKL-SUB                   PIC 9(2)     VALUE ZERO.       IX1204.2
024000 01  REC-CT                        PIC 99       VALUE ZERO.       IX1204.2
024100 01  DELETE-COUNTER                PIC 999      VALUE ZERO.       IX1204.2
024200 01  ERROR-COUNTER                 PIC 999      VALUE ZERO.       IX1204.2
024300 01  INSPECT-COUNTER               PIC 999      VALUE ZERO.       IX1204.2
024400 01  PASS-COUNTER                  PIC 999      VALUE ZERO.       IX1204.2
024500 01  TOTAL-ERROR                   PIC 999      VALUE ZERO.       IX1204.2
024600 01  ERROR-HOLD                    PIC 999      VALUE ZERO.       IX1204.2
024700 01  DUMMY-HOLD                    PIC X(120)   VALUE SPACE.      IX1204.2
024800 01  RECORD-COUNT                  PIC 9(5)     VALUE ZERO.       IX1204.2
024900 01  ANSI-REFERENCE                PIC X(48)    VALUE SPACES.     IX1204.2
025000 01  CCVS-H-1.                                                    IX1204.2
025100     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1204.2
025200     02  FILLER                    PIC X(42)    VALUE             IX1204.2
025300     "OFFICIAL COBOL COMPILER VALIDATION SYSTEM".                 IX1204.2
025400     02  FILLER                    PIC X(39)    VALUE SPACES.     IX1204.2
025500 01  CCVS-H-2A.                                                   IX1204.2
025600   02  FILLER                        PIC X(40)  VALUE SPACE.      IX1204.2
025700   02  FILLER                        PIC X(7)   VALUE "CCVS85 ".  IX1204.2
025800   02  FILLER                        PIC XXXX   VALUE             IX1204.2
025900     "4.2 ".                                                      IX1204.2
026000   02  FILLER                        PIC X(28)  VALUE             IX1204.2
026100            " COPY - NOT FOR DISTRIBUTION".                       IX1204.2
026200   02  FILLER                        PIC X(41)  VALUE SPACE.      IX1204.2
026300                                                                  IX1204.2
026400 01  CCVS-H-2B.                                                   IX1204.2
026500   02  FILLER                        PIC X(15)  VALUE             IX1204.2
026600            "TEST RESULT OF ".                                    IX1204.2
026700   02  TEST-ID                       PIC X(9).                    IX1204.2
026800   02  FILLER                        PIC X(4)   VALUE             IX1204.2
026900            " IN ".                                               IX1204.2
027000   02  FILLER                        PIC X(12)  VALUE             IX1204.2
027100     " HIGH       ".                                              IX1204.2
027200   02  FILLER                        PIC X(22)  VALUE             IX1204.2
027300            " LEVEL VALIDATION FOR ".                             IX1204.2
027400   02  FILLER                        PIC X(58)  VALUE             IX1204.2
027500     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1204.2
027600 01  CCVS-H-3.                                                    IX1204.2
027700     02  FILLER                      PIC X(34)  VALUE             IX1204.2
027800            " FOR OFFICIAL USE ONLY    ".                         IX1204.2
027900     02  FILLER                      PIC X(58)  VALUE             IX1204.2
028000     "COBOL 85 VERSION 4.2, Apr  1993 SSVG                      ".IX1204.2
028100     02  FILLER                      PIC X(28)  VALUE             IX1204.2
028200            "  COPYRIGHT   1985 ".                                IX1204.2
028300 01  CCVS-E-1.                                                    IX1204.2
028400     02 FILLER                       PIC X(52)  VALUE SPACE.      IX1204.2
028500     02 FILLER  PIC X(14) VALUE IS "END OF TEST-  ".              IX1204.2
028600     02 ID-AGAIN                     PIC X(9).                    IX1204.2
028700     02 FILLER                       PIC X(45)  VALUE SPACES.     IX1204.2
028800 01  CCVS-E-2.                                                    IX1204.2
028900     02  FILLER                      PIC X(31)  VALUE SPACE.      IX1204.2
029000     02  FILLER                      PIC X(21)  VALUE SPACE.      IX1204.2
029100     02 CCVS-E-2-2.                                               IX1204.2
029200         03 ERROR-TOTAL              PIC XXX    VALUE SPACE.      IX1204.2
029300         03 FILLER                   PIC X      VALUE SPACE.      IX1204.2
029400         03 ENDER-DESC               PIC X(44)  VALUE             IX1204.2
029500            "ERRORS ENCOUNTERED".                                 IX1204.2
029600 01  CCVS-E-3.                                                    IX1204.2
029700     02  FILLER                      PIC X(22)  VALUE             IX1204.2
029800            " FOR OFFICIAL USE ONLY".                             IX1204.2
029900     02  FILLER                      PIC X(12)  VALUE SPACE.      IX1204.2
030000     02  FILLER                      PIC X(58)  VALUE             IX1204.2
030100     "ON-SITE VALIDATION, NATIONAL INSTITUTE OF STD & TECH.     ".IX1204.2
030200     02  FILLER                      PIC X(13)  VALUE SPACE.      IX1204.2
030300     02 FILLER                       PIC X(15)  VALUE             IX1204.2
030400             " COPYRIGHT 1985".                                   IX1204.2
030500 01  CCVS-E-4.                                                    IX1204.2
030600     02 CCVS-E-4-1                   PIC XXX    VALUE SPACE.      IX1204.2
030700     02 FILLER                       PIC X(4)   VALUE " OF ".     IX1204.2
030800     02 CCVS-E-4-2                   PIC XXX    VALUE SPACE.      IX1204.2
030900     02 FILLER                       PIC X(40)  VALUE             IX1204.2
031000      "  TESTS WERE EXECUTED SUCCESSFULLY".                       IX1204.2
031100 01  XXINFO.                                                      IX1204.2
031200     02 FILLER                       PIC X(19)  VALUE             IX1204.2
031300            "*** INFORMATION ***".                                IX1204.2
031400     02 INFO-TEXT.                                                IX1204.2
031500       04 FILLER                     PIC X(8)   VALUE SPACE.      IX1204.2
031600       04 XXCOMPUTED                 PIC X(20).                   IX1204.2
031700       04 FILLER                     PIC X(5)   VALUE SPACE.      IX1204.2
031800       04 XXCORRECT                  PIC X(20).                   IX1204.2
031900     02 INF-ANSI-REFERENCE           PIC X(48).                   IX1204.2
032000 01  HYPHEN-LINE.                                                 IX1204.2
032100     02 FILLER  PIC IS X VALUE IS SPACE.                          IX1204.2
032200     02 FILLER  PIC IS X(65)    VALUE IS "************************IX1204.2
032300-    "*****************************************".                 IX1204.2
032400     02 FILLER  PIC IS X(54)    VALUE IS "************************IX1204.2
032500-    "******************************".                            IX1204.2
032600 01  TEST-NO                         PIC 99.                      IX1204.2
032700 01  CCVS-PGM-ID                     PIC X(9)   VALUE             IX1204.2
032800     "IX120A".                                                    IX1204.2
032900 PROCEDURE DIVISION.                                              IX1204.2
033000 DECLARATIVES.                                                    IX1204.2
033100                                                                  IX1204.2
033200 SECT-IX105-0002 SECTION.                                         IX1204.2
033300     USE AFTER EXCEPTION PROCEDURE ON IX-FS3.                     IX1204.2
033400 INPUT-PROCESS.                                                   IX1204.2
033500     IF TEST-NO = 5                                               IX1204.2
033600        GO TO D-C-TEST-GF-01-1.                                   IX1204.2
033700     IF STATUS-TEST-10 EQUAL TO 1                                 IX1204.2
033800        IF  IX-FS3-STAT1 EQUAL TO "1"                             IX1204.2
033900            MOVE 1 TO EOF-FLAG                                    IX1204.2
034000        ELSE                                                      IX1204.2
034100           IF  IX-FS3-STAT1 GREATER THAN "1"                      IX1204.2
034200           MOVE 1 TO PERM-ERRORS.                                 IX1204.2
034300     GO TO DECL-EXIT.                                             IX1204.2
034400 D-C-TEST-GF-01-1.                                                IX1204.2
034500     IF IX-FS3-STATUS EQUAL TO "43"                               IX1204.2
034600         GO TO D-C-PASS-GF-01-0.                                  IX1204.2
034700 D-C-FAIL-GF-01-0.                                                IX1204.2
034800     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1204.2
034900     MOVE "43" TO CORRECT-X.                                      IX1204.2
035000     MOVE "IX-5, 1.3.4, (5) C" TO RE-MARK.                        IX1204.2
035100     PERFORM D-FAIL.                                              IX1204.2
035200     GO TO D-C-WRITE-GF-01-0.                                     IX1204.2
035300 D-C-PASS-GF-01-0.                                                IX1204.2
035400     PERFORM D-PASS.                                              IX1204.2
035500 D-C-WRITE-GF-01-0.                                               IX1204.2
035600     PERFORM D-PRINT-DETAIL.                                      IX1204.2
035700 D-CLOSE-FILES.                                                   IX1204.2
035800     CLOSE IX-FS3.                                                IX1204.2
035900P    OPEN I-O RAW-DATA.                                           IX1204.2
036000P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1204.2
036100P    READ RAW-DATA INVALID KEY GO TO D-END-E-2.                   IX1204.2
036200P    MOVE "OK.     " TO C-ABORT.                                  IX1204.2
036300P    MOVE PASS-COUNTER TO C-OK.                                   IX1204.2
036400P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1204.2
036500P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1204.2
036600P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1204.2
036700P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1204.2
036800P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO D-END-E-2.           IX1204.2
036900PD-END-E-2.                                                       IX1204.2
037000P    CLOSE RAW-DATA.                                              IX1204.2
037100     PERFORM D-END-ROUTINE THRU D-END-ROUTINE-13.                 IX1204.2
037200     CLOSE PRINT-FILE.                                            IX1204.2
037300 D-TERMINATE-CCVS.                                                IX1204.2
037400S    EXIT PROGRAM.                                                IX1204.2
037500SD-TERMINATE-CALL.                                                IX1204.2
037600     STOP     RUN.                                                IX1204.2
037700 D-PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.         IX1204.2
037800 D-FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.        IX1204.2
037900 D-PRINT-DETAIL.                                                  IX1204.2
038000     IF   REC-CT NOT EQUAL TO ZERO                                IX1204.2
038100          MOVE "." TO PARDOT-X                                    IX1204.2
038200          MOVE REC-CT TO DOTVALUE.                                IX1204.2
038300     MOVE TEST-RESULTS TO PRINT-REC.                              IX1204.2
038400     PERFORM D-WRITE-LINE.                                        IX1204.2
038500     IF   P-OR-F EQUAL TO "FAIL*"                                 IX1204.2
038600          PERFORM D-WRITE-LINE                                    IX1204.2
038700          PERFORM D-FAIL-ROUTINE THRU D-FAIL-ROUTINE-EX           IX1204.2
038800     ELSE                                                         IX1204.2
038900          PERFORM D-BAIL-OUT THRU D-BAIL-OUT-EX.                  IX1204.2
039000     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1204.2
039100     MOVE SPACE TO CORRECT-X.                                     IX1204.2
039200     IF   REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.           IX1204.2
039300     MOVE SPACE TO RE-MARK.                                       IX1204.2
039400 D-END-ROUTINE.                                                   IX1204.2
039500     MOVE HYPHEN-LINE TO DUMMY-RECORD.                            IX1204.2
039600     PERFORM D-WRITE-LINE 5 TIMES.                                IX1204.2
039700 D-END-RTN-EXIT.                                                  IX1204.2
039800     MOVE CCVS-E-1 TO DUMMY-RECORD.                               IX1204.2
039900     PERFORM D-WRITE-LINE 2 TIMES.                                IX1204.2
040000 D-END-ROUTINE-1.                                                 IX1204.2
040100     ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO       IX1204.2
040200     ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.                IX1204.2
040300     ADD PASS-COUNTER TO ERROR-HOLD.                              IX1204.2
040400     MOVE PASS-COUNTER TO CCVS-E-4-1.                             IX1204.2
040500     MOVE ERROR-HOLD TO CCVS-E-4-2.                               IX1204.2
040600     MOVE CCVS-E-4 TO CCVS-E-2-2.                                 IX1204.2
040700     MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM D-WRITE-LINE.          IX1204.2
040800  D-END-ROUTINE-12.                                               IX1204.2
040900     MOVE "TEST(S) FAILED" TO ENDER-DESC.                         IX1204.2
041000     IF  ERROR-COUNTER IS EQUAL TO ZERO                           IX1204.2
041100         MOVE "NO " TO ERROR-TOTAL                                IX1204.2
041200     ELSE                                                         IX1204.2
041300         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1204.2
041400     MOVE    CCVS-E-2 TO DUMMY-RECORD.                            IX1204.2
041500     PERFORM D-WRITE-LINE.                                        IX1204.2
041600 D-END-ROUTINE-13.                                                IX1204.2
041700     IF  DELETE-COUNTER IS EQUAL TO ZERO                          IX1204.2
041800         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1204.2
041900         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1204.2
042000     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1204.2
042100     MOVE CCVS-E-2 TO DUMMY-RECORD.                               IX1204.2
042200     PERFORM D-WRITE-LINE.                                        IX1204.2
042300     IF   INSPECT-COUNTER EQUAL TO ZERO                           IX1204.2
042400          MOVE "NO " TO ERROR-TOTAL                               IX1204.2
042500     ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                    IX1204.2
042600     MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.             IX1204.2
042700     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1204.2
042800     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM D-WRITE-LINE.         IX1204.2
042900 D-WRITE-LINE.                                                    IX1204.2
043000     ADD 1 TO RECORD-COUNT.                                       IX1204.2
043100Y    IF RECORD-COUNT GREATER 42                                   IX1204.2
043200Y       MOVE DUMMY-RECORD TO DUMMY-HOLD                           IX1204.2
043300Y       MOVE SPACE TO DUMMY-RECORD                                IX1204.2
043400Y       WRITE DUMMY-RECORD AFTER ADVANCING PAGE                   IX1204.2
043500Y       MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1204.2
043600Y       MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM D-WRT-LN 2 TIMES   IX1204.2
043700Y       MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1204.2
043800Y       MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM D-WRT-LN 3 TIMES   IX1204.2
043900Y       MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1204.2
044000Y       MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM D-WRT-LN           IX1204.2
044100Y       MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM D-WRT-LN         IX1204.2
044200Y       MOVE DUMMY-HOLD TO DUMMY-RECORD                           IX1204.2
044300Y       MOVE ZERO TO RECORD-COUNT.                                IX1204.2
044400     PERFORM D-WRT-LN.                                            IX1204.2
044500 D-WRT-LN.                                                        IX1204.2
044600     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1204.2
044700     MOVE SPACE TO DUMMY-RECORD.                                  IX1204.2
044800 D-FAIL-ROUTINE.                                                  IX1204.2
044900     IF   COMPUTED-X NOT EQUAL TO SPACE                           IX1204.2
045000          GO TO D-FAIL-ROUTINE-WRITE.                             IX1204.2
045100     IF   CORRECT-X NOT EQUAL TO SPACE GO TO D-FAIL-ROUTINE-WRITE.IX1204.2
045200     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1204.2
045300     MOVE "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.    IX1204.2
045400     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1204.2
045500     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1204.2
045600     GO TO D-FAIL-ROUTINE-EX.                                     IX1204.2
045700 D-FAIL-ROUTINE-WRITE.                                            IX1204.2
045800     MOVE TEST-COMPUTED TO PRINT-REC PERFORM D-WRITE-LINE         IX1204.2
045900     MOVE ANSI-REFERENCE TO COR-ANSI-REFERENCE.                   IX1204.2
046000     MOVE TEST-CORRECT TO PRINT-REC PERFORM D-WRITE-LINE 2 TIMES. IX1204.2
046100     MOVE SPACES TO COR-ANSI-REFERENCE.                           IX1204.2
046200 D-FAIL-ROUTINE-EX. EXIT.                                         IX1204.2
046300 D-BAIL-OUT.                                                      IX1204.2
046400     IF  COMPUTED-A NOT EQUAL TO SPACE GO TO D-BAIL-OUT-WRITE.    IX1204.2
046500     IF  CORRECT-A EQUAL TO SPACE GO TO D-BAIL-OUT-EX.            IX1204.2
046600 D-BAIL-OUT-WRITE.                                                IX1204.2
046700     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1204.2
046800     MOVE ANSI-REFERENCE TO INF-ANSI-REFERENCE.                   IX1204.2
046900     MOVE XXINFO TO DUMMY-RECORD. PERFORM D-WRITE-LINE 2 TIMES.   IX1204.2
047000     MOVE SPACES TO INF-ANSI-REFERENCE.                           IX1204.2
047100 D-BAIL-OUT-EX. EXIT.                                             IX1204.2
047200 DECL-EXIT.  EXIT.                                                IX1204.2
047300 END DECLARATIVES.                                                IX1204.2
047400                                                                  IX1204.2
047500                                                                  IX1204.2
047600 CCVS1 SECTION.                                                   IX1204.2
047700 OPEN-FILES.                                                      IX1204.2
047800P    OPEN I-O RAW-DATA.                                           IX1204.2
047900P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1204.2
048000P    READ RAW-DATA INVALID KEY GO TO END-E-1.                     IX1204.2
048100P    MOVE "ABORTED " TO C-ABORT.                                  IX1204.2
048200P    ADD 1 TO C-NO-OF-TESTS.                                      IX1204.2
048300P    ACCEPT C-DATE  FROM DATE.                                    IX1204.2
048400P    ACCEPT C-TIME  FROM TIME.                                    IX1204.2
048500P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-1.             IX1204.2
048600PEND-E-1.                                                         IX1204.2
048700P    CLOSE RAW-DATA.                                              IX1204.2
048800     OPEN    OUTPUT PRINT-FILE.                                   IX1204.2
048900     MOVE  CCVS-PGM-ID TO TEST-ID. MOVE CCVS-PGM-ID TO ID-AGAIN.  IX1204.2
049000     MOVE    SPACE TO TEST-RESULTS.                               IX1204.2
049100     PERFORM HEAD-ROUTINE THRU COLUMN-NAMES-ROUTINE.              IX1204.2
049200     MOVE    ZERO TO REC-SKL-SUB.                                 IX1204.2
049300     PERFORM CCVS-INIT-FILE 9 TIMES.                              IX1204.2
049400 CCVS-INIT-FILE.                                                  IX1204.2
049500     ADD     1 TO REC-SKL-SUB.                                    IX1204.2
049600     MOVE    FILE-RECORD-INFO-SKELETON                            IX1204.2
049700          TO FILE-RECORD-INFO (REC-SKL-SUB).                      IX1204.2
049800 CCVS-INIT-EXIT.                                                  IX1204.2
049900     GO TO CCVS1-EXIT.                                            IX1204.2
050000 CLOSE-FILES.                                                     IX1204.2
050100P    OPEN I-O RAW-DATA.                                           IX1204.2
050200P    MOVE CCVS-PGM-ID TO RAW-DATA-KEY.                            IX1204.2
050300P    READ RAW-DATA INVALID KEY GO TO END-E-2.                     IX1204.2
050400P    MOVE "OK.     " TO C-ABORT.                                  IX1204.2
050500P    MOVE PASS-COUNTER TO C-OK.                                   IX1204.2
050600P    MOVE ERROR-HOLD   TO C-ALL.                                  IX1204.2
050700P    MOVE ERROR-COUNTER TO C-FAIL.                                IX1204.2
050800P    MOVE DELETE-COUNTER TO C-DELETED.                            IX1204.2
050900P    MOVE INSPECT-COUNTER TO C-INSPECT.                           IX1204.2
051000P    REWRITE RAW-DATA-SATZ INVALID KEY GO TO END-E-2.             IX1204.2
051100PEND-E-2.                                                         IX1204.2
051200P    CLOSE RAW-DATA.                                              IX1204.2
051300     PERFORM END-ROUTINE THRU END-ROUTINE-13. CLOSE PRINT-FILE.   IX1204.2
051400 TERMINATE-CCVS.                                                  IX1204.2
051500S    EXIT PROGRAM.                                                IX1204.2
051600STERMINATE-CALL.                                                  IX1204.2
051700     STOP     RUN.                                                IX1204.2
051800 INSPT. MOVE "INSPT" TO P-OR-F. ADD 1 TO INSPECT-COUNTER.         IX1204.2
051900 PASS.  MOVE "PASS " TO P-OR-F.  ADD 1 TO PASS-COUNTER.           IX1204.2
052000 FAIL.  MOVE "FAIL*" TO P-OR-F.  ADD 1 TO ERROR-COUNTER.          IX1204.2
052100 DE-LETE.  MOVE "*****" TO P-OR-F.  ADD 1 TO DELETE-COUNTER.      IX1204.2
052200     MOVE "****TEST DELETED****" TO RE-MARK.                      IX1204.2
052300 PRINT-DETAIL.                                                    IX1204.2
052400     IF REC-CT NOT EQUAL TO ZERO                                  IX1204.2
052500             MOVE "." TO PARDOT-X                                 IX1204.2
052600             MOVE REC-CT TO DOTVALUE.                             IX1204.2
052700     MOVE     TEST-RESULTS TO PRINT-REC. PERFORM WRITE-LINE.      IX1204.2
052800     IF P-OR-F EQUAL TO "FAIL*"  PERFORM WRITE-LINE               IX1204.2
052900        PERFORM FAIL-ROUTINE THRU FAIL-ROUTINE-EX                 IX1204.2
053000          ELSE PERFORM BAIL-OUT THRU BAIL-OUT-EX.                 IX1204.2
053100     MOVE SPACE TO P-OR-F. MOVE SPACE TO COMPUTED-X.              IX1204.2
053200     MOVE SPACE TO CORRECT-X.                                     IX1204.2
053300     IF     REC-CT EQUAL TO ZERO  MOVE SPACE TO PAR-NAME.         IX1204.2
053400     MOVE     SPACE TO RE-MARK.                                   IX1204.2
053500 HEAD-ROUTINE.                                                    IX1204.2
053600     MOVE CCVS-H-1  TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1204.2
053700     MOVE CCVS-H-2A TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.  IX1204.2
053800     MOVE CCVS-H-2B TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1204.2
053900     MOVE CCVS-H-3  TO DUMMY-RECORD. PERFORM WRITE-LINE 3 TIMES.  IX1204.2
054000 COLUMN-NAMES-ROUTINE.                                            IX1204.2
054100     MOVE CCVS-C-1 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1204.2
054200     MOVE CCVS-C-2 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1204.2
054300     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE.        IX1204.2
054400 END-ROUTINE.                                                     IX1204.2
054500     MOVE HYPHEN-LINE TO DUMMY-RECORD. PERFORM WRITE-LINE 5 TIMES.IX1204.2
054600 END-RTN-EXIT.                                                    IX1204.2
054700     MOVE CCVS-E-1 TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1204.2
054800 END-ROUTINE-1.                                                   IX1204.2
054900      ADD ERROR-COUNTER TO ERROR-HOLD ADD INSPECT-COUNTER TO      IX1204.2
055000      ERROR-HOLD. ADD DELETE-COUNTER TO ERROR-HOLD.               IX1204.2
055100      ADD PASS-COUNTER TO ERROR-HOLD.                             IX1204.2
055200*     IF PASS-COUNTER EQUAL TO ERROR-HOLD GO TO END-ROUTINE-12.   IX1204.2
055300      MOVE PASS-COUNTER TO CCVS-E-4-1.                            IX1204.2
055400      MOVE ERROR-HOLD TO CCVS-E-4-2.                              IX1204.2
055500      MOVE CCVS-E-4 TO CCVS-E-2-2.                                IX1204.2
055600      MOVE CCVS-E-2 TO DUMMY-RECORD PERFORM WRITE-LINE.           IX1204.2
055700  END-ROUTINE-12.                                                 IX1204.2
055800      MOVE "TEST(S) FAILED" TO ENDER-DESC.                        IX1204.2
055900     IF       ERROR-COUNTER IS EQUAL TO ZERO                      IX1204.2
056000         MOVE "NO " TO ERROR-TOTAL                                IX1204.2
056100         ELSE                                                     IX1204.2
056200         MOVE ERROR-COUNTER TO ERROR-TOTAL.                       IX1204.2
056300     MOVE     CCVS-E-2 TO DUMMY-RECORD.                           IX1204.2
056400     PERFORM WRITE-LINE.                                          IX1204.2
056500 END-ROUTINE-13.                                                  IX1204.2
056600     IF DELETE-COUNTER IS EQUAL TO ZERO                           IX1204.2
056700         MOVE "NO " TO ERROR-TOTAL  ELSE                          IX1204.2
056800         MOVE DELETE-COUNTER TO ERROR-TOTAL.                      IX1204.2
056900     MOVE "TEST(S) DELETED     " TO ENDER-DESC.                   IX1204.2
057000     MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1204.2
057100      IF   INSPECT-COUNTER EQUAL TO ZERO                          IX1204.2
057200          MOVE "NO " TO ERROR-TOTAL                               IX1204.2
057300      ELSE MOVE INSPECT-COUNTER TO ERROR-TOTAL.                   IX1204.2
057400      MOVE "TEST(S) REQUIRE INSPECTION" TO ENDER-DESC.            IX1204.2
057500      MOVE CCVS-E-2 TO DUMMY-RECORD. PERFORM WRITE-LINE.          IX1204.2
057600     MOVE CCVS-E-3 TO DUMMY-RECORD. PERFORM WRITE-LINE.           IX1204.2
057700 WRITE-LINE.                                                      IX1204.2
057800     ADD 1 TO RECORD-COUNT.                                       IX1204.2
057900Y    IF RECORD-COUNT GREATER 42                                   IX1204.2
058000Y        MOVE DUMMY-RECORD TO DUMMY-HOLD                          IX1204.2
058100Y        MOVE SPACE TO DUMMY-RECORD                               IX1204.2
058200Y        WRITE DUMMY-RECORD AFTER ADVANCING PAGE                  IX1204.2
058300Y        MOVE CCVS-H-1  TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1204.2
058400Y        MOVE CCVS-H-2A TO DUMMY-RECORD PERFORM WRT-LN 2 TIMES    IX1204.2
058500Y        MOVE CCVS-H-2B TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1204.2
058600Y        MOVE CCVS-H-3  TO DUMMY-RECORD PERFORM WRT-LN 3 TIMES    IX1204.2
058700Y        MOVE CCVS-C-1  TO DUMMY-RECORD PERFORM WRT-LN            IX1204.2
058800Y        MOVE CCVS-C-2  TO DUMMY-RECORD PERFORM WRT-LN            IX1204.2
058900Y        MOVE HYPHEN-LINE TO DUMMY-RECORD PERFORM WRT-LN          IX1204.2
059000Y        MOVE DUMMY-HOLD TO DUMMY-RECORD                          IX1204.2
059100Y        MOVE ZERO TO RECORD-COUNT.                               IX1204.2
059200     PERFORM WRT-LN.                                              IX1204.2
059300 WRT-LN.                                                          IX1204.2
059400     WRITE    DUMMY-RECORD AFTER ADVANCING 1 LINES.               IX1204.2
059500     MOVE SPACE TO DUMMY-RECORD.                                  IX1204.2
059600 BLANK-LINE-PRINT.                                                IX1204.2
059700     PERFORM WRT-LN.                                              IX1204.2
059800 FAIL-ROUTINE.                                                    IX1204.2
059900     IF     COMPUTED-X NOT EQUAL TO SPACE                         IX1204.2
060000            GO TO   FAIL-ROUTINE-WRITE.                           IX1204.2
060100     IF     CORRECT-X NOT EQUAL TO SPACE GO TO FAIL-ROUTINE-WRITE.IX1204.2
060200     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1204.2
060300     MOVE  "NO FURTHER INFORMATION, SEE PROGRAM." TO INFO-TEXT.   IX1204.2
060400     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1204.2
060500     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1204.2
060600     GO TO  FAIL-ROUTINE-EX.                                      IX1204.2
060700 FAIL-ROUTINE-WRITE.                                              IX1204.2
060800     MOVE   TEST-COMPUTED TO PRINT-REC PERFORM WRITE-LINE         IX1204.2
060900     MOVE   ANSI-REFERENCE TO COR-ANSI-REFERENCE.                 IX1204.2
061000     MOVE   TEST-CORRECT TO PRINT-REC PERFORM WRITE-LINE 2 TIMES. IX1204.2
061100     MOVE   SPACES TO COR-ANSI-REFERENCE.                         IX1204.2
061200 FAIL-ROUTINE-EX. EXIT.                                           IX1204.2
061300 BAIL-OUT.                                                        IX1204.2
061400     IF     COMPUTED-A NOT EQUAL TO SPACE GO TO BAIL-OUT-WRITE.   IX1204.2
061500     IF     CORRECT-A EQUAL TO SPACE GO TO BAIL-OUT-EX.           IX1204.2
061600 BAIL-OUT-WRITE.                                                  IX1204.2
061700     MOVE CORRECT-A TO XXCORRECT. MOVE COMPUTED-A TO XXCOMPUTED.  IX1204.2
061800     MOVE   ANSI-REFERENCE TO INF-ANSI-REFERENCE.                 IX1204.2
061900     MOVE   XXINFO TO DUMMY-RECORD. PERFORM WRITE-LINE 2 TIMES.   IX1204.2
062000     MOVE   SPACES TO INF-ANSI-REFERENCE.                         IX1204.2
062100 BAIL-OUT-EX. EXIT.                                               IX1204.2
062200 CCVS1-EXIT.                                                      IX1204.2
062300     EXIT.                                                        IX1204.2
062400                                                                  IX1204.2
062500 SECT-IX120A-0003 SECTION.                                        IX1204.2
062600 SEQ-INIT-010.                                                    IX1204.2
062700     MOVE ZERO TO TEST-NO.                                        IX1204.2
062800     MOVE "IX-FS3" TO XFILE-NAME (1).                             IX1204.2
062900     MOVE "R1-F-G" TO XRECORD-NAME (1).                           IX1204.2
063000     MOVE CCVS-PGM-ID TO XPROGRAM-NAME (1).                       IX1204.2
063100     MOVE 000240 TO XRECORD-LENGTH (1).                           IX1204.2
063200     MOVE "RC" TO CHARS-OR-RECORDS (1).                           IX1204.2
063300     MOVE 0002 TO XBLOCK-SIZE (1).                                IX1204.2
063400     MOVE 000050 TO RECORDS-IN-FILE (1).                          IX1204.2
063500     MOVE "IX" TO XFILE-ORGANIZATION (1).                         IX1204.2
063600     MOVE "S" TO XLABEL-TYPE (1).                                 IX1204.2
063700     MOVE 000001 TO XRECORD-NUMBER (1).                           IX1204.2
063800     MOVE 0 TO COUNT-OF-RECS.                                     IX1204.2
063900                                                                  IX1204.2
064000******************************************************************IX1204.2
064100*   TEST  1                                                      *IX1204.2
064200*         OPEN OUTPUT ...                 00 EXPECTED            *IX1204.2
064300*         IX-3, 1.3.4 (1) A                                      *IX1204.2
064400*    STATUS 00 CHECK ON OUTPUT FILE IX-FS3                       *IX1204.2
064500*    THE OUTPUT STATEMENT IS SUCCESSFULLY EXECUTED               *IX1204.2
064600******************************************************************IX1204.2
064700 OPN-INIT-GF-01-0.                                                IX1204.2
064800     MOVE 1 TO STATUS-TEST-00.                                    IX1204.2
064900     MOVE SPACES TO IX-FS3-STATUS.                                IX1204.2
065000     MOVE "OPEN I-O   : 00 EXP." TO FEATURE.                      IX1204.2
065100     MOVE "OPN-TEST-GF-01-0" TO PAR-NAME.                         IX1204.2
065200     OPEN                                                         IX1204.2
065300        I-O    IX-FS3.                                            IX1204.2
065400     IF IX-FS3-STATUS EQUAL TO "00"                               IX1204.2
065500         GO TO OPN-PASS-GF-01-0.                                  IX1204.2
065600 OPN-FAIL-GF-01-0.                                                IX1204.2
065700     MOVE "IX-3, 1.3.4, (1) A. " TO RE-MARK.                      IX1204.2
065800     PERFORM FAIL.                                                IX1204.2
065900     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1204.2
066000     MOVE "00" TO CORRECT-X.                                      IX1204.2
066100     GO TO OPN-WRITE-GF-01-0.                                     IX1204.2
066200 OPN-PASS-GF-01-0.                                                IX1204.2
066300     PERFORM PASS.                                                IX1204.2
066400 OPN-WRITE-GF-01-0.                                               IX1204.2
066500     PERFORM PRINT-DETAIL.                                        IX1204.2
066600******************************************************************IX1204.2
066700*   TEST  5                                                      *IX1204.2
066800*         REWRITE WHERE THE LAST EXECUTED I-O STATEMENT PRIOR TO *IX1204.2
066900*         THE REWRITE WAS NOT A SUCCESSFULLY EXECUTED READ        IX1204.2
067000*         STATEMENT.       STATUS 43 EXPECTED.                    IX1204.2
067100*         IX-3, 1.3.4 (3) A                                      *IX1204.2
067200******************************************************************IX1204.2
067300 RWR-INIT-GF-01-0.                                                IX1204.2
067400     MOVE  5 TO TEST-NO.                                          IX1204.2
067500     MOVE SPACES TO IX-FS3-STATUS.                                IX1204.2
067600     MOVE 0 TO STATUS-TEST-00.                                    IX1204.2
067700     MOVE "REWRITE:     43 EXP." TO FEATURE.                      IX1204.2
067800     MOVE "RWR-TEST-GF-01-0" TO PAR-NAME.                         IX1204.2
067900*RWR-READ-GF-01-0.                                                IX1204.2
068000*    READ IX-FS3 AT END GO TO RWR-TEST-GF-01-0.                   IX1204.2
068100*    GO TO RWR-READ-GF-01-0.                                      IX1204.2
068200*RWR-TEST-GF-01-0.                                                IX1204.2
068300*    READ IX-FS3 AT END GO TO RWR-TEST-GF-01-1.                   IX1204.2
068400*    MOVE FILE-RECORD-INFO (1) TO IX-FS3R1-F-G-240.               IX1204.2
068500 RWR-TEST-GF-01-1.                                                IX1204.2
068600     REWRITE IX-FS3R1-F-G-240.                                    IX1204.2
068700     IF IX-FS3-STATUS EQUAL TO "43"                               IX1204.2
068800        MOVE "SHOULD HAVE EXECUTED DECLARATIVES IX-3,1.3.4(4)"    IX1204.2
068900          TO RE-MARK                                              IX1204.2
069000        GO TO RWR-WRITE-GF-01-0.                                  IX1204.2
069100 RWR-FAIL-GF-01-0.                                                IX1204.2
069200     MOVE "IX-5, 1.3.4, (5) C" TO RE-MARK.                        IX1204.2
069300 RWR-WRITE-GF-01-0.                                               IX1204.2
069400     MOVE IX-FS3-STATUS TO COMPUTED-A.                            IX1204.2
069500     MOVE "43" TO CORRECT-X.                                      IX1204.2
069600     PERFORM FAIL.                                                IX1204.2
069700     PERFORM PRINT-DETAIL.                                        IX1204.2
069800     CLOSE IX-FS3.                                                IX1204.2
069900                                                                  IX1204.2
070000 TERMINATE-ROUTINE.                                               IX1204.2
070100     EXIT.                                                        IX1204.2
070200                                                                  IX1204.2
070300 CCVS-EXIT SECTION.                                               IX1204.2
070400 CCVS-999999.                                                     IX1204.2
070500     GO TO CLOSE-FILES.                                           IX1204.2

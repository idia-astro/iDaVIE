#pragma once

#ifdef __cplusplus
extern "C" {
#endif

typedef struct AstObject AstObject;
typedef struct AstFitsChan AstFitsChan;
typedef struct AstFrameSet AstFrameSet;
typedef struct AstFrame AstFrame;
typedef struct AstCmpFrame AstCmpFrame;

#define AST__NULL 0
#define astOK 1
#define astBegin do { } while (0)
#define astEnd do { } while (0)
#define astClearStatus do { } while (0)

int astGetFitsS(...);
int astGetFitsF(...);
void astClear(...);
int astFindFits(...);
void astSetFitsS(...);
void astSetFitsF(...);
AstFitsChan *astFitsChan(...);
void astPutCards(...);
int astTestFits(...);
AstObject *astRead(...);
const char *astGetC(...);
void astShow(...);
AstFrame *astGetFrame(...);
AstObject *astCopy(...);
void astSet(...);
const char *astFormat(...);
char *astToString(...);
int astHasAttribute(...);
void astSetC(...);
int astGetI(...);
void astNorm(...);
double astAxDistance(...);
double astDistance(...);
void astTran2(...);
void astTranN(...);
AstFrameSet *astConvert(...);
void astDelete(...);
void astInvert(...);
void astFree(...);

#ifdef __cplusplus
}
#endif

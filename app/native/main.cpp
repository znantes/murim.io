#define UNICODE
#define _UNICODE
#include <windows.h>
#include <gdiplus.h>
#include <string>
#include <fstream>
#include <random>
#include <algorithm>
#include <cstdint>
#include "murim_engine.hpp"

#pragma comment(lib, "gdiplus.lib")
using namespace Gdiplus;

namespace {
constexpr int ID_NEW=1001, ID_TIME=1002, ID_SAVE=1003, ID_LOAD=1004, ID_FULL=1005;
constexpr int ID_ACTION=1010, ID_TRAIN=1011, ID_REST=1012, ID_HOURS=1013;

struct GameState {
    bool started=false;
    int ageMonths=0, health=100, stamina=100, qi=10, martial=1, trainingHours=0;
    int money=20;
    std::wstring name=L"Inconnu", place=L"Village de naissance";
    std::wstring body=L"Corps ordinaire", spirit=L"Esprit ordinaire", title=L"Aucun titre";
    std::uint64_t seed=318;
    murim::WorldEngine world{318};
} game;

HWND gAction{}, gHours{}, gLog{};
HFONT gFont{}, gSmall{};
HBRUSH gBg{}, gPanel{}, gEdit{};
ULONG_PTR gdiToken{};
bool gFullscreen=false;
WINDOWPLACEMENT gPlacement{sizeof(WINDOWPLACEMENT)};

std::wstring ageText(){
    if(!game.started) return L"—";
    if(game.ageMonths<12) return std::to_wstring(game.ageMonths)+L" mois";
    const int y=game.ageMonths/12, m=game.ageMonths%12;
    return std::to_wstring(y)+(y==1?L" an":L" ans")+(m?L" et "+std::to_wstring(m)+L" mois":L"");
}
void logLine(const std::wstring& s){
    if(!gLog) return;
    int n=GetWindowTextLengthW(gLog);
    SendMessageW(gLog,EM_SETSEL,n,n);
    const std::wstring line=s+L"\r\n";
    SendMessageW(gLog,EM_REPLACESEL,FALSE,(LPARAM)line.c_str());
}
void button(HWND p,const wchar_t* label,int id){
    HWND b=CreateWindowW(L"BUTTON",label,WS_CHILD|WS_VISIBLE|BS_PUSHBUTTON,0,0,120,34,p,(HMENU)(INT_PTR)id,GetModuleHandleW(nullptr),nullptr);
    SendMessageW(b,WM_SETFONT,(WPARAM)gFont,TRUE);
}
void label(HWND p,const wchar_t* s,int x,int y,int w,int h,bool small=false){
    HWND c=CreateWindowW(L"STATIC",s,WS_CHILD|WS_VISIBLE, x,y,w,h,p,nullptr,GetModuleHandleW(nullptr),nullptr);
    SendMessageW(c,WM_SETFONT,(WPARAM)(small?gSmall:gFont),TRUE);
}
void rect(Graphics& g,float x,float y,float w,float h){
    SolidBrush fill(Color(255,15,22,31));
    Pen pen(Color(255,58,72,89),1.0f);
    g.FillRectangle(&fill,x,y,w,h); g.DrawRectangle(&pen,x,y,w,h);
}
void txt(Graphics& g,const std::wstring& s,float x,float y,float size,bool bold=false){
    FontFamily fam(L"Georgia");
    Font f(&fam,size,bold?FontStyleBold:FontStyleRegular,UnitPixel);
    SolidBrush b(Color(255,226,190,116));
    g.DrawString(s.c_str(),-1,&f,PointF(x,y),&b);
}
void bodyTxt(Graphics& g,const std::wstring& s,float x,float y,float size=13){
    FontFamily fam(L"Georgia"); Font f(&fam,size,FontStyleRegular,UnitPixel);
    SolidBrush b(Color(255,202,208,216)); g.DrawString(s.c_str(),-1,&f,PointF(x,y),&b);
}
void drawPortrait(Graphics& g,float x,float y,float w,float h){
    SolidBrush bg(Color(255,24,29,37)); g.FillRectangle(&bg,x,y,w,h);
    Pen frame(Color(255,184,143,76),2); g.DrawRectangle(&frame,x,y,w,h);
    SolidBrush cloak(Color(255,34,42,54));
    g.FillEllipse(&cloak,x+w*.18f,y+h*.66f,w*.64f,h*.48f);
    SolidBrush skin(Color(255,208,161,126));
    g.FillEllipse(&skin,x+w*.31f,y+h*.20f,w*.38f,h*.42f);
    SolidBrush hair(Color(255,31,24,23));
    g.FillEllipse(&hair,x+w*.28f,y+h*.12f,w*.44f,h*.28f);
    g.FillRectangle(&hair,x+w*.27f,y+h*.19f,w*.10f,h*.25f);
    g.FillRectangle(&hair,x+w*.63f,y+h*.19f,w*.10f,h*.25f);
    SolidBrush eye(Color(255,20,20,20));
    g.FillEllipse(&eye,x+w*.40f,y+h*.39f,w*.045f,h*.025f);
    g.FillEllipse(&eye,x+w*.555f,y+h*.39f,w*.045f,h*.025f);
    Pen line(Color(255,118,80,62),1); g.DrawLine(&line,x+w*.49f,y+h*.43f,x+w*.48f,y+h*.50f);
    g.DrawArc(&line,x+w*.43f,y+h*.47f,w*.15f,h*.10f,15,150);
    bodyTxt(g,L"Portrait évolutif",x+12,y+h-27,11);
}
void paint(HWND hwnd){
    RECT r; GetClientRect(hwnd,&r); const int W=r.right,H=r.bottom;
    HDC dc=GetDC(hwnd); Graphics g(dc); g.SetSmoothingMode(SmoothingModeAntiAlias);
    SolidBrush bg(Color(255,7,10,15)); g.FillRectangle(&bg,0,0,W,H);
    SolidBrush header(Color(255,12,18,27)); g.FillRectangle(&header,0,0,W,78);
    txt(g,L"武林 · MURIM",28,13,27,true);
    bodyTxt(g,L"LES MILLE DESTINS · JIANGHU VIVANT · SIMULATION",31,48,11);
    bodyTxt(g,L"ANNÉE 318",W-130,15,12); bodyTxt(g,ageText(),W-130,39,12);

    const float gap=14,left=22,top=96,right=295;
    float pw=270, centerW=(float)W-left-pw-right-gap*2;
    if(centerW<440) centerW=440;
    rect(g,left,top,pw,H-top-20);
    rect(g,left+pw+gap,top,centerW,H-top-20);
    rect(g,left+pw+gap+centerW+gap,top,right,H-top-20);

    txt(g,L"PERSONNAGE",left+18,top+16,19,true);
    drawPortrait(g,left+18,top+51,pw-36,190);
    bodyTxt(g,game.started?game.name:L"Aucune vie",left+18,top+254,18);
    bodyTxt(g,game.started?game.title:L"—",left+18,top+282,11);
    bodyTxt(g,L"Âge · "+ageText(),left+18,top+308,12);
    bodyTxt(g,L"Lieu · "+game.place,left+18,top+330,12);
    txt(g,L"CORPS",left+18,top+366,13,true);
    bodyTxt(g,game.started?game.body:L"—",left+18,top+387,13);
    txt(g,L"ESPRIT",left+18,top+419,13,true);
    bodyTxt(g,game.started?game.spirit:L"—",left+18,top+440,13);
    txt(g,L"RESSOURCES",left+18,top+472,13,true);
    bodyTxt(g,L"Santé  "+std::to_wstring(game.health)+L" / 100",left+18,top+493,12);
    bodyTxt(g,L"Endurance  "+std::to_wstring(game.stamina)+L" / 100",left+18,top+514,12);
    bodyTxt(g,L"Qi  "+std::to_wstring(game.qi),left+18,top+535,12);
    bodyTxt(g,L"Maîtrise  "+std::to_wstring(game.martial),left+18,top+556,12);

    float cx=left+pw+gap;
    txt(g,L"CHRONIQUE",cx+18,top+16,19,true);
    bodyTxt(g,game.started?L"Tu n'es pas le héros. Le monde ne t'attend pas.":L"Commence une nouvelle vie dans le Jianghu.",cx+18,top+47,12);
    rect(g,cx+18,top+73,centerW-36,135);
    txt(g,game.place,cx+34,top+91,17,true);
    bodyTxt(g,game.started?L"Le village vit autour de toi. Les gens travaillent,":L"Une fois ta vie créée, les habitants continueront d'agir.",cx+34,top+125,12);
    bodyTxt(g,game.started?L"vieillissent, voyagent et prennent leurs propres décisions.":L"Tu découvriras progressivement leurs histoires.",cx+34,top+148,12);
    bodyTxt(g,L"Les informations inconnues restent inconnues jusqu'à leur découverte.",cx+34,top+178,11);
    txt(g,L"ACTION LIBRE",cx+18,top+238,16,true);
    bodyTxt(g,L"Pas de quête obligatoire. Décris ce que tu veux faire.",cx+18,top+263,11);
    bodyTxt(g,L"ENTRAÎNEMENT",cx+18,top+315,15,true);
    bodyTxt(g,L"Heures ce mois : "+std::to_wstring(game.trainingHours),cx+18,top+341,13);
    bodyTxt(g,L"Tu choisis la durée. Qualité, talent, corps, maître et récupération",cx+18,top+366,11);
    bodyTxt(g,L"influencent la progression. Le surentraînement peut blesser.",cx+18,top+385,11);
    txt(g,L"DÉCOUVERTE",cx+18,top+430,15,true);
    bodyTxt(g,L"Manuels, techniques, constitutions et secrets sont cachés jusqu'à",cx+18,top+455,11);
    bodyTxt(g,L"ce que tes actions permettent de les découvrir et comprendre.",cx+18,top+474,11);

    float rx=cx+centerW+gap;
    txt(g,L"JIANGHU",rx+18,top+16,19,true);
    const wchar_t* rows[]={L"PNJ · familles · maîtres",L"Relations · confiance · rivalité",L"Manuels · lecture · étude",L"Techniques · débutant → divin",L"Corps · méridiens · constitutions",L"Qi · affinités · dangers",L"Sectes · familles · cultes",L"Métiers · économie · ressources",L"Titres · réputation · renommée",L"Carte · voyages · événements"};
    for(int i=0;i<10;i++) bodyTxt(g,rows[i],rx+18,top+58+i*38,11);
    bodyTxt(g,L"95 % des personnes ont un corps ordinaire.",rx+18,top+H-top-72,11);
    bodyTxt(g,L"Les constitutions sont exceptionnellement rares.",rx+18,top+H-top-49,11);
    ReleaseDC(hwnd,dc);
}
void layout(HWND hwnd){
    RECT r; GetClientRect(hwnd,&r); int W=r.right,H=r.bottom;
    if(gAction) MoveWindow(gAction,310,610,std::max(300,W-650),52,TRUE);
    if(gHours) MoveWindow(gHours,310,560,90,32,TRUE);
    if(gLog) MoveWindow(gLog,310,665,std::max(300,W-650),std::max(100,H-690),TRUE);
    HWND b;
    if((b=GetDlgItem(hwnd,ID_NEW))) MoveWindow(b,W-275,18,120,36,TRUE);
    if((b=GetDlgItem(hwnd,ID_SAVE))) MoveWindow(b,W-145,18,120,36,TRUE);
    if((b=GetDlgItem(hwnd,ID_LOAD))) MoveWindow(b,W-275,58,120,34,TRUE);
    if((b=GetDlgItem(hwnd,ID_FULL))) MoveWindow(b,W-145,58,120,34,TRUE);
    if((b=GetDlgItem(hwnd,ID_TIME))) MoveWindow(b,180,560,110,32,TRUE);
    if((b=GetDlgItem(hwnd,ID_TRAIN))) MoveWindow(b,410,560,130,32,TRUE);
    if((b=GetDlgItem(hwnd,ID_REST))) MoveWindow(b,550,560,120,32,TRUE);
}
void redraw(HWND h){InvalidateRect(h,nullptr,FALSE);}
void newLife(HWND hwnd){
    std::random_device rd; game=GameState{}; game.started=true; game.seed=((std::uint64_t)rd()<<32)^rd(); game.world=murim::WorldEngine(game.seed);
    game.name=(rd()%2)?L"Jin Han":L"Wei Jun"; game.place=L"Village de Qingshui";
    game.world.add_person({1,30,"Village de Qingshui",true}); game.world.add_person({2,28,"Village de Qingshui",true}); game.world.add_person({3,6,"Village de Qingshui",true});
    logLine(L"Une nouvelle vie commence. Tu es un habitant du Jianghu.");
    logLine(L"Ton corps rare, s'il existe, n'est pas révélé gratuitement.");
    logLine(L"Tu décides de tes entraînements, de ton travail et de tes voyages."); redraw(hwnd);
}
void timeStep(HWND hwnd){if(!game.started)return; game.ageMonths++; game.stamina=std::min(100,game.stamina+15); game.world.advance_days(30); logLine(L"Un mois passe. Le Jianghu continue d'évoluer."); redraw(hwnd);}
void freeAction(HWND hwnd){if(!game.started){logLine(L"Commence d'abord une nouvelle vie.");return;} wchar_t b[1024]{};GetWindowTextW(gAction,b,1024);std::wstring a=b;if(a.empty()){logLine(L"Décris une action.");return;} game.world.advance_days(1);game.stamina=std::max(0,game.stamina-2);logLine(L"Action : "+a+L" · 1 jour s'écoule.");SetWindowTextW(gAction,L"");redraw(hwnd);}
void train(HWND hwnd){if(!game.started)return;wchar_t b[32]{};GetWindowTextW(gHours,b,32);int h=_wtoi(b);if(h<=0)h=1;h=std::min(h,24);game.trainingHours+=h;game.stamina=std::max(0,game.stamina-h*3);if(game.stamina<15){game.health=std::max(1,game.health-2);logLine(L"Entraînement excessif : fatigue et risque de blessure.");}else{game.martial=std::min(100,game.martial+std::max(1,h/2));logLine(L"Entraînement de "+std::to_wstring(h)+L" heure(s) terminé.");}game.world.advance_days(1);redraw(hwnd);}
void rest(HWND hwnd){if(!game.started)return;game.stamina=std::min(100,game.stamina+35);game.health=std::min(100,game.health+4);game.world.advance_days(1);logLine(L"Repos : ton corps récupère.");redraw(hwnd);}
void saveGame(HWND hwnd){std::wofstream f(L"murim-save.txt");if(!f){MessageBoxW(hwnd,L"Impossible de sauvegarder.",L"Murim",MB_ICONERROR);return;}f<<game.started<<L' '<<game.ageMonths<<L' '<<game.health<<L' '<<game.stamina<<L' '<<game.qi<<L' '<<game.martial<<L' '<<game.trainingHours<<L' '<<game.money<<L'\n'<<game.name<<L'\n'<<game.place<<L'\n';logLine(L"Partie sauvegardée dans murim-save.txt.");}
void loadGame(HWND hwnd){std::wifstream f(L"murim-save.txt");if(!f){MessageBoxW(hwnd,L"Aucune sauvegarde trouvée.",L"Murim",MB_ICONINFORMATION);return;}int st=0;f>>st>>game.ageMonths>>game.health>>game.stamina>>game.qi>>game.martial>>game.trainingHours>>game.money;f.ignore(1);std::getline(f,game.name);std::getline(f,game.place);game.started=st!=0;game.world=murim::WorldEngine(game.seed);redraw(hwnd);logLine(L"Partie chargée.");}
void fullscreen(HWND hwnd){if(!gFullscreen){gPlacement.length=sizeof(gPlacement);GetWindowPlacement(hwnd,&gPlacement);MONITORINFO mi{sizeof(mi)};GetMonitorInfoW(MonitorFromWindow(hwnd,MONITOR_DEFAULTTONEAREST),&mi);SetWindowLongW(hwnd,GWL_STYLE,WS_POPUP|WS_VISIBLE);SetWindowPos(hwnd,HWND_TOP,mi.rcMonitor.left,mi.rcMonitor.top,mi.rcMonitor.right-mi.rcMonitor.left,mi.rcMonitor.bottom-mi.rcMonitor.top,SWP_FRAMECHANGED);gFullscreen=true;}else{SetWindowLongW(hwnd,GWL_STYLE,WS_OVERLAPPEDWINDOW|WS_VISIBLE);SetWindowPlacement(hwnd,&gPlacement);SetWindowPos(hwnd,nullptr,0,0,0,0,SWP_NOMOVE|SWP_NOSIZE|SWP_NOZORDER|SWP_FRAMECHANGED);gFullscreen=false;}redraw(hwnd);}
LRESULT CALLBACK WndProc(HWND hwnd,UINT msg,WPARAM wp,LPARAM lp){
    switch(msg){
    case WM_CREATE:{
        gFont=CreateFontW(18,0,0,0,FW_NORMAL,0,0,0,DEFAULT_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,CLEARTYPE_QUALITY,FF_ROMAN,L"Georgia");
        gSmall=CreateFontW(14,0,0,0,FW_NORMAL,0,0,0,DEFAULT_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,CLEARTYPE_QUALITY,FF_ROMAN,L"Segoe UI");
        button(hwnd,L"Nouvelle vie",ID_NEW);button(hwnd,L"Temps + 1 mois",ID_TIME);button(hwnd,L"Sauvegarder",ID_SAVE);button(hwnd,L"Charger",ID_LOAD);button(hwnd,L"Plein écran",ID_FULL);
        gAction=CreateWindowW(L"EDIT",L"",WS_CHILD|WS_VISIBLE|WS_BORDER|ES_MULTILINE|ES_AUTOVSCROLL,0,0,400,55,hwnd,(HMENU)(INT_PTR)ID_ACTION,GetModuleHandleW(nullptr),nullptr);SendMessageW(gAction,WM_SETFONT,(WPARAM)gSmall,TRUE);
        gHours=CreateWindowW(L"EDIT",L"2",WS_CHILD|WS_VISIBLE|WS_BORDER|ES_NUMBER,0,0,80,30,hwnd,(HMENU)(INT_PTR)ID_HOURS,GetModuleHandleW(nullptr),nullptr);SendMessageW(gHours,WM_SETFONT,(WPARAM)gSmall,TRUE);
        button(hwnd,L"Agir",ID_ACTION);button(hwnd,L"S'entraîner",ID_TRAIN);button(hwnd,L"Se reposer",ID_REST);
        gLog=CreateWindowW(L"EDIT",L"Chronique\r\n",WS_CHILD|WS_VISIBLE|WS_BORDER|ES_MULTILINE|ES_AUTOVSCROLL|ES_READONLY|WS_VSCROLL,0,0,400,130,hwnd,nullptr,GetModuleHandleW(nullptr),nullptr);SendMessageW(gLog,WM_SETFONT,(WPARAM)gSmall,TRUE);
        layout(hwnd); return 0;}
    case WM_SIZE: layout(hwnd); redraw(hwnd); return 0;
    case WM_COMMAND:{int id=LOWORD(wp);if(id==ID_NEW)newLife(hwnd);else if(id==ID_TIME)timeStep(hwnd);else if(id==ID_SAVE)saveGame(hwnd);else if(id==ID_LOAD)loadGame(hwnd);else if(id==ID_FULL)fullscreen(hwnd);else if(id==ID_ACTION)freeAction(hwnd);else if(id==ID_TRAIN)train(hwnd);else if(id==ID_REST)rest(hwnd);return 0;}
    case WM_PAINT:{PAINTSTRUCT ps;BeginPaint(hwnd,&ps);EndPaint(hwnd,&ps);paint(hwnd);return 0;}
    case WM_ERASEBKGND:return 1;
    case WM_DESTROY:PostQuitMessage(0);return 0;
    case WM_CTLCOLOREDIT:{HDC dc=(HDC)wp;SetTextColor(dc,RGB(225,225,225));SetBkColor(dc,RGB(12,17,24));static HBRUSH b=CreateSolidBrush(RGB(12,17,24));return (LRESULT)b;}
    }
    return DefWindowProcW(hwnd,msg,wp,lp);
}
}
int APIENTRY wWinMain(HINSTANCE h,HINSTANCE,LPWSTR,int n){
    GdiplusStartupInput gi; if(GdiplusStartup(&gdiToken,&gi,nullptr)!=Ok)return 1;
    WNDCLASSW wc{};wc.hInstance=h;wc.lpfnWndProc=WndProc;wc.lpszClassName=L"MurimCleanWindow";wc.hCursor=LoadCursorW(nullptr,IDC_ARROW);wc.hbrBackground=(HBRUSH)(COLOR_WINDOW+1);RegisterClassW(&wc);
    HWND hwnd=CreateWindowW(L"MurimCleanWindow",L"Murim — Les Mille Destins",WS_OVERLAPPEDWINDOW|WS_CLIPCHILDREN,CW_USEDEFAULT,CW_USEDEFAULT,1280,820,nullptr,nullptr,h,nullptr);
    if(!hwnd){GdiplusShutdown(gdiToken);return 1;}ShowWindow(hwnd,n? n:SW_SHOW);UpdateWindow(hwnd);
    MSG m{};while(GetMessageW(&m,nullptr,0,0)>0){TranslateMessage(&m);DispatchMessageW(&m);}GdiplusShutdown(gdiToken);return (int)m.wParam;
}

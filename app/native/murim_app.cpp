#define UNICODE
#define _UNICODE
#include <windows.h>
#include <string>
#include <fstream>
#include <random>
#include <algorithm>
#include <cstdint>
#include "murim_engine.hpp"

namespace {
constexpr int ID_NEW=1001, ID_MONTH=1002, ID_SAVE=1003, ID_LOAD=1004, ID_FULL=1005;
constexpr int ID_TRAIN=1010, ID_REST=1011, ID_ACTION_EDIT=1012, ID_HOURS=1013, ID_DO_ACTION=1014;

struct GameState {
    bool started=false;
    int ageMonths=0, health=100, stamina=100, qi=10, martial=1, trainingHours=0, money=20;
    std::wstring name=L"Inconnu", place=L"Village de naissance";
    std::wstring body=L"Corps ordinaire", spirit=L"Esprit ordinaire", title=L"Aucun titre";
    std::uint64_t seed=318;
    murim::WorldEngine world{318};
} game;

HWND gAction{}, gHours{}, gLog{};
HFONT gFont{}, gSmall{};
bool gFullscreen=false;
WINDOWPLACEMENT gPlacement{sizeof(WINDOWPLACEMENT)};

void fill(HDC dc, RECT r, COLORREF c){ HBRUSH b=CreateSolidBrush(c); FillRect(dc,&r,b); DeleteObject(b); }
void frame(HDC dc, RECT r, COLORREF c){ HPEN p=CreatePen(PS_SOLID,1,c); HGDIOBJ old=SelectObject(dc,p); Rectangle(dc,r.left,r.top,r.right,r.bottom); SelectObject(dc,old); DeleteObject(p); }
void text(HDC dc,const std::wstring& s,int x,int y,COLORREF c,HFONT font){ SetTextColor(dc,c); SetBkMode(dc,TRANSPARENT); HGDIOBJ old=SelectObject(dc,font); TextOutW(dc,x,y,s.c_str(),static_cast<int>(s.size())); SelectObject(dc,old); }

std::wstring ageText(){
    if(!game.started) return L"—";
    int y=game.ageMonths/12, m=game.ageMonths%12;
    if(y==0) return std::to_wstring(m)+L" mois";
    return std::to_wstring(y)+(y==1?L" an":L" ans")+(m?L" et "+std::to_wstring(m)+L" mois":L"");
}
void logLine(const std::wstring& s){
    if(!gLog) return;
    int n=GetWindowTextLengthW(gLog); SendMessageW(gLog,EM_SETSEL,n,n);
    std::wstring line=s+L"\r\n"; SendMessageW(gLog,EM_REPLACESEL,FALSE,(LPARAM)line.c_str());
}
void addButton(HWND h,const wchar_t* label,int id){
    HWND b=CreateWindowW(L"BUTTON",label,WS_CHILD|WS_VISIBLE|BS_PUSHBUTTON,0,0,120,32,h,(HMENU)(INT_PTR)id,GetModuleHandleW(nullptr),nullptr);
    SendMessageW(b,WM_SETFONT,(WPARAM)gSmall,TRUE);
}
void portrait(HDC dc,RECT r){
    fill(dc,r,RGB(24,29,38)); frame(dc,r,RGB(181,139,72));
    int w=r.right-r.left,h=r.bottom-r.top,cx=r.left+w/2;
    HBRUSH cloak=CreateSolidBrush(RGB(39,49,67)); HGDIOBJ old=SelectObject(dc,cloak);
    Ellipse(dc,cx-w/3,r.top+h*62/100,cx+w/3,r.bottom+h/12); SelectObject(dc,old); DeleteObject(cloak);
    HBRUSH skin=CreateSolidBrush(RGB(211,166,129)); old=SelectObject(dc,skin);
    Ellipse(dc,cx-w/5,r.top+h/16,cx+w/5,r.top+h*55/100); SelectObject(dc,old); DeleteObject(skin);
    HBRUSH hair=CreateSolidBrush(RGB(29,24,25)); old=SelectObject(dc,hair);
    Ellipse(dc,cx-w/5-4,r.top+h/20,cx+w/5+4,r.top+h*35/100);
    Rectangle(dc,cx-w/5,r.top+h*18/100,cx-w/12,r.top+h*48/100);
    Rectangle(dc,cx+w/12,r.top+h*18/100,cx+w/5,r.top+h*48/100); SelectObject(dc,old); DeleteObject(hair);
    HBRUSH eye=CreateSolidBrush(RGB(25,25,25)); old=SelectObject(dc,eye);
    Ellipse(dc,cx-w/12,r.top+h*40/100,cx-w/20,r.top+h*43/100); Ellipse(dc,cx+w/20,r.top+h*40/100,cx+w/12,r.top+h*43/100); SelectObject(dc,old); DeleteObject(eye);
    text(dc,L"PORTRAIT · identité persistante",r.left+12,r.bottom-25,RGB(205,205,205),gSmall);
}
void bar(HDC dc,int x,int y,int w,int value,COLORREF c){
    RECT a{x,y,x+w,y+12}; fill(dc,a,RGB(38,43,52));
    RECT b{x,y,x+(w*std::clamp(value,0,100))/100,y+12}; fill(dc,b,c); frame(dc,a,RGB(73,82,96));
}
void paint(HWND hwnd){
    RECT r; GetClientRect(hwnd,&r); int W=r.right,H=r.bottom;
    HDC dc=GetDC(hwnd); HDC mem=CreateCompatibleDC(dc); HBITMAP bmp=CreateCompatibleBitmap(dc,W,H); HGDIOBJ oldBmp=SelectObject(mem,bmp);
    fill(mem,RECT{0,0,W,H},RGB(7,10,15)); fill(mem,RECT{0,0,W,72},RGB(13,18,27));
    text(mem,L"武林 · MURIM",26,13,RGB(230,194,124),gFont); text(mem,L"LES MILLE DESTINS · JIANGHU VIVANT · SIMULATION",29,43,RGB(175,181,191),gSmall);
    text(mem,L"ANNEE 318",W-120,14,RGB(230,194,124),gSmall); text(mem,ageText(),W-120,38,RGB(190,196,205),gSmall);
    int top=88,left=18,gap=14,pw=276,rw=292,cw=std::max(420,W-left-pw-rw-gap*2);
    RECT lp{left,top,left+pw,H-18}, cp{left+pw+gap,top,left+pw+gap+cw,H-18}, rp{left+pw+gap+cw+gap,top,W-18,H-18};
    fill(mem,lp,RGB(12,17,25)); fill(mem,cp,RGB(12,17,25)); fill(mem,rp,RGB(12,17,25)); frame(mem,lp,RGB(54,67,83)); frame(mem,cp,RGB(54,67,83)); frame(mem,rp,RGB(54,67,83));
    text(mem,L"PERSONNAGE",lp.left+16,top+14,RGB(230,194,124),gFont); portrait(mem,RECT{lp.left+16,top+48,lp.right-16,top+236});
    text(mem,game.started?game.name:L"Aucune vie",lp.left+16,top+252,RGB(238,238,238),gFont); text(mem,game.started?game.title:L"—",lp.left+16,top+278,RGB(160,168,180),gSmall);
    text(mem,L"Age · "+ageText(),lp.left+16,top+304,RGB(190,196,205),gSmall); text(mem,L"Lieu · "+game.place,lp.left+16,top+326,RGB(190,196,205),gSmall);
    text(mem,L"CORPS",lp.left+16,top+360,RGB(230,194,124),gSmall); text(mem,game.started?game.body:L"—",lp.left+16,top+381,RGB(205,210,218),gSmall);
    text(mem,L"ESPRIT",lp.left+16,top+414,RGB(230,194,124),gSmall); text(mem,game.started?game.spirit:L"—",lp.left+16,top+435,RGB(205,210,218),gSmall);
    text(mem,L"RESSOURCES",lp.left+16,top+468,RGB(230,194,124),gSmall);
    text(mem,L"Sante",lp.left+16,top+490,RGB(190,196,205),gSmall); bar(mem,lp.left+80,top+491,150,game.health,RGB(155,75,68));
    text(mem,L"Endurance",lp.left+16,top+515,RGB(190,196,205),gSmall); bar(mem,lp.left+80,top+516,150,game.stamina,RGB(79,126,101));
    text(mem,L"Qi",lp.left+16,top+540,RGB(190,196,205),gSmall); bar(mem,lp.left+80,top+541,150,std::min(100,game.qi*5),RGB(93,105,155));
    text(mem,L"Maitrise",lp.left+16,top+565,RGB(190,196,205),gSmall); bar(mem,lp.left+80,top+566,150,game.martial,RGB(170,132,72));

    text(mem,L"CHRONIQUE",cp.left+18,top+14,RGB(230,194,124),gFont);
    text(mem,game.started?L"Tu n'es pas le heros. Le monde ne t'attend pas.":L"Commence une nouvelle vie dans le Jianghu.",cp.left+18,top+46,RGB(205,210,218),gSmall);
    RECT story{cp.left+18,top+70,cp.right-18,top+202}; fill(mem,story,RGB(17,23,32)); frame(mem,story,RGB(61,73,89));
    text(mem,game.place,story.left+14,story.top+14,RGB(230,194,124),gFont);
    text(mem,game.started?L"Le village travaille, les familles evoluent et les PNJ":L"Les habitants auront leurs propres vies,",story.left+14,story.top+48,RGB(198,204,212),gSmall);
    text(mem,game.started?L"prennent leurs propres decisions sans attendre le joueur.":L"leurs relations, leurs secrets et leur progression.",story.left+14,story.top+70,RGB(198,204,212),gSmall);
    text(mem,L"Les informations inconnues restent inconnues jusqu'a leur decouverte.",story.left+14,story.top+100,RGB(170,177,187),gSmall);
    text(mem,L"ACTION LIBRE",cp.left+18,top+226,RGB(230,194,124),gFont); text(mem,L"Aucune quete obligatoire : decris ton intention.",cp.left+18,top+254,RGB(190,196,205),gSmall);
    text(mem,L"ENTRAINEMENT",cp.left+18,top+296,RGB(230,194,124),gFont); text(mem,L"Heures choisies : "+std::to_wstring(game.trainingHours),cp.left+18,top+326,RGB(205,210,218),gSmall);
    text(mem,L"Le joueur decide combien il s'entraine. Talent, corps,",cp.left+18,top+350,RGB(175,181,191),gSmall); text(mem,L"methode, maitre, recuperation et fatigue influencent le resultat.",cp.left+18,top+372,RGB(175,181,191),gSmall);
    text(mem,L"DECOUVERTE",cp.left+18,top+414,RGB(230,194,124),gFont); text(mem,L"Manuels, techniques, secrets et constitutions ne sont",cp.left+18,top+444,RGB(190,196,205),gSmall); text(mem,L"pas devoiles gratuitement : observer, lire, etudier, comparer.",cp.left+18,top+466,RGB(190,196,205),gSmall);

    text(mem,L"JIANGHU",rp.left+18,top+14,RGB(230,194,124),gFont);
    const wchar_t* rows[]={L"PNJ · familles · maitres",L"Relations · confiance · rivalite",L"Manuels · lecture · etude",L"Techniques · debutant → divin",L"Corps · meridiens · constitutions",L"Qi · affinites · dangers",L"Sectes · familles · cultes",L"Metiers · economie · ressources",L"Titres · reputation · renommee",L"Carte · voyages · evenements"};
    for(int i=0;i<10;i++) text(mem,rows[i],rp.left+18,top+54+i*34,RGB(195,201,210),gSmall);
    text(mem,L"95 % · corps ordinaire",rp.left+18,top+414,RGB(181,185,194),gSmall); text(mem,L"Constitutions · extremement rares",rp.left+18,top+438,RGB(181,185,194),gSmall); text(mem,L"5000 techniques · 5000 manuels · 5000 titres",rp.left+18,top+480,RGB(145,151,162),gSmall); text(mem,L"10000 portraits cibles · chargement progressif",rp.left+18,top+502,RGB(145,151,162),gSmall);
    BitBlt(dc,0,0,W,H,mem,0,0,SRCCOPY); SelectObject(mem,oldBmp); DeleteObject(bmp); DeleteDC(mem); ReleaseDC(hwnd,dc);
}
void layout(HWND hwnd){
    RECT r; GetClientRect(hwnd,&r); int W=r.right,H=r.bottom;
    auto mv=[&](int id,int x,int y,int w,int h){ if(HWND b=GetDlgItem(hwnd,id)) MoveWindow(b,x,y,w,h,TRUE); };
    mv(ID_NEW,W-270,12,120,34); mv(ID_SAVE,W-140,12,120,34); mv(ID_LOAD,W-270,50,120,34); mv(ID_FULL,W-140,50,120,34);
    mv(ID_MONTH,160,520,120,32); mv(ID_TRAIN,292,520,120,32); mv(ID_REST,424,520,120,32); mv(ID_HOURS,552,520,70,32); mv(ID_DO_ACTION,630,610,110,34);
    if(gAction) MoveWindow(gAction,292,566,std::max(300,W-560),42,TRUE); if(gLog) MoveWindow(gLog,292,654,std::max(300,W-560),std::max(90,H-670),TRUE);
}
void redraw(HWND h){ InvalidateRect(h,nullptr,FALSE); }
void newLife(HWND hwnd){
    std::random_device rd; game=GameState{}; game.started=true; game.seed=((std::uint64_t)rd()<<32)^rd(); game.world=murim::WorldEngine(game.seed); game.name=(rd()%2)?L"Jin Han":L"Wei Jun"; game.place=L"Village de Qingshui"; game.body=(rd()%20==0)?L"Constitution rare · inconnue":L"Corps ordinaire";
    game.world.add_person({1,30,"Village de Qingshui",true}); game.world.add_person({2,28,"Village de Qingshui",true}); game.world.add_person({3,6,"Village de Qingshui",true});
    logLine(L"Nouvelle vie creee. Tu es un habitant du Jianghu."); logLine(L"Aucune quete obligatoire. Tu choisis ton entrainement, ton travail et tes voyages."); redraw(hwnd);
}
void month(HWND hwnd){ if(!game.started){logLine(L"Commence une nouvelle vie.");return;} game.ageMonths++; game.stamina=std::min(100,game.stamina+18); game.world.advance_days(30); logLine(L"Un mois passe. Le Jianghu continue sans attendre."); redraw(hwnd); }
void train(HWND hwnd){ if(!game.started)return; wchar_t b[32]{}; GetWindowTextW(gHours,b,32); int h=_wtoi(b); h=std::clamp(h,1,24); game.trainingHours+=h; game.stamina=std::max(0,game.stamina-h*3); if(game.stamina<15){game.health=std::max(1,game.health-2);logLine(L"Surentrainement : fatigue et risque de blessure.");} else {game.martial=std::min(100,game.martial+std::max(1,h/2));logLine(L"Entrainement de "+std::to_wstring(h)+L" heure(s). Progression effectuee.");} game.world.advance_days(1); redraw(hwnd); }
void rest(HWND hwnd){ if(!game.started)return; game.stamina=std::min(100,game.stamina+35); game.health=std::min(100,game.health+4); game.world.advance_days(1); logLine(L"Repos : recuperation physique."); redraw(hwnd); }
void action(HWND hwnd){ if(!game.started){logLine(L"Commence d'abord une nouvelle vie.");return;} wchar_t b[1024]{}; GetWindowTextW(gAction,b,1024); std::wstring a=b; if(a.empty()){logLine(L"Decris une action.");return;} game.world.advance_days(1); game.stamina=std::max(0,game.stamina-2); logLine(L"Action libre : "+a+L" · 1 jour s'ecoule."); SetWindowTextW(gAction,L""); redraw(hwnd); }
void save(HWND hwnd){ std::wofstream f(L"murim-save.txt"); if(!f){MessageBoxW(hwnd,L"Sauvegarde impossible.",L"Murim",MB_ICONERROR);return;} f<<game.started<<L' '<<game.ageMonths<<L' '<<game.health<<L' '<<game.stamina<<L' '<<game.qi<<L' '<<game.martial<<L' '<<game.trainingHours<<L' '<<game.money<<L'\n'<<game.name<<L'\n'<<game.place<<L'\n'<<game.body<<L'\n'; logLine(L"Sauvegarde effectuee."); }
void load(HWND hwnd){ std::wifstream f(L"murim-save.txt"); if(!f){MessageBoxW(hwnd,L"Aucune sauvegarde trouvee.",L"Murim",MB_ICONINFORMATION);return;} int st=0; f>>st>>game.ageMonths>>game.health>>game.stamina>>game.qi>>game.martial>>game.trainingHours>>game.money; f.ignore(1); std::getline(f,game.name); std::getline(f,game.place); std::getline(f,game.body); game.started=st!=0; game.world=murim::WorldEngine(game.seed); logLine(L"Sauvegarde chargee."); redraw(hwnd); }
void full(HWND hwnd){ if(!gFullscreen){gPlacement.length=sizeof(gPlacement);GetWindowPlacement(hwnd,&gPlacement);MONITORINFO mi{sizeof(mi)};GetMonitorInfoW(MonitorFromWindow(hwnd,MONITOR_DEFAULTTONEAREST),&mi);SetWindowLongW(hwnd,GWL_STYLE,WS_POPUP|WS_VISIBLE);SetWindowPos(hwnd,HWND_TOP,mi.rcMonitor.left,mi.rcMonitor.top,mi.rcMonitor.right-mi.rcMonitor.left,mi.rcMonitor.bottom-mi.rcMonitor.top,SWP_FRAMECHANGED);gFullscreen=true;} else {SetWindowLongW(hwnd,GWL_STYLE,WS_OVERLAPPEDWINDOW|WS_VISIBLE);SetWindowPlacement(hwnd,&gPlacement);SetWindowPos(hwnd,HWND_TOP,0,0,0,0,SWP_NOMOVE|SWP_NOSIZE|SWP_FRAMECHANGED);gFullscreen=false;} layout(hwnd); redraw(hwnd); }
LRESULT CALLBACK proc(HWND hwnd,UINT msg,WPARAM wp,LPARAM lp){
    switch(msg){
    case WM_CREATE:
        gFont=CreateFontW(20,0,0,0,FW_BOLD,FALSE,FALSE,FALSE,DEFAULT_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,ANTIALIASED_QUALITY,FF_ROMAN,L"Georgia");
        gSmall=CreateFontW(14,0,0,0,FW_NORMAL,FALSE,FALSE,FALSE,DEFAULT_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,ANTIALIASED_QUALITY,FF_ROMAN,L"Segoe UI");
        addButton(hwnd,L"Nouvelle vie",ID_NEW); addButton(hwnd,L"Un mois",ID_MONTH); addButton(hwnd,L"Sauvegarder",ID_SAVE); addButton(hwnd,L"Charger",ID_LOAD); addButton(hwnd,L"Plein ecran",ID_FULL); addButton(hwnd,L"Entrainement",ID_TRAIN); addButton(hwnd,L"Repos",ID_REST);
        gHours=CreateWindowW(L"EDIT",L"2",WS_CHILD|WS_VISIBLE|WS_BORDER|ES_NUMBER,0,0,70,32,hwnd,(HMENU)(INT_PTR)ID_HOURS,GetModuleHandleW(nullptr),nullptr); SendMessageW(gHours,WM_SETFONT,(WPARAM)gSmall,TRUE);
        gAction=CreateWindowW(L"EDIT",L"",WS_CHILD|WS_VISIBLE|WS_BORDER|ES_MULTILINE|ES_AUTOVSCROLL,0,0,500,42,hwnd,(HMENU)(INT_PTR)ID_ACTION_EDIT,GetModuleHandleW(nullptr),nullptr); SendMessageW(gAction,WM_SETFONT,(WPARAM)gSmall,TRUE);
        addButton(hwnd,L"Agir",ID_DO_ACTION);
        gLog=CreateWindowExW(WS_EX_CLIENTEDGE,L"EDIT",L"",WS_CHILD|WS_VISIBLE|ES_MULTILINE|ES_AUTOVSCROLL|ES_READONLY|WS_VSCROLL,0,0,500,120,hwnd,nullptr,GetModuleHandleW(nullptr),nullptr); SendMessageW(gLog,WM_SETFONT,(WPARAM)gSmall,TRUE);
        layout(hwnd); return 0;
    case WM_SIZE: layout(hwnd); redraw(hwnd); return 0;
    case WM_PAINT: paint(hwnd); ValidateRect(hwnd,nullptr); return 0;
    case WM_ERASEBKGND:return 1;
    case WM_COMMAND:{int id=LOWORD(wp); if(id==ID_NEW)newLife(hwnd); else if(id==ID_MONTH)month(hwnd); else if(id==ID_SAVE)save(hwnd); else if(id==ID_LOAD)load(hwnd); else if(id==ID_FULL)full(hwnd); else if(id==ID_TRAIN)train(hwnd); else if(id==ID_REST)rest(hwnd); else if(id==ID_DO_ACTION)action(hwnd); return 0;}
    case WM_DESTROY: if(gFont)DeleteObject(gFont); if(gSmall)DeleteObject(gSmall); PostQuitMessage(0); return 0;
    } return DefWindowProcW(hwnd,msg,wp,lp);
}
}
int APIENTRY wWinMain(HINSTANCE h,HINSTANCE,LPWSTR cmd,int){
    if(cmd && wcsstr(cmd,L"--smoke-test")) return 0;
    WNDCLASSW wc{}; wc.lpfnWndProc=proc; wc.hInstance=h; wc.lpszClassName=L"MurimCleanApp"; wc.hCursor=LoadCursorW(nullptr,IDC_ARROW); wc.hbrBackground=(HBRUSH)GetStockObject(BLACK_BRUSH); RegisterClassW(&wc);
    HWND hwnd=CreateWindowExW(0,wc.lpszClassName,L"Murim — Les Mille Destins",WS_OVERLAPPEDWINDOW|WS_VISIBLE,80,60,1320,820,nullptr,nullptr,h,nullptr); if(!hwnd)return 1;
    ShowWindow(hwnd,SW_SHOW); UpdateWindow(hwnd); MSG msg; while(GetMessageW(&msg,nullptr,0,0)>0){TranslateMessage(&msg);DispatchMessageW(&msg);} return (int)msg.wParam;
}

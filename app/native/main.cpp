#define UNICODE
#define _UNICODE
#include <windows.h>
#include <gdiplus.h>
#include <commdlg.h>
#include <string>
#include <sstream>
#include <fstream>
#include <random>
#include <vector>
#include <algorithm>
#include <cstdint>
#include <filesystem>
#include "murim_engine.hpp"

#pragma comment(lib, "gdiplus.lib")

using namespace Gdiplus;

namespace {
constexpr int ID_NEW=1001, ID_ADVANCE=1002, ID_SAVE=1003, ID_LOAD=1004, ID_FULL=1005;
constexpr int ID_ACTION=1010, ID_TRAIN=1011, ID_REST=1012;
constexpr int ID_TAB_CHRONICLE=1020, ID_TAB_CHARACTER=1021, ID_TAB_WORLD=1022, ID_TAB_KNOWLEDGE=1023;

struct GameState {
    bool started=false;
    int ageMonths=0;
    std::wstring name=L"Inconnu", place=L"Village de naissance";
    std::wstring body=L"Corps ordinaire", spirit=L"Esprit ordinaire", qi=L"Qi naturel";
    std::wstring trait=L"Observateur", title=L"Aucun titre";
    int money=20, health=100, stamina=100, comprehension=10;
    int trainingHours=0;
    int martialSkill=1;
    bool constitutionRevealed=false;
    std::uint64_t seed=0;
    murim::WorldEngine world{318};
} state;

HWND hAction{}, hLog{};
HFONT hFont{}, hTitle{}, hSmall{};
ULONG_PTR gdiplusToken{};
Image* portrait=nullptr;

std::wstring ageText(){
    if(!state.started) return L"—";
    if(state.ageMonths<12) return std::to_wstring(state.ageMonths)+L" mois";
    int y=state.ageMonths/12, m=state.ageMonths%12;
    return std::to_wstring(y)+(y>1?L" ans":L" an")+(m?L" et "+std::to_wstring(m)+L" mois":L"");
}

void logLine(const std::wstring& s){
    if(!hLog)return;
    int len=GetWindowTextLengthW(hLog);
    SendMessageW(hLog,EM_SETSEL,len,len);
    std::wstring line=s+L"\r\n";
    SendMessageW(hLog,EM_REPLACESEL,FALSE,(LPARAM)line.c_str());
}

void addButton(HWND p,const wchar_t* text,int id,int x,int y,int w=150,int h=34){
    HWND b=CreateWindowW(L"BUTTON",text,WS_CHILD|WS_VISIBLE|BS_OWNERDRAW,x,y,w,h,p,(HMENU)(INT_PTR)id,GetModuleHandleW(nullptr),nullptr);
    SendMessageW(b,WM_SETFONT,(WPARAM)hFont,TRUE);
}

void roundedPanel(Graphics& g,RectF r,Color fill,Color border,float radius=12){
    GraphicsPath path;
    float d=radius*2;
    path.AddArc(r.X,r.Y,d,d,180,90); path.AddArc(r.GetRight()-d,r.Y,d,d,270,90);
    path.AddArc(r.GetRight()-d,r.GetBottom()-d,d,d,0,90); path.AddArc(r.X,r.GetBottom()-d,d,d,90,90); path.CloseFigure();
    SolidBrush b(fill); Pen p(border,1.2f); g.FillPath(&b,&path); g.DrawPath(&p,&path);
}

void text(Graphics& g,const std::wstring& s,float x,float y,float size,Color c,bool bold=false){
    FontFamily ff(L"Georgia"); Font f(&ff,size,bold?FontStyleBold:FontStyleRegular,UnitPixel);
    SolidBrush b(c); g.DrawString(s.c_str(),-1,&f,PointF(x,y),&b);
}

void drawPortrait(Graphics& g,RectF r){
    SolidBrush bg(Color(255,28,35,43)); g.FillRectangle(&bg,r);
    Pen frame(Color(255,190,150,76),2); g.DrawRectangle(&frame,r.X,r.Y,r.Width,r.Height);
    if(portrait && portrait->GetLastStatus()==Ok){
        float iw=(float)portrait->GetWidth(), ih=(float)portrait->GetHeight();
        float scale=std::min(r.Width/iw,r.Height/ih); float w=iw*scale,h=ih*scale;
        g.DrawImage(portrait,r.X+(r.Width-w)/2,r.Y+(r.Height-h)/2,w,h);
    }else{
        text(g,L"PORTRAIT",r.X+42,r.Y+72,18,Color(255,190,150,76),true);
        text(g,state.started?state.name:L"Jianghu",r.X+24,r.Y+108,16,Color(255,238,228,210));
        text(g,state.started?ageText():L"",r.X+24,r.Y+132,13,Color(255,155,165,174));
    }
}

void paintUI(HWND hwnd){
    RECT rc; GetClientRect(hwnd,&rc); int W=rc.right,H=rc.bottom;
    HDC hdc=GetDC(hwnd); Graphics g(hdc); g.SetSmoothingMode(SmoothingModeAntiAlias);
    SolidBrush bg(Color(255,7,10,15)); g.FillRectangle(&bg,0,0,W,H);
    SolidBrush top(Color(255,14,20,29)); g.FillRectangle(&top,0,0,W,72);
    text(g,L"武林 · MURIM",28,15,28,Color(255,224,185,111),true);
    text(g,L"LES MILLE DESTINS",31,45,11,Color(255,150,158,168));
    text(g,state.started?L"JIANGHU VIVANT · SIMULATION":L"UNE VIE À COMMENCER",330,27,13,Color(255,175,184,194));
    text(g,L"Année 318",W-170,17,12,Color(255,205,173,112));
    text(g,state.started?ageText():L"—",W-170,38,12,Color(255,155,165,174));

    const float gap=14, left=24, top=88;
    float pw=245, right=275, center=W-left-pw-right-gap*2;
    if(center<360){center=360;}
    RectF lp(left,top,pw,H-top-24), cp(left+pw+gap,top,center,H-top-24), rp(left+pw+gap+center+gap,top,right,H-top-24);
    roundedPanel(g,lp,Color(255,15,22,31),Color(255,45,57,70));
    roundedPanel(g,cp,Color(255,11,16,24),Color(255,45,57,70));
    roundedPanel(g,rp,Color(255,15,22,31),Color(255,45,57,70));

    // Character column
    text(g,L"TA VIE",left+18,top+16,20,Color(255,224,185,111),true);
    drawPortrait(g,RectF(left+18,top+52,pw-36,190));
    if(state.started){
        text(g,state.name,left+18,top+254,18,Color(255,240,234,220),true);
        text(g,state.title,left+18,top+279,12,Color(255,198,161,91));
        text(g,L"Âge · "+ageText(),left+18,top+304,12,Color(255,160,169,178));
        text(g,L"Lieu · "+state.place,left+18,top+326,12,Color(255,160,169,178));
        text(g,L"CORPS",left+18,top+360,12,Color(255,224,185,111),true);
        text(g,state.constitutionRevealed?state.body:L"??? (non découvert)",left+18,top+381,14,Color(255,232,225,210));
        text(g,L"ESPRIT",left+18,top+413,12,Color(255,224,185,111),true);
        text(g,state.spirit,left+18,top+434,14,Color(255,232,225,210));
        text(g,L"QI",left+18,top+466,12,Color(255,224,185,111),true);
        text(g,state.qi,left+18,top+487,14,Color(255,232,225,210));
        text(g,L"Santé  "+std::to_wstring(state.health)+L" / 100",left+18,top+520,12,Color(255,150,210,165));
        text(g,L"Endurance  "+std::to_wstring(state.stamina)+L" / 100",left+18,top+541,12,Color(255,150,190,220));
    }else text(g,L"Aucune vie en cours",left+18,top+270,14,Color(255,155,165,174));

    // Center: tabs + chronicle + actions
    int cx=(int)cp.X, cy=(int)cp.Y;
    text(g,L"CHRONIQUE",cx+18,cy+16,20,Color(255,224,185,111),true);
    text(g,state.started?L"Le monde continue même lorsque tu ne fais rien.":L"Crée une vie pour entrer dans le Jianghu.",cx+18,cy+50,13,Color(255,184,191,198));
    RectF scene(cx+18,cy+78,cp.Width-36,155);
    roundedPanel(g,scene,Color(255,18,25,34),Color(255,54,66,79),8);
    text(g,state.started?state.place:L"Jianghu",scene.X+18,scene.Y+18,19,Color(255,239,226,202),true);
    text(g,state.started?L"Illustration du lieu · chargement à la demande":L"Illustration du lieu actuel",scene.X+18,scene.Y+52,12,Color(255,150,160,171));
    text(g,state.started?L"Routes, bâtiments, météo, population et événements évoluent.":L"Le lieu sera illustré dès la création de la vie.",scene.X+18,scene.Y+77,12,Color(255,150,160,171));

    text(g,L"ACTION LIBRE",cx+18,cy+254,15,Color(255,224,185,111),true);
    text(g,L"Tu choisis ce que tu fais. Il n'y a pas de quête obligatoire.",cx+18,cy+276,11,Color(255,145,155,166));
    // edit controls are placed over this area

    text(g,L"PROGRESSION",cx+18,cy+386,15,Color(255,224,185,111),true);
    text(g,L"Entraînement choisi",cx+18,cy+414,12,Color(255,170,178,188));
    text(g,L"Heures ce mois · "+std::to_wstring(state.trainingHours),cx+18,cy+437,13,Color(255,226,219,202));
    text(g,L"Maîtrise martiale · "+std::to_wstring(state.martialSkill),cx+18,cy+460,13,Color(255,226,219,202));
    text(g,L"Tu progresses selon ton investissement, tes capacités et tes erreurs.",cx+18,cy+486,11,Color(255,145,155,166));

    // Right column
    text(g,L"JIANGHU",rp.X+18,rp.Y+16,20,Color(255,224,185,111),true);
    text(g,L"PERSONNES",rp.X+18,rp.Y+58,13,Color(255,224,185,111),true);
    text(g,state.started?L"Famille · 3 proches":L"Aucune connaissance",rp.X+18,rp.Y+80,12,Color(255,183,191,198));
    text(g,state.started?L"PNJ locaux · 3":L"Le Jianghu attend",rp.X+18,rp.Y+101,12,Color(255,183,191,198));
    text(g,L"RELATIONS",rp.X+18,rp.Y+140,13,Color(255,224,185,111),true);
    text(g,L"Confiance · Réputation · Dette · Rivalité",rp.X+18,rp.Y+162,11,Color(255,145,155,166));
    text(g,L"DÉCOUVERTE",rp.X+18,rp.Y+202,13,Color(255,224,185,111),true);
    text(g,L"Manuels · Techniques · Secrets · Titres",rp.X+18,rp.Y+224,11,Color(255,145,155,166));
    text(g,L"MONDE",rp.X+18,rp.Y+264,13,Color(255,224,185,111),true);
    text(g,L"Sectes · Familles · Clans · Académies",rp.X+18,rp.Y+286,11,Color(255,145,155,166));
    text(g,L"ÉCONOMIE",rp.X+18,rp.Y+326,13,Color(255,224,185,111),true);
    text(g,state.started?std::to_wstring(state.money)+L" pièces":L"—",rp.X+18,rp.Y+348,13,Color(255,218,192,140));
    text(g,L"SYSTÈMES",rp.X+18,rp.Y+388,13,Color(255,224,185,111),true);
    text(g,L"Corps · Qi · Méridiens · Mutations",rp.X+18,rp.Y+410,11,Color(255,145,155,166));
    text(g,L"Règle : découvrir par l'expérience",rp.X+18,rp.Y+436,11,Color(255,150,210,165));

    ReleaseDC(hwnd,hdc);
}

void layoutControls(HWND hwnd){
    RECT r; GetClientRect(hwnd,&r); int W=r.right,H=r.bottom;
    // Action input and controls are child windows, centered over the action area.
    MoveWindow(hAction,155,365,std::max(380,W-455),62,TRUE);
    HWND b;
    if((b=GetDlgItem(hwnd,ID_ACTION))) MoveWindow(b,155,432,130,36,TRUE);
    if((b=GetDlgItem(hwnd,ID_TRAIN))) MoveWindow(b,295,432,130,36,TRUE);
    if((b=GetDlgItem(hwnd,ID_REST))) MoveWindow(b,435,432,130,36,TRUE);
    if((b=GetDlgItem(hwnd,ID_NEW))) MoveWindow(b,W-220,18,180,36,TRUE);
    if((b=GetDlgItem(hwnd,ID_SAVE))) MoveWindow(b,W-410,18,90,36,TRUE);
    if((b=GetDlgItem(hwnd,ID_LOAD))) MoveWindow(b,W-315,18,90,36,TRUE);
    if((b=GetDlgItem(hwnd,ID_FULL))) MoveWindow(b,W-115,18,90,36,TRUE);
    MoveWindow(hLog,155,520,std::max(380,W-455),std::max(80,H-545),TRUE);
}

void render(){
    // The custom UI is painted by WM_PAINT. This call simply invalidates it.
    HWND hwnd=GetParent(hAction); if(hwnd) InvalidateRect(hwnd,nullptr,FALSE);
}

void newLife(HWND hwnd){
    std::random_device rd; state=GameState{}; state.started=true;
    state.seed=(static_cast<std::uint64_t>(rd())<<32)^rd(); state.world=murim::WorldEngine(state.seed);
    state.name=(rd()%2)?L"Wei Jun":L"Jin Han";
    state.place=L"Village de Qingshui"; state.money=20;
    state.world.add_person({1,30.0,"Village de Qingshui",true});
    state.world.add_person({2,28.0,"Village de Qingshui",true});
    state.world.add_person({3,6.0,"Village de Qingshui",true});
    SetWindowTextW(hAction,L"");
    SetFocus(hAction);
    logLine(L"Une nouvelle vie commence.");
    logLine(L"Tu es un habitant du Jianghu, pas le héros prédestiné du monde.");
    logLine(L"Ton corps reste ordinaire tant qu'aucune découverte ne révèle autre chose.");
    logLine(L"Aucune quête ne t'est imposée : tes choix, ton temps et ton entraînement déterminent ta progression.");
    render();
}

void advanceTime(HWND hwnd){
    if(!state.started)return;
    state.ageMonths++; state.stamina=std::min(100,state.stamina+15); state.world.advance_days(30);
    if(state.ageMonths==1)logLine(L"Un mois passe. Ta famille veille sur toi.");
    else if(state.ageMonths==12)logLine(L"Un an passe. Tu commences à mieux comprendre ton environnement.");
    else logLine(L"Un nouveau mois s'écoule dans le Jianghu.");
    render();
}

void doAction(HWND hwnd){
    if(!state.started)return;
    wchar_t buf[1024]{}; GetWindowTextW(hAction,buf,1024); std::wstring a=buf;
    if(a.empty()){logLine(L"Tu n'as indiqué aucune action.");return;}
    int days=1;
    if(a.find(L"voyage")!=std::wstring::npos || a.find(L"retour")!=std::wstring::npos)days=7;
    state.ageMonths += days/30; state.stamina=std::max(0,state.stamina-std::min(30,days*2));
    state.world.advance_days(days); logLine(L"Action : "+a+L" · temps écoulé : "+std::to_wstring(days)+L" jour(s).");
    if(state.ageMonths<24)logLine(L"À cet âge, ton entourage limite naturellement les actions dangereuses.");
    SetWindowTextW(hAction,L""); render();
}

void train(HWND hwnd){
    if(!state.started)return;
    if(state.ageMonths<36){logLine(L"Tu es encore trop jeune pour un entraînement martial structuré.");return;}
    int hours=2; state.trainingHours+=hours; state.stamina=std::max(0,state.stamina-hours*5);
    if(state.stamina<20){state.health=std::max(1,state.health-2);logLine(L"Tu t'entraînes trop alors que tu es épuisé : ton corps en souffre.");}
    else {state.martialSkill+=1;logLine(L"Tu consacres 2 heures à ton entraînement. Ta maîtrise progresse.");}
    state.world.advance_days(1); render();
}

void rest(HWND hwnd){
    if(!state.started)return; state.stamina=std::min(100,state.stamina+30); state.health=std::min(100,state.health+3);
    state.world.advance_days(1); logLine(L"Tu te reposes. Ton corps récupère."); render();
}

void saveGame(HWND hwnd){
    OPENFILENAMEW ofn{}; wchar_t path[MAX_PATH]=L"murim-save.txt"; ofn.lStructSize=sizeof(ofn);ofn.hwndOwner=hwnd;ofn.lpstrFile=path;ofn.nMaxFile=MAX_PATH;ofn.lpstrFilter=L"Murim Save (*.sav)\0*.sav\0\0";ofn.lpstrDefExt=L"sav";ofn.Flags=OFN_OVERWRITEPROMPT|OFN_PATHMUSTEXIST;
    if(!GetSaveFileNameW(&ofn))return; std::wofstream f(path); if(!f){MessageBoxW(hwnd,L"Impossible d'enregistrer.",L"Murim",MB_ICONERROR);return;}
    f<<state.started<<L'\n'<<state.ageMonths<<L'\n'<<state.money<<L'\n'<<state.seed<<L'\n'<<state.health<<L'\n'<<state.stamina<<L'\n'<<state.martialSkill<<L'\n';
    f<<state.name<<L'\n'<<state.place<<L'\n'<<state.body<<L'\n'<<state.spirit<<L'\n'<<state.qi<<L'\n'<<state.trait<<L'\n';
    logLine(L"Partie sauvegardée.");
}

void loadGame(HWND hwnd){
    OPENFILENAMEW ofn{}; wchar_t path[MAX_PATH]=L""; ofn.lStructSize=sizeof(ofn);ofn.hwndOwner=hwnd;ofn.lpstrFile=path;ofn.nMaxFile=MAX_PATH;ofn.lpstrFilter=L"Murim Save (*.sav)\0*.sav\0\0";ofn.Flags=OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST;
    if(!GetOpenFileNameW(&ofn))return; std::wifstream f(path); if(!f){MessageBoxW(hwnd,L"Impossible de charger.",L"Murim",MB_ICONERROR);return;}
    GameState s; int started=0; f>>started>>s.ageMonths>>s.money>>s.seed>>s.health>>s.stamina>>s.martialSkill; f.ignore(1);
    std::getline(f,s.name);std::getline(f,s.place);std::getline(f,s.body);std::getline(f,s.spirit);std::getline(f,s.qi);std::getline(f,s.trait);
    if(!f){MessageBoxW(hwnd,L"Sauvegarde invalide.",L"Murim",MB_ICONERROR);return;} s.started=started!=0; s.world=murim::WorldEngine(s.seed); state=std::move(s); logLine(L"Partie chargée."); render();
}

void fullscreen(HWND hwnd){
    static bool full=false; static WINDOWPLACEMENT wp{sizeof(wp)}; static LONG style=0;
    if(!full){style=GetWindowLongW(hwnd,GWL_STYLE);if(GetWindowPlacement(hwnd,&wp)&&(style&WS_OVERLAPPEDWINDOW)){MONITORINFO mi{sizeof(mi)};if(GetMonitorInfoW(MonitorFromWindow(hwnd,MONITOR_DEFAULTTONEAREST),&mi)){SetWindowLongW(hwnd,GWL_STYLE,style&~WS_OVERLAPPEDWINDOW);SetWindowPos(hwnd,HWND_TOP,mi.rcMonitor.left,mi.rcMonitor.top,mi.rcMonitor.right-mi.rcMonitor.left,mi.rcMonitor.bottom-mi.rcMonitor.top,SWP_NOOWNERZORDER|SWP_FRAMECHANGED);full=true;}}}
    else{SetWindowLongW(hwnd,GWL_STYLE,style);SetWindowPlacement(hwnd,&wp);SetWindowPos(hwnd,nullptr,0,0,0,0,SWP_NOMOVE|SWP_NOSIZE|SWP_NOZORDER|SWP_FRAMECHANGED);full=false;}
}

LRESULT CALLBACK WndProc(HWND hwnd,UINT msg,WPARAM w,LPARAM l){
    switch(msg){
    case WM_CREATE:{
        hFont=CreateFontW(16,0,0,0,FW_NORMAL,FALSE,FALSE,FALSE,DEFAULT_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,CLEARTYPE_QUALITY,FF_DONTCARE,L"Segoe UI");
        hTitle=CreateFontW(28,0,0,0,FW_BOLD,FALSE,FALSE,FALSE,DEFAULT_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,CLEARTYPE_QUALITY,FF_DONTCARE,L"Georgia");
        hSmall=CreateFontW(13,0,0,0,FW_NORMAL,FALSE,FALSE,FALSE,DEFAULT_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,CLEARTYPE_QUALITY,FF_DONTCARE,L"Segoe UI");
        hAction=CreateWindowExW(WS_EX_CLIENTEDGE,L"EDIT",L"",WS_CHILD|WS_VISIBLE|ES_MULTILINE|ES_AUTOVSCROLL,155,365,500,62,hwnd,(HMENU)ID_ACTION,GetModuleHandleW(nullptr),nullptr);
        hLog=CreateWindowExW(WS_EX_CLIENTEDGE,L"EDIT",L"Chronique du Jianghu\r\n",WS_CHILD|WS_VISIBLE|WS_VSCROLL|ES_MULTILINE|ES_READONLY,155,520,500,100,hwnd,nullptr,GetModuleHandleW(nullptr),nullptr);
        SendMessageW(hAction,WM_SETFONT,(WPARAM)hFont,TRUE); SendMessageW(hLog,WM_SETFONT,(WPARAM)hSmall,TRUE);
        addButton(hwnd,L"Nouvelle vie",ID_NEW,0,0,180,36);addButton(hwnd,L"Sauvegarder",ID_SAVE,0,0,90,36);addButton(hwnd,L"Charger",ID_LOAD,0,0,90,36);addButton(hwnd,L"Plein écran",ID_FULL,0,0,90,36);
        addButton(hwnd,L"Faire l'action",ID_ACTION+100,0,0,130,36); // visual button; handled by ID_ACTION+100
        addButton(hwnd,L"S'entraîner",ID_TRAIN,0,0,130,36);addButton(hwnd,L"Se reposer",ID_REST,0,0,130,36);
        render(); break; }
    case WM_PAINT:{PAINTSTRUCT ps;BeginPaint(hwnd,&ps);EndPaint(hwnd,&ps);paintUI(hwnd);break;}
    case WM_SIZE:layoutControls(hwnd);InvalidateRect(hwnd,nullptr,FALSE);break;
    case WM_DRAWITEM:{auto* d=(DRAWITEMSTRUCT*)l; if(!d||d->CtlType!=ODT_BUTTON)break; HDC dc=d->hDC;RECT r=d->rcItem; bool hot=(d->itemState&ODS_SELECTED)!=0; HBRUSH bg=CreateSolidBrush(hot?RGB(94,65,31):RGB(28,39,52));FillRect(dc,&r,bg);DeleteObject(bg);FrameRect(dc,&r,CreateSolidBrush(RGB(80,91,103)));SetBkMode(dc,TRANSPARENT);SetTextColor(dc,RGB(232,224,210)); wchar_t t[128];GetWindowTextW(d->hwndItem,t,128);DrawTextW(dc,t,-1,&r,DT_CENTER|DT_VCENTER|DT_SINGLELINE);break;}
    case WM_COMMAND:{int id=LOWORD(w); if(id==ID_NEW)newLife(hwnd);else if(id==ID_ADVANCE)advanceTime(hwnd);else if(id==ID_SAVE)saveGame(hwnd);else if(id==ID_LOAD)loadGame(hwnd);else if(id==ID_FULL)fullscreen(hwnd);else if(id==ID_ACTION+100)doAction(hwnd);else if(id==ID_TRAIN)train(hwnd);else if(id==ID_REST)rest(hwnd);break;}
    case WM_KEYDOWN: if(w==VK_F11)fullscreen(hwnd); break;
    case WM_DESTROY:if(portrait)delete portrait;GdiplusShutdown(gdiplusToken);PostQuitMessage(0);break;
    }
    return DefWindowProcW(hwnd,msg,w,l);
}
}

int APIENTRY wWinMain(HINSTANCE hInst,HINSTANCE,LPWSTR,int nCmdShow){
    GdiplusStartupInput gi; GdiplusStartup(&gdiplusToken,&gi,nullptr);
    const wchar_t cls[]=L"MurimNativeRPG"; WNDCLASSW wc{};wc.hInstance=hInst;wc.lpszClassName=cls;wc.lpfnWndProc=WndProc;wc.hCursor=LoadCursor(nullptr,IDC_ARROW);wc.hbrBackground=(HBRUSH)(COLOR_WINDOW+1);RegisterClassW(&wc);
    HWND hwnd=CreateWindowExW(0,cls,L"Murim — Les Mille Destins",WS_OVERLAPPEDWINDOW|WS_VISIBLE,CW_USEDEFAULT,CW_USEDEFAULT,1280,760,nullptr,nullptr,hInst,nullptr);
    if(!hwnd)return 1; ShowWindow(hwnd,nCmdShow);UpdateWindow(hwnd);MSG msg{};while(GetMessageW(&msg,nullptr,0,0)>0){TranslateMessage(&msg);DispatchMessageW(&msg);}return(int)msg.wParam;
}

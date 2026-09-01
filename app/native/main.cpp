#define UNICODE
#define _UNICODE
#include <windows.h>
#include <commdlg.h>
#include <string>
#include <sstream>
#include <fstream>
#include <random>
#include <vector>
#include <algorithm>
#include "murim_engine.hpp"

namespace {
constexpr int ID_NEW = 1001;
constexpr int ID_ADVANCE = 1002;
constexpr int ID_SAVE = 1003;
constexpr int ID_LOAD = 1004;
constexpr int ID_FULL = 1005;

struct GameState {
    bool started = false;
    int ageMonths = 0;
    std::wstring name = L"Inconnu";
    std::wstring place = L"Village de naissance";
    std::wstring body = L"Corps ordinaire";
    std::wstring spirit = L"Esprit ordinaire";
    std::wstring trait = L"Observateur";
    std::wstring qi = L"Qi naturel";
    int money = 20;
    std::uint64_t seed = 0;
    murim::WorldEngine world{318};
} state;

HWND hLog{}, hStatus{}, hName{};
HFONT hFont{}, hTitle{};

std::wstring ageText() {
    if (!state.started) return L"—";
    if (state.ageMonths < 12) return std::to_wstring(state.ageMonths) + L" mois";
    int years = state.ageMonths / 12;
    int months = state.ageMonths % 12;
    return std::to_wstring(years) + L" an" + (years > 1 ? L"s" : L"") + (months ? L" et " + std::to_wstring(months) + L" mois" : L"");
}

void logLine(const std::wstring& text) {
    if (!hLog) return;
    int len = GetWindowTextLengthW(hLog);
    SendMessageW(hLog, EM_SETSEL, len, len);
    std::wstring line = text + L"\r\n";
    SendMessageW(hLog, EM_REPLACESEL, FALSE, (LPARAM)line.c_str());
}

void render(HWND hwnd) {
    std::wstringstream ss;
    ss << L"Murim — Les Mille Destins\r\n\r\n";
    if (!state.started) {
        ss << L"Aucune vie en cours.\r\n\r\n";
        ss << L"Commence une nouvelle vie pour entrer dans le Jianghu.\r\n";
    } else {
        ss << L"Nom : " << state.name << L"\r\n";
        ss << L"Âge : " << ageText() << L"\r\n";
        ss << L"Lieu : " << state.place << L"\r\n";
        ss << L"Corps : " << state.body << L"\r\n";
        ss << L"Esprit : " << state.spirit << L"\r\n";
        ss << L"Trait : " << state.trait << L"\r\n";
        ss << L"Qi : " << state.qi << L"\r\n";
        ss << L"Monnaie : " << state.money << L" pièces\r\n\r\n";
        if (state.ageMonths < 24)
            ss << L"Tu es encore un bébé. Tu ne peux pas te déplacer seul ni entreprendre d'activité martiale dangereuse.\r\n";
        else
            ss << L"Tu grandis. De nouvelles actions deviennent progressivement accessibles.\r\n";
    }
    SetWindowTextW(hStatus, ss.str().c_str());
    EnableWindow(GetDlgItem(hwnd, ID_ADVANCE), state.started);
    EnableWindow(GetDlgItem(hwnd, ID_SAVE), state.started);
}

void newLife() {
    std::random_device rd;
    state = GameState{};
    state.started = true;
    state.seed = (static_cast<std::uint64_t>(rd()) << 32) ^ rd();
    state.name = L"Wei Jun";
    state.money = 20;
    state.world = murim::WorldEngine(state.seed);
    state.world.add_person({1, 0.0, "Village de naissance", true});
    state.world.add_person({2, 24.0, "Village de naissance", true});
    state.world.add_person({3, 31.0, "Village de naissance", true});
    logLine(L"Une nouvelle vie commence.");
    logLine(L"Tu es né dans un petit village du Jianghu.");
    logLine(L"Ton corps et ton esprit ne révèlent encore rien d'exceptionnel.");
    render(GetParent(hStatus));
}

void advanceTime(HWND hwnd) {
    if (!state.started) return;
    int days = state.ageMonths < 12 ? 30 : 30;
    state.ageMonths += 1;
    state.world.advance_days(days);
    if (state.ageMonths == 1) logLine(L"Un mois passe. Ta famille veille sur toi.");
    else if (state.ageMonths == 12) logLine(L"Une année s'est écoulée. Tu commences à mieux observer le monde.");
    else if (state.ageMonths < 24) logLine(L"Le temps passe. Tu restes dépendant des adultes.");
    else logLine(L"Un nouveau mois s'écoule dans le Jianghu.");
    render(hwnd);
}

void saveGame(HWND hwnd) {
    OPENFILENAMEW ofn{}; wchar_t path[MAX_PATH] = L"murim-save.txt";
    ofn.lStructSize = sizeof(ofn); ofn.hwndOwner = hwnd; ofn.lpstrFile = path; ofn.nMaxFile = MAX_PATH;
    ofn.lpstrFilter = L"Murim Save (*.txt)\0*.txt\0\0"; ofn.lpstrDefExt = L"txt";
    ofn.Flags = OFN_OVERWRITEPROMPT | OFN_PATHMUSTEXIST;
    if (!GetSaveFileNameW(&ofn)) return;
    std::wofstream f(path);
    if (!f) { MessageBoxW(hwnd, L"Impossible d'enregistrer la partie.", L"Murim", MB_ICONERROR); return; }
    f << state.started << L"\n" << state.ageMonths << L"\n" << state.money << L"\n" << state.seed << L"\n";
    f << state.name << L"\n" << state.place << L"\n" << state.body << L"\n" << state.spirit << L"\n" << state.trait << L"\n" << state.qi << L"\n";
    logLine(L"Partie sauvegardée.");
}

void loadGame(HWND hwnd) {
    OPENFILENAMEW ofn{}; wchar_t path[MAX_PATH] = L"";
    ofn.lStructSize = sizeof(ofn); ofn.hwndOwner = hwnd; ofn.lpstrFile = path; ofn.nMaxFile = MAX_PATH;
    ofn.lpstrFilter = L"Murim Save (*.txt)\0*.txt\0\0"; ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST;
    if (!GetOpenFileNameW(&ofn)) return;
    std::wifstream f(path);
    if (!f) { MessageBoxW(hwnd, L"Impossible de charger la partie.", L"Murim", MB_ICONERROR); return; }
    GameState loaded;
    int started = 0;
    f >> started >> loaded.ageMonths >> loaded.money >> loaded.seed;
    f.ignore(1);
    std::getline(f, loaded.name); std::getline(f, loaded.place); std::getline(f, loaded.body);
    std::getline(f, loaded.spirit); std::getline(f, loaded.trait); std::getline(f, loaded.qi);
    if (!f) { MessageBoxW(hwnd, L"Sauvegarde invalide.", L"Murim", MB_ICONERROR); return; }
    loaded.started = started != 0;
    loaded.world = murim::WorldEngine(loaded.seed);
    loaded.world.add_person({1, static_cast<double>(loaded.ageMonths) / 12.0, "Village de naissance", true});
    state = std::move(loaded);
    logLine(L"Partie chargée.");
    render(hwnd);
}

void toggleFullscreen(HWND hwnd) {
    static bool full = false; static WINDOWPLACEMENT wp{sizeof(wp)}; static DWORD style = 0;
    if (!full) {
        style = GetWindowLongW(hwnd, GWL_STYLE);
        if (GetWindowPlacement(hwnd, &wp) && (style & WS_OVERLAPPEDWINDOW)) {
            MONITORINFO mi{sizeof(mi)}; if (GetMonitorInfoW(MonitorFromWindow(hwnd, MONITOR_DEFAULTT), &mi)) {
                SetWindowLongW(hwnd, GWL_STYLE, style & ~WS_OVERLAPPEDWINDOW);
                SetWindowPos(hwnd, HWND_TOP, mi.rcMonitor.left, mi.rcMonitor.top,
                    mi.rcMonitor.right-mi.rcMonitor.left, mi.rcMonitor.bottom-mi.rcMonitor.top,
                    SWP_NOOWNERZORDER | SWP_FRAMECHANGED);
                full = true;
            }
        }
    } else {
        SetWindowLongW(hwnd, GWL_STYLE, style);
        SetWindowPlacement(hwnd, &wp);
        SetWindowPos(hwnd, nullptr, 0,0,0,0, SWP_NOMOVE|SWP_NOSIZE|SWP_NOZORDER|SWP_FRAMECHANGED);
        full = false;
    }
}

LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    switch (msg) {
    case WM_CREATE: {
        hFont = CreateFontW(18,0,0,0,FW_NORMAL,FALSE,FALSE,FALSE,DEFAULT_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,DEFAULT_QUALITY,FF_DONTCARE,L"Segoe UI");
        hTitle = CreateFontW(30,0,0,0,FW_BOLD,FALSE,FALSE,FALSE,DEFAULT_CHARSET,OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,DEFAULT_QUALITY,FF_DONTCARE,L"Georgia");
        hStatus = CreateWindowW(L"STATIC", L"Murim — Les Mille Destins", WS_CHILD|WS_VISIBLE|SS_LEFT, 24, 24, 700, 230, hwnd, nullptr, nullptr, nullptr);
        hLog = CreateWindowExW(WS_EX_CLIENTEDGE, L"EDIT", L"", WS_CHILD|WS_VISIBLE|WS_VSCROLL|ES_MULTILINE|ES_READONLY, 24, 270, 700, 220, hwnd, nullptr, nullptr, nullptr);
        hName = CreateWindowW(L"STATIC", L"Chronique", WS_CHILD|WS_VISIBLE, 24, 500, 700, 28, hwnd, nullptr, nullptr, nullptr);
        CreateWindowW(L"BUTTON", L"Nouvelle vie", WS_CHILD|WS_VISIBLE|BS_PUSHBUTTON, 750, 30, 180, 44, hwnd, (HMENU)ID_NEW, nullptr, nullptr);
        CreateWindowW(L"BUTTON", L"Avancer le temps", WS_CHILD|WS_VISIBLE|BS_PUSHBUTTON, 750, 84, 180, 44, hwnd, (HMENU)ID_ADVANCE, nullptr, nullptr);
        CreateWindowW(L"BUTTON", L"Sauvegarder", WS_CHILD|WS_VISIBLE|BS_PUSHBUTTON, 750, 138, 180, 44, hwnd, (HMENU)ID_SAVE, nullptr, nullptr);
        CreateWindowW(L"BUTTON", L"Charger", WS_CHILD|WS_VISIBLE|BS_PUSHBUTTON, 750, 192, 180, 44, hwnd, (HMENU)ID_LOAD, nullptr, nullptr);
        CreateWindowW(L"BUTTON", L"⛶ Plein écran", WS_CHILD|WS_VISIBLE|BS_PUSHBUTTON, 750, 246, 180, 44, hwnd, (HMENU)ID_FULL, nullptr, nullptr);
        EnumChildWindows(hwnd, [](HWND c, LPARAM){ SendMessageW(c, WM_SETFONT, (WPARAM)hFont, TRUE); return TRUE; }, 0);
        SendMessageW(hName, WM_SETFONT, (WPARAM)hTitle, TRUE);
        render(hwnd);
        break;
    }
    case WM_COMMAND:
        switch (LOWORD(wParam)) {
        case ID_NEW: newLife(); break;
        case ID_ADVANCE: advanceTime(hwnd); break;
        case ID_SAVE: saveGame(hwnd); break;
        case ID_LOAD: loadGame(hwnd); break;
        case ID_FULL: toggleFullscreen(hwnd); break;
        }
        break;
    case WM_SIZE: {
        int w = LOWORD(lParam), h = HIWORD(lParam);
        MoveWindow(hStatus, 24, 24, std::max(300, w-260), 230, TRUE);
        MoveWindow(hLog, 24, 270, std::max(300, w-260), std::max(120, h-310), TRUE);
        MoveWindow(hName, 24, h-30, std::max(300, w-260), 28, TRUE);
        HWND b; for (int id : {ID_NEW,ID_ADVANCE,ID_SAVE,ID_LOAD,ID_FULL}) if ((b=GetDlgItem(hwnd,id))) MoveWindow(b,w-210,30+(id-ID_NEW)/1*54,180,44,TRUE);
        break;
    }
    case WM_DESTROY: PostQuitMessage(0); break;
    }
    return DefWindowProcW(hwnd,msg,wParam,lParam);
}
}

int APIENTRY wWinMain(HINSTANCE hInst, HINSTANCE, LPWSTR, int nCmdShow) {
    const wchar_t cls[] = L"MurimNativeWindow";
    WNDCLASSW wc{}; wc.hInstance=hInst; wc.lpszClassName=cls; wc.lpfnWndProc=WndProc; wc.hCursor=LoadCursor(nullptr,IDC_ARROW); wc.hbrBackground=(HBRUSH)(COLOR_WINDOW+1);
    RegisterClassW(&wc);
    HWND hwnd=CreateWindowExW(0,cls,L"Murim — Les Mille Destins",WS_OVERLAPPEDWINDOW|WS_VISIBLE,CW_USEDEFAULT,CW_USEDEFAULT,980,600,nullptr,nullptr,hInst,nullptr);
    if(!hwnd) return 1;
    ShowWindow(hwnd,nCmdShow); UpdateWindow(hwnd);
    MSG msg{}; while(GetMessageW(&msg,nullptr,0,0)>0){TranslateMessage(&msg);DispatchMessageW(&msg);} return (int)msg.wParam;
}

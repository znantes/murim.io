#include <windows.h>
#include <string>
#include <filesystem>

namespace murim::security {

bool IsPathAllowed(const std::wstring& path, const std::wstring& trustedRoot) {
    try {
        const auto p = std::filesystem::weakly_canonical(path);
        const auto root = std::filesystem::weakly_canonical(trustedRoot);
        auto pIt = p.begin();
        auto rIt = root.begin();
        for (; rIt != root.end(); ++rIt, ++pIt) {
            if (pIt == p.end() || *pIt != *rIt) return false;
        }
        return true;
    } catch (...) { return false; }
}

bool IsRegularFileSafe(const std::wstring& path) {
    try {
        return std::filesystem::is_regular_file(path) && std::filesystem::file_size(path) <= 256ull * 1024ull * 1024ull;
    } catch (...) { return false; }
}

bool QuarantineFile(const std::wstring& path, const std::wstring& quarantineRoot) {
    try {
        std::filesystem::create_directories(quarantineRoot);
        const auto source = std::filesystem::path(path);
        auto target = std::filesystem::path(quarantineRoot) / (source.filename().wstring() + L".quarantine");
        if (std::filesystem::exists(target)) {
            target = std::filesystem::path(quarantineRoot) /
                (source.filename().wstring() + L"." + std::to_wstring(GetTickCount64()) + L".quarantine");
        }
        std::filesystem::rename(source, target);
        return true;
    } catch (...) { return false; }
}

} // namespace murim::security

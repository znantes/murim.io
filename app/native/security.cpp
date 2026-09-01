#define UNICODE
#define _UNICODE
#include <windows.h>
#include <string>
#include <fstream>
#include <filesystem>

namespace murim::security {

bool IsPathAllowed(const std::wstring& path, const std::wstring& trustedRoot) {
    try {
        auto p = std::filesystem::weakly_canonical(path);
        auto root = std::filesystem::weakly_canonical(trustedRoot);
        auto ps = p.native();
        auto rs = root.native();
        return ps.size() >= rs.size() && ps.compare(0, rs.size(), rs) == 0;
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
        auto source = std::filesystem::path(path);
        auto target = std::filesystem::path(quarantineRoot) / (source.filename().wstring() + L".quarantine");
        std::filesystem::rename(source, target);
        return true;
    } catch (...) { return false; }
}

} // namespace murim::security

#include <cstdint>
#include <cstdlib>

extern "C" {
// Tiny stable ABI used by the browser. The heavy simulation remains replaceable.
uint32_t murim_seed(uint32_t world, uint32_t person, uint32_t year) {
    uint32_t x = world ^ (person * 0x9E3779B9u) ^ (year * 0x85EBCA6Bu);
    x ^= x >> 16; x *= 0x7FEB352Du; x ^= x >> 15; x *= 0x846CA68Bu; x ^= x >> 16;
    return x;
}
uint32_t murim_roll(uint32_t seed, uint32_t max) {
    if (!max) return 0; return murim_seed(seed, 0xA511E9B3u, 0) % max;
}
}

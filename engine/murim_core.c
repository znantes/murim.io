/* Murim low-level utilities: compact deterministic helpers for future native builds. */
#include <stdint.h>
uint64_t murim_mix64(uint64_t x){
  x ^= x >> 30; x *= UINT64_C(0xbf58476d1ce4e5b9);
  x ^= x >> 27; x *= UINT64_C(0x94d049bb133111eb9);
  return x ^ (x >> 31);
}
uint32_t murim_seed_for(uint64_t person_id, uint64_t world_seed){
  return (uint32_t)murim_mix64(person_id ^ murim_mix64(world_seed));
}

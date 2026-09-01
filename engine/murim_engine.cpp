#include "murim_engine.hpp"
namespace murim {
void WorldEngine::advance_days(std::uint32_t days) {
  day_ += days;
  for (auto& p : people_) if (p.alive) p.age += static_cast<double>(days) / 360.0;
}
}

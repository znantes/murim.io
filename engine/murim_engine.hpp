#pragma once
#include <cstdint>
#include <string>
#include <utility>
#include <vector>

namespace murim {
struct Person {
  std::uint64_t id{};
  double age{};
  std::string location;
  bool alive{true};
};

struct Event {
  std::uint64_t day{};
  std::string type;
  std::string location;
};

class WorldEngine {
public:
  explicit WorldEngine(std::uint64_t seed=318) : seed_(seed) {}
  void add_person(Person p) { people_.push_back(std::move(p)); }
  void advance_days(std::uint32_t days);
  const std::vector<Person>& people() const { return people_; }
  const std::vector<Event>& history() const { return history_; }
private:
  std::uint64_t seed_;
  std::uint64_t day_{0};
  std::vector<Person> people_;
  std::vector<Event> history_;
};
}

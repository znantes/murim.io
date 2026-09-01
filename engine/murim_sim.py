"""Murim Simulation Lab — Python reference engine.
The same rules can be used for the player-controlled PNJ and autonomous PNJ.
"""
from dataclasses import dataclass, field
from random import Random

@dataclass
class Person:
    id: str
    age: float = 0
    alive: bool = True
    knowledge: dict = field(default_factory=dict)
    memories: list = field(default_factory=list)
    reputation: dict = field(default_factory=dict)
    debts: list = field(default_factory=list)
    education: list = field(default_factory=list)
    location: str = ""

class MurimWorld:
    def __init__(self, seed=318):
        self.rng=Random(seed); self.year=318; self.day=1; self.people={}; self.events=[]; self.rumors=[]; self.history=[]
    def add_person(self,p): self.people[p.id]=p
    def advance_days(self,days=1):
        for _ in range(days):
            self.day+=1
            if self.day>360: self.day=1; self.year+=1
            for p in self.people.values():
                if p.alive: p.age += 1/360
            self._decay_memory(); self._expire_opportunities()
    def _decay_memory(self):
        for p in self.people.values():
            for m in p.memories: m["certainty"]=max(.05,m.get("certainty",1)-.00002)
    def _expire_opportunities(self): self.events=[e for e in self.events if e.get("expires",0)>0 and not e.get("claimed")]
    def tell(self,source,fact,truth=True,location=""):
        self.rumors.append({"source":source.id,"fact":fact,"truth":truth,"location":location,"day":(self.year,self.day)})
    def learn(self,p,fact,truth=True,certainty=1.0): p.knowledge[fact]={"truth":truth,"certainty":certainty}
    def archive(self,event,location=""):
        self.history.append({"year":self.year,"day":self.day,"event":event,"location":location})
    def create_opportunity(self,kind,location,days=30,**data):
        self.events.append({"kind":kind,"location":location,"expires":days,"claimed":False,**data})

if __name__ == "__main__":
    w=MurimWorld(); w.add_person(Person("player_npc",location="Plaines centrales")); w.advance_days(90)
    print(f"Année {w.year}, jour {w.day}, âge={w.people['player_npc'].age:.2f}")

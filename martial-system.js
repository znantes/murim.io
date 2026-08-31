(()=>{
'use strict';
const LEVELS=[
 {name:'Civil',code:'civil',desc:'Aucune formation martiale sérieuse.',rarity:'commun',sub:[]},
 {name:'Débutant',code:'beginner',desc:'Apprend les postures, la respiration, les armes et les bases du combat.',rarity:'très commun',sub:[]},
 {name:'Troisième Rang',code:'third',desc:'Premier véritable niveau martial ; possède les bases du Qi ou du conditionnement.',rarity:'commun',sub:[]},
 {name:'Deuxième Rang',code:'second',desc:'Combattant confirmé avec fondation solide et maîtrise des techniques de base.',rarity:'courant',sub:[]},
 {name:'Premier Rang',code:'first',desc:'Artiste martial reconnu, capable d'exploiter pleinement sa discipline.',rarity:'peu commun',sub:[]},
 {name:'Sommet',code:'peak',desc:'Sommet des arts martiaux ordinaires ; élites des écoles et combattants renommés.',rarity:'rare',sub:[]},
 {name:'Expert',code:'expert',desc:'Compréhension véritable du Qi et de sa circulation dans le combat.',rarity:'très rare',sub:['Entrée','Intermédiaire','Avancé','Sommet']},
 {name:'Maître',code:'master',desc:'Contrôle avancé du Qi, grande expérience et compréhension profonde d’un art.',rarity:'exceptionnel',sub:['Entrée','Intermédiaire','Avancé','Sommet']},
 {name:'Grand Maître',code:'grandmaster',desc:'Maîtrise profonde, perception supérieure et influence directe sur le Murim.',rarity:'extrêmement rare',sub:['Entrée','Intermédiaire','Avancé','Sommet']},
 {name:'Véritable Grand Maître',code:'true_grandmaster',desc:'Élite absolue ; quelques individus seulement peuvent occuper ce niveau.',rarity:'légendaire',sub:['Entrée','Intermédiaire','Avancé','Sommet']},
 {name:'Grand Maître Éclairé',code:'enlightened',desc:'Niveau presque mythique fondé sur une compréhension martiale exceptionnelle.',rarity:'mythique',sub:['Entrée','Intermédiaire','Avancé','Sommet']},
 {name:'Maître Céleste',code:'heavenly',desc:'Sommet légendaire connu du système martial du monde.',rarity:'quasi unique',sub:['Accomplissement']},
 {name:'Être Divin',code:'divine',desc:'Catégorie exceptionnelle dépassant les limites normales du martial humain.',rarity:'événement historique',sub:['Accomplissement']}
];
const MASTERY=['Novice','Pratiqué','Mature','Accompli','Sommet'];
const DIM=['puissance','qi','technique','compréhension','expérience','corps','esprit'];
function ensure(p){if(!p)return; if(!p.martial)p.martial={level:0,sub:0,mastery:1,title:null,qiControl:0,experience:0,understanding:0}; return p.martial;}
function displayLevel(p){const m=ensure(p),l=LEVELS[m.level]||LEVELS[0];return m.level>=6&&l.sub.length?`${l.name} — ${l.sub[Math.min(m.sub,l.sub.length-1)]}`:l.name;}
function rankScore(p){const m=ensure(p);return (p.body||0)+(p.mind||0)+(p.endurance||0)+(p.qi||0)+(p.speed||0)+m.qiControl+m.understanding+Math.floor(m.experience/10)+m.mastery*2;}
function canBreakthrough(p){const m=ensure(p),score=rankScore(p);const thresholds=[0,4,10,18,28,40,55,75,100,130,165,205,250];return score>=thresholds[Math.min(m.level+1,thresholds.length-1)] && m.understanding>=((m.level>=6)?m.level*2:0);}
function breakthrough(p){const m=ensure(p);if(m.level>=LEVELS.length-1||!canBreakthrough(p))return false;m.level++;m.sub=0;m.mastery=1;m.qiControl+=1;m.understanding+=2;return true;}
function gainExperience(p,days,reason){const m=ensure(p);m.experience+=Math.max(0,Math.floor(days/3));if(reason==='combat')m.experience+=2;if(reason==='study')m.understanding+=1;if(reason==='meditation')m.qiControl+=1;const before=m.level;while(breakthrough(p)){}return m.level>before;}
function techniquePower(t){return {common:1,advanced:2,superior:3,master:4,grandmaster:5,celestial:6,divine:7}[t]||1;}
function riskForTechnique(p,reqPower,bodyFit=0,spiritFit=0){const m=ensure(p);let risk=.08; if((p.age||0)<8)risk+=.35; else if((p.age||0)<14)risk+=.18; risk+=Math.max(0,(reqPower-(p.body||0)-bodyFit)*.045);risk+=Math.max(0,(reqPower-(p.mind||0)-spiritFit)*.04);risk-=Math.min(.22,(m.understanding||0)*.012);return Math.max(.01,Math.min(.95,risk));}
function applyTechniqueRisk(p,reqPower,bodyFit,spiritFit){const risk=riskForTechnique(p,reqPower,bodyFit,spiritFit);if(Math.random()<risk){p.health=Math.max(0,(p.health||100)-Math.round(4+Math.random()*15));p.injuries=(p.injuries||0)+1;return {risk:true,amount:risk};}return {risk:false,amount:risk};}
function install(){if(window.__murimMartialSystem)return;window.__murimMartialSystem=true;const w=window;const oldNew=w.newLife,oldCreate=w.createNPC,oldSeed=w.seedNPCs,oldTrain=w.train,oldLearn=w.learnTechnique,oldRender=w.render,oldAdd=w.addDays;
 w.createNPC=function(...a){const n=oldCreate.apply(this,a);n.martial= n.martial||{level:Math.random()<.7?0:Math.min(6,Math.floor(Math.random()*5)+1),sub:0,mastery:1,title:null,qiControl:0,experience:Math.floor(Math.random()*60),understanding:Math.floor(Math.random()*8)};return n};
 w.seedNPCs=function(...a){const r=oldSeed.apply(this,a);(w.S?.npcs||[]).forEach(n=>ensure(n));return r};
 w.newLife=function(...a){const r=oldNew.apply(this,a);if(w.S?.player){w.S.player.martial={level:0,sub:0,mastery:1,title:null,qiControl:0,experience:0,understanding:0};}return r};
 w.train=function(...a){const r=oldTrain?.apply(this,a);const p=w.S?.player;if(p){gainExperience(p,7,'combat');const m=ensure(p);if((p.age||0)<8&&m.level>1)m.level=1;w.S.chain?.push({action:'entraînement martial',days:7,date:w.now?w.now():String(w.S.days),result:displayLevel(p)});}return r};
 w.learnTechnique=function(...a){const r=oldLearn?.apply(this,a);const p=w.S?.player;if(p){ensure(p);gainExperience(p,3,'study');}return r};
 w.addDays=function(n){const r=oldAdd.apply(this,arguments);const p=w.S?.player;if(p?.alive){gainExperience(p,n,'study');if(p.age<8&&p.martial.level>1)p.martial.level=1;}return r};
 w.martialBreakthrough=()=>{const p=w.S?.player;if(!p)return false;const ok=breakthrough(p);if(ok){w.setStory(`<div class="event"><b>Progression martiale</b><br>Tu franchis un seuil : <b>${displayLevel(p)}</b>.<br>Ce changement résulte de ton expérience, de ton corps, de ton esprit et de ta compréhension.</div>`);w.render?.()}return ok};
 w.useMartialTechnique=(req=1,bodyFit=0,spiritFit=0)=>{const p=w.S?.player;if(!p||!p.alive)return false;const r=applyTechniqueRisk(p,req,bodyFit,spiritFit);gainExperience(p,1,'combat');if(r.risk){w.rumor?.(`${p.name} souffre d'une mauvaise utilisation d'un art martial. Le risque était de ${Math.round(r.amount*100)}%.`,'observation');if(p.health<=0)w.die?.('séquelles martiales');}w.render?.();return r;};
 w.getMartialProfile=p=>{const m=ensure(p);return {level:displayLevel(p),mastery:MASTERY[Math.min(m.mastery-1,MASTERY.length-1)],rarity:(LEVELS[m.level]||LEVELS[0]).rarity,description:(LEVELS[m.level]||LEVELS[0]).desc,score:rankScore(p),qiControl:m.qiControl,understanding:m.understanding,experience:m.experience};};
 w.render=function(...a){oldRender.apply(this,a);const p=w.S?.player;if(!p)return;const box=w.document.getElementById('traits');if(box){box.insertAdjacentHTML('beforeend',`<div class="martial-profile" style="margin-top:8px;padding:9px;border:1px solid #2a3543;border-radius:8px;background:#0b1118"><b>Voie martiale</b><div class="small"><b>${displayLevel(p)}</b> · Maîtrise : ${MASTERY[Math.min(ensure(p).mastery-1,MASTERY.length-1)]}</div><div class="small">Rareté : ${(LEVELS[ensure(p).level]||LEVELS[0]).rarity} · Compréhension ${ensure(p).understanding} · Expérience ${ensure(p).experience}</div></div>`);}};
}
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',install);else install();
})();

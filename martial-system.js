(()=>{
'use strict';

// Progression martiale : le rang du personnage, sa maîtrise réelle et la difficulté des arts sont séparés.
const LEVELS=[
 {name:'Civil',code:'civil',desc:'Aucune formation martiale sérieuse.',rarity:'commun',sub:[]},
 {name:'Débutant',code:'beginner',desc:'Apprend les postures, la respiration, les armes et les bases du combat.',rarity:'très commun',sub:[]},
 {name:'Troisième Rang',code:'third',desc:'Premier véritable niveau martial ; possède des bases solides.',rarity:'commun',sub:[]},
 {name:'Deuxième Rang',code:'second',desc:'Combattant confirmé avec une fondation martiale solide.',rarity:'courant',sub:[]},
 {name:'Premier Rang',code:'first',desc:'Artiste martial reconnu parmi les combattants ordinaires.',rarity:'peu commun',sub:[]},
 {name:'Sommet',code:'peak',desc:'Sommet des arts martiaux ordinaires ; élite des écoles.',rarity:'rare',sub:[]},
 {name:'Expert',code:'expert',desc:'Compréhension véritable du Qi et de sa circulation.',rarity:'très rare',sub:['Entrée','Intermédiaire','Avancé','Sommet']},
 {name:'Maître',code:'master',desc:'Contrôle avancé du Qi, grande expérience et compréhension profonde.',rarity:'exceptionnel',sub:['Entrée','Intermédiaire','Avancé','Sommet']},
 {name:'Grand Maître',code:'grandmaster',desc:'Maîtrise profonde, perception supérieure et influence sur le Murim.',rarity:'extrêmement rare',sub:['Entrée','Intermédiaire','Avancé','Sommet']},
 {name:'Véritable Grand Maître',code:'true_grandmaster',desc:'Élite absolue ; quelques individus seulement.',rarity:'légendaire',sub:['Entrée','Intermédiaire','Avancé','Sommet']},
 {name:'Grand Maître Éclairé',code:'enlightened',desc:'Niveau presque mythique fondé sur une compréhension martiale exceptionnelle.',rarity:'mythique',sub:['Entrée','Intermédiaire','Avancé','Sommet']},
 {name:'Maître Céleste',code:'heavenly',desc:'Sommet légendaire du système martial.',rarity:'quasi unique',sub:['Accomplissement']},
 {name:'Être Divin',code:'divine',desc:'Catégorie exceptionnelle dépassant les limites humaines normales.',rarity:'événement historique',sub:['Accomplissement']}
];
const MASTERY=['Novice','Pratiqué','Mature','Accompli','Sommet'];
const TECH_GRADES=['Commune','Avancée','Supérieure','Maître','Grand Maître','Céleste','Divine'];
const TECH_REQ={
 'Commune':{rank:1,understanding:0,practice:1},
 'Avancée':{rank:2,understanding:4,practice:8},
 'Supérieure':{rank:4,understanding:12,practice:20},
 'Maître':{rank:7,understanding:25,practice:45},
 'Grand Maître':{rank:8,understanding:45,practice:80},
 'Céleste':{rank:11,understanding:75,practice:140},
 'Divine':{rank:12,understanding:120,practice:250}
};
const TECHS={
 'Pas des Feuilles':{grade:'Avancée',body:2,spirit:1,art:'déplacement'},
 'Poing de la Montagne Brisée':{grade:'Supérieure',body:5,spirit:1,art:'corps'},
 'Respiration du Cœur Vide':{grade:'Maître',body:2,spirit:7,art:'interne'},
 'Épée de l’Horizon Vide':{grade:'Grand Maître',body:4,spirit:8,art:'épée'},
 'Aiguilles des Cinq Méridiens':{grade:'Maître',body:2,spirit:7,art:'méridiens'},
 'Lance du Dragon des Neuf Portes':{grade:'Grand Maître',body:8,spirit:5,art:'lance'},
 'Sabre de la Lune Sanglante':{grade:'Supérieure',body:7,spirit:3,art:'sabre'},
 'Art des Armes Cachées':{grade:'Supérieure',body:4,spirit:4,art:'armes cachées'},
 'Voie du Fleuve Silencieux':{grade:'Céleste',body:7,spirit:10,art:'interne'},
 'Méthode des Cent Veines':{grade:'Divine',body:8,spirit:12,art:'méridiens'}
};

function ensure(p){
 if(!p)return;
 if(!p.martial)p.martial={level:0,sub:0,mastery:1,masteryPoints:0,title:null,qiControl:0,experience:0,understanding:0};
 if(!p.techniqueStudies)p.techniqueStudies={};
 return p.martial;
}
function profile(p){const m=ensure(p),l=LEVELS[m.level]||LEVELS[0];return {level:m.level,name:l.name,display:m.level>=6&&l.sub.length?`${l.name} — ${l.sub[Math.min(m.sub,l.sub.length-1)]}`:l.name,mastery:MASTERY[Math.max(0,Math.min(4,(m.mastery||1)-1))],rarity:l.rarity,understanding:m.understanding,experience:m.experience,qiControl:m.qiControl};}
function rankScore(p){const m=ensure(p);return (p.body||0)+(p.mind||0)+(p.endurance||0)+(p.qi||0)+(p.speed||0)+m.qiControl+m.understanding+Math.floor(m.experience/10)+m.mastery*2;}
const thresholds=[0,4,10,18,28,40,55,75,100,130,165,205,250];
function canBreakthrough(p){const m=ensure(p),next=m.level+1;if(next>=LEVELS.length)return false;return rankScore(p)>=thresholds[next]&&m.understanding>=(next>=6?next*2:0);}
function breakthrough(p){const m=ensure(p);if(!canBreakthrough(p))return false;m.level++;m.sub=0;m.mastery=1;m.qiControl++;m.understanding+=2;return true;}
function gainExperience(p,days,reason){const m=ensure(p);m.experience+=Math.max(0,Math.floor(days/3));if(reason==='combat')m.experience+=2;if(reason==='study')m.understanding+=1;if(reason==='meditation')m.qiControl+=1;while(breakthrough(p)){}return profile(p);}

// Une technique se conquiert par étapes : trouver/recevoir le manuel, le comprendre, pratiquer, puis maîtriser.
function stageForStudy(p,name){
 ensure(p);
 const t=TECHS[name]||{grade:'Commune',body:1,spirit:1,art:'général'};
 const req=TECH_REQ[t.grade];
 const s=p.techniqueStudies[name]||{stage:'aucun',study:0,practice:0,understanding:0,failures:0,manual:true,mastery:0};
 // Le manuel peut être trouvé sans pouvoir être utilisé.
 if(s.stage==='aucun')s.stage='manuel_trouve';
 if((s.understanding||0)>=req.understanding && (s.practice||0)>=Math.max(1,Math.floor(req.practice*.2)))s.stage='compris_partiellement';
 if((s.understanding||0)>=req.understanding && (s.practice||0)>=req.practice)s.stage='technique_comprise';
 p.techniqueStudies[name]=s;return s;
}
function canAttemptLearning(p,name){
 ensure(p);const t=TECHS[name]||{grade:'Commune',body:1,spirit:1};const req=TECH_REQ[t.grade];const s=stageForStudy(p,name);
 // Un Débutant ne peut pas apprendre directement une technique Divine/Céleste/Maître. Il lui manque rang, compréhension et pratique.
 if(p.martial.level<req.rank)return {ok:false,reason:`Ton niveau martial (${profile(p).display}) est insuffisant pour travailler réellement cette technique ${t.grade}.`};
 if(p.body< t.body)return {ok:false,reason:`Ton corps n'offre pas encore les prérequis physiques de cette technique.`};
 if(p.mind< t.spirit)return {ok:false,reason:`Ton esprit/concentration n'offre pas encore les prérequis de cette technique.`};
 if(s.understanding<req.understanding)return {ok:false,reason:`Tu possèdes le manuel, mais tu ne le comprends pas encore (${s.understanding}/${req.understanding}).`};
 if(s.practice<req.practice)return {ok:false,reason:`Tu as compris les principes, mais tu n'as pas encore assez pratiqué (${s.practice}/${req.practice}).`};
 return {ok:true,s,req,t};
}
function studyManual(p,name,days=7){
 ensure(p);const t=TECHS[name]||{grade:'Commune'};const req=TECH_REQ[t.grade];const s=stageForStudy(p,name);s.study+=(days>0?days:1);s.understanding+=Math.max(1,Math.floor(days/7));gainExperience(p,days,'study');
 return {stage:s.stage,understanding:s.understanding,required:req.understanding};
}
function practiceTechnique(p,name,days=7){
 ensure(p);const t=TECHS[name]||{grade:'Commune',body:1,spirit:1};const s=stageForStudy(p,name);const req=TECH_REQ[t.grade];
 // La pratique peut fonctionner… ou mal tourner. Plus la technique est éloignée du niveau du pratiquant, plus le risque augmente.
 const rankGap=Math.max(0,req.rank-p.martial.level);const bodyGap=Math.max(0,t.body-p.body);const spiritGap=Math.max(0,t.spirit-p.mind);let risk=.02+rankGap*.08+bodyGap*.045+spiritGap*.045;
 if(p.age<8)risk+=.28;else if(p.age<14)risk+=.14;risk=Math.min(.95,risk);
 if(Math.random()<risk){s.failures++;p.health=Math.max(0,(p.health||100)-RISK_DAMAGE(risk));p.injuries=(p.injuries||0)+1;s.practice+=Math.max(1,Math.floor(days/14));gainExperience(p,days,'combat');return {ok:false,risk,injury:true,stage:s.stage};}
 s.practice+=days;s.understanding+=Math.max(1,Math.floor(days/10));gainExperience(p,days,'combat');if(s.understanding>=req.understanding&&s.practice>=req.practice)s.stage='technique_comprise';return {ok:true,risk,stage:s.stage,practice:s.practice,understanding:s.understanding};
}
function RISK_DAMAGE(r){return Math.max(3,Math.round(4+r*14));}
function attemptTechnique(p,name){
 const c=canAttemptLearning(p,name);if(!c.ok)return c;
 const t=c.t;const compatibility=Math.max(0,1-((Math.max(0,t.body-p.body)+Math.max(0,t.spirit-p.mind))/20));let risk=.04+(1-compatibility)*.35;
 if(p.age<8)risk+=.3;else if(p.age<14)risk+=.16;risk=Math.min(.9,risk);
 if(Math.random()<risk){p.health=Math.max(0,(p.health||100)-RISK_DAMAGE(risk));p.injuries=(p.injuries||0)+1;c.s.failures++;return {ok:false,risk,reason:'La compréhension était suffisante pour essayer, mais ton corps/esprit n’a pas supporté l’exécution.'};}
 if(!Array.isArray(p.techniques))p.techniques=[];if(!p.techniques.includes(name))p.techniques.push(name);c.s.mastery=1;c.s.stage='technique_apprise';return {ok:true,risk,name,grade:t.grade};
}
function findManual(p,name){ensure(p);const s=p.techniqueStudies[name]||{stage:'aucun',study:0,practice:0,understanding:0,failures:0,manual:false,mastery:0};s.manual=true;s.stage='manuel_trouve';p.techniqueStudies[name]=s;return s;}
function techniqueInfo(p,name){ensure(p);const t=TECHS[name]||{grade:'Commune',body:1,spirit:1,art:'général'};const s=p.techniqueStudies[name]||{stage:'aucun',study:0,practice:0,understanding:0,failures:0,manual:false,mastery:0};return {name,grade:t.grade,stage:s.stage,study:s.study,practice:s.practice,understanding:s.understanding,required:TECH_REQ[t.grade],manual:s.manual,mastery:s.mastery,failures:s.failures};}

function install(){
 if(window.__murimMartialSystem)return;window.__murimMartialSystem=true;const w=window;
 const oldNew=w.newLife,oldCreate=w.createNPC,oldSeed=w.seedNPCs,oldTrain=w.train,oldRender=w.render,oldAdd=w.addDays;
 w.createNPC=function(...a){const n=oldCreate.apply(this,a);n.martial=n.martial||{level:Math.random()<.72?0:Math.min(6,Math.floor(Math.random()*5)+1),sub:0,mastery:1,masteryPoints:0,title:null,qiControl:0,experience:Math.floor(Math.random()*60),understanding:Math.floor(Math.random()*8)};n.techniqueStudies={};return n};
 w.seedNPCs=function(...a){const r=oldSeed.apply(this,a);(w.S?.npcs||[]).forEach(n=>ensure(n));return r};
 w.newLife=function(...a){const r=oldNew.apply(this,a);if(w.S?.player){w.S.player.martial={level:0,sub:0,mastery:1,masteryPoints:0,title:null,qiControl:0,experience:0,understanding:0};w.S.player.techniqueStudies={};}return r};
 w.train=function(days=7,...rest){const r=oldTrain?.apply(this,[days,...rest]);const p=w.S?.player;if(p){gainExperience(p,Math.max(1,Number(days)||7),'combat');if(p.age<8&&p.martial.level>1)p.martial.level=1;}return r};
 w.addDays=function(n){const r=oldAdd.apply(this,arguments);const p=w.S?.player;if(p?.alive){gainExperience(p,n,'study');if(p.age<8&&p.martial.level>1)p.martial.level=1;}return r};
 w.martialBreakthrough=()=>{const p=w.S?.player;if(!p)return false;const before=p.martial.level,ok=breakthrough(p);if(ok){w.setStory(`<div class="event"><b>Progression martiale</b><br>Tu franchis un seuil : <b>${profile(p).display}</b>.<br>Cette progression vient de ton expérience, ta compréhension, ton corps et ton esprit.</div>`);w.render?.()}return p.martial.level>before};
 w.findMartialManual=(name)=>{const p=w.S?.player;if(!p)return null;const r=findManual(p,name);w.setStory?.(`<div class="event"><b>Manuel découvert</b><br><b>${name}</b> · niveau ${TECHS[name]?.grade||'inconnu'}<br><br>Posséder un manuel ne signifie pas savoir utiliser la technique. Il faut d'abord comprendre ses principes, puis la pratiquer.</div>`);w.render?.();return r};
 w.studyMartialManual=(name,days=7)=>{const p=w.S?.player;if(!p)return null;const r=studyManual(p,name,days);w.setStory?.(`<div class="event"><b>Étude du manuel</b><br>${name}<br>Compréhension : ${r.understanding}/${r.required}<br><br>Tu progresses dans ta compréhension, mais connaître les principes ne suffit toujours pas à maîtriser la technique.</div>`);w.render?.();return r};
 w.practiceMartialTechnique=(name,days=7)=>{const p=w.S?.player;if(!p)return null;const r=practiceTechnique(p,name,days);w.setStory?.(`<div class="event"><b>Pratique martiale</b><br>${name}<br>${r.ok?'La pratique progresse.':'La pratique échoue et ton corps en paie le prix.'}<br>Étape : ${r.stage}${r.risk!=null?`<br>Risque estimé : ${Math.round(r.risk*100)}%`:''}</div>`);if(p.health<=0)w.die?.('séquelles martiales');w.render?.();return r};
 w.useMartialTechnique=(name)=>{const p=w.S?.player;if(!p)return null;const r=attemptTechnique(p,name);w.setStory?.(`<div class="event"><b>Exécution de technique</b><br>${name}<br>${r.ok?'Tu parviens enfin à utiliser la technique.':'Tu ne parviens pas à la maîtriser correctement.'}${r.reason?`<br>${r.reason}`:''}${r.risk!=null?`<br>Risque : ${Math.round(r.risk*100)}%`:''}</div>`);if(p.health<=0)w.die?.('séquelles martiales');w.render?.();return r};
 w.getMartialProfile=p=>profile(p);w.getTechniqueInfo=(name)=>techniqueInfo(w.S?.player,name);
 w.render=function(...a){oldRender.apply(this,a);const p=w.S?.player;if(!p)return;const box=w.document.getElementById('traits');if(!box)return;box.querySelector('.martial-profile')?.remove();const pr=profile(p);const known=Array.isArray(p.techniques)?p.techniques.length:0;const opts=Object.keys(TECHS).map(n=>`<option value="${n.replace(/"/g,'&quot;')}">${n} — ${TECHS[n].grade}</option>`).join('');box.insertAdjacentHTML('beforeend',`<div class="martial-profile" style="margin-top:8px;padding:9px;border:1px solid #2a3543;border-radius:8px;background:#0b1118"><b>Voie martiale</b><div class="small"><b>${pr.display}</b> · Maîtrise : ${pr.mastery} · ${pr.rarity}</div><div class="small">Compréhension ${pr.understanding} · Expérience ${pr.experience} · Qi ${pr.qiControl} · Techniques apprises ${known}</div><div style="margin-top:8px"><select id="martialTechniqueSelect">${opts}</select><button style="margin-top:5px;width:100%" onclick="findMartialManual(document.getElementById('martialTechniqueSelect').value)">Trouver / recevoir le manuel</button><button style="margin-top:5px;width:100%" onclick="studyMartialManual(document.getElementById('martialTechniqueSelect').value,7)">Étudier 7 jours</button><button style="margin-top:5px;width:100%" onclick="practiceMartialTechnique(document.getElementById('martialTechniqueSelect').value,7)">Pratiquer 7 jours</button><button style="margin-top:5px;width:100%" onclick="useMartialTechnique(document.getElementById('martialTechniqueSelect').value)">Tenter d'exécuter</button></div></div>`);};
}
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',install);else install();
})();

/* Murim V5 — liaison des systèmes du monde vivant.
 * Connecte corps/Qi/traits, relations, économie, déplacements, rumeurs,
 * connaissances, événements et chroniques sans remplacer les systèmes existants.
 */
(()=>{'use strict';
const W=window.MurimWorld||null; if(!W)return;
const G=window.S||{}; G.world=G.world||{};
const now=()=>Date.now();
function person(p){return p||G.player||G.character||{};}
function score(p){p=person(p);return {age:+p.age||0,traits:p.traits||[],body:p.body||p.constitution||null,qi:p.qi||null,location:p.location||G.location||null};}
function event(type,p,data={}){G.world.events=G.world.events||[];const e={type,actor:p?.id||p?.name||'unknown',at:now(),data};G.world.events.push(e);if(G.world.events.length>500)G.world.events.shift();return e;}
function travel(p,to,days,conditions={}){if((+p.age||0)<3)return {ok:false,reason:'un enfant trop jeune ne peut pas voyager seul'};const t=Math.max(1,Math.round(days));G.world.travel={actor:p.id||p.name,to,days:t,conditions};event('travel',p,{to,days:t});return {ok:true,days:t};}
function teachSafely(teacher,student,subject,quality=1){W.teach(teacher,student,subject,quality);event('teaching',student,{teacher:teacher.id||teacher.name,subject,quality});}
function rumorTo(source,fact,location){const r=W.rumor(source,fact,location);event('rumor',source,{fact,location});return r;}
function crime(actor,type,location,witnesses=[]){const c=W.addCrime(actor,type,location,witnesses);event('crime',actor,{type,location});return c;}
function tick(days=1){W.advance(days);G.world.date=(G.world.date||0)+days;return G.world.date;}
window.MurimIntegration={person,score,event,travel,teachSafely,rumorTo,crime,tick};
})();

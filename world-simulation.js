/* Murim — simulation du monde V4
 * Nouvelles mécaniques : connaissances, croyances, transmission de rumeurs,
 * réputation locale, identités, mémoire imparfaite, propriété, agriculture,
 * population/migration, justice, dette, éducation, artisanat, opportunités,
 * transport et traces historiques. Ces données sont neutres : joueur et PNJ
 * utilisent la même structure.
 */
(()=>{'use strict';
const clamp=(n,a,b)=>Math.max(a,Math.min(b,n));
const state=window.S=window.S||{};
state.world=state.world||{};
const W=state.world;
W.knowledge=W.knowledge||{}; W.rumors=W.rumors||[]; W.reputations=W.reputations||{};
W.identities=W.identities||{}; W.properties=W.properties||[]; W.debts=W.debts||[];
W.settlements=W.settlements||[]; W.crimes=W.crimes||[]; W.opportunities=W.opportunities||[];
W.archives=W.archives||[]; W.education=W.education||{}; W.transit=W.transit||{};
function idOf(p){return p?.id||p?.portraitId||p?.name||('p_'+Math.random().toString(36).slice(2));}
function profile(p){const id=idOf(p);return W.knowledge[id]||(W.knowledge[id]={known:[],beliefs:[],memories:[]});}
function know(p,fact,truth=true){const k=profile(p);if(!k.known.some(x=>x.fact===fact))k.known.push({fact,truth,certainty:truth?1:.35,at:Date.now()});return k;}
function rumor(source,fact,location){W.rumors.push({source:idOf(source),fact,location,age:0,distortion:Math.random()*.25});return W.rumors.at(-1);}
function reputation(p,group,delta){const id=idOf(p);W.reputations[id]=W.reputations[id]||{};W.reputations[id][group]=clamp((W.reputations[id][group]||0)+delta,-100,100);return W.reputations[id][group];}
function remember(p,event,weight=1){const k=profile(p);k.memories.push({event,weight,age:0});if(k.memories.length>100)k.memories.shift();}
function addDebt(from,to,amount,reason){const d={from:idOf(from),to:idOf(to),amount:Math.max(0,amount),reason,status:'active'};W.debts.push(d);return d;}
function addProperty(owner,type,name){const x={owner:idOf(owner),type,name,condition:100};W.properties.push(x);return x;}
function addCrime(actor,type,location,witnesses=[]){const c={actor:idOf(actor),type,location,witnesses:witnesses.map(idOf),known:false,solved:false};W.crimes.push(c);return c;}
function opportunity(type,location,expiresInDays,data={}){const o={type,location,createdAt:Date.now(),expiresInDays,data,claimedBy:null};W.opportunities.push(o);return o;}
function archive(event,location,year){W.archives.push({event,location,year,version:1});}
function teach(teacher,student,subject,quality=1){const sid=idOf(student);W.education[sid]=W.education[sid]||[];W.education[sid].push({teacher:idOf(teacher),subject,quality,progress:0});}
function migrate(p,from,to,reason){return {person:idOf(p),from,to,reason,date:Date.now()};}
function advance(days=1){W.rumors.forEach(r=>r.age+=days);Object.values(W.knowledge).forEach(k=>{k.memories.forEach(m=>m.age+=days);k.known.forEach(x=>x.certainty=clamp(x.certainty-(x.truth?.00001:0.00003)*days,.05,1))});W.opportunities.forEach(o=>{o.expiresInDays-=days});W.opportunities=W.opportunities.filter(o=>o.expiresInDays>0||o.claimedBy);}
window.MurimWorld={know,rumor,reputation,remember,addDebt,addProperty,addCrime,opportunity,archive,teach,migrate,advance};
})();

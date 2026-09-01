/* Murim — systèmes de vie avancés V5
 * Ville, généalogie, conflits, archives, bibliothèque, enquête, météo,
 * construction, société, chroniques et opportunités. Aucun statut de héros :
 * ces systèmes fonctionnent pour le joueur et tous les PNJ.
 */
(()=>{'use strict';
const S=window.S=window.S||{}; const W=S.world=S.world||{};
W.cities=W.cities||{}; W.lineages=W.lineages||{}; W.conflicts=W.conflicts||[]; W.library=W.library||{};
W.investigations=W.investigations||[]; W.weather=W.weather||{}; W.buildings=W.buildings||[];
W.social=W.social||{}; W.chronicles=W.chronicles||[]; W.opportunities=W.opportunities||[];
const id=p=>p?.id||p?.name||p?.portraitId||String(Math.random()).slice(2);
const arr=(o,k)=>o[k]||(o[k]=[]);
function city(name,region,population=0){return W.cities[name]||(W.cities[name]={name,region,population,wealth:50,food:50,crime:0,buildings:[],routes:[],history:[]});}
function cityTick(c,days=1){c.food=Math.max(0,c.food-days*.02);c.wealth=Math.max(0,c.wealth-days*.01);if(c.food<15)c.migration=(c.migration||0)+days*.1;c.crime=Math.max(0,Math.min(100,c.crime+(c.food<20?.02:0)-.01*days));}
function lineage(parent,child,relation='child'){const p=id(parent),c=id(child);arr(W.lineages,p).push({child:c,relation});arr(W.lineages,c).push({parent:p,relation});return {parent:p,child:c,relation};}
function conflict(name,sideA,sideB,location){const x={name,sideA,sideB,location,scoreA:0,scoreB:0,supplyA:100,supplyB:100,casualties:0,status:'active'};W.conflicts.push(x);return x;}
function battle(c,a,b,terrain='plain'){const terrainMod={plain:1,forest:.9,mountain:.75,city:.85,river:.7}[terrain]||1;c.scoreA+=(a.power||1)*terrainMod;c.scoreB+=(b.power||1)*terrainMod;c.casualties+=Math.max(0,Math.floor(Math.random()*3));c.supplyA=Math.max(0,c.supplyA-(a.cost||1));c.supplyB=Math.max(0,c.supplyB-(b.cost||1));if(c.supplyA===0||c.supplyB===0)c.status='decisive';return c;}
function book(title,type,owner,truth='unknown'){const b={id:'book_'+Date.now()+Math.random(),title,type,owner:id(owner),truth,readers:[],copies:1,condition:100};W.library[b.id]=b;return b;}
function readBook(b,p){if(!b||!p)return false;if(!b.readers.includes(id(p)))b.readers.push(id(p));return true;}
function investigation(caseName,location,clues=[]){const x={caseName,location,clues:[...clues],suspects:[],witnesses:[],solved:false};W.investigations.push(x);return x;}
function clue(inv,text,reliability=.5){inv.clues.push({text,reliability});return inv;}
function weather(region,season,conditions){W.weather[region]={season,conditions};return W.weather[region];}
function build(owner,cityName,type,cost=0){const c=city(cityName);const b={owner:id(owner),city:cityName,type,cost,condition:100,built:true};W.buildings.push(b);c.buildings.push(b);return b;}
function relation(a,b,type,score=0){const k=id(a)+'|'+id(b);W.social[k]=W.social[k]||{a:id(a),b:id(b),types:{},score:0};W.social[k].types[type]=true;W.social[k].score=Math.max(-100,Math.min(100,W.social[k].score+score));return W.social[k];}
function marriage(a,b){relation(a,b,'married',30);return {a:id(a),b:id(b),date:Date.now()};}
function adoption(parent,child){relation(parent,child,'adoptive-family',20);lineage(parent,child,'adopted-child');return true;}
function scandal(subject,text){return arr(W.social,'scandals').push({subject:id(subject),text,date:Date.now()});}
function chronicle(event,year,location,participants=[]){const c={event,year,location,participants:participants.map(id),importance:1};W.chronicles.push(c);if(window.MurimWorld?.archive)window.MurimWorld.archive(event,location,year);return c;}
function opportunity(type,location,expires,data={}){const o={type,location,expires,claimedBy:null,data};W.opportunities.push(o);return o;}
function tick(days=1){Object.values(W.cities).forEach(c=>cityTick(c,days));W.opportunities.forEach(o=>o.expires-=days);W.opportunities=W.opportunities.filter(o=>o.expires>0||o.claimedBy);}
window.MurimLife={city,lineage,conflict,battle,book,readBook,investigation,clue,weather,build,relation,marriage,adoption,scandal,chronicle,opportunity,tick};
})();

/* Murim — systèmes émergents V6
 * Nouvelles mécaniques: mémoire imparfaite, possibilités perdues, héritage comportemental,
 * mémoire des lieux, réputation par groupe, connaissances perdues, héritage post-mortem,
 * niveau d'information, identités multiples, fausses légendes, accidents de recherche,
 * chaînes causales et simulation hors caméra.
 */
(()=>{'use strict';const root=window.S||{};root.emergent=root.emergent||{};const E=root.emergent;
const id=x=>x?.id||x?.name||('p-'+Math.random().toString(36).slice(2));
const push=(arr,v,max=1000)=>{arr.push(v);if(arr.length>max)arr.splice(0,arr.length-max)};
E.memory=E.memory||{};E.locations=E.locations||{};E.reputations=E.reputations||{};E.knowledge=E.knowledge||{};E.causes=E.causes||{};E.lost=E.lost||[];E.legends=E.legends||[];E.identities=E.identities||{};
E.remember=function(p,key,value,certainty=.9){const pid=id(p);E.memory[pid]??={};E.memory[pid][key]={value,certainty,updated:Date.now()};return E.memory[pid][key]};
E.recall=function(p,key){const m=E.memory[id(p)]?.[key];if(!m)return null;const decay=Math.min(.8,(Date.now()-m.updated)/31536000000*.02);return {value:m.value,certainty:Math.max(0,m.certainty-decay)}};
E.place=function(location,record){const k=typeof location==='string'?location:id(location);E.locations[k]??=[];push(E.locations[k],{...record,at:Date.now()});return E.locations[k]};
E.reputation=function(p,group,delta,reason=''){const pid=id(p);E.reputations[pid]??={};E.reputations[pid][group]??={score:0,history:[]};const r=E.reputations[pid][group];r.score=Math.max(-100,Math.min(100,r.score+delta));push(r.history,{delta,reason,at:Date.now()},100);return r};
E.information=function(topic,truth,source,confidence=.5){E.knowledge[topic]??=[];const x={truth,source:id(source),confidence,at:Date.now()};push(E.knowledge[topic],x);return x};
E.lose=function(kind,data){const x={kind,...data,at:Date.now()};push(E.lost,x,5000);return x};
E.legend=function(text,truth=null,source=null){const x={text,truth,source:source? id(source):null,at:Date.now()};push(E.legends,x,5000);return x};
E.identity=function(p,publicName,secret={}){const pid=id(p);E.identities[pid]??=[];const x={publicName,secret,at:Date.now()};push(E.identities[pid],x,20);return x};
E.cause=function(event,cause=null){const n={id:id(event),cause:cause? id(cause):null,at:Date.now()};E.causes[n.id]=n;return n};
E.chain=function(event,children=[]){const n={id:id(event),children:children.map(id),at:Date.now()};E.causes[n.id]=n;return n};
E.recordMissed=function(event,reason){return E.lose('missed-event',{event:id(event),reason})};
E.tick=function(days=1){E.days=(E.days||0)+Math.max(0,days);return E.days};
})();

/* Murim — Simulation Core V6
 * Orchestrates time, causality, LOD and cross-system consequences.
 * Existing systems remain authoritative; this module coordinates them.
 */
(()=>{'use strict';
const root=window; const S=root.S=root.S||{};
const C=S.simulation=S.simulation||{};
C.version='6.0'; C.day=C.day||0; C.events=C.events||[]; C.queue=C.queue||[]; C.stats=C.stats||{ticks:0,near:0,regional:0,distant:0};
const n=v=>Number.isFinite(+v)?+v:0;
function lod(distance,importance){const d=n(distance),i=n(importance);if(i>=80||d<1)return 'near';if(d<5||i>=50)return 'regional';return 'distant'}
function enqueue(type,payload,delay=0,importance=0,distance=0){C.queue.push({type,payload,execute:C.day+Math.max(0,n(delay)),importance,distance});return C.queue[C.queue.length-1]}
function emit(type,payload={},importance=0,distance=0){const e={id:'E'+(C.events.length+1),type,day:C.day,payload,importance,distance,lod:lod(distance,importance)};C.events.push(e);if(C.events.length>1000)C.events.shift();return e}
function tick(days=1){days=Math.max(1,Math.floor(n(days)));for(let i=0;i<days;i++){C.day++;C.stats.ticks++;const due=C.queue.filter(e=>e.execute<=C.day);C.queue=C.queue.filter(e=>e.execute>C.day);for(const e of due){const mode=lod(e.distance,e.importance);C.stats[mode]++;emit(e.type,e.payload,e.importance,e.distance);if(typeof root.MurimIntegration?.event==='function')root.MurimIntegration.event(e.type,e.payload,{day:C.day,lod:mode})}}return C.day}
function causality(source,action,consequences=[]){const e=emit('causal-action',{source,action},80,0);for(const c of consequences)enqueue(c.type,c.payload,c.delay||0,c.importance||30,c.distance||0);return e}
function snapshot(){return JSON.parse(JSON.stringify({day:C.day,events:C.events.slice(-50),queue:C.queue,stats:C.stats}))}
root.MurimSimulation={lod,enqueue,emit,tick,causality,snapshot,core:C};
})();

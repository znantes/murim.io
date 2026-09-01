/* Murim — 5,000 manuels déterministes. Les illustrations restent externes dans assets/manuals/. */
(()=>{'use strict';
const domains=['arts martiaux','médecine','cuisine','pilules','monstres','sectes','anciennes sectes','familles','métiers','herboristerie','poisons','forgeronnerie','géographie','histoire','cultivation','formations','armes','animaux','économie','étiquette'];
const forms=['Notes','Traité','Registre','Carnet','Atlas','Classique','Recueil','Chronique','Guide','Manuel','Codex','Archives','Fragments','Livre secret','Mémoires'];
const qualities=['médiocre','commun','correct','soigné','rare','ancien','précieux','exceptionnel','légendaire','mythique'];
const scripts=['standard','ancien','archaïque','chiffré','incomplet','scellé'];
const out=[]; for(let i=1;i<=5000;i++){const tier=Math.min(11,Math.floor((i-1)/417)); out.push({id:`MAN-${String(i).padStart(4,'0')}`,domain:domains[(i-1)%domains.length],title:`${forms[(i-1)%forms.length]} ${domains[(i-1)%domains.length]} n°${String(i).padStart(4,'0')}`,rank:tier+1,quality:qualities[(i-1)%qualities.length],script:scripts[(i-1)%scripts.length],difficulty:5+tier*8+(i%19),illustration:`assets/manuals/MAN-${String(i).padStart(4,'0')}/cover.webp`,pages:3+(i%18),discoverable:true,actions:['lire','étudier','comparer']});}
if(typeof window!=='undefined'){window.MurimManuals5000=out;window.MurimContent=window.MurimContent||{};window.MurimContent.manuals=out;} if(typeof globalThis!=='undefined')globalThis.MurimManuals5000=out;
})();

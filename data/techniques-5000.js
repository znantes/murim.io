/* Murim — 5,000 techniques distinctes. Génération déterministe, sans images chargées au démarrage. */
(()=>{'use strict';
const schools=['Poing','Paume','Doigt','Épée','Sabre','Lance','Bâton','Griffe','Jambe','Aiguille','Dague','Chaîne','Marteau','Arc','Corps','Qi','Souffle','Pas','Formation','Art'];
const forms=['Basique','Simple','Brisée','Silencieuse','Rapide','Lourde','Tournante','Sept Coups','Treize Gestes','Trente-Trois Ombres','Cent Fentes','Sans Nom','du Vieux Pont','du Forgeron','du Voyageur','des Trois Rochers','des Neuf Rivières','du Dragon Endormi','de la Lune Fendue','du Ciel Inversé','des Mille Lames','du Roi Déchu','de l’Horizon Noir','du Soleil Blanc','des Cieux Éternels'];
const endings=['Méthode','Style','Voie','Art','Manière','Technique','Secret','Forme','Manuel de combat','Principe'];
const ranks=['Débutant','Basique','Commun','Adepte','Avancé','Expert','Maître','Grand Maître','Transcendant','Céleste','Saint','Divin'];
const qi=['neutre','vent','eau','feu','terre','foudre','glace','bois','métal','ombre','lumière','poison'];
const out=[]; let id=1;
for(let a=0;a<schools.length && out.length<5000;a++)for(let b=0;b<forms.length && out.length<5000;b++)for(let c=0;c<endings.length && out.length<5000;c++){
 const i=out.length, tier=Math.min(11,Math.floor(i/417)), quality=i%10;
 out.push({id:`TECH-${String(id++).padStart(4,'0')}`,name:`${schools[a]} ${forms[b]} — ${endings[c]}`,rank:ranks[tier],level:tier+1,quality:['médiocre','banale','correcte','bonne','soignée','rare','remarquable','exceptionnelle','légendaire','mythique'][quality],category:schools[a].toLowerCase(),qi:qi[i%qi.length],difficulty:5+tier*8+(i%17),comprehension:10+tier*8+(i%23),complexity:1+tier+(i%6),secret:i%17===0});
}}
rootData=out;
if(typeof window!=='undefined'){window.MurimTechniques5000=out;window.MurimContent=window.MurimContent||{};window.MurimContent.techniques=out;}
if(typeof globalThis!=='undefined')globalThis.MurimTechniques5000=out;
})();

/* Registre des familles de manuels. Les illustrations sont chargées à la demande. */
(()=>{'use strict';
const categories=[
 ['monsters','Bestiaire et monstres'],['martial-arts','Arts martiaux'],['medicine','Médecine'],['alchemy','Alchimie'],['pills','Pilules'],['poisons','Poisons et antidotes'],['sects','Sectes actuelles'],['ancient-sects','Anciennes sectes'],['families','Familles et lignées'],['clans','Clans et organisations'],['qi','Qi et cultivation'],['meridians','Méridiens et circulation'],['weapons','Armes et forge'],['strategy','Stratégie et guerre'],['geography','Géographie et routes'],['cuisine','Cuisine et recettes'],['crafts','Artisanat'],['history','Histoire et chroniques']
].map(([id,name])=>({id,name,portrait:`manual-category-${id}`}));
const types=['traité','journal','codex','guide','atlas','recueil','registre','fragment','rouleau','carnet','encyclopédie','manuel secret'];
function createManual(id,name,category,difficulty=20){return {id,name,category,difficulty,type:types[id%types.length],readableAt:difficulty,portrait:`manual-${id}`,damaged:false,pages:20+(id%181),secrets:id%13===0,knowledge:{language:10+(id%70),theory:difficulty,qi:10+(id%90),body:10+(id%85),meridians:10+(id%90),spirit:10+(id%95)}}}
if(typeof window!=='undefined'){window.MurimManualCategories=categories;window.MurimManuals=window.MurimManuals||[];window.MurimContent=window.MurimContent||{};window.MurimContent.manualCategories=categories;window.MurimContent.createManual=createManual;}
if(typeof globalThis!=='undefined')globalThis.MurimManualCategories=categories;
})();

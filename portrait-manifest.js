/* Catalogue de production des portraits IA.
 * 10 000 identités visuelles sont réservées par le moteur.
 * Les assets finaux sont rangés sous assets/portraits/pnj/<id>/age_<âge>.webp.
 * Aucun SVG n'est utilisé comme portrait final.
 */
(function(){
  'use strict';
  const catalog={};
  const pad=n=>String(n).padStart(6,'0');
  const ages=[0,1,3,6,10,13,16,20,25,30,40,50,60,70,80,90];
  for(let i=1;i<=10000;i++){
    const id='p'+pad(i), entry={id,ages:{}};
    ages.forEach(age=>{entry.ages[age]=`assets/portraits/pnj/${id}/age_${age}.webp`;});
    catalog[id]=entry;
  }
  window.MURIM_PORTRAIT_CATALOG=catalog;
  window.MURIM_PORTRAIT_TARGET=10000;
})();

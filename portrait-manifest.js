/* Catalogue de production des portraits IA.
 * 10 000 identités visuelles sont réservées par le moteur.
 * Les assets peuvent être déposés sous portraits/pnj/<id>/age_<âge>.webp.
 * On ne fabrique pas de faux portraits SVG : une absence d'asset affiche un état neutre.
 */
(function(){
  'use strict';
  const catalog={};
  const pad=n=>String(n).padStart(6,'0');
  // 10 000 emplacements d'identité persistante, extensibles sans casser les sauvegardes.
  for(let i=1;i<=10000;i++){
    const id='p'+pad(i);
    catalog[id]={
      id,
      ages:{
        0:`portraits/pnj/${id}/age_0.webp`,
        1:`portraits/pnj/${id}/age_1.webp`,
        3:`portraits/pnj/${id}/age_3.webp`,
        6:`portraits/pnj/${id}/age_6.webp`,
        10:`portraits/pnj/${id}/age_10.webp`,
        13:`portraits/pnj/${id}/age_13.webp`,
        16:`portraits/pnj/${id}/age_16.webp`,
        20:`portraits/pnj/${id}/age_20.webp`,
        25:`portraits/pnj/${id}/age_25.webp`,
        30:`portraits/pnj/${id}/age_30.webp`,
        40:`portraits/pnj/${id}/age_40.webp`,
        50:`portraits/pnj/${id}/age_50.webp`,
        60:`portraits/pnj/${id}/age_60.webp`,
        70:`portraits/pnj/${id}/age_70.webp`,
        80:`portraits/pnj/${id}/age_80.webp`,
        90:`portraits/pnj/${id}/age_90.webp`
      }
    };
  }
  window.MURIM_PORTRAIT_CATALOG=catalog;
  window.MURIM_PORTRAIT_TARGET=10000;
})();

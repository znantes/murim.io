/* Murim — correctifs de gameplay exécutés DANS le document du jeu. */
(function(){
  'use strict';
  if(window.__murimGameFixes)return;
  window.__murimGameFixes=true;

  const REGION_HOME={'Plaines Centrales':'Maison familiale','Nord impérial':'Fort du Givre','Monts Cendrés':'Temple de la Cloche Vide','Forêt de Jade':'Forêt Verte','Vallée Blanche':'Académie de la Lune','Frontière Ouest':'Poste impérial','Rive des Mille Lanternes':'Quai marchand','Archipel des Brumes':'Port des Brumes'};
  const places={'Plaines Centrales':[['Village de Qingshui',20],['Capitale des Neuf Portes',35],['Marché des Trois Rivières',42],['Maison familiale',0]],'Nord impérial':[['Col des Neuf Nuages',18],['Fort du Givre',40]],'Monts Cendrés':[['Temple de la Cloche Vide',22],['Ruines du Pic Noir',55]],'Forêt de Jade':[['Forêt Verte',35],['Sanctuaire des Cent Pins',48]],'Vallée Blanche':[['Académie de la Lune',24],['Clinique de jade',30]],'Frontière Ouest':[['Poste impérial',28],['Camp des caravanes',60]],'Rive des Mille Lanternes':[['Quai marchand',25],['Cité des Lanternes',45]],'Archipel des Brumes':[['Port des Brumes',30],['Île du Silence',70]]};
  const anchors={'Plaines Centrales':0,'Rive des Mille Lanternes':120,'Forêt de Jade':180,'Vallée Blanche':250,'Monts Cendrés':310,'Nord impérial':390,'Frontière Ouest':520,'Archipel des Brumes':700};
  function regionOf(place){for(const [r,list] of Object.entries(places))if(list.some(x=>x[0]===place))return r;return null}
  function dist(a,b){return Math.abs((anchors[a]||0)-(anchors[b]||0))}
  function baby(p){return p&&p.age<2}
  function speed(p){return Math.max(3,4+(p.speed||1)*.85+(p.endurance||1)*.25)}
  function story(html){if(typeof window.setStory==='function')window.setStory(html);}
  function safeRender(){if(typeof window.render==='function')window.render();}

  function babyUI(){
    const p=window.S&&window.S.player;if(!p)return;
    const map=document.getElementById('map');
    if(map){
      const r=p.region||regionOf(p.location)||'Plaines Centrales';
      const local=places[r]||[];
      map.innerHTML=`<div style="grid-column:1/-1;padding:10px;border:1px solid #2a3542;border-radius:8px;background:#0b1118"><b>Région actuelle : ${r}</b><div style="font-size:12px;color:#9ca8b5;margin-top:4px">${baby(p)?'Tu es encore un bébé. Tu ne voyages pas seul. Les déplacements extérieurs sont verrouillés jusqu’à l’enfance.':'Les destinations affichées appartiennent à ta région actuelle. Les autres régions passent par un vrai trajet interrégional.'}</div></div>`;
      local.forEach(([name,km])=>{const b=document.createElement('button');b.textContent=`${name} · ${km} km`;b.style.cssText='width:100%;margin-top:6px;padding:9px;background:#1a2532;color:#eee;border:1px solid #3a4756;border-radius:8px;cursor:pointer';b.disabled=baby(p)&&name!==p.location;b.onclick=()=>window.travelTo(name);map.appendChild(b)});
      const regions=document.createElement('div');regions.style.cssText='grid-column:1/-1;margin-top:7px;padding:9px;border-top:1px solid #2a3542';regions.innerHTML='<b>Autres régions</b><div style="font-size:12px;color:#9ca8b5;margin:4px 0">Distance indicative depuis ta région. Ces voyages sont impossibles pour un bébé.</div>';
      Object.keys(places).filter(x=>x!==r).forEach(x=>{const b=document.createElement('button');b.textContent=`${x} · ~${dist(r,x)} km`;b.disabled=baby(p);b.style.cssText='width:auto;margin:4px 4px 0 0;padding:7px;background:#1a2532;color:#eee;border:1px solid #3a4756;border-radius:8px;cursor:pointer';b.onclick=()=>window.travelTo(REGION_HOME[x]);regions.appendChild(b)});
      map.appendChild(regions);
    }
    const custom=document.getElementById('custom');
    if(custom)custom.placeholder=baby(p)?'Ex. Je reste auprès de ma famille. Je regarde les visages. Je dors. Je consulte le médecin.':'Ex. Je retourne à ma famille à pied. Je cherche un médecin. Je veux travailler chez un forgeron.';
  }

  function install(){
    if(!window.S||!window.S.player){setTimeout(install,100);return;}
    if(window.__murimGameFixesInstalled)return;
    window.__murimGameFixesInstalled=true;

    const originalRender=window.render;
    if(typeof originalRender==='function'){
      window.render=function(){originalRender();babyUI();};
    }

    const originalAddDays=window.addDays;
    if(typeof originalAddDays==='function'){
      window.addDays=function(n){
        n=Math.max(0,Math.floor(Number(n)||0));
        for(let i=0;i<n;i++){
          if(window.S.player){window.S.player.ageDays=(window.S.player.ageDays||0)+1;window.S.player.age=Math.floor(window.S.player.ageDays/365)}
          originalAddDays(1);
        }
        safeRender();
      };
    }

    const originalTravel=window.travelTo;
    if(typeof originalTravel==='function'){
      window.travelTo=function(place){
        const p=window.S.player;if(!p||!p.alive)return;
        if(baby(p)){story('<div class="event"><b>Trop jeune pour voyager seul</b><br>Tu es encore un bébé. Tu restes sous la surveillance de ta famille ou de la personne qui te garde.</div>');safeRender();return;}
        const fromRegion=p.region||regionOf(p.location)||'Plaines Centrales',toRegion=regionOf(place)||fromRegion;
        const localKm=(places[fromRegion]?.find(x=>x[0]===p.location)?.[1]||0)+(places[toRegion]?.find(x=>x[0]===place)?.[1]||0);
        const km=Math.max(1,dist(fromRegion,toRegion)+localKm);
        const days=Math.max(1,Math.ceil(km/speed(p)));
        p.region=toRegion;p.location=place;p.travel=(p.travel||0)+days;
        if(typeof window.addDays==='function')window.addDays(days);
        if(typeof window.rumor==='function')window.rumor(`${p.name} arrive à ${place} après ${days} jour(s) de voyage.`,'voyage');
        story(`<div class="event"><b>Voyage terminé</b><br>Tu arrives à <b>${place}</b> après <b>${days} jour(s)</b> et environ ${km} km de trajet.<br><br>Pendant ton absence, les PNJ ont eux aussi vécu ${days} jour(s).</div>`);
        safeRender();
      };
    }

    const originalCustom=window.doCustom;
    if(typeof originalCustom==='function'){
      window.doCustom=function(){
        const p=window.S.player;if(!p||!p.alive)return;
        const el=document.getElementById('custom'),raw=el?.value?.trim()||'',t=raw.toLowerCase();
        if(baby(p)){
          if(/retour|rentre|voyage|pars|partir|aller à|aller au|aller en|quitter|route|marcher|marche/.test(t)){story('<div class="event"><b>Action impossible à cet âge</b><br>Tu es encore un bébé. Tu ne peux pas entreprendre seul un voyage ou quitter ton lieu de vie.</div>');safeRender();return;}
          if(!/observe|regarde|regarder|dors|dormir|repos|médecin|docteur|famille|mère|père|frère|soeur|sœur|joue|jouer|écoute|écouter/.test(t)){story('<div class="event"><b>Action adaptée à l’enfance</b><br>À ton âge, cette action est trop ambitieuse. Le monde continue autour de toi, mais tu dépends encore des adultes.</div>');safeRender();return;}
        }
        originalCustom();
      };
    }

    const originalNewLife=window.newLife;
    if(typeof originalNewLife==='function'){
      window.newLife=function(){originalNewLife();if(window.S.player){window.S.player.ageDays=0;window.S.player.age=0;window.S.player.location=REGION_HOME[window.S.player.region]||'Maison familiale'}safeRender();};
    }
    babyUI();safeRender();
  }
  install();
})();

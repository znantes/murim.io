/* Murim — interface RPG / gestion du Jianghu V4.1
 * Couche visuelle indépendante : ne remplace pas le moteur de simulation.
 */
(function(){'use strict';
if(window.__murimRPGUI)return; window.__murimRPGUI=true;
const D=document;
function esc(v){return String(v??'—').replace(/[&<>"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));}
function make(tag,cls,html){const e=D.createElement(tag);if(cls)e.className=cls;if(html!==undefined)e.innerHTML=html;return e;}
function getState(){return window.S||{};}
function inject(){
 const wrap=D.querySelector('.wrap'), layout=D.querySelector('.layout'), top=D.querySelector('.top');
 if(!wrap||!layout||!top)return false;
 if(!D.getElementById('murimRpgHud')){
  const hud=make('section','murim-rpg-hud');hud.id='murimRpgHud';
  hud.innerHTML='<div class="rpg-hud-main"><div class="rpg-campaign"><span class="rpg-kicker">CHRONIQUE</span><strong>Les Mille Destins</strong><span id="rpgDate">—</span></div><div class="rpg-hud-stats"><div><small>ÂGE</small><b id="rpgAge">—</b></div><div><small>LIEN</small><b id="rpgLoc">—</b></div><div><small>QI</small><b id="rpgQi">—</b></div><div><small>RÉPUTATION</small><b id="rpgRep">—</b></div></div></div><div class="rpg-world-state"><span class="rpg-live-dot"></span><span id="rpgWorld">Jianghu en attente</span><button type="button" id="rpgFocusBtn">Mode immersion</button></div>';
  top.insertAdjacentElement('afterend',hud);
 }
 if(!D.getElementById('murimRpgNav')){
  const nav=make('nav','murim-rpg-nav');nav.id='murimRpgNav';
  nav.innerHTML='<button data-target="chronique" class="active">⚔ Chronique</button><button data-target="carte">🗺 Carte</button><button data-target="personnages">👥 Jianghu</button><button data-target="economie">◈ Économie</button><button data-target="organisations">☯ Factions</button><span class="rpg-nav-spacer"></span><button data-action="compact">▣ Interface</button>';
  layout.parentNode.insertBefore(nav,layout);
  nav.addEventListener('click',e=>{const b=e.target.closest('button');if(!b)return;if(b.dataset.action==='compact'){D.body.classList.toggle('murim-compact');return;}const target=b.dataset.target;nav.querySelectorAll('button[data-target]').forEach(x=>x.classList.toggle('active',x===b));focusSection(target);});
 }
 decoratePanels();
 return true;
}
function focusSection(target){
 const main=D.querySelector('main.panel'); if(!main)return;
 D.body.classList.remove('murim-tab-chronique','murim-tab-carte','murim-tab-personnages','murim-tab-economie','murim-tab-organisations');
 D.body.classList.add('murim-tab-'+target);
 const map=D.getElementById('map'), people=D.getElementById('people'), economy=D.getElementById('economy'), orgs=D.getElementById('orgs'), story=D.getElementById('story');
 const el={chronique:story,carte:map,personnages:people,economie:economy,organisations:orgs}[target]; if(el)el.scrollIntoView({behavior:'smooth',block:'nearest'});
}
function decoratePanels(){
 const left=D.querySelector('.layout>aside:first-child'),main=D.querySelector('.layout>main'),right=D.querySelector('.layout>aside:last-child');
 if(left&&!left.querySelector('.rpg-panel-label'))left.insertAdjacentHTML('afterbegin','<div class="rpg-panel-label">FICHE DU DESTIN</div>');
 if(main&&!main.querySelector('.rpg-panel-label'))main.insertAdjacentHTML('afterbegin','<div class="rpg-panel-label">FIL DE DESTINÉE</div>');
 if(right&&!right.querySelector('.rpg-panel-label'))right.insertAdjacentHTML('afterbegin','<div class="rpg-panel-label">ÉTAT DU JIANGHU</div>');
 const p=D.getElementById('portraitBox');if(p)p.classList.add('rpg-portrait-stage');
 const story=D.getElementById('story');if(story)story.classList.add('rpg-story-window');
}
function update(){
 const s=getState(),p=s.player||{};
 const set=(id,v)=>{const e=D.getElementById(id);if(e)e.textContent=v??'—';};
 set('rpgDate',typeof window.now==='function'?window.now():'—');
 set('rpgAge',p.age===undefined?'—':p.age+' an'+(p.age>1?'s':''));
 set('rpgLoc',p.location||p.region||'—');
 set('rpgQi',p.qi===undefined?'—':p.qi);
 set('rpgRep',p.reputation===undefined?'—':p.reputation);
 set('rpgWorld',`${(s.npcs||[]).filter(n=>n.alive).length} PNJ vivants · ${s.days||0} jour(s) écoulé(s)`);
 const brand=D.querySelector('.brand');if(brand)brand.textContent='武林 · MURIM';
}
function observe(){update();setTimeout(update,80);setTimeout(update,500);}
function boot(){if(!inject()){setTimeout(boot,120);return;}update();
 const btn=D.getElementById('rpgFocusBtn');if(btn)btn.onclick=()=>D.body.classList.toggle('murim-focus');
 ['click','change'].forEach(ev=>D.addEventListener(ev,observe,true));
 setInterval(update,1000);
}
if(D.readyState==='loading')D.addEventListener('DOMContentLoaded',boot,{once:true});else boot();
})();

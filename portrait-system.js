/* Murim — système de portraits IA
 * Architecture: identité persistante + vieillissement + état + faction + chargement lazy.
 * Les fichiers IA réels sont référencés par portrait-manifest.js lorsqu'ils sont disponibles.
 * Aucun SVG n'est utilisé comme portrait.
 */
(function(){
  'use strict';
  const W=window, D=document;
  if(W.__murimPortraitSystem)return;
  W.__murimPortraitSystem=true;

  const AGE_BUCKETS=[0,1,3,6,10,13,16,20,25,30,40,50,60,70,80,90];
  const sexLabel=s=>s==='F'?'Femme':s==='M'?'Homme':'Personne';
  const ageBucket=age=>AGE_BUCKETS.reduce((best,x)=>Math.abs(x-age)<Math.abs(best-age)?x:best,AGE_BUCKETS[0]);
  const esc=v=>String(v??'').replace(/[&<>\"']/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','\"':'&quot;',"'":'&#39;'}[m]));

  function portraitMeta(n){
    const age=Math.max(0,Number(n.age)||0);
    const id=n.id||n.portraitId||n.name||'unknown';
    const faction=n.faction||'Indépendant';
    const job=n.job||'inconnu';
    const key=String(id).replace(/[^a-zA-Z0-9_-]/g,'_');
    const bucket=ageBucket(age);
    let url='';
    if(W.MURIM_PORTRAIT_CATALOG && W.MURIM_PORTRAIT_CATALOG[key]){
      const entry=W.MURIM_PORTRAIT_CATALOG[key];
      url=(entry.ages&&entry.ages[bucket])||entry.src||'';
    }
    if(!url && W.MURIM_PORTRAIT_BASE_URL){
      url=`${String(W.MURIM_PORTRAIT_BASE_URL).replace(/\/$/,'')}/${encodeURIComponent(key)}/age_${bucket}.webp`;
    }
    return {id:key,age,bucket,faction,job,gender:n.gender||'',url};
  }

  function imageHTML(n,size){
    const m=portraitMeta(n), cls=size==='small'?'murimPortraitSmall':'murimPortrait';
    if(!m.url){
      return `<div class="${cls} murimPortraitPending" data-portrait-id="${esc(m.id)}" aria-label="Portrait IA en attente"><div class="murimPortraitPendingIcon">✦</div><span>Portrait IA</span></div>`;
    }
    return `<img class="${cls}" data-portrait-id="${esc(m.id)}" data-portrait-url="${esc(m.url)}" src="${esc(m.url)}" alt="Portrait de ${esc(n.name||'PNJ')}" loading="lazy" decoding="async">`;
  }

  function ensureStyles(){
    if(D.getElementById('murimPortraitStyles'))return;
    const s=D.createElement('style');s.id='murimPortraitStyles';
    s.textContent=`
      .murimPortrait,.murimPortraitSmall{display:block;object-fit:cover;background:#0b1118;border:1px solid #2a3543}
      .murimPortrait{width:104px;height:104px;border-radius:12px}.murimPortraitSmall{width:72px;height:72px;border-radius:50%}
      .murimPortraitPending{display:flex;flex-direction:column;align-items:center;justify-content:center;color:#9ca8b5;font:11px Georgia,serif;gap:5px;overflow:hidden}
      .murimPortraitPendingIcon{font-size:24px;color:#d8af69;line-height:1}
      .murimPortraitError{font-size:11px;text-align:center;padding:8px;color:#9ca8b5}
      .portraitMeta{font-size:11px;color:#9ca8b5;line-height:1.4;margin-top:6px}
      .portraitBadge{display:inline-block;border:1px solid #2a3543;border-radius:999px;padding:2px 6px;margin:2px 2px 0 0}
    `;D.head.appendChild(s);
  }

  function renderMainPortrait(){
    const S=W.S;if(!S||!S.player)return;
    const box=D.getElementById('portraitBox');if(!box)return;
    const p=S.player,m=portraitMeta(p);
    box.innerHTML=`<h2>Portrait</h2><div style="display:flex;gap:10px;align-items:flex-start">${imageHTML(p)}<div class="portraitMeta"><b style="color:#f0d49a">${esc(p.name||'Sans nom')}</b><br>${esc(sexLabel(p.gender))} · ${m.age} ans<br><span class="portraitBadge">${esc(m.faction)}</span><span class="portraitBadge">${esc(m.job)}</span><br><span class="muted">Identité visuelle : ${esc(m.id)}</span></div></div>`;
    bindImageFallbacks(box);
  }

  function renderKnownPeople(){
    const S=W.S;if(!S)return;
    const box=D.getElementById('people');if(!box)return;
    const people=(S.npcs||[]).filter(n=>n&&n.alive).slice(0,60);
    if(!people.length){box.innerHTML='<div class="small">Aucun PNJ connu pour le moment.</div>';return;}
    box.innerHTML=people.map(n=>`<div class="person" title="${esc(n.name)} — ${esc(n.faction||'Indépendant')}">${imageHTML(n,'small')}<div>${esc(n.name||'Inconnu')}</div><div>${Math.max(0,Number(n.age)||0)} ans</div></div>`).join('');
    bindImageFallbacks(box);
  }

  function bindImageFallbacks(root){
    root.querySelectorAll('img[data-portrait-url]').forEach(img=>{
      if(img.dataset.bound)return;img.dataset.bound='1';
      img.addEventListener('error',()=>{
        const d=D.createElement('div');d.className=img.className+' murimPortraitPending';d.innerHTML='<div class="murimPortraitPendingIcon">✦</div><span>Portrait IA indisponible</span>';img.replaceWith(d);
      },{once:true});
    });
  }

  function injectCatalog(){
    // Le catalogue complet peut être remplacé par un fichier de production sans modifier le moteur.
    if(typeof W.MURIM_PORTRAIT_CATALOG==='undefined')W.MURIM_PORTRAIT_CATALOG={};
  }

  function patchRender(){
    if(typeof W.render!=='function')return false;
    if(W.render.__portraitPatched)return true;
    const original=W.render;
    function wrapped(){original();ensureStyles();renderMainPortrait();renderKnownPeople();}
    wrapped.__portraitPatched=true;W.render=wrapped;return true;
  }

  function init(){
    ensureStyles();injectCatalog();
    if(!patchRender())setTimeout(init,150);
    else if(typeof W.render==='function')W.render();
  }

  W.MurimPortraits={
    ageBucket,
    meta:portraitMeta,
    refresh:function(){ensureStyles();renderMainPortrait();renderKnownPeople();},
    setBaseURL:function(url){W.MURIM_PORTRAIT_BASE_URL=url||'';this.refresh();},
    catalog:function(c){W.MURIM_PORTRAIT_CATALOG=c||{};this.refresh();},
    targetCount:10000,
    ageBuckets:AGE_BUCKETS.slice()
  };
  init();
})();

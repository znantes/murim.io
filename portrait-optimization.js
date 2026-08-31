/* Murim — optimisation du chargement des portraits IA
 * Ne télécharge que les images nécessaires, évite les doublons et privilégie
 * une vignette pour les listes. Les assets sont servis en WebP par le manifeste.
 */
(function(){
  'use strict';
  const W=window;
  if(W.MurimPortraitOptimizer)return;
  const cache=new Map();
  const inflight=new Map();
  const MAX_CACHE=80;
  function trim(){while(cache.size>MAX_CACHE){const first=cache.keys().next().value;cache.delete(first);}}
  function load(url){
    if(!url)return Promise.reject(new Error('portrait-url-missing'));
    if(cache.has(url))return Promise.resolve(cache.get(url));
    if(inflight.has(url))return inflight.get(url);
    const p=new Promise((resolve,reject)=>{
      const img=new Image();
      img.decoding='async';
      img.loading='lazy';
      img.onload=()=>{cache.set(url,img);trim();inflight.delete(url);resolve(img);};
      img.onerror=()=>{inflight.delete(url);reject(new Error('portrait-load-failed'));};
      img.src=url;
    });
    inflight.set(url,p);return p;
  }
  function preloadVisible(root){
    const scope=root||document;
    const els=scope.querySelectorAll('[data-portrait-url]');
    els.forEach(el=>{
      const url=el.dataset.portraitUrl;
      if(!url)return;
      if('IntersectionObserver' in W){
        const io=new IntersectionObserver(entries=>{entries.forEach(e=>{if(e.isIntersecting){load(url).catch(()=>{});io.disconnect();}});},{rootMargin:'200px'});
        io.observe(el);
      }else load(url).catch(()=>{});
    });
  }
  W.MurimPortraitOptimizer={load,preloadVisible,clear:function(){cache.clear();}};
  W.addEventListener('load',()=>preloadVisible(document),{once:true});
})();

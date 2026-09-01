/* Murim portrait runtime: keeps one persistent portrait family per PNJ. */
(function(){'use strict';
  const d=Object.getOwnPropertyDescriptor(HTMLImageElement.prototype,'src');
  if(!d||!d.set)return;
  const set=d.set;
  Object.defineProperty(HTMLImageElement.prototype,'src',{...d,set:function(v){
    if(typeof v==='string' && /assets\/portraits\/pnj\/p\d{6}\/age_\d+\.webp$/.test(v)) v=v.replace(/age_\d+\.webp$/,'age_20.webp');
    set.call(this,v);
  }});
})();

/* Optional WASM accelerator. The game keeps a JS fallback when WASM is unavailable. */
(function(){'use strict';
  const M=window.MurimEngine=window.MurimEngine||{ready:false,backend:'javascript'};
  M.init=async function(url){
    try{
      if(!WebAssembly.instantiateStreaming) throw new Error('streaming-unavailable');
      const r=await fetch(url,{cache:'no-store'});
      const mod=await WebAssembly.instantiateStreaming(r,{});
      M.instance=mod.instance; M.exports=mod.instance.exports; M.ready=true; M.backend='wasm';
      return M;
    }catch(e){ console.info('[Murim] WASM indisponible, fallback JS actif.',e); return M; }
  };
})();

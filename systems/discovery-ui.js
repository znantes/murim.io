/* Interface pont : lecture, étude et comparaison sans révélation omnisciente. */
(()=>{'use strict';const W=window,D=W.MurimDiscovery;if(!D)return;function skill(){const p=W.S?.player||{};return {theory:Number(p.mind||0),mind:Number(p.mind||0),qi:Number(p.qi||0),meridians:Number(p.body||0)}}
function find(id){return (W.MurimTechniques5000||[]).find(x=>x.id===id)||(W.MurimManuals||[]).find(x=>x.id===id);}
W.readDiscovered=(id)=>{const x=find(id);if(!x)return null;return D.read(x,skill().mind)};
W.studyDiscovered=(id)=>{const x=find(id);if(!x)return null;return D.study(x,skill())};
W.compareDiscovered=(a,b)=>{const x=find(a),y=find(b);if(!x||!y)return null;return D.compare(x,y,skill())};
W.discoverManual=(id,source)=>{const x=find(id);return x?D.discover(x,source||'rencontre'):null};
W.discoverTechnique=(id,source)=>{const x=find(id);return x?D.discover(x,source||'observation'):null};
W.getDiscoveryView=(id)=>{const x=find(id);return x?D.reveal(x,skill().mind):{known:false,text:'Inconnu'}};
W.MurimContent=W.MurimContent||{};W.MurimContent.discoveryUI={read:W.readDiscovered,study:W.studyDiscovered,compare:W.compareDiscovered};
})();

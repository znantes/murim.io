/* Murim — découverte progressive. Rien n'est connu avant observation/apprentissage. */
(()=>{'use strict';const W=window;const D=W.MurimDiscovery=W.MurimDiscovery||{};D.version='1.0';D.knowledge=D.knowledge||{};
function state(id){return D.knowledge[id]||(D.knowledge[id]={id,seen:false,read:0,understood:0,studied:0,compared:0,mastered:false,notes:[]})}
function difficulty(item){return Math.max(1,Number(item?.comprehension||item?.difficulty||50))}
function canRead(item,skill=0){return Number(skill)>=difficulty(item)*0.45}
function readability(item,skill=0){const r=Math.max(0,Math.min(1,Number(skill)/(difficulty(item)||1)));if(r<.25)return 'incompréhensible';if(r<.45)return 'fragments';if(r<.65)return 'partiellement lisible';if(r<.85)return 'lisible';return 'claire'}
function discover(item,source='observation'){if(!item||!item.id)return null;const k=state(item.id);k.seen=true;k.sources=k.sources||[];if(!k.sources.includes(source))k.sources.push(source);return k}
function read(item,skill=0){const k=discover(item,'lecture');const level=readability(item,skill);if(canRead(item,skill))k.read=Math.min(100,k.read+Math.max(1,Math.floor(skill/12)));k.notes.push({action:'read',level,at:Date.now()});return {known:k,readable:canRead(item,skill),level}}
function study(item,stats={}){const k=discover(item,'étude');const base=Number(stats.theory||stats.mind||0)+Number(stats.qi||0)+Number(stats.meridians||0);const score=base/3;const gain=Math.max(0,Math.floor((score-difficulty(item)*.35)/5));k.studied=Math.min(100,k.studied+gain);k.understood=Math.max(k.understood,Math.min(100,Math.floor(score/difficulty(item)*100)));return k}
function compare(a,b,stats={}){const ka=discover(a,'comparaison'),kb=discover(b,'comparaison');const score=Number(stats.theory||stats.mind||0)+Number(stats.qi||0)+Number(stats.meridians||0);const threshold=(difficulty(a)+difficulty(b))*.5;const clarity=score>=threshold?'principes visibles':score>=threshold*.6?'similarités partielles':'comparaison trop complexe';ka.compared++;kb.compared++;return {clarity,a:ka,b:kb}}
function reveal(item,skill=0){const k=state(item.id);if(!k.seen)return {known:false,text:'Tu ne connais pas encore cet objet.'};const pct=Math.max(0,Math.min(100,Math.floor(Number(skill)/(difficulty(item)||1)*100)));return {known:true,pct,level:readability(item,skill),text:pct<45?'Les signes restent incompréhensibles.':pct<65?'Tu distingues quelques principes.':pct<85?'Tu comprends une partie de la structure.':'Le contenu devient intelligible.'}}
D.state=state;D.discover=discover;D.read=read;D.study=study;D.compare=compare;D.reveal=reveal;D.readability=readability;
// Pont léger avec le jeu existant : aucune entrée n'est ajoutée aux connaissances sans découverte.
W.MurimContent=W.MurimContent||{};W.MurimContent.discovery=D;
})();

(()=>{'use strict';
const GOODS={
  riz:{nom:'Riz',base:10,volatilite:.16},
  fer:{nom:'Fer',base:24,volatilite:.22},
  herbes:{nom:'Herbes médicinales',base:18,volatilite:.28},
  soie:{nom:'Soie',base:40,volatilite:.2},
  bois:{nom:'Bois',base:7,volatilite:.18},
  sel:{nom:'Sel',base:9,volatilite:.15},
  poisson:{nom:'Poisson séché',base:12,volatilite:.25},
  cheval:{nom:'Cheval',base:160,volatilite:.3},
  papier:{nom:'Papier',base:6,volatilite:.17},
  encens:{nom:'Encens',base:14,volatilite:.21}
};
const MERCHANTS=['Marchand de grains','Herboriste ambulant','Armurier','Marchand de soie','Négociant en chevaux','Marchand de remèdes','Marchand des quais','Colporteur'];
function install(){if(window.__murimCommerce)return;window.__murimCommerce=true;
 const S=window.S;if(!S)return;
 S.market=S.market||{prices:{},stock:{},orders:[],knownMerchants:[]};
 Object.keys(GOODS).forEach(k=>{if(!S.market.prices[k])S.market.prices[k]=GOODS[k].base;if(!S.market.stock[k])S.market.stock[k]=Math.floor(Math.random()*80)+20});
 S.market.knownMerchants=S.market.knownMerchants.length?S.market.knownMerchants:MERCHANTS.map((job,i)=>({id:'merchant-'+i,name:job,trust:Math.floor(Math.random()*50)+25,wealth:Math.floor(Math.random()*500)+100}));
 const oldAdd=window.addDays;
 if(typeof oldAdd==='function')window.addDays=function(n){oldAdd.apply(this,arguments);updateMarket(Math.max(0,Number(n)||0));};
 window.marketBuy=function(key,qty=1){const p=window.S?.player;if(!p||!p.alive)return;qty=Math.max(1,Math.floor(qty));const price=(window.S.market.prices[key]||GOODS[key]?.base||10)*qty;if(p.money<price){window.setStory?.('<div class="event"><b>Commerce impossible</b><br>Tu n\'as pas assez d\'argent.</div>');return false}p.money-=price;window.S.market.stock[key]=Math.max(0,(window.S.market.stock[key]||0)-qty);p.inventory.push(`${GOODS[key].nom} x${qty}`);window.discover?.(`Achat : ${GOODS[key].nom} x${qty} pour ${price} pièces.`);return true};
 window.marketSell=function(key,qty=1){const p=window.S?.player;if(!p||!p.alive)return false;qty=Math.max(1,Math.floor(qty));const price=Math.round((window.S.market.prices[key]||GOODS[key]?.base||10)*.72)*qty;p.money+=price;window.S.market.stock[key]=(window.S.market.stock[key]||0)+qty;window.discover?.(`Vente : ${GOODS[key].nom} x${qty} pour ${price} pièces.`);return true};
 window.tradeEvent=function(){const k=Object.keys(GOODS).filter(x=>x!=='cheval');const key=k[Math.floor(Math.random()*k.length)];const delta=Math.max(1,Math.round(GOODS[key].base*(Math.random()*.6+.1)));const up=Math.random()<.5;window.S.market.prices[key]=Math.max(1,(window.S.market.prices[key]||GOODS[key].base)+(up?delta:-delta));window.rumor?.(`Le prix de ${GOODS[key].nom} ${up?'augmente':'baisse'} dans plusieurs marchés.`,`commerce`);};
 function updateMarket(n){if(!window.S?.market)return;for(let i=0;i<Math.max(1,Math.floor(n/3));i++){Object.keys(GOODS).forEach(k=>{const g=GOODS[k];let p=window.S.market.prices[k]||g.base;p*=1+(Math.random()-.5)*g.volatilite;window.S.market.prices[k]=Math.max(1,Math.round(p));});if(Math.random()<.25)tradeEvent();}}
}
if(document.readyState==='loading')document.addEventListener('DOMContentLoaded',install);else install();
})();

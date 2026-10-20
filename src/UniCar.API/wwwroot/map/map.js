(()=>{
'use strict';
const params=new URLSearchParams(location.search),nonce=params.get('nonce'),vehicleType=params.get('vehicle')||'CAR',placeOnly=params.get('mode')==='place';
let active=params.get('focus')==='destination'?'destination':'origin',generation=0,map,line,locationRequest;const points={origin:null,destination:null},markers={},searchVersions={origin:0,destination:0};
const state=document.getElementById('state'),calculate=document.getElementById('calculate');
function emit(action,request){if(!nonce)return;location.href='unicar-map://selection?data='+encodeURIComponent(JSON.stringify({nonce,action,requestId:generation,request}));}
function invalidate(){generation++;calculate.disabled=!points.origin||!points.destination;if(line){line.setMap(null);line=null;}emit('invalidate');}
function setPoint(kind,latitude,longitude,label,placeId=null){if(placeOnly){emit('place',{label,latitude,longitude,placeId});return;}searchVersions[kind]++;points[kind]={latitude,longitude,label,placeId};document.getElementById(kind+'-label').value=label;if(markers[kind])markers[kind].map=null;markers[kind]=new google.maps.marker.AdvancedMarkerElement({map,position:{lat:latitude,lng:longitude},title:kind==='origin'?'Saída':'Destino'});invalidate();state.textContent='Confirme os pontos de encontro e calcule o percurso.';}
window.renderRoute=(polyline,requestId)=>{if(requestId!==generation)return;if(line)line.setMap(null);const path=google.maps.geometry.encoding.decodePath(polyline);line=new google.maps.Polyline({map,path,strokeColor:'#052355',strokeWeight:5});const bounds=new google.maps.LatLngBounds();path.forEach(p=>bounds.extend(p));map.fitBounds(bounds);};
window.showRouteState=(message,requestId)=>{if(requestId===generation)state.textContent=message;};
window.applyCurrentLocation=(lat,lng,requestId)=>{if(map&&locationRequest&&locationRequest.generation===requestId&&generation===requestId){const kind=locationRequest.kind;locationRequest=null;setPoint(kind,lat,lng,kind==='origin'?'Minha saída':'Meu destino');map.panTo({lat,lng});}};
for(const kind of ['origin','destination']){
 document.getElementById('pick-'+kind).addEventListener('click',()=>{active=kind;for(const k of ['origin','destination'])document.getElementById('pick-'+k).classList.toggle('active',k===kind);});
 document.getElementById(kind+'-label').addEventListener('input',e=>{searchVersions[kind]++;if(points[kind]){points[kind].label=e.target.value;invalidate();}});
}
document.getElementById('location').addEventListener('click',()=>{locationRequest={kind:active,generation};emit('location');});
calculate.addEventListener('click',()=>{if(!points.origin||!points.destination)return;calculate.disabled=true;emit('selection',{origin:points.origin,destination:points.destination,vehicleType});setTimeout(()=>{calculate.disabled=!points.origin||!points.destination;},1000);});
window.gm_authFailure=()=>{state.textContent='Não foi possível autorizar o Google Maps. Confira a configuração do projeto.';calculate.disabled=true;};
async function boot(){
 try{
  const response=await fetch('/api/routes/map-config');if(!response.ok)throw Error('config');const config=await response.json();
  if(!config.configured){state.textContent='Mapas não configurados. A seleção do percurso será habilitada após configurar Google Maps, Places e Routes.';return;}
  await new Promise((resolve,reject)=>{window.unicarMapReady=resolve;const script=document.createElement('script');script.src='https://maps.googleapis.com/maps/api/js?key='+encodeURIComponent(config.browserApiKey)+'&libraries=places,geometry,marker&loading=async&callback=unicarMapReady&language=pt-BR';script.onerror=reject;document.head.append(script);});
  if(!placeOnly){map=new google.maps.Map(document.getElementById('map'),{center:{lat:-25.43,lng:-49.27},zoom:12,mapId:config.mapId,mapTypeControl:false,streetViewControl:false});
  map.addListener('click',e=>setPoint(active,e.latLng.lat(),e.latLng.lng(),active==='origin'?'Saída selecionada':'Destino selecionado'));}
  for(const kind of ['origin','destination']){
   if(placeOnly&&kind==='origin')continue;
   const input=new google.maps.places.PlaceAutocompleteElement();input.includedRegionCodes=['br'];input.setAttribute('placeholder',kind==='origin'?'De onde você vai sair?':'Para onde você vai?');input.addEventListener('gmp-error',()=>{state.textContent='Não foi possível buscar locais. Confira a conexão e tente novamente.';});input.setAttribute('aria-label',kind==='origin'?'Pesquisar saída':'Pesquisar destino');document.getElementById(kind+'-search').append(input);
   input.addEventListener('gmp-select',async event=>{const version=++searchVersions[kind];try{const place=event.placePrediction.toPlace();await place.fetchFields({fields:['location','displayName','formattedAddress','id']});if(version!==searchVersions[kind]||!place.location)return;const label=(place.displayName||place.formattedAddress||'Ponto selecionado').slice(0,150);setPoint(kind,place.location.lat(),place.location.lng(),label,place.id);map?.panTo(place.location);}catch{state.textContent='Não foi possível selecionar o endereço. Tente novamente.';}});
  }
  if(placeOnly){document.body.classList.add('place-only');document.querySelector('h1').textContent='Pesquisar destino';state.textContent='Digite um endereço, campus ou estabelecimento no Brasil.';}
  document.getElementById('pick-'+active).click();
  if(!placeOnly&&active==='destination')document.getElementById('destination-search').scrollIntoView?.({block:'start'});
  state.textContent=placeOnly?'Selecione uma sugestão de local no Brasil.':vehicleType==='MOTO'?'Escolha saída e destino. A rota de moto é uma estimativa; confira a sinalização do percurso.':'Pesquise endereços ou marque saída e destino no mapa.';
 }catch{state.textContent='Não foi possível carregar o mapa. Confira sua conexão e tente abrir novamente.';}
}
boot();
})();

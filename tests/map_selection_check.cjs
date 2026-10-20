// Executa map.js real em um DOM/provedor controlado; não chama Google.
const vm=require('node:vm'),fs=require('node:fs'),assert=require('node:assert/strict');
const placeMode=process.argv.includes('--place');
const elements=new Map();function el(){return {value:'',textContent:'',disabled:false,children:[],handlers:{},classList:{toggle(){}},setAttribute(){},click(){this.handlers.click?.()},append(x){this.children.push(x)},addEventListener(k,f){this.handlers[k]=f}};}
const document={body:{classList:{add(){}}},querySelector(){return el()},getElementById(id){if(!elements.has(id))elements.set(id,el());return elements.get(id)},createElement(){return el()},head:{append(){queueMicrotask(()=>context.unicarMapReady())}}};
let clicked;const context={document,location:{search:'?nonce=test&vehicle=CAR'+(placeMode?'&mode=place':''),href:''},URLSearchParams,encodeURIComponent,setTimeout,console,fetch:async()=>({ok:true,json:async()=>({configured:true,browserApiKey:'fixture',mapId:'fixture'})}),google:{maps:{Map:class{addListener(k,f){clicked=f}panTo(){}},marker:{AdvancedMarkerElement:class{}},places:{PlaceAutocompleteElement:class{constructor(){return el()}}},geometry:{encoding:{}},Polyline:class{},LatLngBounds:class{}}}};context.window=context;
vm.runInNewContext(fs.readFileSync(require('node:path').join(__dirname,'../src/UniCar.API/wwwroot/map/map.js'),'utf8'),context);
(async()=>{
 await new Promise(resolve=>setImmediate(resolve));
 if(placeMode){
  assert.equal(document.getElementById('origin-search').children.length,0);
  const search=document.getElementById('destination-search').children[0];
  assert.deepEqual(Array.from(search.includedRegionCodes),['br']);
  const place={location:{lat:()=>-25,lng:()=>-49},displayName:'Campus de teste',id:'fixture',fetchFields:async()=>{}};
  await search.handlers['gmp-select']({placePrediction:{toPlace:()=>place}});
  const selected=JSON.parse(decodeURIComponent(context.location.href.split('?data=')[1]));
  assert.equal(selected.action,'place');assert.equal(selected.nonce,'test');assert.equal(selected.request.label,'Campus de teste');
  assert.equal(selected.request.latitude,-25);assert.equal(selected.request.longitude,-49);
  console.log('PASS: Brazil-only single-place search returns the chosen place without a route');return;
 }
 const origin=document.getElementById('origin-search').children[0];let finish;
 assert.deepEqual(Array.from(origin.includedRegionCodes||[]),['br'],'Origin autocomplete must restrict results to Brazil');
 assert.deepEqual(Array.from(document.getElementById('destination-search').children[0].includedRegionCodes||[]),['br'],'Destination autocomplete must restrict results to Brazil');
 const pending=new Promise(resolve=>finish=resolve);
 const old={location:{lat:()=>1,lng:()=>2},displayName:'Resultado antigo',id:'old',fetchFields:()=>pending};
 const resolving=origin.handlers['gmp-select']({placePrediction:{toPlace:()=>old}});
 clicked({latLng:{lat:()=>-25,lng:()=>-49}});finish();await resolving;
 assert.equal(document.getElementById('origin-label').value,'Saída selecionada','Delayed Places result replaced a newer click');
 document.getElementById('location').handlers.click();const message=JSON.parse(decodeURIComponent(context.location.href.split('?data=')[1]));
 document.getElementById('pick-destination').handlers.click();
 context.applyCurrentLocation(-25.1,-49.1,message.requestId);
 assert.equal(document.getElementById('origin-label').value,'Minha saída','Location applied to the wrong endpoint');
 assert.equal(document.getElementById('destination-label').value,'');
 console.log('PASS: delayed Places and location preserve selected endpoints');
})();

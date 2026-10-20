"""Checks isolados: mysql local root sem senha por padrão; UNICAR_TEST_MYSQL substitui conexão.
Cria e remove somente unicar_check_<id>, inicia API de teste na porta 5056.
"""
import pathlib, subprocess, secrets, sys, os, json, urllib.request, urllib.error, time, datetime, concurrent.futures
ROOT=pathlib.Path(__file__).resolve().parents[1]
DB='unicar_check_'+secrets.token_hex(5)
parts=dict(p.split('=',1) for p in os.environ.get('UNICAR_TEST_MYSQL','Server=127.0.0.1;Port=3306;User=root;Password=;').split(';') if '=' in p)
def sql(statement,database=None):
 args=['mysql','-h',parts.get('Server','127.0.0.1'),'-P',parts.get('Port','3306'),'-u',parts.get('User','root'),'-N']+([database] if database else [])
 env=os.environ.copy();env['MYSQL_PWD']=parts.get('Password','')
 p=subprocess.run(args,input=statement,text=True,capture_output=True,env=env)
 if p.returncode:raise RuntimeError(p.stderr)
 return p.stdout.strip()
def migration():
 script=ROOT/'database/004_pricing_negotiation.sql';assert script.exists(),'FAIL: pricing migration missing'
 sql(f'CREATE DATABASE {DB};')
 try:
  sql('''CREATE TABLE users(id INT PRIMARY KEY); CREATE TABLE rides(id INT PRIMARY KEY,driver_id INT,available_seats INT);
CREATE TABLE ride_reservations(Id INT PRIMARY KEY,RideId INT,PassengerId INT,CreatedAt DATETIME);
INSERT INTO users VALUES(1),(2); INSERT INTO rides VALUES(1,1,1); INSERT INTO ride_reservations VALUES(1,1,2,NOW());''',DB)
  body=script.read_text().replace('USE unicar_db;',f'USE {DB};');sql(body);sql(body)
  assert sql("SELECT CONCAT(pricing_mode,':',original_seats,':',available_seats) FROM rides WHERE id=1",DB)=='LegacyUnknown:2:1'
  assert sql('SELECT COUNT(*) FROM ride_reservations WHERE AgreedAmount IS NULL AND Id=1',DB)=='1'
  print('PASS: migration preserves legacy rows and is repeatable')
 finally:sql(f'DROP DATABASE {DB}')
def api(path,body=None,token=None,method=None):
 headers={'Content-Type':'application/json'}
 if token:headers['Authorization']='Bearer '+token
 req=urllib.request.Request('http://127.0.0.1:5056/api/'+path,data=None if body is None else json.dumps(body).encode(),headers=headers,method=method)
 try:
  with urllib.request.urlopen(req,timeout=20) as resp:return resp.status,json.loads(resp.read() or b'{}')
 except urllib.error.HTTPError as resp:return resp.code,json.loads(resp.read() or b'{}')
def run_http(stage):
 sql(f'CREATE DATABASE {DB}')
 env=os.environ.copy();env['ConnectionStrings__DefaultConnection']=';'.join(f'{k}={v}' for k,v in parts.items())+f';Database={DB};'
 env['ASPNETCORE_ENVIRONMENT']='Development'
 process=None
 log=ROOT/'tests'/'api-check.log'
 try:
  subprocess.run(['dotnet','run','--project',str(ROOT/'tests/UniCar.API.Checks'),'--','initdb'],env=env,check=True,stdout=subprocess.DEVNULL)
  with log.open('w') as stream:
   process=subprocess.Popen(['dotnet',str(ROOT/'src/UniCar.API/bin/Debug/net10.0/UniCar.API.dll'),'--urls','http://127.0.0.1:5056'],cwd=ROOT/'src/UniCar.API',env=env,stdout=stream,stderr=subprocess.STDOUT)
  for attempt in range(100):
   if process.poll() is not None:raise RuntimeError('API de teste não iniciou; confira tests/api-check.log')
   try:
    if api('Rides')[0]==200:break
   except (OSError,urllib.error.URLError):pass
   time.sleep(.1)
  else:raise RuntimeError('Timeout inicialização API de teste')
  users=[];suffix=secrets.token_hex(5);password=secrets.token_urlsafe(20)
  for i in range(3):
   email=f'{suffix}.{i}@positivo.edu.br'
   code,user=api('Auth/register',{'name':f'Fixture {i}','email':email,'password':password,'phone':'41999999999','raEnrollment':suffix+str(i),'userType':'PASSENGER'})
   assert code==200;users.append(user)
  sql(f"UPDATE users SET user_type='DRIVER' WHERE id={users[0]['userId']}",DB)
  code,users[0]=api('Auth/login',{'email':f'{suffix}.0@positivo.edu.br','password':password});assert code==200
  if stage=='all':
   subprocess.run(['dotnet','run','--project',str(ROOT/'tests/UniCar.API.Checks'),'--','publish'],env=env,check=True)
  if stage in ('activities','all'):
   for i in range(21):
    ride=sql(f"INSERT INTO rides(driver_id,origin,destination,departure_time,available_seats,vehicle_type,pricing_mode,original_seats) VALUES({users[0]['userId']},'Origem','UP',DATE_ADD(NOW(),INTERVAL 1 DAY),1,'CAR','LegacyUnknown',2); SELECT LAST_INSERT_ID();",DB)
    sql(f"INSERT INTO ride_reservations(RideId,PassengerId,CreatedAt,AgreedAmount,Currency,PricingMode) VALUES({ride},{users[1]['userId']},NOW(),NULL,'BRL','LegacyUnknown')",DB)
   c,page1=api('activities/trips?page=1',token=users[1]['token']);assert c==200,'FAIL: authenticated activities endpoint missing'
   c,page2=api('activities/trips?page=2',token=users[1]['token']);assert c==200 and len(page1['items'])==20 and len(page2['items'])==1
   ids=[x['rideSummary']['id'] for x in page1['items']+page2['items']];assert len(set(ids))==21
   assert all(x['agreedAmount'] is None for x in page1['items'])
   c,private=api('activities/trips',token=users[2]['token']);assert c==200 and private['items']==[]
   c,driver=api('activities/trips',token=users[0]['token']);assert c==200 and all(x['role']=='Driver' and x['passengers'][0]['name']=='Fixture 1' for x in driver['items'])
   assert api('activities/trips')[0]==401
   print('PASS: private activities, legacy amounts, driver view, stable pagination, authentication')
  if stage in ('negotiations','all'):negotiations(users)
  if stage=='all':baseline(users,password)
  if stage=='live-maps':live_maps(users)
 finally:
  if process:
   process.terminate()
   try:process.wait(timeout=10)
   except subprocess.TimeoutExpired:process.kill();process.wait()
  sql(f'DROP DATABASE {DB}')
  log.unlink(missing_ok=True)
def live_maps(users):
 driver,pax,_=users
 from urllib.parse import urlencode
 session=secrets.token_hex(16)
 path='places?'+urlencode({'query':'Universidade Positivo Ecoville','session':session})
 assert api(path)[0]==401
 c,places=api(path,token=driver['token']);assert c==200 and places,(c,places)
 c,point=api('places/resolve?'+urlencode({'id':places[0]['id'],'session':session}),token=driver['token']);assert c==200 and point['label'] and -34<point['latitude']<6,(c,point)
 print('PASS: real authenticated inline autocomplete and selected Brazilian coordinates')
 request={'origin':{'latitude':-25.43,'longitude':-49.27,'label':'Ponto A de teste'},'destination':{'latitude':-25.447,'longitude':-49.356,'label':'Ponto B de teste'},'vehicleType':'CAR'}
 c,quote=api('routes/quotes',request,driver['token']);assert c==200,(c,quote)
 assert quote['distanceMeters']>0 and quote['durationSeconds']>0 and quote['encodedPolyline']
 from decimal import Decimal, ROUND_HALF_UP
 for vehicle,seats,rate in [('CAR',2,Decimal('0.70')),('MOTO',1,Decimal('0.35'))]:
  if vehicle=='CAR': priced_quote=quote
  else:
   c,priced_quote=api('routes/quotes',{**request,'vehicleType':vehicle},driver['token']);assert c==200,(c,priced_quote)
  c,priced=api(f'routes/quotes/{priced_quote["id"]}/suggestion',{'originalSeats':seats,'pricingMode':'Negotiable'},driver['token'])
  expected=(Decimal(priced_quote['distanceMeters'])/1000*rate/(seats+1)).quantize(Decimal('.01'),rounding=ROUND_HALF_UP)
  assert c==200 and Decimal(str(priced['amount']))==expected,(c,priced,expected)
  print(f'PASS: {vehicle} configured rate {rate}/km, {priced_quote["distanceMeters"]} m, {seats} seat(s), contribution BRL {expected}')
 c,suggestion=api(f'routes/quotes/{quote["id"]}/suggestion',{'originalSeats':2,'pricingMode':'Free'},driver['token']);assert c==200 and suggestion['amount']==0
 c,created=api('rides',{'origin':'Ponto A de teste','destination':'Ponto B de teste','vehicleType':'CAR','availableSeats':2,'departureTime':(datetime.datetime.now()+datetime.timedelta(days=1)).replace(microsecond=0).isoformat(),'routeQuoteId':quote['id'],'pricingMode':'Free'},driver['token']);assert c==200,(c,created)
 c,receipt=api(f'rides/{created["rideId"]}/join',{},pax['token']);assert c==200 and receipt['agreedAmount']==0
 c,history=api('activities/trips',token=pax['token']);assert c==200 and history['items'][0]['rideSummary']['id']==created['rideId']
 print('PASS: real Google route through API, valid quote, publication, reservation and history in isolated database')
def baseline(users,password):
 driver,pax,other=users
 ids=[]
 for vehicle in ['CAR','MOTO']:
  ids.append(int(sql(f"INSERT INTO rides(driver_id,origin,destination,departure_time,available_seats,vehicle_type,pricing_mode,reference_amount,original_seats) VALUES({driver['userId']},'Baseline','Baseline UP',DATE_ADD(CURDATE(),INTERVAL 1 DAY)+INTERVAL 12 HOUR,1,'{vehicle}','Free',0,1);SELECT LAST_INSERT_ID();",DB)))
 tomorrow=(datetime.date.today()+datetime.timedelta(days=1)).isoformat()
 c,found=api(f'Rides?vehicleType=MOTO&date={tomorrow}&destination=Baseline');assert c==200 and len(found)==1 and found[0]['id']==ids[1]
 c,receipt=api(f'rides/{ids[0]}/join',{},pax['token']);assert c==200
 c,replay=api(f'rides/{ids[0]}/join',{},pax['token']);assert c==200 and receipt['reservationId']==replay['reservationId']
 c,logged=api('Auth/login',{'email':pax['email'],'password':password});assert c==200
 c,saved=api('rides/reservations',token=logged['token']);assert c==200 and any(r['id']==ids[0] and r['agreedAmount']==0 for r in saved)
 c,private=api('rides/reservations',token=other['token']);assert c==200 and all(r['id']!=ids[0] for r in private)
 with concurrent.futures.ThreadPoolExecutor(2) as pool:
  result=list(pool.map(lambda u:api(f'rides/{ids[1]}/join',{},u['token'])[0],[pax,other]))
 assert sorted(result)==[200,409] and sql(f'SELECT available_seats FROM rides WHERE id={ids[1]}',DB)=='0'
 print('PASS: reservation regressions (mode/date filters, new login, privacy, idempotence and last-seat race)')
def negotiations(users):
 driver,passenger,other=users
 def ride(mode='Negotiable',seats=2):
  return int(sql(f"INSERT INTO rides(driver_id,origin,destination,departure_time,available_seats,vehicle_type,pricing_mode,reference_amount,original_seats) VALUES({driver['userId']},'Centro','UP',DATE_ADD(NOW(),INTERVAL 1 DAY),{seats},'CAR','{mode}',10,{seats}); SELECT LAST_INSERT_ID();",DB))
 rid=ride()
 assert api(f'Rides/{rid}/join',{},passenger['token'])[0]==400,'Paid join bypassed negotiation'
 c,n=api(f'rides/{rid}/negotiations',{'expectedVersion':0,'amount':8},passenger['token']);assert c==200,'FAIL: proposal endpoint missing'
 nid=n['id'];assert n['version']==1 and sql(f'SELECT available_seats FROM rides WHERE id={rid}',DB)=='2'
 assert api(f'negotiations/{nid}/accept',{'expectedVersion':1},passenger['token'])[0]==403
 assert api(f'negotiations/{nid}',token=other['token'])[0]==404
 c,n=api(f'negotiations/{nid}/counter',{'expectedVersion':1,'amount':9.5},driver['token']);assert c==200 and n['version']==2
 assert api(f'negotiations/{nid}/accept',{'expectedVersion':1},passenger['token'])[0]==409
 c,receipt=api(f'negotiations/{nid}/accept',{'expectedVersion':2},passenger['token']);assert c==200 and receipt['agreedAmount']==9.5
 c,replay=api(f'negotiations/{nid}/accept',{'expectedVersion':2},passenger['token']);assert c==200 and replay['reservationId']==receipt['reservationId']
 assert sql(f'SELECT available_seats FROM rides WHERE id={rid}',DB)=='1'
 sql(f'UPDATE rides SET reference_amount=99 WHERE id={rid}',DB)
 c,history=api('activities/trips',token=passenger['token']);assert c==200 and any(x['rideSummary']['id']==rid and x['agreedAmount']==9.5 for x in history['items'])
 assert api(f'negotiations/{nid}/counter',{'expectedVersion':3,'amount':2},driver['token'])[0]==409
 free=ride('Free',1)
 assert api(f'rides/{free}/negotiations',{'expectedVersion':0,'amount':1},passenger['token'])[0]==400
 c,zero=api(f'rides/{free}/join',{},passenger['token']);assert c==200 and zero['agreedAmount']==0
 last=ride(seats=1)
 ids=[]
 for u in [passenger,other]:
  c,n=api(f'rides/{last}/negotiations',{'expectedVersion':0,'amount':0},u['token']);assert c==200;ids.append(n['id'])
 with concurrent.futures.ThreadPoolExecutor(2) as pool:
  results=list(pool.map(lambda i:api(f'negotiations/{i}/accept',{'expectedVersion':1},driver['token'])[0],ids))
 assert sorted(results)==[200,409]
 assert sql(f'SELECT available_seats FROM rides WHERE id={last}',DB)=='0'
 assert sql(f'SELECT COUNT(*) FROM ride_reservations WHERE RideId={last}',DB)=='1'
 fresh=ride()
 c,n=api(f'rides/{fresh}/negotiations',{'expectedVersion':0,'amount':5},passenger['token']);assert c==200
 assert api(f'negotiations/{n["id"]}/decline',{'expectedVersion':1},driver['token'])[0]==200
 c,n=api(f'rides/{fresh}/negotiations',{'expectedVersion':2,'amount':6},passenger['token']);assert c==200 and n['version']==3
 assert api(f'negotiations/{n["id"]}/withdraw',{'expectedVersion':3},passenger['token'])[0]==200
 assert api(f'rides/{fresh}/negotiations',{'expectedVersion':3,'amount':7},passenger['token'])[0]==409
 assert api('negotiations/1')[0]==401
 print('PASS: proposals/counteroffers, ownership, version conflicts, immutable price, free rides, withdrawal/reopening, idempotence, simultaneous final seat')
if __name__=='__main__':
 stage=sys.argv[1] if len(sys.argv)>1 else 'all'
 if stage in ('migration','all'):migration()
 if stage!='migration':run_http(stage)

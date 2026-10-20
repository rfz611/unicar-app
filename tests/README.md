# Verificações

A partir da raiz do projeto:

```sh
dotnet run --project tests/UniCar.API.Checks
dotnet run --project tests/UniCar.Mobile.ServiceChecks
python3 tests/api_negotiation_check.py all
```

O último comando requer cliente mysql no PATH, MySQL local e .NET. Cria banco descartável `unicar_check_<id>`, inicia uma API de teste em 5056 e remove banco/processo ao terminar, sem usar o banco de usuários como fixture. Por padrão usa 127.0.0.1:3306 com root sem senha (instalação local de desenvolvimento). Para outra conexão, configurar `UNICAR_TEST_MYSQL` no ambiente, com Server, Port, User e Password. Não registrar credenciais em comandos salvos/arquivos de entrega.

A API precisa estar compilada previamente para a execução HTTP:

```sh
dotnet build src/UniCar.API
```

`python3 tests/api_reservations_check.py` é uma entrada de compatibilidade para as regressões integradas isoladas. `api_negotiation_check.py migration` testa somente preservação e reaplicação da migração; `activities` ou `negotiations` executam as partes HTTP correspondentes.

`UniCar.API.Checks` valida a fórmula e adapter de rotas com respostas controladas. No harness isolado, o modo `publish` também executa criação/atualização dos controllers com cotação do provedor de fixture, sem chave externa e sem adicionar bypass ao servidor real.

Mapas reais têm uma etapa distinta de validação: ver docs/MAPAS-E-CONTRIBUICAO.md. Não confundir testes com fixture com uma integração Google configurada e validada.

Regressão de seleção assíncrona de mapa: `node tests/map_selection_check.cjs`. Integração real: `python3 tests/api_negotiation_check.py live-maps` usa credenciais locais e valida custos carro 0,70/moto 0,35 aprovados para este ambiente; consome chamadas Google.

`node tests/map_selection_check.cjs --place` verifica pesquisa de local único, restrição ao Brasil e callback com coordenadas.

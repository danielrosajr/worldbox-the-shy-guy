# TESTES ESPECIFICOS v0.1.3

1. Em um mundo de teste, gere um SCP e um humano.
2. Posicione cursor no humano, aperte F7 e confira `Alvo: N tiles`.
3. Sem esperar a perseguicao, clique `Teste: eliminar alvo selecionado (manual)`.
4. Esperado: alvo deixa de estar vivo, painel mostra `TESTE DE MORTE OK: Actor.dieSimpleNone()`; se nao, registra metodo ausente ou excecao.
5. Depois gere outro humano, marque F7, espere TRIGGERED→RAGE e aproxime. A morte automatica deve acontecer no alcance configurado (1.7 tiles).
6. Se o alvo nao morrer, pressione F9 para registrar assinatura real e campos de sombra.
7. Repita opcionalmente com WorldFall, comparando erros `FirstPerson` e FPS.
8. Nunca use o teste de morte em um mundo importante.

# Plano de teste da v0.1.2 alpha

## Antes de testar

- [ ] Backup dos saves e mundo descartavel.
- [ ] WorldBox 2D, inicialmente sem WorldFall.
- [ ] Pasta da v0.1.0 removida, mantendo somente v0.1.2.
- [ ] Painel visivel no canto direito com `Mapa: Mapa pronto`.

## Diagnostico

- [ ] Botao `Teste: humano` cria um humano comum perto do centro.
- [ ] Botao `Gerar aqui` cria SCP-096 perto do centro, ou mostra falha no painel.
- [ ] Se falhou, registrar texto `REGISTRO FALHOU` ou `SPAWN FALHOU`.
- [ ] `Posicionar SCP` + clique no terreno deve tentar criar o ator no local clicado.
- [ ] Se houver SCP, segundo spawn deve exibir aviso de duplicata.
- [ ] `Diagnostico` deixa no log assinaturas do mapa, spawn, clone e actor.

## Fluxo de IA (somente se o spawn funcionar)

- [ ] `Marcar alvo` proximo de NPC mostra estado Triggered.
- [ ] Apos tempo de preparacao, estado vai para Rage.
- [ ] Se o SCP nao perseguir, registrar `Perseguicao nao confirmada` e logs.
- [ ] Se alcancar o alvo, tentar ciclo nativo de morte; registrar falha se houver.
- [ ] Cancelar alvo e validar Rage -> Calming -> Docile.
- [ ] Salvar e recarregar mundo de teste, verificar duplicacao e persistencia.

## Para enviar a proxima correcao

Captura do painel apos clicar `Gerar aqui`, trecho de Player.log com `[SCP096]`, versao do WorldBox/NML. Se a assinatura real for diferente, envie os assemblies listados em `scripts/collect-reference-assemblies.sh`.

## Regressao 0.1.2: erros do log do usuario

- [ ] Ao abrir mundo novo, nao ocorre aviso de sombra `(0.00,0.00)` nem de `texture_path_baby` associado a `scp096_shyguy`.
- [ ] O SCP nasce com sprite herdado do humano nesta versao de estabilidade (verificar visual).
- [ ] Painel mostra a distancia do alvo diminuindo durante RAGE.
- [ ] Durante RAGE, esperar no minimo 8 segundos para observar eventual aviso de ator parado.
- [ ] Comparar cenario com WorldFall desligado, ligado e `Perseguir/atacar` desligado.
- [ ] Guardar capturas do painel e trecho do `Player.log` com `[SCP096]` + `FirstPerson`.

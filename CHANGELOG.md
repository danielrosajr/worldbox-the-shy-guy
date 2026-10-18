# CHANGELOG

## 0.1.6-alpha — 18/10/2026
- Corrige erros `LocalizedTextManager: missing text: trait_ArvakSCP096_Witness` e `trait_ArvakSCP096_RageSpeed`, adicionando IDs corretos de nome e descricao.
- Adiciona traducao brasileira e `Locales/pt.json`, usando convencao ja comprovada no The Unrelenting.
- Inclui no log a distancia ao executar cada ataque automatico, para distinguir perseguicao real de morte remota.
- Mantem as mecanicas da v0.1.5 sem alteracoes, incluindo a selecao, fila de alvos e regras de alcance.
- Validacao estrutural apenas; pacote nao compilado/testado no WorldBox desta sessao.


## 0.1.5-alpha — 18/10/2026
- Reaproveita do The Unrelenting o padrão `Actor.cancelAllBeh()` + `Actor.goTo(WorldTile, false)` e intervalos adaptativos de navegação.
- Adiciona `ArvakSCP096_Witness`, trait `ArvakSCP096_RageSpeed` (+85 speed) e fila FIFO de testemunhas com máximo configurável.
- Marca por proximidade <=5 tiles por padrão como aproximação temporária, configurável no NML e painel; leitura de rosto permanece pendente.
- Varre traits a cada 12s para restaurar fila de portadores após carregar mundo, sem garantia de preservar a ordem histórica.
- Corrige classificação prematura de falha de `dieSimpleNone()` aguardando confirmação posterior.
- Complementa animações `swim_0`..`swim_3`, redireciona `texture_path_baby` e ajusta shadow_size mínimo experimentalmente.
- Seleção distante pode ser ativada pelo botão Escolher alvo mapa e clique posterior; Shift+F7 seleciona o NPC mais próximo do cursor sem limite de raio.
- Testes estáticos apenas; execução no jogo e compatibilidade WorldFall pendentes.


## 0.1.4-alpha — 18/10/2026
- Troca template humano por template selvagem `sheep` para evitar comportamentos civilizados e construcao de aldeias.
- Reintroduz tentativa de carregamento sprite exclusivo SCP com referencias herdadas de sombra e recursos infantis.
- Adiciona modo experimental de alvo global sem limite de distancia, separado de F7 proximo ao cursor.
- DiagnosticReadiness/F9 inclui origem, status visual, flags efetivamente desativadas.
- Reforca aviso de instalacao limpa e identifica v0.1.2 ainda carregada no log recebido.
- Mantem teste de morte da v0.1.3 (so confirmado apos teste local do usuario).

# CHANGELOG

## 0.1.3-alpha — 18/10/2026
- Troca tentativa especulativa `die()`/`killHimself()` por `Actor.dieSimpleNone()` (evidencia externa, WorldBox 0.51.2 build 719).
- Confere registro `units_only_alive` antes/depois da chamada: nao considera sucesso somente por reflexao sem excecao.
- Adiciona teste de morte manual separado da perseguicao.
- Limita a tres tentativas de morte por alvo, com 2s de espacamento e logs claros.
- Diagnostico F9 de campos de sombra do asset SCP e humano. Avisos ainda nao corrigidos.
- Nenhuma mudanca no algoritmo de navegacao; perseguição permanece experimental.

# Changelog

## 0.1.2-alpha — diagnostico de movimento e estabilidade visual

- Corrigido o codigo responsavel pelos avisos de sprites baby/child e sombras 0x0: o novo ator nao substitui mais o `texture_asset` herdado do humano por um asset incompleto.
- Sprites SCP seguem disponiveis no pacote, mas desabilitados temporariamente no jogo ate validar carregamento 2D completo. O ator pode aparecer com visual de humano por enquanto.
- Painel agora mostra distancia ao alvo em tiles.
- Adicionado controle para suspender perseguição/ataque sem desinstalar o mod, permitindo diagnosticar erro possivelmente relacionado a `FirstPerson`.
- Reenvio de comandos de perseguição reduzido de 0,75s para 1,5s; emite aviso se o SCP ficar parado durante RAGE, sem teleportes de risco.
- Preservada geracao manual e fluxo original de estados.
- Nao corrigido: `NullReferenceException` em `FirstPerson.WalkPace.Step`, cuja causa requer teste de isolamento/assemblies.

# CHANGELOG

## v0.1.1-alpha — Diagnostico de geracao e painel visivel (2026-10-18)

### Corrigido
- Painel IMGUI em tela com status do mapa, registro do asset e confirmacao da ultima acao.
- F6 agora tenta criar o SCP em terreno no centro da tela; spawn isolado passou para Shift+F6.
- Novo modo de posicionamento: clicar no mapa para escolher onde criar o SCP.
- Acesso a erro de registro e falha de spawn sem precisar abrir o log antes.
- Botao `Teste: humano` distingue problemas na criacao de unidades de problemas no asset customizado.
- Parametro `adult=true` ao gerar unidades onde a assinatura do jogo o permitir.
- Comandos para log e diagnostico da assinatura de clone, spawn, tiles e unidades.

### Limitacoes
- Nenhum teste executado no WorldBox local do usuario.
- API de clone e compatibilidade do sprite/IA ainda precisam das DLLs ou do Player.log real.

## v0.1.0-alpha — Primeiro prototipo
- Registro experimental do SCP, sprites, maquina de estados e hotkeys basicos.
- Perseguicao e ataque dependentes de reflexao em runtime ainda nao validados.

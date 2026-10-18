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

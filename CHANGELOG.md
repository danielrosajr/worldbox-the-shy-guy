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

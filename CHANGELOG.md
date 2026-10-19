# CHANGELOG

## 0.2.9-alpha — O Implacável
- Ajuste experimental do flash em WorldFall 3D com um apresentador IMGUI tardio independente do Canvas.
- Diagnóstico do flash: conta os frames de desenho no último passe (`lateGuiFrames`).
- Aumenta a vida base da criatura de 10.000 para 40.000 para novos spawns.
- Adiciona regeneração gradual via `Actor.restoreHealth(Int32)` (700/2000 HP por segundo, configurável), sem imortalidade.
- Aceleração por vítima em RAGE, com quatro traits mutuamente exclusivas: +85/+105/+125/+145 de velocidade ao longo de 45 segundos. Ao trocar alvo, reinicia no estágio zero.
- Preserva áudio FMOD, bus musical, cooldown, fila FIFO, o gatilho do WorldFall e o ataque a curta distância.
- Novas traduções e configurações para regeneração/aceleração.
- Testes estáticos concluídos; verificação dentro do jogo pendente.

## 0.2.8-alpha — Immersion
- Flash por Canvas Unity e controlador de música original isolado via FMOD bus.
- Som customizado permanece audível, áudio ambiente inalterado.

# SCP-096 — v0.2.3-alpha | WorldFall Runtime Bridge

**Nesta versão a câmera WorldFall não é uma UnityEngine.Camera.** Analisamos a DLL real (`Worldfall.dll`, 16/10/2026), verificando o tipo `FirstPerson.WorldBoxMod` que deriva de `MonoBehaviour` e expõe `get_IsFirstPerson():bool`, `get_Host():Actor` e `get_View():FirstPerson.Core.ViewState` (X,Y,Z,Yaw,Pitch,Fov).

## Teste de integração 3D

1. Remova a instalação anterior e extraia só `SCP096-TheShyGuy-0.2.3` em `worldbox/Mods`.
2. Abra um **mundo descartável**, crie o SCP com F6, selecione um aldeão e entre no WorldFall usando G.
3. Na primeira pessoa, aproxime-se do SCP. O log deve indicar `WORLD FALL 3D ATIVO: Host=Actor`.
4. Pressione F10 para gerar diagnóstico manual **ou** acompanhe o registro automático de transição. Se F10 não funcionar, o log automático é suficiente para comprovar o estado.
5. **F11 = teste manual:** marca o aldeão atualmente controlado (dentro do alcance configurado). Não significa reconhecimento facial; serve para verificar o vínculo do Actor à fila e à perseguição já funcional.
6. Saia de primeira pessoa e confira a fila/execução.

```bash
grep -nE 'SCP096|WorldfallSight|WORLD FALL 3D|error CS|Exception' ~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -150
```

## Limitação

A observação **automática do rosto ainda não está habilitada**. A DLL mostra que o WorldFall desenha a cena num renderizador 3D próprio (`FirstPerson.Gpu.GpuRenderer`, com `PickTag`, `PickDepth`, `RequestPick`) e não num conjunto de `UnityEngine.Camera` e `Transform` por criatura. A correspondência entre o código de picking, o Actor e **a cabeça visível**, incluindo a checagem de obstáculos, ainda exige validação. Não iremos ativar falso positivo ao ver alguém de costas ou atrás de uma parede.

A lógica de spawn, traits, FIFO, ataque fatal e `Actor.moveTo(WorldTile)` da versão 0.1.6 foi preservada. A marcação por proximidade 2D pode continuar habilitada em modo mapa; ao entrar em primeira pessoa, ela é suspensa por padrão.

Veja `docs/WORLDFALL_INSPECTION.md` para detalhes da inspeção.

## Atualização GitHub

Substituir: `ModClass.cs`, `src/WorldfallSight.cs`, `mod.json`, `README.md`, `CHANGELOG.md`, `docs/TEST_PLAN.md`. Adicionar: `docs/WORLDFALL_INSPECTION.md`. Remover: nenhum arquivo (apague apenas instalações antigas). Versionar como `0.2.3-alpha`. Commit: `feat: bridge Worldfall possession and view state from inspected assembly`.

**Status:** análise estática concluída; não compilado contra as DLLs do jogo nem testado in-game neste ambiente.

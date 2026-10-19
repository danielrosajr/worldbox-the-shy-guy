# SCP-096: The Shy Guy v0.2.4-alpha — WorldFall Look Prototype

Esta versao tenta marcar automaticamente o personagem controlado ao **apontar para o SCP visivel, de frente, por 0,45 segundo** no WorldFall 3D. Usa `FirstPerson.WorldBoxMod.get_Picked()` (campo `_picked`) e os caches `Creatures.States`/`SceneCollector._headings` identificados na sua Worldfall.dll. **Ainda nao e detecao precisa de pixels do rosto**: a mira identifica o ator renderizado e a checagem frontal e uma aproximacao. Leia `docs/WORLDFALL_SIGHT_024.md`.

## Instalar

Fechar WorldBox; remover a pasta `SCP096-TheShyGuy-0.2.3` antiga; extrair somente `SCP096-TheShyGuy-0.2.4` em `Mods`. `mod.json` deve ter version 0.2.4. O GLB nao mudou, entao nao e preciso reinstalar o pack 3D se ja estiver ativo.

## Testar

Em um mundo de TESTE, criar SCP com F6. A opcao de proximidade 2D vem desligada para nao confundir os resultados. Controlar um NPC com G, mirar no SCP de frente por meio segundo. Observar log da trait Witness. Se nao marcar, pressionar F10 olhando de frente e repetir olhando para tras dele: campos `AIM`, `FACE`, `RESULT` explicam o resultado. Se a frente estiver invertida, usar F5 > Inverter sentido do rosto (offset 180). F11 continua como marcacao **manual** de diagnostico. F7 mantido.

Falha segura: se nao for possivel provar que SCP esta sob a mira e de frente, nao aciona. Um objeto atras de uma parede nao deve ser selecionado pelo renderizador, mas a semantica do picker deve ser testada. Nunca usar saves importantes.

```bash
grep -nE 'SCP096|WorldfallSight|error CS|Exception' ~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -120
```

## Referencias e limites

- Leia `docs/WORLDFALL_SIGHT_024.md` para a analise da DLL e detalhes de visibilidade e orientacao.
- Leia `docs/TEST_PLAN.md` para o teste completo em mundo descartavel.
- O GLB original e o instalador opcional seguem na pasta `Worldfall/`.
- O pacote nao inclui Worldfall.dll, assemblies da Unity ou arquivos do jogo.
- Licenciamento da fanwork SCP em `LICENSE.md` e `CREDITS.md`.

## Versao / desenvolvimento

Versionar como `0.2.4-alpha`. Commit: `feat: add experimental Worldfall gaze targeting using render picks and actor facing`.

**Aviso**: analise estatica e inspeção de metadados da DLL efetuadas; ainda nao foi compilado nem executado no jogo do usuario.

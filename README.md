# SCP-096: The Shy Guy — 0.2.9-alpha

Mod experimental para WorldBox 0.51.2 e NeoModLoader, com integração opcional ao WorldFall.

**Avanços desta versão:** resistência alta porém finita, regeneração, velocidade progressiva limitada por vítima, e nova tentativa de flash WorldFall 3D com IMGUI tardio. A detecção pelo rosto no WorldFall, a fila de testemunhas, a morte, o sistema FMOD e a mixagem de música foram mantidos.

## Instalar no Ubuntu
1. Feche WorldBox.
2. Exclua ou mova para fora de `Mods` as versões antigas do SCP-096 (não o The Unrelenting).
3. Extraia uma única pasta `SCP096-TheShyGuy-0.2.9` dentro de `~/.steam/debian-installation/steamapps/common/worldbox/Mods`.
4. Abra um mundo descartável **novo** para testar vida de 40.000 do asset atualizado.
5. Se o modelo 3D já funcionava na versão anterior, não precisa reinstalar o GLB; ele não mudou.

## Novas regras
- Em DOCILE, a regeneração é de 700 HP/s. Durante TRIGGERED/RAGE, 2.000 HP/s, a cada 1s.
- Vida base inicial 40.000 HP. O SCP **não é imortal**. Dano extremo ou dano acima da cura pode matá-lo.
- Em RAGE: bônus de velocidade +85 (0s), +105 (15s), +125 (30s), +145 (45s). Nunca se somam; ao trocar alvo volta para +85.
- O grito local, a preparação de aproximadamente 55s, a fila, cooldown e o silenciamento da música original permanecem como antes.

## Flash 3D: modo de teste
- `Shift+F10` dentro do WorldFall: prévia do flash.
- `F9`: diagnóstico, procurar `OVERLAY=... lateGuiFrames=...`.
- Se o log confirmar `FLASH 3D LATE GUI` mas a imagem ainda não ficar visível, o renderizador do WorldFall está sobrepondo a nova camada; o próximo ajuste precisa atuar no ponto real de composição, não só mudar `sortingOrder`.

## Log de diagnóstico
```bash
grep -nE 'SCP096|REGEN|RAGE ACCEL|FLASH 3D LATE GUI|error CS|Exception' \
  ~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -150
```

Documentação técnica: `docs/WORLDFALL_RENDER_AND_DURABILITY_029.md`.

## Direitos de mídia
O pacote **personal-test** contém WAVs e imagem fornecidos pelo usuário só para uso privado/local. **Não publicar** esses arquivos sem autorizações e licenças confirmadas. O pacote **code-only** não inclui as mídias.

**Validação:** arquivos inspecionados e testes estáticos executados. Esta versão não foi compilada nem testada no runtime real do WorldBox neste ambiente.

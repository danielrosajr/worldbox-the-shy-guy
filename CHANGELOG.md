## v0.2.15-alpha — The Shy Guy: Reborn (3D)
- REBUILT: novo GLB original do SCP-096, em vez do antigo corpo 3D simples.
- Visual: olhos profundamente escuros, lágrimas, boca alongada com dentes, clavículas/costelas salientes, torso atrofiado, braços e dedos exageradamente longos, pés e articulações remodelados.
- Geometry: 11 meshes, ~14.230 triângulos, altura ~2,21 unidades. Frente -Y, +Z vertical, mesma escala do pack anterior.
- Packaging: script reprodutível `scripts/build_scp096_glb.py`, instalação separada do pack WorldFall e verificações estáticas de geometria.
- PRESERVED: código NML da v0.2.14, áudio, música, fila FIFO, regeneração, aceleração e Dread RenderTexture.
- NO ANIMATIONS YET: GLB ainda é uma pose estática. Verificar o novo visual dentro do WorldFall antes de testar rigging e clipes.
- No WorldBox runtime compilation/execution performed; user gameplay validation required.

## 0.2.14-alpha

- Fix: manter teste manual de Dread ativado independentemente da presença de SCP vivo.
- Fix: atualizar WorldfallSight.Refresh em Update e LateUpdate, inclusive antes do retorno sem SCP.
- Diagnose: logs de preview e causas de GPU/material indisponíveis.
- Inspect: Worldfall.dll oficial confirma FirstPerson.WorldBoxMod._gpu, FirstPerson.Gpu.GpuRenderer._present/Output, FirstPerson.SettingsMenu e ModsTab.
- Unchanged: perseguição, regeneração, aceleração e FMOD.

# Changelog

## v0.2.13-alpha — Dread RenderTexture
- Corrige o no-op de sombra GPU: ShadowDarkness=1.00 já estava no máximo.
- Experimental: escurece a própria saída GPU 3D do WorldFall usando `_present` RenderTexture e camada alpha azul-escura.
- Dread só enquanto o personagem controlado for o alvo corrente (ou Shift+F9 preview); escurecimento gradual e restauração ao acabar.
- Acrescenta logs `DREAD RT` de material, texture de destino e frames compostos.
- Preserva controlador de música v0.2.12, regeneração, aceleração e perseguicao/FIFO.
- Não compilado nem executado no WorldBox; requer verificação visual real.

# Changelog

## v0.2.12-alpha — Compile hotfix CS1501
- Corrige `src/DynamicMusicController.cs`: FMOD Core `ChannelGroup.getVolume(out float)` aceita UM argumento de saída.
- Mantém `FMOD.Studio.Bus.getVolume(out float, out float)` com DOIS argumentos, pois é outra API.
- Não altera estados, IA, aceleração, áudio próprio do SCP, Canvas nem vinheta do WorldFall.
- Aviso: correção verificada estaticamente, ainda precisa da compilação via NML.

# Changelog

## v0.2.12-alpha — Native Music Group & Dread POV
- Investigação estrutural da DLL do WorldFall: `FirstPersonAudio._music` é `FMOD.ChannelGroup`, separado dos efeitos ambientes.
- Silenciamento da trilha nativa do WorldFall no grupo `_music`, com snapshot/restauração de volume e estado mute; `bus:/Music` do WorldBox permanece controlado.
- Novo `WorldfallDreadFilter`: vinheta e sombras nativas mais fortes apenas enquanto o personagem controlado é a **próxima vítima** do SCP-096.
- Prévia de escuridão via `Shift+F9` no WorldFall 3D; opção de ativar e regular a intensidade pelo painel F5/configurações.
- Não altera visual do mapa 2D, nem outros NPCs; restaura parâmetros originais quando o filtro é desativado.
- Mantidos FMOD do SCP, áudio ambiente, regeneração, aceleração limitada por vítima, cooldown, fila FIFO e detecção 3D.
- Componentes específicos do WorldFall são opcionais. Sem Harmony nem DLL Worldfall redistribuída.
- **Experimental**: requer teste real do mute no grupo e dos campos de GPU/vinheta.

## v0.2.10-alpha — Global BGM mute
- `bus:/Music` e `FirstPersonAudio.MusicVolume` alterados quando o SCP está em TRIGGERED ou RAGE, independentemente da posse.

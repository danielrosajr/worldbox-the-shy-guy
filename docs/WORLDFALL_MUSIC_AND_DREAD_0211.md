# WorldFall native music group and POV dread — 0.2.11-alpha

## Observações verificadas na DLL disponibilizada pelo usuário

O tipo `FirstPerson.FirstPersonAudio` contém campos `MusicVolume:float` e `_music:FMOD.ChannelGroup`. Outros campos confirmados: `_battle:FMOD.Channel[]`, `_calm:FMOD.Channel`, `_sting:FMOD.Channel` e `_group` (outra coleção de áudio). Isso demonstra por que alterar apenas `MusicVolume` não necessariamente interrompe a música já tocando no WorldFall. A v0.2.10 encontrava `bus:/Music` e `MusicVolume`, mas o usuário continuava ouvindo música no modo 3D.

A nova implementação atua no `FirstPersonAudio._music` (grupo dedicado ao áudio musical do WorldFall), mantendo a implementação anterior para `bus:/Music` e `MusicVolume`. A busca é por Reflection, sem link com Worldfall.dll. Armazena e restaura `getVolume`, `getMute` originais do grupo, sem atuar no master nem no grupo de efeitos de WorldFall, nem nos canais próprios de SCP-096. Ao alterar cena/grupo, tenta restaurar o anterior antes de capturar o novo.

## Filtro sombrio do POV

O WorldFall também contém campos nativos `FirstPerson.WorldBoxMod.Renderer:FirstPerson.Core.FrameRenderer` e `_gpu:FirstPerson.Gpu.GpuRenderer`, ambos com `ShadowDarkness:float`, enquanto `FrameRenderer` tem `Vignette:float`. O novo `WorldfallDreadFilter` usa esses campos para aumentar vinheta e sombra apenas no modo WorldFall 3D quando a vítima atual da fila é o personagem controlado. A modificação não usa Canvas, já que Canvas/OnGUI renderizam apenas no 2D na instalação do usuário. `Time.unscaledDeltaTime` controla entrada e saída; os valores originais são restaurados ao sair da perseguição, deixar primeira pessoa, morrer, trocar de alvo, fechar mundo/desativar mod.

**Limitações**: a alteração de campos de sombra/vinheta ainda precisa ser visualmente validada em CPU/GPU na versão instalada; WorldFall pode recalcular os campos ou shaders internamente. A v0.2.11 não garante um efeito pós-processado em todos os modos de renderização. Também não substitui a interface da tela de morte do WorldFall.

## Teste recomendado
1. Em mundo descartável, abra WorldFall em primeira pessoa (`G`) e pressione `Shift+F9` para alternar a prévia da sombra/vinheta sem estar marcado; pressione novamente para restaurar.
2. Pressione `Shift+F11` para alternar o silenciamento da música, mesmo sem o SCP ter vítima; depois desligue e confirme a restauração.
3. Faça um NPC disparar TRIGGERED e entre no WorldFall. A música **original** do WorldBox/WorldFall deve sumir; a música de preparação do SCP ainda toca enquanto o aviso estiver ativo, e seu grito permanece local em RAGE.
4. Faça o personagem controlado ser a vítima **primeira da fila** e observe o escurecimento. Ao trocar de personagem/alvo, o filtro deve sumir.
5. Verifique o log:

```
grep -nE 'SCP096|BGM GLOBAL|BGM OVERRIDE|DREAD POV|error CS|Exception' \
~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -150
```

Esperado se suportado: `BGM GLOBAL: WorldFall _music FMOD.ChannelGroup capturado` e `DREAD POV: renderizadores encontrados`. Ausência do primeiro log indica que o grupo musical não estava disponível e o som pode persistir. O segundo indica apenas que os parâmetros foram escritos, não confirma efeito visível no shader.

## Empacotamento e licenças

No pacote `personal-test` os WAVs e imagem fornecidos pelo usuário são incluídos apenas para testes locais. O pacote `code-only` não inclui essas mídias, cuja licença individual não foi confirmada. A DLL Worldfall não é redistribuída.

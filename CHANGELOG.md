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

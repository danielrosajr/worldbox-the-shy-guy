# Global BGM controller — v0.2.10

- Fonte de verdade da ameaça: `ShyGuyBrain.State` (`Triggered` ou `Rage`).
- Não é preciso personagem possuído, flash visível, câmera 3D ou alvo local.
- Ao atingir `Calming`, `Request(false)` inicia restauração. Ao resetar mapa/mod/scp, `RestoreImmediately()` devolve valores imediatamente.
- `DynamicMusicController`: salva `_busOriginal` para `bus:/Music` e `MusicVolume` original de `FirstPersonAudio`. Reaplica redução em `LateUpdate()`.
- A DLL `Worldfall.dll` inspecionada (fornecida pelo usuário) declara `WorldBoxMod.get_Audio()` e `FirstPersonAudio.MusicVolume`; a IL de `UpdateMusic` contém uma referência a esse campo. Isso não comprova o comportamento final em runtime.
- Não alterar FMOD Master nem o grupo de efeitos; sinais próprios do SCP são tocados em FMOD Core.
- Caso a trilha do WorldFall continue audível, registrar `BGM GLOBAL` para orientar inspeção dos canais específicos. Não aumentar cegamente o nível de muting para outros buses.

## Teste de regressão
1. WorldBox 2D: testemunha NPC sem jogador controlado -> BGM zero durante Triggered/Rage.
2. WorldFall 3D: BGM próprio mudo, ambiente e gritos audíveis.
3. Múltiplas testemunhas -> nenhuma reativação prematura da trilha.
4. Calming/Docile -> volume original restaurado.
5. Mudar de mundo/mod disabled -> volume restaurado.
6. Confirmar que 0.2.9 segue como rollback se houver nova exceção.

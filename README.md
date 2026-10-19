# SCP-096 v0.2.6-alpha — FMOD Audio Fix

**Alteração crítica:** WorldBox desabilita o sistema padrão Unity AudioSource/AudioClip e usa FMOD.
A v0.2.5 falhava em todos os `AudioClip.SetData`, por isso não havia sons. Agora
`src/ShyGuyAudio.cs` usa o FMOD Core do próprio jogo via `FMODUnity.RuntimeManager`.

**Não testado dentro do WorldBox neste ambiente.** Testar em mundo descartável.
O padrão de reprodução foi comparado ao `NeoModLoader.utils.CustomAudioManager`.

## Novidades
- Áudio WAV carregado pelo FMOD Core e reproduzido em canais nativos.
- Aviso TRIGGERED e música globais, grito inicial de RAGE global de uma execução.
- Choro em DOCILE, grito em loop durante RAGE e som de execução com atenuação local por tiles.
- Som em loop usa arquivos em streaming para reduzir RAM.
- Botões no F5: **Testar som global**, **Testar musica** e **Parar sons de teste**.
- Mensagens `Audio FMOD carregado`, `Audio FMOD PLAY`, com motivos de falha.
- Flash com entrada suave, duração default 0,95s e opacidade default 0,24; dois controles no F5.
- Traduções dos campos e descrições do menu NML em português e inglês.
- A perseguição, o ataque e a câmera WorldFall 3D foram preservados do código anterior.

## Instalação
1. Feche o WorldBox.
2. Remova todas as pastas antigas `SCP096-TheShyGuy-0.2.x` de `Mods`.
3. Extraia **somente** a pasta `SCP096-TheShyGuy-0.2.6` deste ZIP em `Mods`.
4. Abra um mundo de TESTE, gere o SCP. Pressione F5 e clique **Testar som global**.
5. Confira o `Player.log`, particularmente `Audio FMOD`.
6. Se o modelo 3D já estava funcionando, não é necessário reinstalar o pack WorldFall.

```bash
grep -nE 'SCP096|Audio FMOD|error CS|LocalizedTextManager: missing|Exception' \
  ~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -140
```

## Teste das fases
- DOCILE: choro apenas junto ao SCP.
- TRIGGERED: aviso global imediatamente, música global 4s após o disparo; tempo total 48s.
- RAGE: um grito global, seguido de grito em loop SOMENTE perto da criatura.
- EXECUCAO: efeito local junto da criatura, não do outro lado do mapa.
- CALMING -> DOCILE: pausa de 15 segundos antes de novas testemunhas.
- F5 permite testar o cue e música separadamente e alterar fade/opacidade do flash.

## Licença
O ZIP `personal-test` inclui apenas para avaliação local mídias enviadas pelo usuário,
com direitos individuais ainda não verificados. **Não publique esses sons/imagem no
GitHub nem na Steam Workshop.** O ZIP `code-only` exclui `PrivateMedia`.

As alterações são código-fonte novo; arquivos de WorldBox, NML e WorldFall não estão incluídos.


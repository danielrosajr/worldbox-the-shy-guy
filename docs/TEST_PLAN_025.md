# Roteiro de testes da v0.2.5

1. Abra mundo descartável. Confira `[SCP096] v0.2.5 THE SCREAM` e `[SCP096] Audio: assets locais detectados`.
2. Com SCP em DOCILE, aproximar câmera/WorldFall: choro aumenta; afastar-se 28 tiles: choro desaparece.
3. Entre com G controlando aldeão, olhe frente do rosto: deve gerar testemunha e toque global `trigger.wav`.
4. Ao 4º segundo do TRIGGERED, música `warning_music.wav` começa; só no 48º segundo começa a perseguição.
5. Quando vira RAGE, grito global toca uma vez e depois de sua duração segue grito em loop local.
6. Adicione outras testemunhas durante RAGE; áudio global não deve recomeçar. O SCP deve matar alvos a <=1.7 tiles na ordem da fila.
7. Após ultima morte, aguarde CALMING→DOCILE. Por 15s, proximidade/olhar não pode gerar nova testemunha. Após 15s volta a marcar.
8. Teste flash ligando/desligando no painel. Fique atento a fotossensibilidade e não teste efeitos intensos se houver desconforto.
9. Em 2D, NPCs próximos ao SCP devem ser marcados. No WorldFall, aldeão controlado não deve ser marcado por proximidade sem olhar.
10. Desligue `Audio de terror` e confira que para de tocar.
11. Remova temporariamente `PrivateMedia` e confira se mod inicializa normalmente e persegue sem som.
12. Confira `Player.log` depois de todos os cenários. Não teste em save importante.

**Não realizado neste ambiente:** compilação NeoModLoader, qualidade perceptiva, reprodução de áudio em 2D/WorldFall e sensibilidade do face cue.

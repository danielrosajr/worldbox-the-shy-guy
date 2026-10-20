# SCP-096: The Shy Guy — v0.2.12-alpha (compile hotfix)

**Nesta versão:** tratamento separado do grupo FMOD de música do WorldFall (`FirstPersonAudio._music`) e filtro visual nativo de medo para a vítima atual, sem depender do Canvas invisível no 3D.

## Comportamento
- NPCs no 2D continuam marcados por proximidade; personagem possuído no WorldFall 3D é marcado olhando para o rosto.
- TRIGGERED dura no mínimo 55 segundos (ou a soma real do áudio de aviso + música).
- Durante TRIGGERED/RAGE, silencia as músicas originais do jogo globalmente, inclusive quando a testemunha é um NPC, sem silenciar a música de aviso própria do SCP.
- Durante RAGE, grito do SCP somente local e em loop; execução com áudio local.
- RAGE acelera progressivamente para o alvo atual (quatro estágios com limite) e reinicia velocidade a cada troca de vítima.
- Regeneração disponível, cooldown DOCILE 15s.
- POV sombrio opcional: somente se você está possuindo o **alvo atual** em WorldFall 3D. A vinheta e as sombras são aumentadas gradualmente, e depois restauradas.
- O antigo flash continua funcionando no 2D; a imagem sobreposta no 3D continua experimental e não foi corrigida nesta versão.

## Instalação
Feche o WorldBox. Remova qualquer pasta antiga do Shy Guy e coloque **uma única pasta `SCP096-TheShyGuy-0.2.12`** na pasta Mods. WorldFall e o GLB devem permanecer como estavam.

## Atalhos e testes
- `F6`: criar SCP; `F7`: marcar alvo no mapa; `G`: possuir ator com WorldFall.
- `Shift+F9`: prévia do escurecimento do POV, sem precisar ser marcado. Repetir para restaurar.
- `Shift+F11`: alternar prévia do silêncio musical. Repetir para restaurar.
- `F9`: diagnóstico geral; `F10`: diagnóstico 3D.
- `F5`: painel para regular intensidade da escuridão e desativá-la, ajustar som e outras opções.

```
grep -nE 'SCP096|BGM GLOBAL|BGM OVERRIDE|DREAD POV|error CS|Exception' \
~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -150
```

**Limitações**: estas alterações foram verificadas apenas por inspeção de código/metadata da DLL; não houve compilação ou execução em jogo neste ambiente. O efeito de sombra no renderer GPU e o controle de grupo musical precisam da confirmação do usuário.

## Licença
Os dois ZIPs são separados: **personal-test** com sons e imagem enviados pelo usuário (somente uso local/teste), e **code-only** sem mídias de licença ainda não confirmada. Não publique o pacote de teste no GitHub/Steam Workshop sem confirmar os direitos específicos dessas mídias.

## Corrigido nesta versão
O erro `CS1501: No overload for method 'getVolume' takes 2 arguments` foi causado por uso
incorreto da assinatura FMOD Core no método `ApplyWorldfallOwnMusic`.
A única mudança de lógica é `group.getVolume(out checkVolume)`.
`FMOD.Studio.Bus.getVolume(out original, out final)` continua correto e inalterado.

**Se ainda aparecerem erros, envie o trecho do Player.log com `error CS`.**

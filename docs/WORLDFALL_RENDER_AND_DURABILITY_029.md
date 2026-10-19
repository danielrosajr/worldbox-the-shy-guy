# v0.2.9 | Flash WorldFall, durabilidade e aceleração

## O que o teste anterior comprovou
- O WorldFall foi identificado pelo bridge `FirstPerson.WorldBoxMod`.
- Canvas da v0.2.8 retornou `canvas=True` no log, mas o usuário confirmou que a imagem era visível apenas no mapa 2D.
- A música do jogo foi corretamente silenciada pelo `bus:/Music` e restaurada depois.
- Os ataques `dieSimpleNone()` estavam funcionando com a distância real adequada.

## Tentativa de correção 3D
- A v0.2.9 mantém o Canvas usado no WorldBox 2D.
- Um `ShyGuyOverlayLateGui : MonoBehaviour` com ordem de execução tardia tenta desenhar **também** o flash no `OnGUI` de uma camada independente para o WorldFall 3D.
- A nova mensagem `FLASH 3D LATE GUI: primeiro frame desenhado` confirma que o código enviou o comando de desenho, não que o usuário viu o resultado final na tela.
- O `Status` registra `lateGuiFrames=N`. Se N > 0 e a tela não mostrar o flash, a próxima etapa terá de inspecionar a composição final do renderizador WorldFall, possivelmente com patch localizado; aumentar o Canvas novamente não é suficiente.

## Vida e regeneração
- Base de vida aumenta de 10.000 para 40.000 no asset registrado (aplica-se melhor a **novos spawns**).
- Usa `Actor.restoreHealth(Int32)` por reflexão, método já identificado nos diagnósticos da instalação.
- Recupera 700 HP por segundo em DOCILE/CALMING e 2.000 HP em TRIGGERED/RAGE por padrão, com intervalos de 1 segundo, sem curar se já estiver na vida máxima (quando o jogo disponibiliza `getHealthRatio()`).
- A entidade pode morrer se o dano for grande o suficiente antes da cura. Lava pode continuar perigosa; se morrer muito rápido, compartilhar Player.log e resultado do teste. Nenhum trait invulnerável ou immortality foi adicionado.

## Aceleração sem efeito Sonic
- Em RAGE e com alvo vivo, quatro estágios de trait exclusivos: +85 → +105 → +125 → +145 de `speed` (bônus, não velocidade final).
- Cada estágio dura cerca de 15 segundos com `RageRampSeconds=45`; mudança de vítima reseta ao estágio inicial.
- As quatro traits não se acumulam. A fila FIFO é preservada.
- Ao sair de RAGE, remove os quatro estágios.
- A velocidade real ainda depende do movimento, terreno e recálculo de estatísticas do WorldBox 0.51.2. A mudança foi implementada, ainda não confirmada no jogo.

## Como testar
1. Feche o jogo e remova instalações antigas do SCP; mantenha só v0.2.9.
2. Mundo **novo**: gere o SCP, pois criatura antiga pode preservar sua vida máxima anterior.
3. No WorldFall 3D, pressione Shift+F10; durante o flash, rode F9 depois e procure `lateGuiFrames`.
4. Marque uma vítima distante com F7, aguarde RAGE e confira `RAGE ACCEL` depois de 15, 30 e 45s.
5. Marque outra vítima e confira se aparece `novo alvo; velocidade voltou ao estagio 0`.
6. Em mundo descartável, teste fogo/lava por curto período e depois tire o SCP do perigo para verificar recuperação.
7. Confirme áudio e BGM como na v0.2.8.

```
grep -nE 'SCP096|REGEN|RAGE ACCEL|FLASH 3D LATE GUI|FLASH CANVAS|error CS|Exception' \
  ~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -150
```

## Limitações
Este pacote passou em testes estáticos locais, mas não foi compilado nem executado no WorldBox. A camada IMGUI tardia e a regeneração por reflexão requerem teste prático e podem depender da implementação do runtime.

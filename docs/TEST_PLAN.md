# Testes da v0.1.6-alpha — WorldBox 0.51.2 build 719

**Use um mundo novo e descartável. Não carregue o save importante.**

1. Instale apenas `SCP096-TheShyGuy-0.1.6` na pasta Mods e confira versão no painel.
2. Verifique `Traits: registradas` no log. Caso falhe, envie stack trace completo do Player.log.
3. Com a opção de proximidade desligada no painel, crie SCP e um humano 10–20 tiles longe. Aponte o cursor para o humano, F7. Aguarde os 8 segundos de TRIGGERED. Confirme no log `Perseguicao: Actor.goTo(WorldTile,Boolean)` e distância decrescendo. Espere a criatura se aproximar e matar o humano.
4. Repita com proximidade ligada: coloque 3 humanos a até 5 tiles do SCP. O painel deverá mostrar fila=3 e, após cada execução, mudar para a próxima vítima. Se o SCP ficar travado, pressione F9.
5. Crie um humano a 30–50 tiles do SCP em terra contínua. Aponte o cursor para o humano e clique `Escolher alvo mapa`. Confirme que o alvo foi marcado mesmo longe do SCP. Não exija travessia de oceanos.
6. Confirme se o sprite é branco e se os avisos `missing all animation_swim`, `child idle` e `Shadow size is too small` desaparecem. Se continuarem, envie log e diagnóstico F9.
7. Salve e recarregue o mundo, verificando se Witness permanece nos NPCs e é detectado novamente. Atenção: a ordem FIFO antes do save ainda não é persistida.
8. Apenas depois de validar sem WorldFall, teste com ele ativado. Compare FPS e exceções com/sem a perseguição do SCP.

Comando para enviar:

```bash
grep -nE 'SCP096|Trait|Shadow size|animation_|Exception|goTo|FirstPerson' \
  ~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -200
```

## Teste especifico v0.1.6

1. Abra o menu de traits e procure por **Testemunha do SCP-096** e **Fúria do SCP-096**. Não deve aparecer `LocalizedTextManager: missing text: trait_ArvakSCP096_...`.
2. Marque um humano e aguarde o SCP aproximar-se. No `Player.log`, procure `distancia no ataque=`: deve ser igual ou inferior ao `alcance=1.7 tiles` configurado (salvo modificação do alcance no NML).
3. Se aparecer `Actor.moveTo(WorldTile) (fallback)`, isso não comprova o caminho real percorrido; observe a criatura no mapa e compare as distâncias.
4. O botão **Teste: eliminar primeiro alvo** mata sem requisito de alcance, de propósito, e NÃO deve ser usado para validar a perseguição.

# SCP-096: The Shy Guy — v0.1.6-alpha

**Alvo: WorldBox 0.51.2, build 719 + NeoModLoader em Ubuntu.**

Código experimental preparado a partir de `SCP096 v0.1.4` e da análise do ZIP **The Unrelenting** fornecido pelo usuário. Esta entrega **não foi compilada com Assembly-CSharp.dll nem testada dentro do WorldBox**. Use apenas mundos descartáveis e faça backup de saves importantes.

## O que mudou na v0.1.6

- Corrigidas as chaves de localizacao das traits `trait_ArvakSCP096_Witness` e `trait_ArvakSCP096_RageSpeed` (com textos de descricao `_info`).
- Inclusos `Locales/en.json`, `Locales/pt_BR.json` e `Locales/pt.json`, seguindo o mesmo formato de chaves usado pelo The Unrelenting.
- O log de **ataques automaticos** agora registra a distancia real na hora da execucao e o alcance configurado. O botao de teste de morte segue funcionando sem alcance por definicao.
- Nenhuma alteracao na IA, fila FIFO, trait de velocidade, mecanismo de morte ou assets. A v0.1.5 foi testada pelo usuario e apresentou apenas avisos de localizacao no ultimo relatorio.

## O que mudou na v0.1.5

### 1. Perseguição com API que já funcionou no The Unrelenting

O código antigo priorizava `moveTo(WorldTile)`, que reportava sucesso sem efetivamente deslocar a criatura. O código enviado no The Unrelenting usa:

```csharp
_host.cancelAllBeh();
_host.goTo(_target.current_tile, false);
```

Agora `WorldBridge.TryChase` localiza `goTo(WorldTile, bool)` por reflexão e usa `false` como segundo argumento. A cada três segundos, quando em perseguição, `cancelAllBeh()` interrompe rotinas concorrentes. Os comandos de rota têm intervalos adaptativos (1.6s a 0.18s conforme distância). O método de um parâmetro permanece como fallback de diagnóstico. **Isso não prova ainda travessia de oceanos nem navegação livre de obstáculos.**

### 2. Testemunhas com trait e fila FIFO

- `ArvakSCP096_Witness`: trait própria, pode ser colocada manualmente na lista de traits do jogo, se o registro estiver disponível.
- `ArvakSCP096_RageSpeed`: trait técnica temporária que dá +85 de velocidade durante RAGE e é removida ao se acalmar.
- Novas testemunhas entram no **final da fila**, sem duplicatas. O SCP segue sempre o primeiro alvo vivo. Após a morte, passa ao próximo.
- Qualquer NPC/animal que passe a até **5 tiles** do SCP pode ser marcado automaticamente. Isso é uma **aproximação experimental** do olhar, não uma detecção visual real! A opção vem ligada para facilitar o teste; pode ser desligada no painel ou na configuração. O limite inicial é 64 testemunhas.
- Durante RAGE, novas unidades que chegam perto também entram no fim da fila.
- Um scan global de traits ocorre a cada 12 segundos, além do scan de proximidade configurável.
- A trait persiste pelo mecanismo de traits/salvamento do jogo (a verificar no WorldBox). Ao carregar um mundo, a fila será recriada a partir dos portadores da trait, **mas a ordem histórica original NÃO é preservada** na versão atual.

### 3. Morte e confirmação

`dieSimpleNone()` já eliminou vítimas no teste do usuário. A versão anterior registrava erroneamente falha logo depois da chamada e sucesso no tick seguinte. Agora a execução só é contada como confirmada depois da morte observada. Se o método é chamado sem exceção, a mensagem diz **EXECUCAO SOLICITADA**, não `FALHOU`.

### 4. Visuais

Foram adicionados `swim_0` a `swim_3` para **Main** e **child**, reutilizando os sprites de caminhada originais como placeholders. Foi configurado `texture_path_baby` para a pasta própria e aumentados os valores mínimos de sombra do clone experimental. O resultado visual precisa ser verificado dentro do jogo; não há garantia de eliminação de todos os avisos.

## Instalação limpa

1. Feche WorldBox.
2. Localize a pasta `Mods` do NeoModLoader. No Ubuntu com Steam, uma instalação frequente é:
   `~/.steam/debian-installation/steamapps/common/worldbox/Mods`
3. **Mova todas as pastas anteriores de SCP096 para fora da pasta Mods.** Não altere o The Unrelenting ou WorldFall.
4. Extraia este ZIP para a pasta `Mods`, ficando:
   `Mods/SCP096-TheShyGuy-0.1.6/mod.json`.
5. Abra o jogo e use um **mundo pequeno e descartável**. Confirme no painel a versão `v0.1.5`.

## Controles

| Botão/tecla | Resultado |
|---|---|
| F5 | Mostrar/esconder painel |
| F6 | Gerar próximo ao centro da câmera |
| Shift + F6 | Gerar isolado |
| Posicionar SCP no mapa | Gerar clicando no terreno |
| F7 | Adicionar NPC perto do cursor (raio de 12 tiles) ao final da fila |
| Shift + F7 | Adicionar unidade viva mais próxima do cursor, sem limite de distância entre SCP e vítima |
| Escolher alvo mapa | Ativar o modo de marcação: clique depois no mapa perto do NPC (até 12 tiles do clique) |
| Marcar NPCs a 5 tiles | Liga/desliga o gatilho experimental por proximidade |
| F8 | Cancelar toda a fila, retirar as traits Witness das vítimas encontradas |
| Teste: humano | Criar humano para teste |
| Teste: eliminar primeiro alvo | Executar comando de morte sem requisito de distância (somente teste) |
| F9 | Diagnóstico no Player.log |

## Limitações assumidas

- Não há leitura do rosto, direção da câmera do WorldFall, obstrução visual nem fotografias.
- Não há persistência confiável da **ordem** FIFO entre sessões; a lista é reconstruída pela leitura das traits salvas.
- A criatura é clonada a partir de `sheep` para impedir comportamento de aldeão; atributos de espécie/animações ainda podem demandar ajustes.
- Perseguição transoceânica, gelo e liberação de trajetos presos não implementados.
- Mudança de velocidade depende de a engine recalcular o bônus ao colocar a trait na unidade.
- O conjunto de sprites é incompleto para funcionalidades futuras. Correções atuais são hipóteses para os erros registrados.
- WorldFall não foi carregado nem inspecionado nesta entrega; eventuais exceções `FirstPerson` devem ser investigadas separadamente.

## Logs

```bash
grep -nE 'SCP096|ActorAssetLibrary|Shadow size|Exception|goTo|MORTE' \
~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -180
```

Em especial, confirme que constam `v0.1.5 alpha`, `Traits: registradas` e `Perseguicao: Actor.goTo(WorldTile,Boolean)`.

## Licenciamento

SCP-096 foi criado por **Dr Dan** no site SCP Wiki e está sujeito à licença Creative Commons BY-SA 3.0. Consulte `CREDITS.md` e `LICENSE.md`. As artes desta versão são recursos próprios do projeto; áudio do The Unrelenting não foi copiado.

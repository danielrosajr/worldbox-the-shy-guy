# SCP-096: The Shy Guy — v0.1.1 alpha

> **ALFA DIAGNOSTICA.** Arquivos C# de NeoModLoader. Nao compilada nem testada no jogo deste ambiente. Salve e faca backup dos mundos importantes antes de testar.

## O que foi corrigido

A v0.1.0 dependia de atalhos F6 e Shift+F6, mas nao tinha nenhum elemento de interface que confirmasse as acoes. O F6 gerava o SCP longe do centro da tela, fazendo parecer que nada tinha ocorrido. Esta versao inclui um **painel de teste visivel no canto superior direito**, mensagens de retorno, spawn no centro da camera e posicionamento por clique no mapa.

Tambem forcamos o parametro opcional `adult=true` no metodo `spawnNewUnit`, quando presente, para evitar gerar um filhote. Esta mudanca corresponde a assinatura documentada de WorldBox 0.51.2, nao constitui validacao da sua versao.

## Instalar (Ubuntu / Steam)

1. Feche o WorldBox.
2. Localize a pasta usada pelo NML para carregar mods (em instalacoes comuns, `~/.steam/debian-installation/steamapps/common/worldbox/Mods`).
3. **Remova a pasta ANTIGA** `SCP096-TheShyGuy-0.1.0` da pasta de mods para nao carregar as duas versoes.
4. Extraia a pasta `SCP096-TheShyGuy-0.1.1` contida no ZIP diretamente na pasta de mods.
5. Abra WorldBox e crie **um mundo descartavel** com bastante terra firme; inicialmente teste sem WorldFall.
6. Procure um pequeno painel `SCP-096 - THE SHY GUY | v0.1.1` no canto superior direito.

Exemplo de instalacao por terminal, executado na pasta onde voce baixou o ZIP:

```bash
GAME="$HOME/.steam/debian-installation/steamapps/common/worldbox"
rm -rf "$GAME/Mods/SCP096-TheShyGuy-0.1.0"
rm -rf "$GAME/Mods/SCP096-TheShyGuy-0.1.1"
unzip -o SCP096-TheShyGuy-v0.1.1-alpha.zip -d "$GAME/Mods"
```

Confirme o caminho `GAME` antes de executar, pois algumas instalacoes Steam usam outro diretorio. A exclusao acima atinge apenas as pastas identificadas deste mod. Nao edite a pasta bootstrap `worldbox_Data/StreamingAssets/mods`.

## Controles e painel

| Acao | Como testar |
|---|---|
| Gerar aqui | Botao `Gerar aqui [F6]` ou tecla F6. Tenta criar SCP sobre terra proxima ao **centro da tela**. |
| Gerar isolado | Botao `Gerar isolado` ou **Shift+F6**. Tenta criar em local remoto; a criatura **nao estara necessariamente visivel**. |
| Posicionar no mapa | Clique `Posicionar SCP: clicar no mapa`, depois clique no terreno. ESC cancela. |
| Marcar vitima | Botao `Marcar alvo [F7]` ou F7. Procura um personagem perto do cursor ou SCP. |
| Cancelar alvo | Botao `Cancelar [F8]` ou F8. |
| Teste humano | Botao `Teste: humano`: cria um humano **vanilla** no centro da tela para investigar se a API de spawn funciona. E apenas uma ferramenta diagnostica. |
| Diagnostico | Botao `Diagnostico [F9]` ou F9. Detalhes no `Player.log`. |
| Mostrar/ocultar painel | F5 ou botao `X`. |

**Shift sozinho nao cria nada.** F6 pode ser interceptado pelo sistema operacional ou jogo; por isso prefira o painel, que nao depende dos atalhos.

O painel mostra o estado do mapa, se o asset foi registrado, se existe um SCP e o resultado da ultima acao. Se a criatura existir mas o sprite ainda aparecer como humano, isso e uma limitacao visual da versao alfa.

## Roteiro minimo de depuracao

1. Clique **Teste: humano**. Se nao funcionar, a incompatibilidade provavelmente esta no acesso a mapa/tile/spawn.
2. Clique **Gerar aqui**. Se nao funcionar, leia `REGISTRO FALHOU` ou `SPAWN FALHOU` no painel.
3. Se o SCP nascer, tente **Marcar alvo** perto de uma cidade com habitantes. Aguarde o estado passar de `Triggered` para `Rage`.
4. Se ele nao perseguir, clique **Diagnostico** e veja as linhas `UNRESOLVED` e assinaturas de metodos no log.

Se precisar de ajuda, envie uma captura do painel apos tentar criar, e/ou estas linhas do log:

```bash
LOG=$(find "$HOME/.config/unity3d" -name Player.log -type f 2>/dev/null | head -1)
echo "Log encontrado: $LOG"
if [ -n "$LOG" ]; then
  grep -nE 'SCP096|NeoModLoader|error CS|Exception' "$LOG" | tail -100
fi
```

Nao assuma que a presenca do mod na lista de mods prova que suas classes, sprites e acoes carregaram. O painel e o log ajudam a distinguir os casos.

## Situacao tecnica

- `BasicMod<T>` com `OnModLoad -> Update` esta documentado pelo projeto de exemplo do NML.
- O painel usa `MonoBehaviour.OnGUI` do Unity, sem depender de uma API customizada de janelas do WorldBox. E temporario e voltado a testes.
- A verificacao de um SCP por mapa usa `AssetId` e varredura de unidades. Nao cria automaticamente uma criatura em mundos novos.
- O clone do ator humano, os sprites, IA de perseguicao, invulnerabilidade, ataque fatal e persistencia **ainda nao foram validados no seu executavel**.
- A maquina de estados e a vitima marcada permanecem apenas em memoria. Nao se trata ainda do sistema definitivo.
- Qualquer integracao com WorldFall esta desativada nesta versao.

Fontes de referencia de APIs (outras versoes e outros ambientes):
- https://github.com/WorldBoxOpenMods/ModExample/blob/master/ExampleModCode.cs
- https://github.com/WorldBoxOpenMods/ModLoader/blob/master/api/BasicMod.cs
- https://github.com/fullya99/worldbox-mcp/blob/main/docs/game-api-notes.md (WorldBox 0.51.2)

## Creditos e licenciamento

Inspirado no [SCP-096](https://scp-wiki.wikidot.com/scp-096), de **Dr Dan**. Obra derivada sob [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/). Veja `CREDITS.md` e `LICENSE.md`. Os sprites deste pacote foram criados especificamente para o projeto.

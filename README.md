# SCP-096: The Shy Guy — v0.2.15-alpha (The Shy Guy: Reborn)

**Foco desta versão: modelo 3D original reconstruído.** A IA, o FMOD, a fila, a regeneração, a aceleração e o Dread são os da v0.2.14.

## Instalação (IMPORTANTE: são DUAS pastas!)

1. Feche o WorldBox.
2. Extraia `SCP096-TheShyGuy-0.2.15` na pasta `Mods` do WorldBox, substituindo/removendo a versão antiga do SCP.
3. O WorldFall usa uma cópia do modelo dentro de `FirstPerson/packs`. Execute, no terminal **dentro da pasta recém-extraída**:

   ```bash
   bash scripts/install-worldfall-pack.sh
   ```

4. Confirme que foi atualizado:
   `~/.config/unity3d/mkarpenko/WorldBox/FirstPerson/packs/arvak_scp096_worldfall/models/animal_scp096_shyguy.glb`
5. Inicie WorldBox/WorldFall. Se necessário, no WorldFall: `F2 > Mods > Manage` e deixe o pack do SCP ativo. Compare o rosto, mãos e postura com a v0.2.14.

**Não basta atualizar apenas a pasta do NML:** o GLB antigo pode permanecer no pack WorldFall.

## O que esperar do modelo

- Cor pálida/cinza, corpo descarnado e estreito; aproximadamente 2.21 unidades de altura, semelhante à escala anterior.
- Face voltada para **-Y**, olhos fundamente escuros, lágrimas, boca alongada e dentes assimétricos.
- Dedos finos e muito longos; juntas e costelas salientes.
- Criado inteiramente de geometria procedural original; não contém modelos copiados ou texturas externas.
- **Esta versão NÃO contém rigging ou animações novas.** Primeiro testar compatibilidade visual, depois estudar suporte a bones/GLB animation no loader.

## Como testar (sem atalhos com Shift)

1. Gere ou encontre o SCP no WorldBox 2D e assuma alguém no WorldFall.
2. Aproxime-se e observe o rosto, silhueta, braços e mãos; circule a criatura.
3. Verifique se a criatura não gira de lado/de costas ou aparece enterrada no chão; se isso ocorrer envie screenshot do modelo e `FirstPerson.log`.
4. Confirme que o Dread pelo botão F5 e o som continuam iguais à v0.2.14.

## Pacotes

- `personal-test`: modelo 3D, mídia de áudio/imagem para testes locais e fontes.
- `code-only`: código, sprites, modelo 3D procedural e documentação; sem áudios/imagem de terceiros.
- Não publique mídia de terceiros antes de verificar licenças.

Detalhes: `docs/WORLDFALL_MODEL_REBORN_0215.md`.

---

# Atualização v0.2.14-alpha (teste experimental)

**Correção:** a visualização Dread pelo botão F5 não é mais cancelada automaticamente quando não há SCP-096 no mapa. A leitura do estado 3D do WorldFall roda independentemente da detecção de vítimas.

**Como testar:** no WorldBox 2D, com ou sem SCP criado, abra F5, clique em **Testar POV macabro (sem Shift)**, feche o painel e entre no WorldFall. Confira a imagem. Volte ao 2D e clique **Parar preview de medo**.

**Logs esperados:** `DREAD PREVIEW F5`, `DREAD PREVIEW: 3D=True`, `DREAD RT: WorldFall GPU encontrado`, `DREAD RT: frame=1`. Se não ocorrer, os diagnósticos registram a etapa que falhou. Logs não substituem confirmação visual.

O menu `FirstPerson.SettingsMenu` e `ModsTab` existem na DLL atual do WorldFall, mas esta versão não modifica o menu Esc: ainda não foi estabelecido um ponto de extensão seguro.

Não houve alterações intencionais no áudio, fila de vítimas, execução, regeneração, detecção de rosto ou aceleração. Não há compilação ou teste em jogo neste ambiente.

---

# SCP-096 — The Shy Guy v0.2.13-alpha

**Foco desta atualização:** fazer o Dread escurecer o WorldFall 3D. A v0.2.12 detectava a vítima, mas o shader não alterava a imagem pois o parâmetro GPU `ShadowDarkness` já estava no máximo (`1.00`). Esta versão experimental tenta escurecer o **RenderTexture** apresentado pelo WorldFall com uma camada azul-escura translucida, aplicada tardiamente no frame. Não depende de Canvas.

## Instalar e testar

1. Feche o WorldBox e **remova a pasta antiga** do mod.
2. Copie a pasta `SCP096-TheShyGuy-0.2.13` para `Mods`.
3. Entre em um mundo de teste e assuma um personagem no WorldFall com `G`.
4. Pressione `Shift + F9` para testar o efeito mesmo sem ser a vítima. Aguarde 2 segundos; pressione novamente para desligar.
5. Depois marque seu personagem como próximo alvo e confira se o efeito se ativa automaticamente.
6. Confira o log:

```bash
grep -nE 'SCP096|DREAD RT|error CS|Exception' \
~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -130
```

`DREAD RT: frame=1` indica tentativa de pintura, **não prova que a imagem apareceu**. Veja mais em `docs/WORLDFALL_RENDER_TEXTURE_DREAD_0213.md`.

Todos os controles do SCP, música global do WorldBox/WorldFall, vida/regeneração, velocidade por alvo, áudio e visibilidade de rosto permanecem como na v0.2.12.

## Licenciamento

O ZIP `personal-test` contém sons/imagem de terceiros apenas para testes locais. Não publique essas mídias no GitHub/Workshop sem confirmar as respectivas licenças. O ZIP `code-only` as exclui.

**Validação:** somente verificações estruturais/estáticas. Necessita compilar e testar no NeoModLoader.

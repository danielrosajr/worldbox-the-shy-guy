# 0.2.13 — Dread 3D no RenderTexture do WorldFall (protótipo)

## Por que não funcionou na 0.2.12

O log confirmou a identificação da vítima, e `FirstPerson.Gpu.GpuRenderer.ShadowDarkness` estava em `1.00` quando o efeito foi ligado. A implementação antiga fazia `Mathf.Clamp01(base + offset)`, que continuava em `1.00` — sem alteração no GPU. Alterar a vinheta do `FirstPerson.Core.FrameRenderer` não resolve se o jogo está renderizando pelo GPU. `Canvas` e `OnGUI` foram renderizados atrás do visual 3D na máquina do usuário.

## DLL inspecionada

A DLL entregue pelo usuário contém, em `FirstPerson.WorldBoxMod`, o campo `_gpu:FirstPerson.Gpu.GpuRenderer`. O tipo `GpuRenderer` contém `_present:UnityEngine.RenderTexture`, `Output:UnityEngine.RenderTexture` e `FrameNumber:int`.

## Implementação experimental

`src/WorldfallDreadFilter.cs` agora recebe a condição de alvo via `Tick()`. Um `MonoBehaviour` de execução tardia (`DefaultExecutionOrder(30000)`) executa a composição de preto azulado translúcido **diretamente sobre `_present` do GPU**, ou `Output` caso `_present` ainda não exista, via `RenderTexture.active` e desenho imediato `GL` com shader Unity `Hidden/Internal-Colored`.

- Nunca modifica a câmera do WorldBox 2D.
- Não aplica nenhum efeito quando outra vítima está à frente na fila.
- O ganho escurece progressivamente e é limitado a 68% de opacidade.
- Não depende de DLL WorldFall para compilar; descobre os campos por Reflection e falha de forma segura.
- Evita pintar mais de uma vez a mesma `FrameNumber` da GPU.
- Ao desativar o modo 3D, trocar de vítima ou desativar o mod, o filtro deixa de compor e libera seus objetos no encerramento do componente. A próxima renderização do WorldFall substitui os pixels alterados.

**Risco técnico:** a aplicação no RenderTexture depende da ordem do pipeline específico do WorldFall. `DREAD RT: frame=` comprova que o comando de pintura foi emitido, mas ainda não que ele foi exibido na imagem final. Somente o teste do usuário confirma isso. Em placas/drivers que não suportem o shader interno, o filtro permanecerá desligado com diagnóstico.

## Teste

1. Entrar no WorldFall 3D com `G`.
2. Acionar `Shift + F9` para habilitar o Dread sem precisar provocar SCP.
3. Esperar dois segundos, conferir se o POV realmente escureceu.
4. Alternar `Shift + F9` novamente; a imagem deve voltar ao normal.
5. Conferir o `Player.log`:

```bash
grep -nE 'DREAD RT|SCP096|error CS|Exception' \
  ~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -100
```

Procurar: `DREAD RT: GPU encontrado`, `DREAD RT: material AlphaBlend criado`, `DREAD RT: frame=1 target=_present`.

Caso o preview ainda não apareça, enviar o log completo destas mensagens e informar se a tela ficou preta, piscou ou não mudou. Não continuar tentando aumentar o Canvas: seria necessário integrar no ponto de apresentação `GpuRenderer.Present` com hook específico para a versão do WorldFall do usuário.

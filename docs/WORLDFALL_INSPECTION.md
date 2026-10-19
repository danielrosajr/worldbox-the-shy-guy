# Inspeção da Worldfall.dll real

Origem: arquivo `Worldfall-inspection.tar.gz` fornecido pelo usuário. Assembly PE32 Mono/.NET, ~61 MB (extraído). Inspeção de tabelas CLI TypeDef, FieldDef e MethodDef, incluindo assinaturas de membros. **Não foram executados métodos dentro do jogo.**

## Tipos e assinaturas confirmados

- `FirstPerson.WorldBoxMod : UnityEngine.MonoBehaviour`
- `WorldBoxMod.get_IsFirstPerson(): bool`
- `WorldBoxMod.get_Host(): Actor`
- `WorldBoxMod.get_View(): FirstPerson.Core.ViewState`
- `WorldBoxMod._host : Actor` (instance)
- `WorldBoxMod._view : FirstPerson.Core.ViewState` (instance)
- `FirstPerson.Core.ViewState` campos `X,Y,Z,Yaw,Pitch,Fov,Shear` (float), `Free` (bool)
- `FirstPerson.Gpu.GpuRenderer` campos `PickTag:int`, `PickDepth:float`, `PickFrame:int`
- `GpuRenderer.RequestPick(int,int,float):void`
- `WorldBoxMod._gpuTagged: List<BaseSimObject>[]`, associação com os identificadores do renderizador ainda não verificada
- `FirstPerson.Gpu.EyeCamera` estrutura própria da renderização 3D

## Descoberta

A versão anterior procurava `UnityEngine.Camera` perspectiva e componentes 3D para cada unidade. Isto é inadequado para a renderização GPU do WorldFall. Esta versão usa somente os getters verificados do `WorldBoxMod` e não requer uma câmera Unity perspectiva.

## Pendências

- Confirmar `get_IsFirstPerson/get_Host/get_View` durante gameplay (F10 + log automático).
- Conferir coordenadas, yaw e FOV em 3D; medir variação ao virar o rosto.
- Inspecionar e validar o mapping GPU `PickTag` -> `BaseSimObject`, e verificar se o pixel alvo é a **cabeça** e está na orientação frontal.
- Só então ativar o gatilho automático com permanência de 0,45 s e controle de oclusão.
- Testar integração do GLB próprio, animações e recortes.

Não redistribuímos `Worldfall.dll` no ZIP; o código usa reflexão opcional e preserva funcionamento sem WorldFall.

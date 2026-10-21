# WorldFall v0.2.14 – inspeção e diagnóstico

Inspecionado o ZIP original enviado pelo usuário: `Worldfall.dll` (61.234.688 bytes, PE32 .NET), `FirstPerson.log` e `FirstPerson.prev.log`.

Os metadados .NET confirmam: `FirstPerson.WorldBoxMod._gpu : FirstPerson.Gpu.GpuRenderer`, métodos `get_IsFirstPerson`, `get_Host`, `get_View`, `get_Picked`, `PresentGpu`; `FirstPerson.Gpu.GpuRenderer._present` e `Output` são `UnityEngine.RenderTexture`, com métodos `Present`/`PresentTo`. A UI é `FirstPerson.SettingsMenu` com campo `ModsTab` e método `Build` (não confirmado como extensível).

Os logs confirmam que o WorldFall usa OpenGLCore no Linux, renderiza em GPU, carrega shaders próprios de `FirstPerson.gpu.linux.bundle` e informa `gpu: picture check ... drawn`. Isso não comprova ainda uma interceptação correta da apresentação da imagem pelo mod.

Bug encontrado no código v0.2.13: `WorldfallSight.Check()` era chamado só depois de `if (_scp == null) return`, e o scan periodicamente forçava `_dreadPreview=false` quando não havia SCP. A correção força atualização independente e preserva a prévia.

Teste: entrar em mundo, F5 > Testar POV macabro, entrar em WorldFall 3D. Grep: `grep -nE 'DREAD PREVIEW|DREAD RT|error CS|Exception' ~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -120`.

Se logar `DREAD RT: frame=1` e a imagem continuar normal, o problema remanescente é apresentação/ordem de renderização; a próxima investigação deve considerar `WorldBoxMod.PresentGpu`/`GpuRenderer.Present`, sem patch global nesta versão.

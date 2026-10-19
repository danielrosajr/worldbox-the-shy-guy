# WorldFall v0.2.4 — deteccao experimental de olhar

Fonte: Worldfall.dll enviada pelo usuario (Steam Workshop, 2026-10-18). API inspecionada via metadados .NET; sem compilar ou executar o jogo aqui.

## API confirmada por inspecao

- `FirstPerson.WorldBoxMod.get_IsFirstPerson()` -> booleano.
- `get_Host()` -> Actor do jogador, `get_View()` -> ViewState.
- `get_Picked()` -> BaseSimObject apontado. O bytecode de WorldBoxMod.GpuFrame e RenderFrame mostra atualizacoes do campo `_picked` com resultados do renderer.
- `FirstPerson.SceneCollector._headings` -> Dictionary<Actor, Heading>; Heading.Angle -> float.
- `FirstPerson.Creatures.States` -> Dictionary<Actor, State>; State.Heading -> float, importante para o SCP derivado de `sheep`.
- A unidade exata, sentido do eixo frontal e yaw sao **inferencias que ainda precisam de calibracao em gameplay**.

## Como o algoritmo funciona

1. Reutiliza Host, View e SCP do WorldBox, apenas em primeira pessoa.
2. Exige que o SCP esteja a <= 35 tiles (configuravel).
3. Exige que `get_Picked()` seja exatamente o Actor do SCP. Isto deve excluir coisas bloqueadas pela renderizacao (mas a semantica exata requer teste em jogo).
4. Exige `Creatures.States[Actor].Heading` ou `SceneCollector._headings[Actor].Angle` frontal com cone de 65 graus e offset ajustavel (por padrao estrito e sem fallback se heading inexistente).
5. Exige olhar sustentado por 0,45s.
6. Marca somente o Actor controlado, respeitando fila FIFO e logica de perseguicao anterior.

### Limites IMPORTANTES

- `get_Picked` identifica o OBJETO sob a mira; nao identifica vertices ou pixels especificamente da CABECA. **Isto nao e ainda um detector definitivo de olhos/rosto**.
- Um alvo parcialmente visivel pode ser identificado. A precisao de oclusao depende do picker CPU/GPU do WorldFall.
- A orientacao `Heading.Angle` e a convencao de frente precisam de calibracao na sua instalacao. F5 oferece offset de 0 ou 180 graus, e F10 fornece FACE delta.
- `get_Picked` pode funcionar somente a uma distancia de interacao menor do que a distancia configurada; testar com e sem obstaculos.
- Modo estrito (padrao) bloqueia o gatilho se a orientacao nao for encontrada. Desative "Exigir SCP de frente" apenas para teste; sem esse bloqueio, olhar para as costas podera marcar o jogador.
- Proximidade 2D desativada por padrao para nao mascarar gatilho 3D; reative se desejar no painel.
- Nao foram adicionadas dependencias da Worldfall.dll a instalacao do mod.

## Testes em mundo descartavel

- Em G, olhar 0.5s para a frente do SCP -> ver `TESTEMUNHA MARCADA via rosto na camera Worldfall (3D)`.
- Olhar para as costas => nao marcar (estrito).
- Parede fechada => `get_Picked` deve ser parede ou null; nao marcar.
- Virar o olhar para fora antes de 0.45s => nao marcar.
- Olhar sem a proximidade 2D ativa => marcar somente se criterios 3D forem atendidos.
- Se nao marcar, pressione F10 em frente e atras para comparar `AIM` e `FACE` e calibrar 180 graus.
- Se F10 nao encontrar heading em `Creatures.States` nem em `_headings`, manter estrito ligado, capturar log e rever proxima versao.

### Diagnostico

```bash
grep -nE 'SCP096|WorldfallSight|CAPTURA F10|error CS|Exception' ~/.config/unity3d/mkarpenko/WorldBox/Player.log | tail -120
```

Antes de publicar como estavel: compilar no NeoModLoader, testar oclusao e reconhecer pixels da cabeca com precisao.

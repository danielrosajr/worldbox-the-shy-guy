# Changelog

## 0.2.4-alpha — 2026-10-18

- `WorldfallSight` atualizado para usar `get_Picked()` do renderizador real, sem fazer GPU readbacks proprios.
- Verificacao de frente com `Creatures.States[Actor].Heading` (para species como sheep) ou `SceneCollector._headings[Actor].Angle`, calibracao 0/180.
- Hold visual de 0,45s; uma marcacao por sequencia de olhar.
- F10 agora reporta AIM / FACE / RESULT em campos separados.
- Proximidade 2D desativada por padrao para isolar testes 3D (pode reativar no F5).
- Mantidos fila FIFO, trait Witness, Rage, movimento `Actor.moveTo`, e morte somente a 1,7 tiles.
- Testes internos: validacao estrutural. **Nao compilado/executado no WorldBox.**

# Changelog

## v0.2.3-alpha
- Integração com a instância real `FirstPerson.WorldBoxMod`, obtida de `MonoBehaviour`.
- Descoberta e leitura de `get_IsFirstPerson`, `get_Host`, `get_View`, `ViewState` (posição e orientação).
- Diagnósticos automáticos ao entrar/sair do WorldFall; F10 detalhado.
- F11 marca manualmente o ator controlado no 3D para teste de integração, respeitando distância e sem ativação visual fictícia.
- Detecção automática de rosto mantida desativada até validar o GPU picking de cabeça e obstáculos.
- Lógica de perseguição e eliminação da v0.1.6 preservada.

# CHANGELOG

## 0.2.2-alpha (Worldfall camera diagnostics)

- Log de transicao entre perspectiva 3D e camera 2D; uma vez por transicao, nao por frame.
- F10 grava captura detalhada de camera e campos FirstPerson em tempo de jogo, sem abrir painel.
- Retirada a exigencia arbitraria de camera ocupando 65% da tela; ainda requer camera perspectiva ativa.
- Registro da ultima camera perspectiva vista no diagnostico, mesmo apos retornar ao mapa.
- Preservados spawn, traits, fila FIFO, movimento, ataque e morte da baseline v0.1.6.
- Detecao automatica continua experimental; nao prometer ativacao se Worldfall nao expuser o Actor, a cabeca e o collider.

# Changelog — SCP-096 The Shy Guy

## 0.2.1-alpha — Hotfix de compilação

- FIX: removida referência a `_worldfallSightLogged` em `ModClass.cs` (CS0103). O campo não existia e não era necessário.
- KEEP: lógica de perseguir, fila FIFO, traits, eliminação e protótipo 3D/visão experimental da v0.2.0.
- TEST: validação estática do pacote e verificação de ausência da referência inválida. **Não compilado dentro do NML** e **não testado no jogo**.

## 0.2.0-alpha — Worldfall / first-person preview

- ADD: `src/WorldfallSight.cs` — inspeção opcional de câmera, personagem controlado, componente 3D do SCP, orientação frontal da cabeça e oclusão por raycast; falha segura quando não consegue confirmar rosto visível.
- ADD: modelo humanoide GLB original e `Worldfall/worldfall.json` (pack ativado separadamente em F2 > Mods), sem DLL de terceiro.
- ADD: `scripts/install-worldfall-pack.sh`, diagnóstico de câmera no painel, configurações de alcance e tempo mínimo do olhar.
- CHANGE: desativa automaticamente a marcação por proximidade de 5 tiles **durante a câmera 3D do Worldfall** (a menos que explicitamente permitido em config). No WorldBox 2D permanece disponível.
- KEEP: sistema de traits, fila FIFO, spawn selvagem, `moveTo` fallback, limite de 1.7 tiles e `dieSimpleNone()` da base v0.1.6 testada.
- KNOWN: renderização GPU do Worldfall talvez não exponha um Transform/collider individual por unidade; modo estrito não dispara sem isso. Precisa da DLL local para validação real da ponte.

## 0.1.6-alpha

- FIX: chaves localizadas de Witness e RageSpeed.
- ADD: distância real registrada na hora da execução.
- Testada pelo usuário no WorldBox 0.51.2 (build 719): perseguição, fila, morte e retorno ao estado passivo.

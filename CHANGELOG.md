# Changelog

## v0.2.6-alpha — FMOD audio fix
- Corrige silencio: substitui AudioClip.Create/SetData da Unity pelo Core FMOD ja presente no WorldBox.
- Torna streaming a leitura dos cinco WAVs e registra erros nativos por arquivo/canal.
- Corrige grito global inicial para tocar uma so vez; mantem grito local em loop depois.
- Adiciona botoes de teste de cue/musica no painel F5.
- Flash com fade-in, hold e fade-out, opacidade/duracao ajustaveis.
- Adiciona traducoes e descricoes para todas as configuracoes do mod.
- Preserva movimentacao, morte, fila, cooldown e deteccao de olhar no WorldFall 3D.
- Aviso: codigo nao compilado/testado no runtime do usuario nesta entrega.

# Changelog

## 0.2.5-alpha — The Scream

- feat: áudio local de choro, grito em loop, execução, música global e grito inicial global.
- feat: `ShyGuyAudio` desacoplado da lógica de perseguição (o mod funciona sem WAVs).
- feat: TRIGGERED de 48 segundos com música de 44 segundos agendada 4 segundos após o gatilho.
- feat: cooldown de 15 segundos após CALMING → DOCILE, sem reiniciar quando novas testemunhas entram em RAGE.
- feat: tela de aviso fugaz para testemunha controlada no WorldFall, desativável; aviso 2D somente quando evento ocorre com SCP em tela.
- fix: marcação de NPCs por proximidade continua ativa no WorldFall, mas não marca automaticamente o personagem possuído.
- feat: configuração para volume, áudio, flash e cooldown.
- chore: pacote publicável sem mídia de terceiros e pacote separado de teste pessoal a partir de uploads do usuário.

**Status:** somente validação estrutural; requer compilação e testes no WorldBox 0.51.2.

## 0.2.4-alpha — WorldFall Look

Preservação da detecção de rosto via `get_Picked`, Heading e permanência do olhar; perseguição e fila FIFO testadas pelo usuário.
